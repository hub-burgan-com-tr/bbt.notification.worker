using bbt.framework.kafka;
using bbt.notification.worker.Helper;
using bbt.notification.worker.Models;
using Elastic.Apm;
using Elastic.Apm.Api;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace bbt.notification.worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> logger;
    private readonly KafkaSettings kafkaSettings;
    private readonly ITracer tracer;
    BaseModel baseModel = new BaseModel();
    private readonly ILogHelper logHelper;
    public Worker(

    ILogger<Worker> _logger,
    IOptions<KafkaSettings> _options,
    ITracer _tracer,ILogHelper _logHelper
    )
    {
        logger = _logger;
        kafkaSettings = _options.Value;
        tracer = _tracer;
        logHelper = _logHelper;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        await tracer.CaptureTransaction("ExecuteAsync", ApiConstants.TypeRequest, async () =>
        {
            try
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                ApiHelper.InitializeClient();
                NotificationServicesCall serviceCall = new NotificationServicesCall(tracer,logHelper);
                TopicModel topicModel = await serviceCall.GetTopicDetailsAsync();
                if (topicModel != null)
                {
                    kafkaSettings.Topic = new string[] { topicModel.topic };
                    kafkaSettings.BootstrapServers = topicModel.kafkaUrl;
                    kafkaSettings.GroupId =  topicModel.title_TR;
                   // kafkaSettings.GroupId = "test13";
                  //  kafkaSettings.SslCaLocation = baseModel.GetKafkaCertPath();
                    kafkaSettings.SslCaLocation = topicModel.kafkaCertificate;
                    var consumer = new TopicConsumer(kafkaSettings, stoppingToken, tracer, logger, topicModel,logHelper);
           
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
