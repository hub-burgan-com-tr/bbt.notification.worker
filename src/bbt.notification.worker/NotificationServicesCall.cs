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
                var Topic_Id = Environment.GetEnvironmentVariable("Topic_Id") is null ? "10158" : Environment.GetEnvironmentVariable("Topic_Id");
                string path = baseModel.GetTopicDetailEndpoint().Replace("{id}", Topic_Id);
                Console.WriteLine(baseModel.GetTopicDetailEndpoint());
                Console.WriteLine("=>>" + path);
                HttpResponseMessage response = await ApiHelper.ApiClient.GetAsync(path);
                if (response.IsSuccessStatusCode)
                {
                    topicModel = await response.Content.ReadAsAsync<TopicModel>();
                }
                return topicModel;
            }
            catch (Exception e)
            {
                Console.WriteLine("GetTopicDetailsAsync" + e.Message);
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
                if (response.IsSuccessStatusCode)
                {
                    consumerModel = await response.Content.ReadAsAsync<ConsumerModel>();
                    Console.WriteLine("BAŞARILI => PostConsumerDetailAsync" + response.StatusCode + "=>" + response.RequestMessage);

                    return consumerModel;
                }
                else if ((int)response.StatusCode == 470)
                {
                    Console.WriteLine("BAŞARISIZ => PostConsumerDetailAsync" + response.StatusCode + "=>" + response.RequestMessage);
                    return consumerModel;
                }
                Console.WriteLine("BAŞARISIZ => PostConsumerDetailAsync" + response.StatusCode + "=>" + response.RequestMessage);

                return null;

            }
            catch (Exception e)
            {
                Console.WriteLine("CATCH = > " + JsonConvert.SerializeObject(requestModel));
                return null;
            }
        }
    }
}