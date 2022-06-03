using bbt.framework.kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging;
using bbt.notification.worker;

var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
var builder = new ConfigurationBuilder();
var builder = new ConfigurationBuilder()
    .AddJsonFile($"appsettings.json", true, true)
    .AddJsonFile($"appsettings.{environment}.json", true, true)
    .AddEnvironmentVariables();




//only add secrets in development
#if DEBUG
builder.AddUserSecrets<Program>();
#endif




IConfigurationRoot configuration = builder.Build();


IHost host = Host.CreateDefaultBuilder(args)
.ConfigureServices(services =>
{
    // services.AddHostedService<ProducerWorker>();
    services.AddHostedService<Worker>();
    services.AddLogging(b =>
    {
        b.AddConsole();
        b.SetMinimumLevel(LogLevel.Information);
    }
    );

    services.Configure<KafkaSettings>(configuration.GetSection(nameof(KafkaSettings)));

})
.Build();

await host.RunAsync();
