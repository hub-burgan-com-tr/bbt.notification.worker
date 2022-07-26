namespace bbt.notification.worker.Models
{
    public class TopicModel
    {
        public int id { get; set; }
        public string topic { get; set; }
        public string pushServiceReference { get; set; }
        public string smsServiceReference { get; set; }
        public string emailServiceReference { get; set; }
        public string title_TR { get; set; }
        public string title_EN { get; set; }
        public object parentId { get; set; }
        public int displayType { get; set; }
        public string apiKey { get; set; }
        public string secret { get; set; }
        public string clientIdJsonPath { get; set; }
        public string kafkaUrl { get; set; }

        public  string kafkaCertificate { get; set; }

        public List<SourceServices> ServiceUrlList { get; set; }

    }

    public class SourceServices
    {
        public int id { get; set; }
        public string ServiceUrl { get; set; }
    }

}
