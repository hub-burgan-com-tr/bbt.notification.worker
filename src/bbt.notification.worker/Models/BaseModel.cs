namespace bbt.notification.worker.Models
{
    public class BaseModel
    {
        private readonly IConfiguration _config;
        public BaseModel()
        {
            _config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json").Build();
        }


        public string GetTopicDetailEndpoint()
        {
            return _config.GetSection("NotificationServices:EndPoints:GetTopicDetail").Value;
        }
        public string GetConsumerDetailEndpoint()
        {
            return _config.GetSection("NotificationServices:EndPoints:GetConsumerDetail").Value;
        }
    }
}