using System.Reflection;
using bbt.framework.kafka;
using bbt.notification.worker.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace bbt.notification.worker
{
    public class TopicConsumer : BaseConsumer<String>
    {
        TopicModel topicModel;
        BaseModel baseModel = new BaseModel();

        public TopicConsumer(
        KafkaSettings _kafkaSettings,
        CancellationToken _cancellationToken,


        ILogger _logger, TopicModel _topicModel) : base(_kafkaSettings, _cancellationToken, _logger)
        {


            topicModel = _topicModel;

        }
        public override async Task<bool> Process(string model)
        {
            try
            {

                JObject o = JObject.Parse(model);
                JToken clientId = o.SelectToken(topicModel.clientIdJsonPath);
                PostConsumerDetailRequestModel postConsumerDetailRequestModel = new PostConsumerDetailRequestModel();
                postConsumerDetailRequestModel.client = Convert.ToInt32(clientId);
                postConsumerDetailRequestModel.sourceId = Convert.ToInt32(Environment.GetEnvironmentVariable("Topic_Id") is null ? "10158" : Environment.GetEnvironmentVariable("Topic_Id"));
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
                        EnrichmentServiceResponseModel enrichmentServiceResponseModel = await EnrichmentServicesCall.GetEnrichmentServiceAsync(item.ServiceUrl, enrichmentServiceRequestModel);
                        Console.WriteLine(item.ServiceUrl);
                        Console.WriteLine(JsonConvert.SerializeObject(enrichmentServiceRequestModel));
                        Console.WriteLine(JsonConvert.SerializeObject(enrichmentServiceResponseModel));

                        postConsumerDetailRequestModel.jsonData = enrichmentServiceResponseModel.dataModel;

                    }
                }
                Console.WriteLine("STEP3" + JsonConvert.SerializeObject(postConsumerDetailRequestModel));
                ConsumerModel consumerModel = await NotificationServicesCall.PostConsumerDetailAsync(postConsumerDetailRequestModel);
                Console.WriteLine("STEP4=>" + JsonConvert.SerializeObject(consumerModel));
                DengageRequestModel dengageRequestModel = new DengageRequestModel();
                Console.WriteLine("STEP5=>" + baseModel.GetSendSmsEndpoint());
                string path = baseModel.GetSendSmsEndpoint();
                Console.WriteLine("STEP6");
                dengageRequestModel.phone.countryCode = consumerModel.consumers[0].phone.countryCode;
                Console.WriteLine("STEP7");
                dengageRequestModel.phone.prefix = consumerModel.consumers[0].phone.prefix;
                Console.WriteLine("STEP8");
                dengageRequestModel.phone.number = consumerModel.consumers[0].phone.number;
                Console.WriteLine("STEP9");
                dengageRequestModel.template = topicModel.smsServiceReference;
                Console.WriteLine("STEP10");
                dengageRequestModel.templateParams = postConsumerDetailRequestModel.jsonData;
                Console.WriteLine("STEP11");
                dengageRequestModel.process.name = "Notification-Cashback";
                Console.WriteLine("STEP12");
                
                 HttpResponseMessage response = await ApiHelper.ApiClient.PostAsJsonAsync(path, dengageRequestModel);
                Console.WriteLine(response.StatusCode);
                if (response.IsSuccessStatusCode)
                {
                    consumerModel = await response.Content.ReadAsAsync<ConsumerModel>();
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return true;
            }
        }

    }


}