namespace bbt.notification.worker.Models
{
    public class PostConsumerDetailRequestModel
    {
        public int sourceId { get; set; }
        public long client { get; set; }
        public string jsonData { get; set; }

    }
}