using bbt.notification.worker.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace bbt.notification.worker
{
    public class NotificationServicesCall
    {
        public static async Task<TopicModel> GetTopicDetailsAsync()
        {
            BaseModel baseModel = new BaseModel();
            TopicModel topicModel = new TopicModel();


            try
            {
                var Topic_Id = Environment.GetEnvironmentVariable("Topic_Id") is null ? "1" : Environment.GetEnvironmentVariable("Topic_Id");
                string path = baseModel.GetTopicDetailEndpoint().Replace("{id}", Topic_Id);

                Console.WriteLine(path);
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

        public static async Task<ConsumerModel> PostConsumerDetailAsync(PostConsumerDetailRequestModel requestModel)
        {
            BaseModel baseModel = new BaseModel();
            ConsumerModel consumerModel = new ConsumerModel();
            try
            {
                string path = baseModel.GetConsumerDetailEndpoint();
                HttpResponseMessage response = await ApiHelper.ApiClient.PostAsJsonAsync(path, requestModel);
                response.EnsureSuccessStatusCode();

                if (response.IsSuccessStatusCode)
                {
                    consumerModel = await response.Content.ReadAsAsync<ConsumerModel>();
                    Console.WriteLine(consumerModel.consumers[0].phone.number);
                    return consumerModel;
                }
                return consumerModel;
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}