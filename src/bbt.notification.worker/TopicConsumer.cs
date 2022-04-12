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
        public TopicConsumer(
        KafkaSettings _kafkaSettings,
        CancellationToken _cancellationToken,

        ILogger _logger, TopicModel _topicModel) : base(_kafkaSettings, _cancellationToken, _logger)
        {


            topicModel = _topicModel;

        }
        public override async Task Process(string model)
        {


            JObject o = JObject.Parse(model);
            JToken clientId = o.SelectToken(topicModel.clientIdJsonPath);
            PostConsumerDetailRequestModel postConsumerDetailRequestModel = new PostConsumerDetailRequestModel();
            postConsumerDetailRequestModel.client = Convert.ToInt32(clientId);
            postConsumerDetailRequestModel.sourceId=Convert.ToInt32(Environment.GetEnvironmentVariable("Topic_Id") is null ? "1" : Environment.GetEnvironmentVariable("Topic_Id"));

            if (topicModel.ServiceUrlList is not null)
            {
                foreach (var item in topicModel.ServiceUrlList)
                {
                    EnrichmentServiceRequestModel enrichmentServiceRequestModel = new EnrichmentServiceRequestModel();
                    enrichmentServiceRequestModel.customerId = Convert.ToInt32(clientId);
                    enrichmentServiceRequestModel.dataModel = o.SelectToken("message.data").ToString();
                    EnrichmentServiceResponseModel enrichmentServiceResponseModel = await EnrichmentServicesCall.GetEnrichmentServiceAsync(item.ServiceUrl, enrichmentServiceRequestModel);
                    postConsumerDetailRequestModel.jsonData = enrichmentServiceResponseModel.dataModel;

                }
            }
            ConsumerModel consumerModel = await NotificationServicesCall.PostConsumerDetailAsync(postConsumerDetailRequestModel);

            if (consumerModel is not null)
            {
               foreach (var customer in consumerModel.consumers)
               {
                   // sms service call
               }
            }
        }

    }


}