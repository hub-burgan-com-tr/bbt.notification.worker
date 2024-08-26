using Confluent.Kafka;

namespace bbt.notification.worker.Models.Kafka
{
    public class NotificationKafkaSettings
    {
        public string BootstrapServers { get; set; }
        public string GroupId { get; set; }
        public string ClientId { get; set; }
        public IsolationLevel IsolationLevel { get; set; } = Confluent.Kafka.IsolationLevel.ReadCommitted;
        public string[] Topic { get; set; }
        public string SslCaLocation { get; set; }
        public SecurityProtocol SecurityProtocol { get; set; } = Confluent.Kafka.SecurityProtocol.Ssl;
        public AutoOffsetReset AutoOffsetReset { get; set; } = AutoOffsetReset.Earliest;
    }
}