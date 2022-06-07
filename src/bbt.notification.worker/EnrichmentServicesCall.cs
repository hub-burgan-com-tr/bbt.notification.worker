using bbt.notification.worker.Models;
using Newtonsoft.Json;

namespace bbt.notification.worker
{
    public class EnrichmentServicesCall
    {

        public static async Task<EnrichmentServiceResponseModel> GetEnrichmentServiceAsync(string path, EnrichmentServiceRequestModel topicModel)
        {
            EnrichmentServiceResponseModel responseModel = new EnrichmentServiceResponseModel();

            try
            {
                HttpResponseMessage response = await ApiHelper.ApiClient.PostAsJsonAsync(path, topicModel);
                response.EnsureSuccessStatusCode();
                if (response.IsSuccessStatusCode)
                {
                    responseModel = await response.Content.ReadAsAsync<EnrichmentServiceResponseModel>();
                    Console.WriteLine("BAŞARILI => GetEnrichmentServiceAsync" + response.StatusCode + "=>" + response.RequestMessage);

                    return responseModel;
                }
                else
                {
                    Console.WriteLine("BAŞARISIZ => GetEnrichmentServiceAsync" + response.StatusCode + "=>" + response.RequestMessage);
                    return responseModel;
                }
            }
            catch (Exception e)
            {

                Console.WriteLine("CATCH => GetEnrichmentServiceAsync" + e.Message);
                return null;
            }
        }
    }
}