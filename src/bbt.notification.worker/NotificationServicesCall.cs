using bbt.notification.worker.Enum;
using bbt.notification.worker.Helper;
using bbt.notification.worker.Models;
using Elastic.Apm.Api;
using Newtonsoft.Json;

namespace bbt.notification.worker
{
    public class NotificationServicesCall
    {
        private readonly ITracer _tracer;
        private readonly ILogHelper _logHelper;
        private readonly IConfiguration _configuration;
        public NotificationServicesCall(
        ITracer tracer, ILogHelper logHelper, IConfiguration configuration
      )
        {
            _tracer = tracer;
            _logHelper = logHelper;
            _configuration = configuration;
        }
        public async Task<TopicModel> GetTopicDetailsAsync()
        {
            var baseModel = new BaseModel();
            var topicModel = new TopicModel();

            await _tracer.CaptureTransaction("GetTopicDetailsAsync", ApiConstants.TypeRequest, async () =>
            {
                try
                {
                    var topicId = CommonHelper.GetWorkerTopicId(_configuration);
                    var path = baseModel.GetTopicDetailEndpoint().Replace("{id}", topicId);
                  
                    var response = await ApiHelper.ApiClient.GetAsync(path);

                    if (response.IsSuccessStatusCode)
                    {
                        topicModel = await response.Content.ReadAsAsync<TopicModel>();
                        return topicModel;
                    }
                    else if (response.StatusCode.ToString() == EnumHelper.GetDescription<StatusCodeEnum>(StatusCodeEnum.StatusCode460))
                    {
                        Console.WriteLine(topicId + " topic not found");
                        _logHelper.LogCreate(topicId, topicModel, "GetTopicDetailsAsync", StructStatusCode.StatusCode460.ToString());

                        return topicModel = null;
                    }

                    return topicModel;
                }
                catch (Exception e)
                {
                    _logHelper.LogCreate(null, topicModel, "GetTopicDetailsAsync", e.Message);
                    _tracer.CaptureException(e);
                    Console.WriteLine("GetTopicDetailsAsync" + e.Message);

                    return topicModel = null;
                }
            });

            return topicModel;
        }
     
        public async Task<ConsumerModel> PostConsumerDetailAsync(PostConsumerDetailRequestModel requestModel)
        {
            var baseModel = new BaseModel();
            var consumerModel = new ConsumerModel();

            await _tracer.CaptureTransaction("PostConsumerDetailAsync", ApiConstants.TypeRequest, async () =>
            {
                try
                {
                    var path = baseModel.GetConsumerDetailEndpoint();
                    var response = await ApiHelper.ApiClient.PostAsJsonAsync(path, requestModel);

                    if (response.IsSuccessStatusCode)
                    {
                        consumerModel = await response.Content.ReadAsAsync<ConsumerModel>();
                        Console.WriteLine("BAŞARILI => PostConsumerDetailAsync" + response.StatusCode + "=>" + response.RequestMessage);
                        _logHelper.LogCreate(requestModel, consumerModel, "PostConsumerDetailAsync", "BAŞARILI");
                        
                        return consumerModel;
                    }
                    else if (response.StatusCode.ToString() == EnumHelper.GetDescription<StatusCodeEnum>(StatusCodeEnum.StatusCode470))
                    {
                        Console.WriteLine("BAŞARISIZ => PostConsumerDetailAsync" + response.StatusCode + "=>" + response.RequestMessage);
                        _logHelper.LogCreate(requestModel, consumerModel, "PostConsumerDetailAsync", "470CODE-BAŞARISIZ");
                       
                        return consumerModel;
                    }

                    Console.WriteLine("BAŞARISIZ => PostConsumerDetailAsync" + response.StatusCode + "=>" + response.RequestMessage);
                    _logHelper.LogCreate(requestModel, consumerModel, "PostConsumerDetailAsync", response.StatusCode.ToString());
                    
                    return consumerModel = null;
                }
                catch (Exception e)
                {
                    _logHelper.LogCreate(requestModel, consumerModel, "PostConsumerDetailAsync", e.Message);
                    Console.WriteLine("CATCH = > " + JsonConvert.SerializeObject(requestModel));

                    return consumerModel = null;
                }
            });

            return consumerModel;
        }
    }
}