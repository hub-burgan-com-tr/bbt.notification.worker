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
            //int mahmut = Convert.ToInt32(clientId);
            await NotificationServicesCall.GetConsumerDetailAsync(topicModel.id,Convert.ToInt32(clientId)); 
        }

    }


}