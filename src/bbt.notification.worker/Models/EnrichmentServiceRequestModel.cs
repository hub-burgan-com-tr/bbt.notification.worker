namespace bbt.notification.worker.Models
{
    public class EnrichmentServiceRequestModel
    {
        public long customerId { get; set; }
        public string jsonData { get; set; }
    }
}