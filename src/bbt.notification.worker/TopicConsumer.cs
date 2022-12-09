using System.Reflection;
using bbt.framework.kafka;
using bbt.notification.worker.Enum;
using bbt.notification.worker.Helper;
using bbt.notification.worker.Models;
using Elastic.Apm.Api;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace bbt.notification.worker
{
    public class TopicConsumer : BaseConsumer<String>
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
                    PostConsumerDetailRequestModel postConsumerDetailRequestModel = new PostConsumerDetailRequestModel();
                    postConsumerDetailRequestModel.client = Convert.ToInt32(clientId);
                    postConsumerDetailRequestModel.sourceId = Convert.ToInt32(Environment.GetEnvironmentVariable("Topic_Id") is null ? (_configuration.GetSection("TopicId").Value) : Environment.GetEnvironmentVariable("Topic_Id"));
                    postConsumerDetailRequestModel.jsonData = o.SelectToken("message.data").ToString();
                    postConsumerDetailRequestModel.jsonData = postConsumerDetailRequestModel.jsonData.Replace(System.Environment.NewLine, string.Empty);

                    if (topicModel.ServiceUrlList.Count > 0)
                    {
                        foreach (var item in topicModel.ServiceUrlList)
                        {

                            EnrichmentServiceRequestModel enrichmentServiceRequestModel = new EnrichmentServiceRequestModel();
                            enrichmentServiceRequestModel.customerId = Convert.ToInt32(clientId);
                            enrichmentServiceRequestModel.dataModel = o.SelectToken("message.data").ToString();
                            enrichmentServiceRequestModel.dataModel = enrichmentServiceRequestModel.dataModel.Replace(System.Environment.NewLine, string.Empty);
                            EnrichmentServicesCall enrichmentServicesCall = new EnrichmentServicesCall(_tracer, _logHelper);
                            EnrichmentServiceResponseModel enrichmentServiceResponseModel = await enrichmentServicesCall.GetEnrichmentServiceAsync(item.ServiceUrl, enrichmentServiceRequestModel);

                            if (enrichmentServiceResponseModel != null && !string.IsNullOrEmpty(enrichmentServiceResponseModel.dataModel))
                            {
                                postConsumerDetailRequestModel.jsonData = enrichmentServiceResponseModel.dataModel;
                            }

                        }
                    }

                    NotificationServicesCall notificationServicesCall = new NotificationServicesCall(_tracer, _logHelper, _configuration);
                    ConsumerModel consumerModel = await notificationServicesCall.PostConsumerDetailAsync(postConsumerDetailRequestModel);

                    if (!String.IsNullOrEmpty(topicModel.smsServiceReference) && topicModel.smsServiceReference != "string")
                    {
                        if (consumerModel != null && consumerModel.consumers.Count() > 0 && consumerModel.consumers[0].isSmsEnabled == true)
                        {

                            bool sendSms = await SendSms(o, consumerModel, postConsumerDetailRequestModel);

                        }

                    }
                    if (!String.IsNullOrEmpty(topicModel.emailServiceReference) && topicModel.emailServiceReference != "string")
                    {
                       
                        if (consumerModel != null && consumerModel.consumers.Count() > 0 && consumerModel.consumers[0].isEmailEnabled == true)
                        {
                         
                            bool sendEmail = await SendEmail(o, consumerModel, postConsumerDetailRequestModel);
                        }

                    }
                    if (!String.IsNullOrEmpty(topicModel.pushServiceReference) && topicModel.pushServiceReference != "string")
                    {
                      
                        if (consumerModel != null && consumerModel.consumers.Count() > 0 && consumerModel.consumers[0].isPushEnabled == true)
                        {
                            bool sendPushNotfication = await SendPushNotification(consumerModel, postConsumerDetailRequestModel);
                        }
                    }

                    return true;
                }
                catch (Exception e)
                {
                    _logHelper.LogCreate(model, false, "Process", e.Message);
                    _tracer.CaptureException(e);
                    Console.WriteLine(e.Message);
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
                if (!String.IsNullOrEmpty(o.SelectToken("message.headers.timestamp").ToString()))
                {
                    kafkaDataTime = Convert.ToDateTime(o.SelectToken("message.headers.timestamp"));
                }

                if (topicModel.RetentationTime == 0 || kafkaDataTime >= DateTime.Now.AddMinutes(-(topicModel.RetentationTime)))

                {
                    DengageRequestModel dengageRequestModel = new DengageRequestModel();
                    string path = baseModel.GetSendSmsEndpoint();
                    if (consumerModel != null && consumerModel.consumers != null && consumerModel.consumers.Count > 0)
                    {
                        dengageRequestModel.phone.countryCode = consumerModel.consumers[0].phone.countryCode;
                        dengageRequestModel.phone.prefix = consumerModel.consumers[0].phone.prefix;
                        dengageRequestModel.phone.number = consumerModel.consumers[0].phone.number;
                        Console.WriteLine(consumerModel.consumers[0].phone.prefix + "" + consumerModel.consumers[0].phone.number);
                        dengageRequestModel.template = topicModel.smsServiceReference;
                        dengageRequestModel.templateParams = postConsumerDetailRequestModel.jsonData;
                        dengageRequestModel.process.name = "Notification-Cashback";
                        dengageRequestModel.process.ItemId = "1";
                        dengageRequestModel.process.Action = "Notification";
                        dengageRequestModel.process.Identity = "1";
                        SendMessageResponseModel respModel = new SendMessageResponseModel();
                        GeneratedMessage generatedMessageModel = new GeneratedMessage();
                        HttpResponseMessage response = await ApiHelper.ApiClient.PostAsJsonAsync(path, dengageRequestModel);

                        Console.WriteLine("SMS=>" + response.StatusCode);

                        if (response.IsSuccessStatusCode)
                        {
                            respModel = await response.Content.ReadAsAsync<SendMessageResponseModel>();
                            if (respModel != null && respModel.TxnId != null)
                            {
                                string pathGeneratedMessage = baseModel.GetGeneratedMessageEndPoint().Replace("{txnId}", respModel.TxnId.ToString());
                                Console.WriteLine(pathGeneratedMessage);
                                HttpResponseMessage htpResponse = await ApiHelper.ApiClient.GetAsync(pathGeneratedMessage);
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
                        JToken clientId = o.SelectToken(topicModel.clientIdJsonPath);
                        _logHelper.LogCreate(Convert.ToInt32(clientId), false, "SendSms", "Consumer null");

                        Console.WriteLine(Convert.ToInt32(clientId) + "Consumer null");
                        return false;
                    }



                }
                else
                {
                    Console.WriteLine("Kafka data is timeout");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logHelper.LogCreate(consumerModel, false, "SendSms", ex.Message);
                _tracer.CaptureException(ex);
                Console.WriteLine(ex.Message);
                return false;
            }
        }
        public async Task<bool> SendEmail(JObject o, ConsumerModel consumerModel, PostConsumerDetailRequestModel postConsumerDetailRequestModel)
        {
            try
            {
                EmailRequestModel emailRequestModel = new EmailRequestModel();
                string path = baseModel.GetSendEmailEndpoint();
                if (consumerModel != null && consumerModel.consumers != null && consumerModel.consumers.Count > 0)
                {
                    emailRequestModel.CustomerNo = consumerModel.consumers[0].client;
                    emailRequestModel.Email = consumerModel.consumers[0].email;
                    if (!String.IsNullOrEmpty(emailRequestModel.Email))
                    {
                        Console.WriteLine(consumerModel.consumers[0].email);

                        emailRequestModel.TemplateParams = postConsumerDetailRequestModel.jsonData;
                        emailRequestModel.Template = topicModel.emailServiceReference;
                        emailRequestModel.Process = new DengageRequestModel.Process();
                        emailRequestModel.Process.name = "Notification-Cashback";
                        emailRequestModel.Process.ItemId = "1";
                        emailRequestModel.Process.Action = "Notification";
                        emailRequestModel.Process.Identity = "1";
                        SendMessageResponseModel respModel = new SendMessageResponseModel();
                        GeneratedMessage generatedMessageModel = new GeneratedMessage();
                        HttpResponseMessage response = await ApiHelper.ApiClient.PostAsJsonAsync(path, emailRequestModel);

                        Console.WriteLine("EMAIL=>" + response.StatusCode + "" + emailRequestModel.Email);
                        // _logHelper.MessageNotificationLogCreate(postConsumerDetailRequestModel.client, postConsumerDetailRequestModel.sourceId, "", consumerModel.consumers[0] != null ? consumerModel.consumers[0].email : null, emailRequestModel, response, NotificationTypeEnum.EMAIL.GetHashCode(), response.StatusCode.ToString());


                        if (response.IsSuccessStatusCode)
                        {
                            respModel = await response.Content.ReadAsAsync<SendMessageResponseModel>();
                            if (respModel != null && respModel.TxnId != null)
                            {
                                string pathGeneratedMessage = baseModel.GetGeneratedMessageEndPoint().Replace("{txnId}", respModel.TxnId.ToString());
                                Console.WriteLine(pathGeneratedMessage);
                                HttpResponseMessage htpResponse = await ApiHelper.ApiClient.GetAsync(pathGeneratedMessage);
                                if (htpResponse.IsSuccessStatusCode)
                                {
                                    generatedMessageModel = await htpResponse.Content.ReadAsAsync<GeneratedMessage>();
                                    if (generatedMessageModel != null)
                                    {
                                        _logHelper.MessageNotificationLogCreate(postConsumerDetailRequestModel.client, postConsumerDetailRequestModel.sourceId, "", consumerModel.consumers[0] != null ? consumerModel.consumers[0].email : null, emailRequestModel, response, NotificationTypeEnum.EMAIL.GetHashCode(), response.StatusCode.ToString(), generatedMessageModel.Content,consumerModel.consumers[0].isStaff);

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
                        Console.WriteLine(emailRequestModel.CustomerNo + " Email address is null");
                        return false;
                    }
                }
                else
                {
                    JToken clientId = o.SelectToken(topicModel.clientIdJsonPath);
                    _logHelper.LogCreate(Convert.ToInt32(clientId), false, "SendEmail", "Consumer  null");
                    Console.WriteLine(Convert.ToInt32(clientId) + "Consumer null");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logHelper.LogCreate(consumerModel, false, "SendEmail", ex.Message);
                _tracer.CaptureException(ex);
                Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }

        public async Task<bool> SendPushNotification(ConsumerModel consumerModel, PostConsumerDetailRequestModel postConsumerDetailRequestModel)
        {
            try
            {
                PushNotificaitonRequestModel pushNotificationRequestModel = new PushNotificaitonRequestModel();
                string path = baseModel.GetSendPushnotificationEndpoint();
                if (consumerModel != null && consumerModel.consumers != null && consumerModel.consumers.Count > 0)
                {
                    pushNotificationRequestModel.CustomerNo = consumerModel.consumers[0].client.ToString();
                    pushNotificationRequestModel.TemplateParams = postConsumerDetailRequestModel.jsonData;
                    //    pushNotificationRequestModel.TemplateParams = "";
                    pushNotificationRequestModel.Template = topicModel.pushServiceReference;
                    pushNotificationRequestModel.Process = new DengageRequestModel.Process();
                    pushNotificationRequestModel.Process.name = "Notification-Cashback";
                    pushNotificationRequestModel.Process.ItemId = "1";
                    pushNotificationRequestModel.Process.Action = "Notification";
                    pushNotificationRequestModel.Process.Identity = "1";
                    SendMessageResponseModel respModel = new SendMessageResponseModel();
                    GeneratedMessage generatedMessageModel = new GeneratedMessage();
                    HttpResponseMessage response = await ApiHelper.ApiClient.PostAsJsonAsync(path, pushNotificationRequestModel);
                    Console.WriteLine("PUSHNOTIFICATION=>" + response.StatusCode);

                    if (response.IsSuccessStatusCode)
                    {
                        respModel = await response.Content.ReadAsAsync<SendMessageResponseModel>();
                        if (respModel != null && respModel.TxnId != null)
                        {
                            string pathGeneratedMessage = baseModel.GetGeneratedMessageEndPoint().Replace("{txnId}", respModel.TxnId.ToString());
                            Console.WriteLine(pathGeneratedMessage);
                            HttpResponseMessage htpResponse = await ApiHelper.ApiClient.GetAsync(pathGeneratedMessage);
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

                    Console.WriteLine("Consumer null");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logHelper.LogCreate(consumerModel, false, "SendPushNotification", ex.Message);
                _tracer.CaptureException(ex);
                Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }
    }
}