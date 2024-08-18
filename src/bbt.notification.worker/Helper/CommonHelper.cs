using bbt.notification.worker.Models;

namespace bbt.notification.worker.Helper
{
    public static class CommonHelper
    {
        public static string? GetWorkerTopicId(IConfiguration configuration)
        {
            return Environment.GetEnvironmentVariable("Topic_Id") is null
                 ? (configuration.GetSection("TopicId").Value)
                 : Environment.GetEnvironmentVariable("Topic_Id");
        }
    }
}