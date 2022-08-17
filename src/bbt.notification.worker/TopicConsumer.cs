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
        public TopicConsumer(
        KafkaSettings _kafkaSettings,
        CancellationToken _cancellationToken,
        ITracer tracer,
        ILogger _logger, TopicModel _topicModel, ILogHelper logHelper) : base(_kafkaSettings, _cancellationToken, _logger)
        {
            _tracer = tracer;
            topicModel = _topicModel;
            _logHelper = logHelper;

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

                    postConsumerDetailRequestModel.sourceId = Convert.ToInt32(Environment.GetEnvironmentVariable("Topic_Id") is null ? "10170" : Environment.GetEnvironmentVariable("Topic_Id"));
                    postConsumerDetailRequestModel.jsonData = o.SelectToken("message.data").ToString();
                    postConsumerDetailRequestModel.jsonData = postConsumerDetailRequestModel.jsonData.Replace(System.Environment.NewLine, string.Empty);

                    if (topicModel.ServiceUrlList is not null)
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
                    Console.WriteLine("consumerRequestModel=>" + JsonConvert.SerializeObject(postConsumerDetailRequestModel));
                    NotificationServicesCall notificationServicesCall = new NotificationServicesCall(_tracer, _logHelper);
                    ConsumerModel consumerModel = await notificationServicesCall.PostConsumerDetailAsync(postConsumerDetailRequestModel);
                    Console.WriteLine("consumerresponseModel=>" + JsonConvert.SerializeObject(consumerModel));
                    if (!String.IsNullOrEmpty(topicModel.smsServiceReference) && topicModel.smsServiceReference != "string")
                    {
                        bool sendSms =await SendSms(o, consumerModel, postConsumerDetailRequestModel);
                     

                    }
                    if (!String.IsNullOrEmpty(topicModel.emailServiceReference) && topicModel.emailServiceReference!="string")
                    {
                        bool sendEmail = await SendEmail(consumerModel, postConsumerDetailRequestModel);
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

                if (topicModel.KafkaDataTime == 0 || kafkaDataTime >= DateTime.Now.AddMinutes(-(topicModel.KafkaDataTime)))

                {
                    DengageRequestModel dengageRequestModel = new DengageRequestModel();
                    string path = baseModel.GetSendSmsEndpoint();
                    if (consumerModel.consumers != null)
                    {
                        dengageRequestModel.phone.countryCode = consumerModel.consumers[0].phone.countryCode;
                        dengageRequestModel.phone.prefix = consumerModel.consumers[0].phone.prefix;
                        dengageRequestModel.phone.number = consumerModel.consumers[0].phone.number;
                    }
                    dengageRequestModel.template = topicModel.smsServiceReference;
                    dengageRequestModel.templateParams = postConsumerDetailRequestModel.jsonData;
                    dengageRequestModel.process.name = "Notification-Cashback";

                    HttpResponseMessage response = await ApiHelper.ApiClient.PostAsJsonAsync(path, dengageRequestModel);
                    Console.WriteLine("SMS=>" + response.StatusCode);
                  
                    if (response.IsSuccessStatusCode)
                    {
                        consumerModel = await response.Content.ReadAsAsync<ConsumerModel>();
                        _logHelper.LogCreate(consumerModel, true, "SendSms", ResultEnum.SUCCESS.ToString());
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
        public async Task<bool> SendEmail(ConsumerModel consumerModel, PostConsumerDetailRequestModel postConsumerDetailRequestModel)
        {
            try
            {


                EmailRequestModel emailRequestModel = new EmailRequestModel();
                string path = baseModel.GetSendEmailEndpoint();
                if (consumerModel.consumers != null)
                {
                    emailRequestModel.CustomerNo = consumerModel.consumers[0].client;
                 
                 
                    emailRequestModel.Email = consumerModel.consumers[0].email;
                }
                // emailRequestModel.TemplateParams = postConsumerDetailRequestModel.jsonData;
                emailRequestModel.TemplateParams = "";
                emailRequestModel.Template = topicModel.emailServiceReference;
                emailRequestModel.Process = new DengageRequestModel.Process();
                emailRequestModel.Process.name = "Notification-Cashback";
                emailRequestModel.Process.ItemId ="1" ;
                emailRequestModel.Process.Action = "Notification";
                emailRequestModel.Process.Identity = "1";



                HttpResponseMessage response = await ApiHelper.ApiClient.PostAsJsonAsync(path, emailRequestModel);
                Console.WriteLine("EMAIL=>" + response.StatusCode);
               
                if (response.IsSuccessStatusCode)
                {
                    consumerModel = await response.Content.ReadAsAsync<ConsumerModel>();
                    _logHelper.LogCreate(consumerModel, true, "SendEmail", ResultEnum.SUCCESS.ToString());
                }
            }
            catch (Exception ex)
            {
                _logHelper.LogCreate(consumerModel, false, "Process", ex.Message);
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
                if (consumerModel.consumers != null)
                {
                    pushNotificationRequestModel.CustomerNo = consumerModel.consumers[0].client.ToString();



                }
                // emailRequestModel.TemplateParams = postConsumerDetailRequestModel.jsonData;
                pushNotificationRequestModel.TemplateParams = "";
                pushNotificationRequestModel.Template = topicModel.pushServiceReference;
                pushNotificationRequestModel.Process = new DengageRequestModel.Process();
                pushNotificationRequestModel.Process.name = "Notification-Cashback";
                pushNotificationRequestModel.Process.ItemId = "1";
                pushNotificationRequestModel.Process.Action = "Notification";
                pushNotificationRequestModel.Process.Identity = "1";



                HttpResponseMessage response = await ApiHelper.ApiClient.PostAsJsonAsync(path, pushNotificationRequestModel);
                Console.WriteLine("PUSHNOTIFICATION=>" + response.StatusCode);
             
                if (response.IsSuccessStatusCode)
                {
                    consumerModel = await response.Content.ReadAsAsync<ConsumerModel>();
                    _logHelper.LogCreate(consumerModel, true, "SendPushNotification", ResultEnum.SUCCESS.ToString());
                }
            }
            catch (Exception ex)
            {
                _logHelper.LogCreate(consumerModel, false, "Process", ex.Message);
                _tracer.CaptureException(ex);
                Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }
    }
}