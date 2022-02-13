using bbt.notification.worker.Models;
namespace bbt.notification.worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            ApiHelper.InitializeClient();
            var topicModel=NotificationServicesCall.GetTopicDetailsAsync();
            await Task.Delay(1000, stoppingToken);
        }
    }
}
