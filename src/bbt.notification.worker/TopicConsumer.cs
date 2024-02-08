using bbt.framework.kafka;
using bbt.notification.worker.Enum;
using bbt.notification.worker.Helper;
using bbt.notification.worker.Models;
using Elastic.Apm.Api;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace bbt.notification.worker
{
    public class TopicConsumer : BaseConsumer<string>
    {
        TopicModel topicModel;
        BaseModel baseModel = new BaseModel();
        private readonly ITracer _tracer;
        private readonly ILogHelper _logHelper;
        private readonly IConfiguration _configuration;

        public TopicConsumer(
        KafkaSettings _kafkaSettings,
        CancellationToken _cancellationToken,
        ITracer tracer,
        ILogger _logger, TopicModel _topicModel, ILogHelper logHelper, IConfiguration configuration) : base(_kafkaSettings, _cancellationToken, _logger)
        {
            _tracer = tracer;
            topicModel = _topicModel;
            _logHelper = logHelper;
            _configuration = configuration;

        }
        public override async Task<bool> Process(string model)
        {
            await _tracer.CaptureTransaction("Process", ApiConstants.TypeRequest, async () =>
            {
                try
                {
                    JObject o = JObject.Parse(model);

                    JToken clientId = o.SelectToken(topicModel.clientIdJsonPath);

                    var postConsumerDetailRequestModel = new PostConsumerDetailRequestModel
                    {
                        client = Convert.ToInt32(clientId),
                        sourceId = Convert.ToInt32(Environment.GetEnvironmentVariable("Topic_Id") ?? _configuration.GetSection("TopicId").Value),
                        jsonData = o.SelectToken("message.data").ToString().Replace(Environment.NewLine, string.Empty)
                    };

                    if (topicModel.ServiceUrlList.Count > 0)
                    {
                        foreach (var item in topicModel.ServiceUrlList)
                        {
                            var enrichmentServiceRequestModel = new EnrichmentServiceRequestModel
                            {
                                customerId = Convert.ToInt32(clientId),
                                dataModel = o.SelectToken("message.data").ToString().Replace(Environment.NewLine, string.Empty)
                            };

                            var enrichmentServicesCall = new EnrichmentServicesCall(_tracer, _logHelper);

                            var enrichmentServiceResponseModel = await enrichmentServicesCall.GetEnrichmentServiceAsync(item.ServiceUrl, enrichmentServiceRequestModel);

                            if (enrichmentServiceResponseModel != null && !string.IsNullOrEmpty(enrichmentServiceResponseModel.dataModel))
                            {
                                postConsumerDetailRequestModel.jsonData = enrichmentServiceResponseModel.dataModel;
                            }
                        }
                    }

                    var alwaysSendTypeList = ((AlwaysSendType)topicModel.alwaysSendType).ToEnumArray();

                    var notificationServicesCall = new NotificationServicesCall(_tracer, _logHelper, _configuration);
                    var consumerModel = await notificationServicesCall.PostConsumerDetailAsync(postConsumerDetailRequestModel);

                    if (HasValidServiceReference(topicModel.smsServiceReference) && HasConsumer(consumerModel) &&
                        (consumerModel.consumers[0].isSmsEnabled || alwaysSendTypeList.Contains(AlwaysSendType.Sms)))
                    {
                        await SendSms(o, consumerModel, postConsumerDetailRequestModel);
                    }

                    if (HasValidServiceReference(topicModel.emailServiceReference) && HasConsumer(consumerModel) &&
                        (consumerModel.consumers[0].isEmailEnabled || alwaysSendTypeList.Contains(AlwaysSendType.EMail)))
                    {
                        await SendEmail(o, consumerModel, postConsumerDetailRequestModel);
                    }

                    if (HasValidServiceReference(topicModel.pushServiceReference) && HasConsumer(consumerModel) &&
                    (consumerModel.consumers[0].isPushEnabled || alwaysSendTypeList.Contains(AlwaysSendType.Push)))
                    {
                        await SendPush(o, consumerModel, postConsumerDetailRequestModel);
                    }

                    return true;
                }
                catch (Exception e)
                {
                    _logHelper.LogCreate(model, false, "Process", e.Message);
                    _tracer.CaptureException(e);
                    return false;
                }
            });

            return true;
        }
        public async Task<bool> SendSms(JObject o, ConsumerModel consumerModel, PostConsumerDetailRequestModel postConsumerDetailRequestModel)
        {
            try
            {
                DateTime kafkaDataTime = DateTime.Today;

                if (!string.IsNullOrEmpty(o.SelectToken("message.headers.timestamp").ToString()))
                {
                    kafkaDataTime = Convert.ToDateTime(o.SelectToken("message.headers.timestamp"));
                }

                JToken clientId = o.SelectToken(topicModel.clientIdJsonPath);

                if (topicModel.RetentationTime == 0 || kafkaDataTime >= DateTime.Now.AddMinutes(-topicModel.RetentationTime))
                {
                    var dengageRequestModel = new DengageRequestModel();
                    var path = baseModel.GetSendSmsEndpoint();

                    if (HasConsumer(consumerModel))
                    {
                        var processItemId = "1";

                        if (!string.IsNullOrEmpty(topicModel.processItemId))
                        {
                            processItemId = GetJsonValue(o, topicModel.processItemId);
                        }

                        dengageRequestModel.phone.countryCode = consumerModel.consumers[0].phone.countryCode;
                        dengageRequestModel.phone.prefix = consumerModel.consumers[0].phone.prefix;
                        dengageRequestModel.phone.number = consumerModel.consumers[0].phone.number;                     
                        dengageRequestModel.template = topicModel.smsServiceReference;
                        dengageRequestModel.templateParams = postConsumerDetailRequestModel.jsonData;
                        dengageRequestModel.process.name = string.IsNullOrEmpty(topicModel.processName) ? topicModel.topic : topicModel.processName;
                        dengageRequestModel.process.ItemId = processItemId;
                        dengageRequestModel.process.Action = "Notification";
                        dengageRequestModel.process.Identity = "1";

                        var respModel = new SendMessageResponseModel();
                        var generatedMessageModel = new GeneratedMessage();
                        var response = await ApiHelper.ApiClient.PostAsJsonAsync(path, dengageRequestModel);

                        if (response.IsSuccessStatusCode)
                        {
                            respModel = await response.Content.ReadAsAsync<SendMessageResponseModel>();

                            if (respModel != null && respModel.TxnId != null)
                            {
                                string pathGeneratedMessage = baseModel.GetGeneratedMessageEndPoint().Replace("{txnId}", respModel.TxnId.ToString());
                                
                                var htpResponse = await ApiHelper.ApiClient.GetAsync(pathGeneratedMessage);

                                if (htpResponse.IsSuccessStatusCode)
                                {
                                    generatedMessageModel = await htpResponse.Content.ReadAsAsync<GeneratedMessage>();

                                    if (generatedMessageModel != null)
                                    {
                                        _logHelper.MessageNotificationLogCreate(postConsumerDetailRequestModel.client, postConsumerDetailRequestModel.sourceId, consumerModel.consumers[0] != null ? consumerModel.consumers[0].phone.prefix + "" + consumerModel.consumers[0].phone.number : null, "", dengageRequestModel, response, NotificationTypeEnum.SMS.GetHashCode(), response.StatusCode.ToString(), generatedMessageModel.Content, consumerModel.consumers[0].isStaff);
                                    }
                                    else
                                    {
                                        _logHelper.LogCreate(path, generatedMessageModel, "SendSmsContentResponseModel", "Content is null");
                                    }
                                }
                                else
                                {
                                    _logHelper.LogCreate(path, htpResponse, "SendSmsContentError", "GeneratedMessage status false");
                                }
                            }
                            else
                            {
                                _logHelper.LogCreate(respModel, false, "SendSmsContentResponseTxnModel", "TxnId is null");
                            }
                        }
                    }
                    else
                    {
                        _logHelper.LogCreate(Convert.ToInt32(clientId), false, "SendSms", "Consumer null");

                        return false;
                    }
                }
                else
                {
                    _logHelper.LogCreate(Convert.ToInt32(clientId), false, "SendSms", "Kafka data is timeout");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logHelper.LogCreate(consumerModel, false, "SendSms", ex.Message);
                _tracer.CaptureException(ex);
                return false;
            }
        }
        public async Task<bool> SendEmail(JObject o, ConsumerModel consumerModel, PostConsumerDetailRequestModel postConsumerDetailRequestModel)
        {
            try
            {
                var emailRequestModel = new EmailRequestModel();
                var path = baseModel.GetSendEmailEndpoint();

                if (HasConsumer(consumerModel))
                {
                    emailRequestModel.CustomerNo = consumerModel.consumers[0].client;
                    emailRequestModel.Email = consumerModel.consumers[0].email;

                    if (!string.IsNullOrEmpty(emailRequestModel.Email))
                    {
                        var processItemId = "1";

                        if (!string.IsNullOrEmpty(topicModel.processItemId))
                        {
                            processItemId = GetJsonValue(o, topicModel.processItemId);
                        }

                        emailRequestModel.TemplateParams = postConsumerDetailRequestModel.jsonData;
                        emailRequestModel.Template = topicModel.emailServiceReference;
                        emailRequestModel.Process = new DengageRequestModel.Process();
                        emailRequestModel.Process.name = string.IsNullOrEmpty(topicModel.processName) ? topicModel.topic : topicModel.processName;
                        emailRequestModel.Process.ItemId = processItemId;
                        emailRequestModel.Process.Action = "Notification";
                        emailRequestModel.Process.Identity = "1";

                        var respModel = new SendMessageResponseModel();
                        var generatedMessageModel = new GeneratedMessage();
                        var response = await ApiHelper.ApiClient.PostAsJsonAsync(path, emailRequestModel);

                        if (response.IsSuccessStatusCode)
                        {
                            respModel = await response.Content.ReadAsAsync<SendMessageResponseModel>();

                            if (respModel != null && respModel.TxnId != null)
                            {
                                var pathGeneratedMessage = baseModel.GetGeneratedMessageEndPoint().Replace("{txnId}", respModel.TxnId.ToString());
                                
                                var htpResponse = await ApiHelper.ApiClient.GetAsync(pathGeneratedMessage);

                                if (htpResponse.IsSuccessStatusCode)
                                {
                                    generatedMessageModel = await htpResponse.Content.ReadAsAsync<GeneratedMessage>();

                                    if (generatedMessageModel != null)
                                    {
                                        _logHelper.MessageNotificationLogCreate(postConsumerDetailRequestModel.client, postConsumerDetailRequestModel.sourceId, "", consumerModel.consumers[0] != null ? consumerModel.consumers[0].email : null, emailRequestModel, response, NotificationTypeEnum.EMAIL.GetHashCode(), response.StatusCode.ToString(), generatedMessageModel.Content, consumerModel.consumers[0].isStaff);

                                    }
                                    else
                                    {
                                        _logHelper.LogCreate(path, generatedMessageModel, "SendEmailContentResponseModel", "Content is null");
                                    }
                                }
                                else
                                {
                                    _logHelper.LogCreate(path, htpResponse, "SendEmailContentError", "GeneratedMessage status false");
                                }

                            }
                            else
                            {
                                _logHelper.LogCreate(respModel, false, "SendEmailContentResponseTxnModel", "TxnId is null");
                            }
                        }
                    }
                    else
                    {
                        _logHelper.LogCreate("CustomerNo=>" + emailRequestModel.CustomerNo, false, "SendEmail", " Email address is null");
                        return false;
                    }
                }
                else
                {
                    JToken clientId = o.SelectToken(topicModel.clientIdJsonPath);
                    _logHelper.LogCreate(Convert.ToInt32(clientId), false, "SendEmail", "Consumer  null");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logHelper.LogCreate(consumerModel, false, "SendEmail", ex.Message);
                _tracer.CaptureException(ex);
                return false;
            }
            return true;
        }

        public async Task<bool> SendPush(JObject o, ConsumerModel consumerModel, PostConsumerDetailRequestModel postConsumerDetailRequestModel)
        {
            try
            {
                var pushNotificationRequestModel = new PushNotificaitonRequestModel();
                var path = baseModel.GetSendPushnotificationEndpoint();

                if (HasConsumer(consumerModel))
                {
                    var processItemId = "1";

                    if (!string.IsNullOrEmpty(topicModel.processItemId))
                    {
                        processItemId = GetJsonValue(o, topicModel.processItemId);
                    }

                    pushNotificationRequestModel.CustomerNo = consumerModel.consumers[0].client.ToString();
                    pushNotificationRequestModel.TemplateParams = postConsumerDetailRequestModel.jsonData;

                    pushNotificationRequestModel.Template = topicModel.pushServiceReference;
                    pushNotificationRequestModel.SaveInbox = topicModel.saveInbox;
                    pushNotificationRequestModel.Process = new DengageRequestModel.Process();
                    pushNotificationRequestModel.Process.name = string.IsNullOrEmpty(topicModel.processName) ? topicModel.topic : topicModel.processName;
                    pushNotificationRequestModel.Process.ItemId = processItemId;
                    pushNotificationRequestModel.Process.Action = "Notification";
                    pushNotificationRequestModel.Process.Identity = "1";

                    var respModel = new SendMessageResponseModel();
                    var generatedMessageModel = new GeneratedMessage();
                    var response = await ApiHelper.ApiClient.PostAsJsonAsync(path, pushNotificationRequestModel);                  

                    if (response.IsSuccessStatusCode)
                    {
                        respModel = await response.Content.ReadAsAsync<SendMessageResponseModel>();

                        if (respModel != null && respModel.TxnId != null)
                        {
                            var pathGeneratedMessage = baseModel.GetGeneratedMessageEndPoint().Replace("{txnId}", respModel.TxnId.ToString());
                            var htpResponse = await ApiHelper.ApiClient.GetAsync(pathGeneratedMessage);

                            if (htpResponse.IsSuccessStatusCode)
                            {
                                generatedMessageModel = await htpResponse.Content.ReadAsAsync<GeneratedMessage>();

                                if (generatedMessageModel != null)
                                {
                                    _logHelper.MessageNotificationLogCreate(postConsumerDetailRequestModel.client, postConsumerDetailRequestModel.sourceId, consumerModel.consumers[0] != null ? consumerModel.consumers[0].phone.prefix + "" + consumerModel.consumers[0].phone.number : null, "", JsonConvert.SerializeObject(pushNotificationRequestModel, Formatting.Indented), response, NotificationTypeEnum.PUSHNOTIFICATION.GetHashCode(), response.StatusCode.ToString(), generatedMessageModel.Content, consumerModel.consumers[0].isStaff);
                                }
                                else
                                {
                                    _logHelper.LogCreate(path, generatedMessageModel, "SendPushContentResponseModel", "Content is null");
                                }
                            }
                            else
                            {
                                _logHelper.LogCreate(path, htpResponse, "SendPushContentError", "GeneratedMessage status false");
                            }
                        }
                        else
                        {
                            _logHelper.LogCreate(respModel, false, "SendPushContentResponseTxnModel", "TxnId is null");
                        }
                    }
                }
                else
                {
                    _logHelper.LogCreate("consumerModel", false, "SendPushNotification", "Consumer null");
                    
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logHelper.LogCreate(consumerModel, false, "SendPushNotification", ex.Message);
                _tracer.CaptureException(ex);
                return false;
            }

            return true;
        }
        public static string GetJsonValue(JObject obj, string path)
        {
            var retVal = "";

            var token = obj.SelectToken(path);

            if (token != null)
            {
                retVal = token.ToString();
            }

            return retVal;
        }

        private bool HasConsumer(ConsumerModel consumerModel)
        {
            return consumerModel != null && consumerModel.consumers != null && consumerModel.consumers.Count() > 0;
        }
        private bool HasValidServiceReference(string serviceReference)
        {
            return !string.IsNullOrEmpty(serviceReference) && serviceReference != "string";
        }
    }
}