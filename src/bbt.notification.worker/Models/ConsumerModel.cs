namespace bbt.notification.worker.Models
{
    public class ConsumerModel
    {
        public List<Consumer> consumers { get; set; }
    }

    public class Consumer
    {
        public string id { get; set; }
        public int client { get; set; }
        public int user { get; set; }
        public string filter { get; set; }
        public bool isPushEnabled { get; set; }
        public object deviceKey { get; set; }
        public bool isSmsEnabled { get; set; }
        public Phone phone { get; set; }
        public bool isEmailEnabled { get; set; }
        public string email { get; set; }
        public bool isStaff { get; set; }
    }

        public class Phone
    {
        public int countryCode { get; set; }
        public int prefix { get; set; }
        public int number { get; set; }
    }
}