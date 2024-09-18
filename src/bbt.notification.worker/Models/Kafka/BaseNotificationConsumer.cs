using bbt.framework.kafka;
using bbt.notification.worker.Helper;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static Confluent.Kafka.ConfigPropertyNames;

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
                EnableAutoCommit = true,
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

                    foreach (string topic in kafkaSettings.Topic)
                        Console.WriteLine($"Subscribed to {topic}");

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
                                        try
                                        {
                                            IsSuccess = await Process(model);
                                        }
                                        catch (Exception ex)
                                        {
                                            logger.LogError(ex.ToString());
                                        }

                                        OnConsume?.Invoke(model);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                if (((KafkaException)ex).Error.Code == ErrorCode.UnknownTopicOrPart)
                                {
                                    await CreateTopicIfNotExist(kafkaSettings.Topic[0]);
                                }

                                logger.LogError("KAFKA_ERROR");
                                logger.LogError(message);
                                logger.LogError("ConsumeError: " + ex.ToString());
                                ProcessUnhealtyKafka();
                            }
                        }
                    });

                    consumer.Close();
                }
            }
            catch (Exception ex)
            {
                logger.LogError("KAFKA_ERROR");
                logger.LogError("SubscribeError: " + ex.ToString());
                ProcessUnhealtyKafka();
            }
        }

        private void ProcessUnhealtyKafka()
        {
            HealtCheckHelper.WriteUnhealthy();
            _logHelper.LogCreate(false, false, "BaseConsumeAsync", "KAFKA_ERROR");
        }

        public abstract Task<bool> Process(T model);


        private async Task CreateTopicIfNotExist(string topicName)
        {
            var config = GetConsumerConfig();

            using (var adminClient = new AdminClientBuilder(
                    new AdminClientConfig
                    {
                        BootstrapServers = config.BootstrapServers,
                        SecurityProtocol = config.SecurityProtocol,
                        SslCaLocation = config.SslCaLocation                        
                    }
                    ).Build()
                )
            {
                try
                {
                    await adminClient.CreateTopicsAsync(new TopicSpecification[] {
                    new TopicSpecification { Name = topicName, ReplicationFactor = 3, NumPartitions = 1 } });
                }
                catch (CreateTopicsException e)
                {
                    _logHelper.LogCreate(false, false, "CreateTopicIfNotExist", $"An error occured creating topic {e.Results[0].Topic}: {e.Results[0].Error.Reason}");
                }
            }
        }
    }
}