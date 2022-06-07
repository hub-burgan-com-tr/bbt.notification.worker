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
                        enrichmentServiceRequestModel.jsonData = o.SelectToken("message.data").ToString();
                        enrichmentServiceRequestModel.jsonData = enrichmentServiceRequestModel.jsonData.Replace(System.Environment.NewLine, string.Empty);
                        EnrichmentServiceResponseModel enrichmentServiceResponseModel = await EnrichmentServicesCall.GetEnrichmentServiceAsync(item.ServiceUrl, enrichmentServiceRequestModel);
                        Console.WriteLine(item.ServiceUrl);
                        Console.WriteLine(JsonConvert.SerializeObject(enrichmentServiceResponseModel));
                        Console.WriteLine(JsonConvert.SerializeObject(enrichmentServiceRequestModel));

                        if (enrichmentServiceResponseModel != null && !string.IsNullOrEmpty(enrichmentServiceResponseModel.dataModel))
                        {
                            postConsumerDetailRequestModel.jsonData = enrichmentServiceResponseModel.dataModel;
                        }
                    }
                }
                ConsumerModel consumerModel = await NotificationServicesCall.PostConsumerDetailAsync(postConsumerDetailRequestModel);

                DengageRequestModel dengageRequestModel = new DengageRequestModel();
                string path = baseModel.GetSendSmsEndpoint();
                dengageRequestModel.phone.countryCode = consumerModel.consumers[0].phone.countryCode;
                dengageRequestModel.phone.prefix = consumerModel.consumers[0].phone.prefix;
                dengageRequestModel.phone.number = consumerModel.consumers[0].phone.number;
                dengageRequestModel.template = topicModel.smsServiceReference;
                dengageRequestModel.templateParams = postConsumerDetailRequestModel.jsonData;
                dengageRequestModel.process.name = "Notification-Cashback";
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