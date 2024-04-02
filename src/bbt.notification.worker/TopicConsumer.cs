using bbt.framework.kafka;
using bbt.notification.worker.Enum;
using bbt.notification.worker.Helper;
using bbt.notification.worker.Models;
using Elastic.Apm.Api;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace bbt.notification.worker
{
    public class TopicConsumer : BaseConsumer<string>
    {
        BaseModel baseModel = new BaseModel();
        private TopicModel _topicModel;
        private readonly ITracer _tracer;
        private readonly ILogHelper _logHelper;
        private readonly IConfiguration _configuration;

        public TopicConsumer(
        KafkaSettings _kafkaSettings,
        CancellationToken _cancellationToken,
        ITracer tracer,
        ILogger _logger, TopicModel topicModel, ILogHelper logHelper, IConfiguration configuration) : base(_kafkaSettings, _cancellationToken, _logger)
        {
            _tracer = tracer;
            _topicModel = topicModel;
            _logHelper = logHelper;
            _configuration = configuration;
        }
        public override async Task<bool> Process(string model)
        {
            var retVal = true;

            await _tracer.CaptureTransaction("Process", ApiConstants.TypeRequest, async () =>
            {
                try
                {
                    Console.WriteLine("model:" + model);

                    Console.WriteLine("If contains source cdc:" + (model.Contains("cdctablenotificationreminderdbosources")).ToString());

                    if (model.Contains("cdctablenotificationreminderdbosources"))
                    {
                        await _tracer.CurrentTransaction.CaptureSpan("ProcessSourceUpdates", ApiConstants.TypeRequest, async () =>
                         {
                             retVal = await ProcessSourceUpdates(model);
                         });
                    }
                    else
                    {
                        await _tracer.CurrentTransaction.CaptureSpan("ProcessNotifications", ApiConstants.TypeRequest, async () =>
                        {
                            retVal = await ProcessNotifications(model);
                        });
                    }
                }
                catch (Exception e)
                {
                    _logHelper.LogCreate(model, false, "Process", e.Message);
                    _tracer.CaptureException(e);
                    retVal = false;
                }
            });

            return retVal;
        }

        private async Task<bool> ProcessSourceUpdates(string model)
        {
            try
            {
                var obj = JObject.Parse(model);

                Console.WriteLine("message.data.Id:" + (obj.SelectToken("message.data.Id").ToString()).ToString());
                Console.WriteLine("GetWorkerTopicId:" + (CommonHelper.GetWorkerTopicId(_configuration)).ToString());
                Console.WriteLine("message.headers.operation:" + (obj.SelectToken("message.headers.operation").ToString()).ToString());

                if (obj.SelectToken("message.data.Id").ToString() == CommonHelper.GetWorkerTopicId(_configuration)
                    && obj.SelectToken("message.headers.operation").ToString() == "UPDATE")
                {
                    var serviceCall = new NotificationServicesCall(_tracer, _logHelper, _configuration);
                    _topicModel = await serviceCall.GetTopicDetailsAsync();

                    Console.WriteLine("_topicModel" + (Newtonsoft.Json.JsonConvert.SerializeObject(_topicModel)));
                }

                return true;
            }
            catch (Exception e)
            {
                _logHelper.LogCreate(model, false, "ProcessSourceUpdates", e.Message);
                _tracer.CaptureException(e);
                return false;
            }
        }

        private async Task<bool> ProcessNotifications(string model)
        {
            try
            {
                var obj = JObject.Parse(model);
                var clientId = Convert.ToInt32(obj.SelectToken(_topicModel.clientIdJsonPath));

                var postConsumerDetailRequestModel = new PostConsumerDetailRequestModel
                {
                    client = clientId,
                    sourceId = Convert.ToInt32(CommonHelper.GetWorkerTopicId(_configuration)),
                    jsonData = obj.SelectToken("message.data").ToString().Replace(Environment.NewLine, string.Empty)
                };

                await SetTemplateData(postConsumerDetailRequestModel, obj, clientId);

                var notificationServicesCall = new NotificationServicesCall(_tracer, _logHelper, _configuration);
                var consumerModel = await notificationServicesCall.PostConsumerDetailAsync(postConsumerDetailRequestModel);

                if (!HasConsumer(consumerModel))
                {
                    _logHelper.LogCreate(consumerModel, false, "ProcessNotifications", "Consumer null Client:" + clientId.ToString());
                    return true;
                }

                if (ShouldSendNotification(consumerModel, NotificationTypeEnum.SMS))
                {
                    await SendSms(obj, consumerModel, postConsumerDetailRequestModel);
                }

                if (ShouldSendNotification(consumerModel, NotificationTypeEnum.EMAIL))
                {
                    await SendEmail(obj, consumerModel, postConsumerDetailRequestModel);
                }

                if (ShouldSendNotification(consumerModel, NotificationTypeEnum.PUSHNOTIFICATION))
                {
                    await SendPush(obj, consumerModel, postConsumerDetailRequestModel);
                }

                return true;
            }
            catch (Exception e)
            {
                _logHelper.LogCreate(model, false, "ProcessNotifications", e.Message);
                _tracer.CaptureException(e);
                return false;
            }
        }

        private async Task SetTemplateData(PostConsumerDetailRequestModel postConsumerDetailRequestModel, JObject obj, int clientId)
        {
            if (_topicModel.ServiceUrlList.Count <= 0)
                return;

            foreach (var item in _topicModel.ServiceUrlList)
            {
                var enrichmentServiceRequestModel = new EnrichmentServiceRequestModel
                {
                    customerId = clientId,
                    dataModel = obj.SelectToken("message.data").ToString().Replace(Environment.NewLine, string.Empty)
                };

                var enrichmentServicesCall = new EnrichmentServicesCall(_tracer, _logHelper);

                var enrichmentServiceResponseModel = await enrichmentServicesCall.GetEnrichmentServiceAsync(item.ServiceUrl, enrichmentServiceRequestModel);

                if (enrichmentServiceResponseModel != null && !string.IsNullOrEmpty(enrichmentServiceResponseModel.dataModel))
                {
                    postConsumerDetailRequestModel.jsonData = enrichmentServiceResponseModel.dataModel;
                }
            }
        }

        private bool ShouldSendNotification(ConsumerModel consumerModel, NotificationTypeEnum notificationType)
        {
            var alwaysSendTypeList = ((AlwaysSendType)_topicModel.alwaysSendType).ToEnumArray();

            switch (notificationType)
            {
                case NotificationTypeEnum.SMS:
                    if (HasValidServiceReference(_topicModel.smsServiceReference) &&
                         (consumerModel.consumers[0].isSmsEnabled || alwaysSendTypeList.Contains(AlwaysSendType.Sms)))
                    {
                        return true;
                    }
                    break;
                case NotificationTypeEnum.EMAIL:
                    if (HasValidServiceReference(_topicModel.emailServiceReference) &&
                        (consumerModel.consumers[0].isEmailEnabled || alwaysSendTypeList.Contains(AlwaysSendType.EMail)))
                    {
                        return true;
                    }
                    break;
                case NotificationTypeEnum.PUSHNOTIFICATION:
                    if (HasValidServiceReference(_topicModel.pushServiceReference) &&
                        (consumerModel.consumers[0].isPushEnabled || alwaysSendTypeList.Contains(AlwaysSendType.Push)))
                    {
                        return true;
                    }
                    break;
            }

            return false;
        }

        private async Task<bool> SendSms(JObject obj, ConsumerModel consumerModel, PostConsumerDetailRequestModel postConsumerDetailRequestModel)
        {
            try
            {
                var kafkaDataTime = DateTime.Today;

                if (!string.IsNullOrEmpty(obj.SelectToken("message.headers.timestamp").ToString()))
                {
                    kafkaDataTime = Convert.ToDateTime(obj.SelectToken("message.headers.timestamp"));
                }

                var clientId = Convert.ToInt32(obj.SelectToken(_topicModel.clientIdJsonPath));

                if (_topicModel.RetentationTime != 0 && kafkaDataTime < DateTime.Now.AddMinutes(-_topicModel.RetentationTime))
                {
                    _logHelper.LogCreate(clientId, false, "SendSms", "Kafka data is timeout");
                    return false;
                }

                var dengageRequestModel = new DengageRequestModel();
                dengageRequestModel.phone.countryCode = consumerModel.consumers[0].phone.countryCode;
                dengageRequestModel.phone.prefix = consumerModel.consumers[0].phone.prefix;
                dengageRequestModel.phone.number = consumerModel.consumers[0].phone.number;
                dengageRequestModel.template = _topicModel.smsServiceReference;
                dengageRequestModel.templateParams = postConsumerDetailRequestModel.jsonData;
                dengageRequestModel.process.name = string.IsNullOrEmpty(_topicModel.processName) ? _topicModel.topic : _topicModel.processName;
                dengageRequestModel.process.ItemId = GetProcessItemId(obj, _topicModel.processItemId);
                dengageRequestModel.process.Action = "Notification";
                dengageRequestModel.process.Identity = "1";

                var sendSmsPath = baseModel.GetSendSmsEndpoint();
                var response = await ApiHelper.ApiClient.PostAsJsonAsync(sendSmsPath, dengageRequestModel);

                if (!response.IsSuccessStatusCode)
                    return true;

                var respModel = await response.Content.ReadAsAsync<SendMessageResponseModel>();

                if (respModel == null || respModel.TxnId == null)
                {
                    _logHelper.LogCreate(respModel, false, "SendSmsContentResponseTxnModel", "TxnId is null");
                }
                else
                {
                    var pathGeneratedMessage = baseModel.GetGeneratedMessageEndPoint().Replace("{txnId}", respModel.TxnId.ToString());

                    var htpResponse = await ApiHelper.ApiClient.GetAsync(pathGeneratedMessage);

                    if (!htpResponse.IsSuccessStatusCode)
                    {
                        _logHelper.LogCreate(sendSmsPath, htpResponse, "SendSmsContentError", "GeneratedMessage status false");
                    }
                    else
                    {
                        var generatedMessageModel = await htpResponse.Content.ReadAsAsync<GeneratedMessage>();

                        if (generatedMessageModel == null)
                        {
                            _logHelper.LogCreate(sendSmsPath, generatedMessageModel, "SendSmsContentResponseModel", "Content is null");
                        }
                        else
                        {
                            _logHelper.MessageNotificationLogCreate(postConsumerDetailRequestModel.client, postConsumerDetailRequestModel.sourceId,
                                consumerModel.consumers[0] != null ? consumerModel.consumers[0].phone.prefix + "" + consumerModel.consumers[0].phone.number : null,
                                "", dengageRequestModel, response, NotificationTypeEnum.SMS.GetHashCode(), response.StatusCode.ToString(),
                                generatedMessageModel.Content, consumerModel.consumers[0].isStaff);
                        }
                    }
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
        private async Task<bool> SendEmail(JObject obj, ConsumerModel consumerModel, PostConsumerDetailRequestModel postConsumerDetailRequestModel)
        {
            try
            {
                var emailRequestModel = new EmailRequestModel();
                emailRequestModel.CustomerNo = consumerModel.consumers[0].client;
                emailRequestModel.Email = consumerModel.consumers[0].email;

                if (string.IsNullOrEmpty(emailRequestModel.Email))
                {
                    _logHelper.LogCreate("CustomerNo=>" + emailRequestModel.CustomerNo, false, "SendEmail", " Email address is null");
                    return false;
                }

                emailRequestModel.TemplateParams = postConsumerDetailRequestModel.jsonData;
                emailRequestModel.Template = _topicModel.emailServiceReference;
                emailRequestModel.Process = new DengageRequestModel.Process();
                emailRequestModel.Process.name = string.IsNullOrEmpty(_topicModel.processName) ? _topicModel.topic : _topicModel.processName;
                emailRequestModel.Process.ItemId = GetProcessItemId(obj, _topicModel.processItemId);
                emailRequestModel.Process.Action = "Notification";
                emailRequestModel.Process.Identity = "1";

                var sendEmailPath = baseModel.GetSendEmailEndpoint();
                var response = await ApiHelper.ApiClient.PostAsJsonAsync(sendEmailPath, emailRequestModel);

                if (!response.IsSuccessStatusCode)
                    return true;

                var respModel = await response.Content.ReadAsAsync<SendMessageResponseModel>();

                if (respModel == null || respModel.TxnId == null)
                {
                    _logHelper.LogCreate(respModel, false, "SendEmailContentResponseTxnModel", "TxnId is null");
                }
                else
                {
                    var pathGeneratedMessage = baseModel.GetGeneratedMessageEndPoint().Replace("{txnId}", respModel.TxnId.ToString());

                    var htpResponse = await ApiHelper.ApiClient.GetAsync(pathGeneratedMessage);

                    if (!htpResponse.IsSuccessStatusCode)
                    {
                        _logHelper.LogCreate(sendEmailPath, htpResponse, "SendEmailContentError", "GeneratedMessage status false");
                    }
                    else
                    {
                        var generatedMessageModel = await htpResponse.Content.ReadAsAsync<GeneratedMessage>();

                        if (generatedMessageModel == null)
                        {
                            _logHelper.LogCreate(sendEmailPath, generatedMessageModel, "SendEmailContentResponseModel", "Content is null");
                        }
                        else
                        {
                            _logHelper.MessageNotificationLogCreate(postConsumerDetailRequestModel.client, postConsumerDetailRequestModel.sourceId, "",
                                consumerModel.consumers[0] != null ? consumerModel.consumers[0].email : null,
                                emailRequestModel, response, NotificationTypeEnum.EMAIL.GetHashCode(), response.StatusCode.ToString(),
                                generatedMessageModel.Content, consumerModel.consumers[0].isStaff);

                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logHelper.LogCreate(consumerModel, false, "SendEmail", ex.Message);
                _tracer.CaptureException(ex);
                return false;
            }
        }

        private async Task<bool> SendPush(JObject obj, ConsumerModel consumerModel, PostConsumerDetailRequestModel postConsumerDetailRequestModel)
        {
            try
            {
                var pushNotificationRequestModel = new PushNotificaitonRequestModel
                {
                    CustomerNo = consumerModel.consumers[0].client.ToString(),
                    TemplateParams = postConsumerDetailRequestModel.jsonData,
                    Template = _topicModel.pushServiceReference,
                    SaveInbox = _topicModel.saveInbox,
                    NotificationType = _topicModel.productCodeName,

                    Process = new DengageRequestModel.Process
                    {
                        name = string.IsNullOrEmpty(_topicModel.processName) ? _topicModel.topic : _topicModel.processName,
                        ItemId = GetProcessItemId(obj, _topicModel.processItemId),
                        Action = "Notification",
                        Identity = "1"
                    }
                };

                var sendPushPath = baseModel.GetSendPushnotificationEndpoint();
                var response = await ApiHelper.ApiClient.PostAsJsonAsync(sendPushPath, pushNotificationRequestModel);

                if (!response.IsSuccessStatusCode)
                    return true;

                var respModel = await response.Content.ReadAsAsync<SendMessageResponseModel>();

                if (respModel == null || respModel.TxnId == null)
                {
                    _logHelper.LogCreate(respModel, false, "SendPushContentResponseTxnModel", "TxnId is null");
                }
                else
                {
                    var pathGeneratedMessage = baseModel.GetGeneratedMessageEndPoint().Replace("{txnId}", respModel.TxnId.ToString());
                    var htpResponse = await ApiHelper.ApiClient.GetAsync(pathGeneratedMessage);

                    if (!htpResponse.IsSuccessStatusCode)
                    {
                        _logHelper.LogCreate(sendPushPath, htpResponse, "SendPushContentError", "GeneratedMessage status false");
                    }
                    else
                    {
                        var generatedMessageModel = await htpResponse.Content.ReadAsAsync<GeneratedMessage>();

                        if (generatedMessageModel == null)
                        {
                            _logHelper.LogCreate(sendPushPath, generatedMessageModel, "SendPushContentResponseModel", "Content is null");
                        }
                        else
                        {
                            _logHelper.MessageNotificationLogCreate(postConsumerDetailRequestModel.client, postConsumerDetailRequestModel.sourceId, consumerModel.consumers[0] != null ? consumerModel.consumers[0].phone.prefix + "" + consumerModel.consumers[0].phone.number : null, "", JsonConvert.SerializeObject(pushNotificationRequestModel, Formatting.Indented), response, NotificationTypeEnum.PUSHNOTIFICATION.GetHashCode(), response.StatusCode.ToString(), generatedMessageModel.Content, consumerModel.consumers[0].isStaff);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logHelper.LogCreate(consumerModel, false, "SendPushNotification", ex.Message);
                _tracer.CaptureException(ex);
                return false;
            }
        }
        private static string GetJsonValue(JObject obj, string path)
        {
            var retVal = "";

            var token = obj.SelectToken(path);

            if (token != null)
            {
                retVal = token.ToString();
            }

            return retVal;
        }

        private static string GetProcessItemId(JObject obj, string processItemIdPath)
        {
            if (string.IsNullOrWhiteSpace(processItemIdPath))
                return "1";

            var tmp = processItemIdPath.Split(';');
            var sb = new StringBuilder();

            foreach (var item in tmp)
            {
                if (string.IsNullOrWhiteSpace(item))
                    continue;

                var tmpProcessItemId = GetJsonValue(obj, item);

                if (!string.IsNullOrWhiteSpace(tmpProcessItemId))
                    sb.Append(tmpProcessItemId + "-");
            }

            return sb.ToString().TrimEnd('-');
        }

        private static bool HasConsumer(ConsumerModel consumerModel)
        {
            return consumerModel != null && consumerModel.consumers != null && consumerModel.consumers.Count() > 0;
        }
        private static bool HasValidServiceReference(string serviceReference)
        {
            return !string.IsNullOrEmpty(serviceReference) && serviceReference != "string";
        }
    }
}