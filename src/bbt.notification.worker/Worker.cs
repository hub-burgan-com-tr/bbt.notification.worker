using bbt.framework.kafka;
using bbt.notification.worker.Models;
using Microsoft.Extensions.Options;

namespace bbt.notification.worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> logger;
    private readonly KafkaSettings kafkaSettings;
    BaseModel baseModel = new BaseModel();
    public Worker(

    ILogger<Worker> _logger,
    IOptions<KafkaSettings> _options

    )
    {
        logger = _logger;
        kafkaSettings = _options.Value;
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
        ApiHelper.InitializeClient();
        TopicModel topicModel = await NotificationServicesCall.GetTopicDetailsAsync();
        kafkaSettings.Topic = topicModel.topic;
        kafkaSettings.BootstrapServers = topicModel.kafkaUrl;
        kafkaSettings.GroupId = topicModel.title_TR;
        kafkaSettings.SslCaLocation = baseModel.GetKafkaCertPath();
        var consumer = new TopicConsumer(kafkaSettings, stoppingToken, logger, topicModel);
        await consumer.ConsumeAsync();
    }
}
