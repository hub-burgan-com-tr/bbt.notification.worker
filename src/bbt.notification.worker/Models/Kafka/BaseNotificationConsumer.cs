using bbt.framework.kafka;
using bbt.notification.worker.Helper;
using Confluent.Kafka;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace bbt.notification.worker.Models.Kafka
{
    public abstract class BaseNotificationConsumer<T>
    {
        public Action<T>? OnConsume { get; set; }

        NotificationKafkaSettings kafkaSettings;
        CancellationToken cancellationToken;

        ILogger logger;
        JsonSerializerSettings jsonSettings = null;

        private readonly ILogHelper _logHelper;

        public BaseNotificationConsumer
            (NotificationKafkaSettings _kafkaSettings, 
            CancellationToken _cancellationToken, 
            ILogger _logger,
            ILogHelper logHelper
            )
        {
            kafkaSettings = _kafkaSettings;
            cancellationToken = _cancellationToken;
            logger = _logger;
            _logHelper = logHelper;

            jsonSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
        }

        ConsumerConfig GetConsumerConfig()
        {
            return new ConsumerConfig
            {
                BootstrapServers = kafkaSettings.BootstrapServers,
                GroupId = kafkaSettings.GroupId,
                SecurityProtocol = kafkaSettings.SecurityProtocol,
                SslCaLocation = kafkaSettings.SslCaLocation,
                AutoOffsetReset = kafkaSettings.AutoOffsetReset,
                EnableAutoCommit = false,
                IsolationLevel = kafkaSettings.IsolationLevel,
                ClientId = kafkaSettings.ClientId
            };
        }

        public async Task ConsumeAsync()
        {
            HealtCheckHelper.WriteHealthy();

            try
            {
                using (var consumer = new ConsumerBuilder<Ignore, string>(GetConsumerConfig())
                       .Build())
                {
                    consumer.Subscribe(kafkaSettings.Topic);

                    Console.WriteLine($"Subscribed to {kafkaSettings.Topic}");

                    await Task.Run(async () =>
                    {
                        bool taskIsRunning = true;
                        cancellationToken.Register(() =>
                        {
                            taskIsRunning = false;
                        });

                        while (taskIsRunning)
                        {
                            string message = string.Empty;

                            try
                            {
                                var consumeResult = consumer.Consume(cancellationToken);
                                

                                if (consumeResult.Message is Message<Ignore, string> result)
                                {

                                    bool IsIgnore = false;
                                    bool IsSuccess = true;

                                    T model;
                                    if (typeof(T).Equals(typeof(string)))
                                    {
                                        model = (T)Convert.ChangeType(result.Value, typeof(T));
                                    }
                                    else
                                    {
                                        message = result.Value;
                                        model = JsonConvert.DeserializeObject<T>(result.Value, jsonSettings);

                                        if (model.GetType().IsGenericType && typeof(KafkaModel<>) == model.GetType().GetGenericTypeDefinition())
                                        {

                                            JObject jsonObject = JObject.Parse(result.Value);

                                            JToken data = jsonObject.SelectToken("message.data");
                                            JToken beforeData = jsonObject.SelectToken("message.beforeData");

                                            if (data.Equals(beforeData))
                                            {
                                                IsIgnore = true;
                                            }
                                        }
                                    }

                                    if (!IsIgnore)
                                    {
                                        IsSuccess = await Process(model);
                                        OnConsume?.Invoke(model);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(message);
                                logger.LogError(ex.ToString());
                                ProcessUnhealtyKafka();
                            }
                        }
                    });

                    consumer.Close();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
                ProcessUnhealtyKafka();
            }
        }

        private void ProcessUnhealtyKafka()
        {
            HealtCheckHelper.WriteUnhealthy();
            _logHelper.LogCreate(false, false, "BaseConsumeAsync", "KAFKA ERROR");
        }

        public abstract Task<bool> Process(T model);
    }
}