using bbt.notification.worker.Helper;
using bbt.notification.worker.Models;
using Elastic.Apm.Api;

namespace bbt.notification.worker
{
    public class CustomerApiCall
    {
        private readonly ITracer _tracer;
        private readonly ILogHelper _logHelper;
        private readonly IConfiguration _configuration;
        public CustomerApiCall(
        ITracer tracer, ILogHelper logHelper, IConfiguration configuration
      )
        {
            _tracer = tracer;
            _logHelper = logHelper;
            _configuration = configuration;
        }
        public async Task<long> GetCustomer(string customerSearchNumber)
        {
            long customerNo = 0;

            await _tracer.CaptureTransaction("GetCustomer", ApiConstants.TypeRequest, async () =>
            {
                try
                {
                    var customerSearchRequestModel = new CustomerSearchRequestModel();
                    customerSearchRequestModel.Name = customerSearchNumber;

                    var path = _configuration.GetSection("CustomerApiUrl").Value;

                    var response = await ApiHelper.ApiClient.PostAsJsonAsync(path, customerSearchRequestModel);

                    if (response.IsSuccessStatusCode)
                    {
                        var customerResponseModel = await response.Content.ReadAsAsync<CustomerResponseModel>();

                        if (customerResponseModel != null && customerResponseModel.CustomerList != null && customerResponseModel.CustomerList.Count > 0)
                        {
                            customerNo = customerResponseModel.CustomerList[0].CustomerNumber;
                        }
                        else
                        {
                            _logHelper.LogCreate(customerSearchNumber, false, "GetCustomer", customerResponseModel?.ReturnDescription ?? "GetCustomer Return Empty");
                        }
                    }
                    else
                    {
                        _logHelper.LogCreate(customerSearchNumber, false, "GetCustomer", "CustomerApi Call Error:");
                    }
                }
                catch (Exception e)
                {
                    _logHelper.LogCreate(customerSearchNumber, false, "GetCustomer", e.Message);
                    _tracer.CaptureException(e);
                }
            });

            return customerNo;
        }
    }
}