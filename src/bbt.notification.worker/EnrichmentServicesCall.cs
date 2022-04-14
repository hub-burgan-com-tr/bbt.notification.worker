using bbt.notification.worker.Models;
using Newtonsoft.Json;

namespace bbt.notification.worker
{
    public class EnrichmentServicesCall
    {

        public static async Task<EnrichmentServiceResponseModel> GetEnrichmentServiceAsync(string path, EnrichmentServiceRequestModel topicModel)
        {
            try
            {
                HttpResponseMessage response = await ApiHelper.ApiClient.PostAsJsonAsync(path, topicModel);
                response.EnsureSuccessStatusCode();
                if (response.IsSuccessStatusCode)
                {
                    EnrichmentServiceResponseModel responseModel = await response.Content.ReadAsAsync<EnrichmentServiceResponseModel>();
                    return responseModel;
                }
            
                return null;

            }
            catch (Exception e)
            {

                Console.WriteLine("GetEnrichmentServiceAsync"+e.Message);

                return null;
            }
        }

    }
}