namespace bbt.notification.worker.Models
{
    public class TopicModel
    {
        public int id { get; set; }
        public string topic { get; set; }
        public string pushServiceReference { get; set; }
        public string smsServiceReference { get; set; }
        public string emailServiceReference { get; set; }
    }
}