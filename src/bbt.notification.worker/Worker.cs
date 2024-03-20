using bbt.framework.kafka;
using bbt.notification.worker.Helper;
using Elastic.Apm.Api;
using Microsoft.Extensions.Options;

namespace bbt.notification.worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> logger;
    private readonly KafkaSettings kafkaSettings;
    private readonly ITracer tracer;
    private readonly IConfiguration _configuration;
    private readonly ILogHelper logHelper;
    public Worker(

    ILogger<Worker> _logger,
    IOptions<KafkaSettings> _options,
    ITracer _tracer, ILogHelper _logHelper, IConfiguration configuration
    )
    {
        logger = _logger;
        kafkaSettings = _options.Value;
        tracer = _tracer;
        logHelper = _logHelper;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await tracer.CaptureTransaction("ExecuteAsync", ApiConstants.TypeRequest, async () =>
        {
            try
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                ApiHelper.InitializeClient();

                var serviceCall = new NotificationServicesCall(tracer, logHelper, _configuration);
                var topicModel = await serviceCall.GetTopicDetailsAsync();

                if (topicModel != null)
                {
                    kafkaSettings.Topic = new string[] { topicModel.topic, "ENT.ReminderSources" };
                    kafkaSettings.BootstrapServers = topicModel.kafkaUrl;
                    kafkaSettings.GroupId = topicModel.title_TR;
                    kafkaSettings.SslCaLocation = topicModel.kafkaCertificate;
                    var consumer = new TopicConsumer(kafkaSettings, stoppingToken, tracer, logger, topicModel, logHelper, _configuration);

                    await consumer.ConsumeAsync();
                }
            }
            catch (Exception e)
            {
                logHelper.LogCreate(stoppingToken, kafkaSettings, "ExecuteAsync", e.Message);
                tracer.CaptureException(e);
            }
        });
    }
}