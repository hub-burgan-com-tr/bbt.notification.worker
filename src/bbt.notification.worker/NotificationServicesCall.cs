using bbt.notification.worker.Models;
using Microsoft.Extensions.Caching.Memory;

namespace bbt.notification.worker
{
    public class NotificationServicesCall
    {

        public static async Task<TopicModel> GetTopicDetailsAsync()
        {

            try
            {

                var Topic_Id = Environment.GetEnvironmentVariable("Topic_Id");
                string path = $"https://localhost:7249/sources/id/1";

                TopicModel topicModel = new TopicModel();
                HttpResponseMessage response = await ApiHelper.ApiClient.GetAsync(path);
                if (response.IsSuccessStatusCode)
                {
                    topicModel = await response.Content.ReadAsAsync<TopicModel>();
                }
                return topicModel;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }

        }

        public static async Task<ConsumerModel> GetConsumerDetailAsync(dynamic topicId, int clientId)
        {
            try
            {
                ConsumerModel consumerModel = new ConsumerModel();
                string path = $"https://localhost:7249/sources/" + topicId + "/consumers-by-client/" + clientId;
                HttpResponseMessage response = await ApiHelper.ApiClient.GetAsync(path);
                Console.WriteLine(path);
                if (response.IsSuccessStatusCode)
                {
                    consumerModel = await response.Content.ReadAsAsync<ConsumerModel>();
                    if (consumerModel.consumers.Count()!=0)
                    {
                            // TODO
                    }
                }
                return consumerModel;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }
    }
}