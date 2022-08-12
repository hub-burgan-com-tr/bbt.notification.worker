using bbt.notification.worker.Helper;
using bbt.notification.worker.Models;
using Elastic.Apm;
using Elastic.Apm.Api;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace bbt.notification.worker
{
    public class NotificationServicesCall
    {
        private readonly ITracer _tracer;
        private readonly ILogHelper _logHelper;
        public NotificationServicesCall(
        ITracer tracer, ILogHelper logHelper
      )
        {
            _tracer = tracer;
            _logHelper = logHelper;
        }
        public async Task<TopicModel> GetTopicDetailsAsync()
        {
            BaseModel baseModel = new BaseModel();
            TopicModel topicModel = new TopicModel();


            await _tracer.CaptureTransaction("GetTopicDetailsAsync", ApiConstants.TypeRequest, async () =>
            {
                try
                {
                    var Topic_Id = Environment.GetEnvironmentVariable("Topic_Id") is null ? "1" : Environment.GetEnvironmentVariable("Topic_Id");
                    string path = baseModel.GetTopicDetailEndpoint().Replace("{id}", Topic_Id);
                    Console.WriteLine(baseModel.GetTopicDetailEndpoint());
                    Console.WriteLine("=>>" + path);
                    HttpResponseMessage response = await ApiHelper.ApiClient.GetAsync(path);
                    if (response.IsSuccessStatusCode)
                    {
                        topicModel = await response.Content.ReadAsAsync<TopicModel>();
                    }
                }
                catch (Exception e)
                {
                    _logHelper.LogCreate(null, topicModel, "GetTopicDetailsAsync", e.Message);
                    _tracer.CaptureException(e);
                    Console.WriteLine("GetTopicDetailsAsync" + e.Message);

                }
           });
            return topicModel;
        }
        //Static
        public async Task<ConsumerModel> PostConsumerDetailAsync(PostConsumerDetailRequestModel requestModel)
        {
            BaseModel baseModel = new BaseModel();
            ConsumerModel consumerModel = new ConsumerModel();
            await _tracer.CaptureTransaction("PostConsumerDetailAsync", ApiConstants.TypeRequest, async () =>
            {
                try
                {
                    string path = baseModel.GetConsumerDetailEndpoint();
                    HttpResponseMessage response = await ApiHelper.ApiClient.PostAsJsonAsync(path, requestModel);
                    if (response.IsSuccessStatusCode)
                    {
                        consumerModel = await response.Content.ReadAsAsync<ConsumerModel>();
                        Console.WriteLine("BAŞARILI => PostConsumerDetailAsync" + response.StatusCode + "=>" + response.RequestMessage);
                        _logHelper.LogCreate(requestModel,"SUCCESS"+ consumerModel, "PostConsumerDetailAsync","SUCCESS");
                        return consumerModel;
                    }
                    else if ((int)response.StatusCode == 470)
                    {
                        Console.WriteLine("BAŞARISIZ => PostConsumerDetailAsync" + response.StatusCode + "=>" + response.RequestMessage);
                        _logHelper.LogCreate(requestModel, "BAŞARISIZ" + consumerModel, "PostConsumerDetailAsync", "470CODE-BAŞARISIZ");
                        return consumerModel;
                    }
                    Console.WriteLine("BAŞARISIZ => PostConsumerDetailAsync" + response.StatusCode + "=>" + response.RequestMessage);
                    return null;
                }
                catch (Exception e)
                {
                    _logHelper.LogCreate(requestModel, consumerModel, "PostConsumerDetailAsync", e.Message);
                    Console.WriteLine("CATCH = > " + JsonConvert.SerializeObject(requestModel));
                    return null;
                }
            });
            return consumerModel;

        }
    }
}