namespace bbt.notification.worker.Models
{
    public class BaseModel
    {
        private readonly IConfiguration _config;


        public BaseModel()
        {

            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{"Test"}.json", true, true)
            .AddJsonFile($"appsettings.{"Prod"}.json", true, true)
            .AddEnvironmentVariables();
            _config = builder.Build();
        }

        string? GetEnviroment()
        {
            return Environment.GetEnvironmentVariable("ENVIRONMENT");
        }
        public string GetTopicDetailEndpoint()
        {
            return _config.GetSection("NotificationServices:EndPoints:GetTopicDetail").Value;
        }
        public string GetConsumerDetailEndpoint()
        {
            return _config.GetSection("NotificationServices:EndPoints:GetConsumerDetail").Value;
        }
        public string GetKafkaCertPath()
        {
            return _config.GetSection("SslCaLocation").Value;
        }
        public string GetSendSmsEndpoint()
        {
            return _config.GetSection("MessagingGateway:EndPoints:SendSms").Value;
        }
    }
}