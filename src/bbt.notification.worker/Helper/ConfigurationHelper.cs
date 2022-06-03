namespace bbt.notification.worker.Helper
{
    public class ConfigurationHelper
    {
        private readonly IConfiguration _config;
        public ConfigurationHelper()
        {
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{"Test"}.json", true, true)
            .AddEnvironmentVariables();
            _config = builder.Build();
        }
        public string GetReminderConnectionString()
        {
            return _config.GetSection("ConnectionStrings:ReminderConnectionString").Value;
        }
        public string GetCustomerProfileEndpoint()
        {
            return _config.GetSection("CustomerProfileEndpoint").Value;
        }
    }

}