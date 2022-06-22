using bbt.notification.worker.Helper;
using bbt.notification.worker.Models;
using Elastic.Apm.Api;
using Newtonsoft.Json;
using System.Reflection;

namespace bbt.notification.worker
{
    public class EnrichmentServicesCall
    {
        private readonly ITracer _tracer;
        private readonly ILogHelper _logHelper;
        public EnrichmentServicesCall(
        ITracer tracer, ILogHelper logHelper
      )
        {
            _tracer = tracer;
            _logHelper = logHelper;
        }
        //Static olduğu için eklenmiyor.
        public async Task<EnrichmentServiceResponseModel> GetEnrichmentServiceAsync(string path, EnrichmentServiceRequestModel topicModel)
        {
            EnrichmentServiceResponseModel responseModel = new EnrichmentServiceResponseModel();
            await _tracer.CaptureTransaction("GetEnrichmentServiceAsync", ApiConstants.TypeRequest, async () =>
            {
                try
                {
                    HttpResponseMessage response = await ApiHelper.ApiClient.PostAsJsonAsync(path, topicModel);
                    response.EnsureSuccessStatusCode();
                    if (response.IsSuccessStatusCode)
                    {
                        responseModel = await response.Content.ReadAsAsync<EnrichmentServiceResponseModel>();
                        return responseModel;
                    }
                    else
                    {
                        Console.WriteLine("TRY => GetEnrichmentServiceAsync" + response.StatusCode + "=>" + response.RequestMessage);
                        return responseModel;
                    }
                }
                catch (Exception e)
                {
                    var req = new
                    {
                        path = path,
                        topicModel = topicModel
                    };
                    _logHelper.LogCreate(req, responseModel, MethodBase.GetCurrentMethod().Name, e.Message);
                    Console.WriteLine("CATCH => GetEnrichmentServiceAsync" + e.Message);
                    return null;
                }
            });
            return responseModel;
        }
    }
}