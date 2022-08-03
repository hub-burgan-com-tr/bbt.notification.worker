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
                    DateTime kafkaDataTime = DateTime.Today;
                    JObject o = JObject.Parse(model);
                    if (!String.IsNullOrEmpty(o.SelectToken("message.headers.timestamp").ToString()))
                    {
                        kafkaDataTime = Convert.ToDateTime(o.SelectToken("message.headers.timestamp"));
                    }

                    if (topicModel.KafkaDataTime == 0 || kafkaDataTime >= DateTime.Now.AddMinutes(-(topicModel.KafkaDataTime)))
                    {
                        JToken clientId = o.SelectToken(topicModel.clientIdJsonPath);

                        PostConsumerDetailRequestModel postConsumerDetailRequestModel = new PostConsumerDetailRequestModel();
                        postConsumerDetailRequestModel.client = Convert.ToInt32(clientId);

                        postConsumerDetailRequestModel.sourceId = Convert.ToInt32(Environment.GetEnvironmentVariable("Topic_Id") is null ? "1" : Environment.GetEnvironmentVariable("Topic_Id"));
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
                        DengageRequestModel dengageRequestModel = new DengageRequestModel();
                        string path = baseModel.GetSendSmsEndpoint();
                        dengageRequestModel.phone.countryCode = consumerModel.consumers[0].phone.countryCode;
                        dengageRequestModel.phone.prefix = consumerModel.consumers[0].phone.prefix;
                        dengageRequestModel.phone.number = consumerModel.consumers[0].phone.number;
                        dengageRequestModel.template = topicModel.smsServiceReference;
                        dengageRequestModel.templateParams = postConsumerDetailRequestModel.jsonData;
                        dengageRequestModel.process.name = "Notification-Cashback";

                        HttpResponseMessage response = await ApiHelper.ApiClient.PostAsJsonAsync(path, dengageRequestModel);
                        Console.WriteLine("SMS=>" + response.StatusCode);
                        _logHelper.LogCreate(model, true, "Process", ResultEnum.SUCCESS.ToString());
                        if (response.IsSuccessStatusCode)
                        {
                            consumerModel = await response.Content.ReadAsAsync<ConsumerModel>();
                        }
                    }
                    else
                    {
                        Console.WriteLine("Kafka data is timeout");
                        return false;
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


    }
}