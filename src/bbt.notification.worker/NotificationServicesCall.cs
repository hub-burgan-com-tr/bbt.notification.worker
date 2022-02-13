using bbt.notification.worker.Models;

namespace bbt.notification.worker
{
    public class NotificationServicesCall
    {

        public static async Task<TopicModel> GetTopicDetailsAsync()
        {
            var Topic_Id = Environment.GetEnvironmentVariable("Topic_Id");
            string path=$"https://localhost:7249/sources/id/"+Topic_Id;
            TopicModel topicModel = new TopicModel();

            HttpResponseMessage response = await ApiHelper.ApiClient.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                 topicModel = await response.Content.ReadAsAsync<TopicModel>();
            }
            return topicModel;
        }
        

    }
}