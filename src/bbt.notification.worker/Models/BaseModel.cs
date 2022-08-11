namespace bbt.notification.worker.Models
{
    public class BaseModel
    {
        private readonly IConfiguration _config;


        public BaseModel()
        {

            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
            var builder = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true)
                .AddJsonFile($"appsettings.{environment}.json", true, true)
                .AddEnvironmentVariables();
            _config = builder.Build();
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

        public string GetSendEmailEndpoint()
        {
            return _config.GetSection("MessagingGateway:EndPoints:SendTemplatedMail").Value;
        }
    }
}