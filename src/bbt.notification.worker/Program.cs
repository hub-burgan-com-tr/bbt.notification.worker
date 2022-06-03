using bbt.framework.kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging;
using bbt.notification.worker;
using Elastic.Apm.NetCoreAll;
using Elastic.Apm;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
using bbt.notification.worker.Helper;

var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
var builder = new ConfigurationBuilder()
    .AddJsonFile($"appsettings.json", true, true)
    .AddJsonFile($"appsettings.{environment}.json", true, true)
    .AddEnvironmentVariables();

//only add secrets in development
#if DEBUG
builder.AddUserSecrets<Program>();
#endif




IConfigurationRoot configuration = builder.Build();
Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
IHost host = Host.CreateDefaultBuilder(args)
      .ConfigureHostConfiguration(builder =>
      {
          builder.SetBasePath(Directory.GetCurrentDirectory());
          builder.AddCommandLine(args);
      })
.ConfigureServices(services =>
{
    // services.AddHostedService<ProducerWorker>();
    services.AddHostedService<Worker>();
    services.AddDbContext<DatabaseContext>();
    services.AddSingleton<ILogHelper, LogHelper>();
    services.AddLogging(b =>
    {
        b.AddConsole();
        b.SetMinimumLevel(LogLevel.Information);
    }
    );

    services.Configure<KafkaSettings>(configuration.GetSection(nameof(KafkaSettings)));
   services.AddSingleton(n => Agent.Tracer);



}).UseAllElasticApm()
.Build();
await host.RunAsync();
