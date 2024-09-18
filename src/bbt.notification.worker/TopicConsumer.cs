using bbt.notification.worker.Enum;
using bbt.notification.worker.Helper;
using bbt.notification.worker.Models;
using bbt.notification.worker.Models.Kafka;
using Elastic.Apm.Api;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace bbt.notification.worker
{
    public class TopicConsumer : BaseNotificationConsumer<string>
    {
        BaseModel baseModel = new BaseModel();
        private TopicModel _topicModel;
        private readonly ITracer _tracer;
        private readonly ILogHelper _logHelper;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        long _clientId = 0;

        public TopicConsumer(
        NotificationKafkaSettings _kafkaSettings,
        CancellationToken _cancellationToken,
        ITracer tracer,
        ILogger logger, TopicModel topicModel, ILogHelper logHelper, IConfiguration configuration) : base(_kafkaSettings, _cancellationToken, logger, logHelper)
        {
            _tracer = tracer;
            _topicModel = topicModel;
            _logHelper = logHelper;
            _configuration = configuration;
            _logger = logger;
        }
        public override async Task<bool> Process(string model)
        {
            var retVal = true;

            await _tracer.CaptureTransaction("Process", ApiConstants.TypeRequest, async () =>
            {
                try
                {
                    if (model.Contains("cdctablenotificationreminderdbosources"))
                    {
                        await _tracer.CurrentTransaction.CaptureSpan("ProcessSourceUpdates", ApiConstants.TypeRequest, async () =>
                         {
                             retVal = await ProcessSourceUpdates(model);
                         });
                    }
                    else
                    {
                        await _tracer.CurrentTransaction.CaptureSpan("ProcessNotifications", ApiConstants.TypeRequest, async () =>
                        {
                            retVal = await ProcessNotifications(model);
                        });
                    }
                }
                catch (Exception e)
                {
                    _logHelper.LogCreate(model, false, "Process", e.Message);
                    _tracer.CaptureException(e);
                    retVal = true;
                }
            });

            return retVal;
        }

        private async Task<bool> ProcessSourceUpdates(string model)
        {
            try
            {
                var obj = JObject.Parse(model);

                if (obj.SelectToken("message.data.Id").ToString().Trim() == CommonHelper.GetWorkerTopicId(_configuration).Trim()
                    && obj.SelectToken("message.headers.operation").ToString().Trim() == "UPDATE")
                {

                    var serviceCall = new NotificationServicesCall(_tracer, _logHelper, _configuration);
                    _topicModel = await serviceCall.GetTopicDetailsAsync();

                    if (_topicModel == null || string.IsNullOrWhiteSpace(_topicModel.topic))
                    {
                        HealtCheckHelper.WriteUnhealthy();
                        _logger.LogError("SOURCE_TOPIC_ERROR");
                        _logHelper.LogCreate(false, false, "ProcessSourceUpdates", "SOURCE_TOPIC_ERROR");
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                _logHelper.LogCreate(model, false, "ProcessSourceUpdates", e.Message);
                _tracer.CaptureException(e);
                return true;
            }
        }

        private async Task<bool> ProcessNotifications(string model)
        {
            try
            {
                _clientId = await GetClientId(model);

                if (_clientId == 0)
                {
                    _logHelper.LogCreate(model, false, "ProcessNotifications", "CustomerNo is not found");
                    return true;
                }

                var obj = JObject.Parse(model);

                if (_topicModel.messageDataFieldType == (int)MessageDataFieldType.String)
                {
                    model = GetReplacedJsonString(model);
                }

                var postConsumerDetailRequestModel = new PostConsumerDetailRequestModel
                {
                    client = _clientId,
                    sourceId = Convert.ToInt32(CommonHelper.GetWorkerTopicId(_configuration)),
                    jsonData = GetJsonValue(obj, _topicModel.messageDataJsonPath).Replace(Environment.NewLine, string.Empty)
                };

                await SetServiceUrlData(postConsumerDetailRequestModel, obj);

                var notificationServicesCall = new NotificationServicesCall(_tracer, _logHelper, _configuration);
                var consumerModel = await notificationServicesCall.PostConsumerDetailAsync(postConsumerDetailRequestModel);

                if (!HasConsumer(consumerModel))
                {
                    _logHelper.LogCreate(consumerModel, false, "ProcessNotifications", "Consumer null Client:" + _clientId.ToString());
                    return true;
                }

                if (ShouldSendNotification(consumerModel, NotificationTypeEnum.SMS))
                {
                    await SendSms(obj, consumerModel, postConsumerDetailRequestModel);
                }

                if (ShouldSendNotification(consumerModel, NotificationTypeEnum.EMAIL))
                {
                    await SendEmail(obj, consumerModel, postConsumerDetailRequestModel);
                }

                if (ShouldSendNotification(consumerModel, NotificationTypeEnum.PUSHNOTIFICATION))
                {
                    await SendPush(obj, consumerModel, postConsumerDetailRequestModel);
                }

                return true;
            }
            catch (Exception e)
            {
                _logHelper.LogCreate(model, false, "ProcessNotifications", e.Message);
                _tracer.CaptureException(e);
                return true;
            }
        }

        private async Task<long> GetClientId(string model)
        {
            if (_topicModel.messageDataFieldType == (int)MessageDataFieldType.String && _topicModel.clientIdJsonPath.StartsWith(_topicModel.messageDataJsonPath))
            {
                model = GetReplacedJsonString(model);
            }

            var obj = JObject.Parse(model);
            var strClientId = GetJsonValue(obj, _topicModel.clientIdJsonPath);

            if (strClientId.Length == 10 || strClientId.Length == 11) // Citizenship or Tax Number
            {
                var customerApiCall = new CustomerApiCall(_tracer, _logHelper, _configuration);

                return await customerApiCall.GetCustomer(strClientId);
            }
            else
            {
                try { return Convert.ToInt64(strClientId); } catch { return 0; }
            }
        }

        private async Task SetServiceUrlData(PostConsumerDetailRequestModel postConsumerDetailRequestModel, JObject obj)
        {
            if (_topicModel.ServiceUrlList.Count <= 0)
                return;

            foreach (var item in _topicModel.ServiceUrlList)
            {
                var enrichmentServiceRequestModel = new EnrichmentServiceRequestModel
                {
                    customerId = _clientId,
                    dataModel = GetJsonValue(obj, _topicModel.messageDataJsonPath).Replace(Environment.NewLine, string.Empty)
                };

                var enrichmentServicesCall = new EnrichmentServicesCall(_tracer, _logHelper);

                var enrichmentServiceResponseModel = await enrichmentServicesCall.GetEnrichmentServiceAsync(item.ServiceUrl, enrichmentServiceRequestModel);

                if (enrichmentServiceResponseModel != null && !string.IsNullOrEmpty(enrichmentServiceResponseModel.dataModel))
                {
                    postConsumerDetailRequestModel.jsonData = enrichmentServiceResponseModel.dataModel;
                }
            }
        }

        private bool ShouldSendNotification(ConsumerModel consumerModel, NotificationTypeEnum notificationType)
        {
            var alwaysSendTypeList = ((AlwaysSendType)_topicModel.alwaysSendType).ToEnumArray();

            switch (notificationType)
            {
                case NotificationTypeEnum.SMS:
                    if (HasValidServiceReference(_topicModel.smsServiceReference) &&
                         (consumerModel.consumers[0].isSmsEnabled || alwaysSendTypeList.Contains(AlwaysSendType.Sms)))
                    {
                        return true;
                    }
                    break;
                case NotificationTypeEnum.EMAIL:
                    if (HasValidServiceReference(_topicModel.emailServiceReference) &&
                        (consumerModel.consumers[0].isEmailEnabled || alwaysSendTypeList.Contains(AlwaysSendType.EMail)))
                    {
                        return true;
                    }
                    break;
                case NotificationTypeEnum.PUSHNOTIFICATION:
                    if (HasValidServiceReference(_topicModel.pushServiceReference) &&
                        (consumerModel.consumers[0].isPushEnabled || alwaysSendTypeList.Contains(AlwaysSendType.Push)))
                    {
                        return true;
                    }
                    break;
            }

            return false;
        }

        private async Task<bool> SendSms(JObject obj, ConsumerModel consumerModel, PostConsumerDetailRequestModel postConsumerDetailRequestModel)
        {
            try
            {
                if (IsNoticationExpired(obj, "SendSms"))
                    return false;

                var dengageRequestModel = new DengageRequestModel();
                dengageRequestModel.phone.countryCode = consumerModel.consumers[0].phone.countryCode;
                dengageRequestModel.phone.prefix = consumerModel.consumers[0].phone.prefix;
                dengageRequestModel.phone.number = consumerModel.consumers[0].phone.number;
                dengageRequestModel.CustomerNo = consumerModel.consumers[0].client;
                dengageRequestModel.template = _topicModel.smsServiceReference;
                dengageRequestModel.templateParams = postConsumerDetailRequestModel.jsonData;
                dengageRequestModel.process.name = string.IsNullOrEmpty(_topicModel.processName) ? _topicModel.topic : _topicModel.processName;
                dengageRequestModel.process.ItemId = GetProcessItemId(obj, _topicModel.processItemId);
                dengageRequestModel.process.Action = "Notification";
                dengageRequestModel.process.Identity = "1";

                if (dengageRequestModel.phone.number == 0)
                {
                    _logHelper.LogCreate(dengageRequestModel.CustomerNo, false, "SendSms", "PhoneNumber is empty");
                    return false;
                }

                var notificationResponse = await ApiHelper.ApiClient.PostAsJsonAsync(baseModel.GetSendSmsEndpoint(), dengageRequestModel);

                await MessageNotificationLogCreate(
                                                    notificationResponse,
                                                    postConsumerDetailRequestModel,
                                                    consumerModel.consumers[0].phone.prefix + "" + consumerModel.consumers[0].phone.number,
                                                    "",
                                                    dengageRequestModel,
                                                    NotificationTypeEnum.SMS,
                                                    dengageRequestModel.template,
                                                    consumerModel.consumers[0].isStaff
                                                  );
                return true;
            }
            catch (Exception ex)
            {
                _logHelper.LogCreate(consumerModel, false, "SendSms", ex.Message);
                _tracer.CaptureException(ex);
                return false;
            }
        }
        private async Task<bool> SendEmail(JObject obj, ConsumerModel consumerModel, PostConsumerDetailRequestModel postConsumerDetailRequestModel)
        {
            try
            {
                if (IsNoticationExpired(obj, "SendEmail"))
                    return false;

                var emailRequestModel = new EmailRequestModel
                {
                    CustomerNo = consumerModel.consumers[0].client,
                    Email = consumerModel.consumers[0].email,
                    TemplateParams = postConsumerDetailRequestModel.jsonData,
                    Template = _topicModel.emailServiceReference,
                    Process = new DengageRequestModel.Process
                    {
                        name = string.IsNullOrEmpty(_topicModel.processName) ? _topicModel.topic : _topicModel.processName,
                        ItemId = GetProcessItemId(obj, _topicModel.processItemId),
                        Action = "Notification",
                        Identity = "1"
                    }
                };

                if (string.IsNullOrEmpty(emailRequestModel.Email))
                {
                    _logHelper.LogCreate(emailRequestModel.CustomerNo, false, "SendEmail", " Email address is null");
                    return false;
                }

                var notificationResponse = await ApiHelper.ApiClient.PostAsJsonAsync(baseModel.GetSendEmailEndpoint(), emailRequestModel);

                await MessageNotificationLogCreate(
                                                    notificationResponse,
                                                    postConsumerDetailRequestModel,
                                                    "",
                                                    consumerModel.consumers[0].email,
                                                    emailRequestModel,
                                                    NotificationTypeEnum.EMAIL,
                                                    emailRequestModel.Template,
                                                    consumerModel.consumers[0].isStaff
                                                  );
                return true;
            }
            catch (Exception ex)
            {
                _logHelper.LogCreate(consumerModel, false, "SendEmail", ex.Message);
                _tracer.CaptureException(ex);
                return false;
            }
        }

        private async Task<bool> SendPush(JObject obj, ConsumerModel consumerModel, PostConsumerDetailRequestModel postConsumerDetailRequestModel)
        {
            try
            {
                if (IsNoticationExpired(obj, "SendPush"))
                    return false;

                var pushNotificationRequestModel = new PushNotificaitonRequestModel
                {
                    CustomerNo = consumerModel.consumers[0].client.ToString(),
                    TemplateParams = postConsumerDetailRequestModel.jsonData,
                    Template = _topicModel.pushServiceReference,
                    SaveInbox = _topicModel.saveInbox,
                    NotificationType = _topicModel.productCodeName,

                    Process = new DengageRequestModel.Process
                    {
                        name = string.IsNullOrEmpty(_topicModel.processName) ? _topicModel.topic : _topicModel.processName,
                        ItemId = GetProcessItemId(obj, _topicModel.processItemId),
                        Action = "Notification",
                        Identity = "1"
                    }
                };

                var notificationResponse = await ApiHelper.ApiClient.PostAsJsonAsync(baseModel.GetSendPushnotificationEndpoint(), pushNotificationRequestModel);

                await MessageNotificationLogCreate(
                                                    notificationResponse,
                                                    postConsumerDetailRequestModel,
                                                    consumerModel.consumers[0].phone.prefix + "" + consumerModel.consumers[0].phone.number,
                                                    "",
                                                    JsonConvert.SerializeObject(pushNotificationRequestModel, Formatting.Indented),
                                                    NotificationTypeEnum.PUSHNOTIFICATION,
                                                    pushNotificationRequestModel.Template,
                                                    consumerModel.consumers[0].isStaff
                                                  );
                return true;
            }
            catch (Exception ex)
            {
                _logHelper.LogCreate(consumerModel, false, "SendPushNotification", ex.Message);
                _tracer.CaptureException(ex);
                return false;
            }
        }
        private static string GetJsonValue(JObject obj, string path)
        {
            var retVal = "";

            var token = obj.SelectToken(path);

            if (token != null)
            {
                retVal = token.ToString();
            }

            return retVal;
        }

        private static string GetProcessItemId(JObject obj, string processItemIdPath)
        {
            if (string.IsNullOrWhiteSpace(processItemIdPath))
                return "1";

            var tmp = processItemIdPath.Split(';');
            var sb = new StringBuilder();

            foreach (var item in tmp)
            {
                if (string.IsNullOrWhiteSpace(item))
                    continue;

                var tmpProcessItemId = GetJsonValue(obj, item);

                if (!string.IsNullOrWhiteSpace(tmpProcessItemId))
                    sb.Append(tmpProcessItemId + "-");
            }

            return sb.ToString().TrimEnd('-');
        }

        private static bool HasConsumer(ConsumerModel consumerModel)
        {
            return consumerModel != null && consumerModel.consumers != null && consumerModel.consumers.Count() > 0;
        }
        private static bool HasValidServiceReference(string serviceReference)
        {
            return !string.IsNullOrEmpty(serviceReference) && serviceReference != "string";
        }

        private bool IsNoticationExpired(JObject obj, string methodName)
        {
            var kafkaDataTime = DateTime.Today;

            if (!string.IsNullOrEmpty(obj.SelectToken("message.headers.timestamp").ToString()))
            {
                kafkaDataTime = Convert.ToDateTime(obj.SelectToken("message.headers.timestamp"));
            }

            if (_topicModel.RetentationTime != 0 && kafkaDataTime < DateTime.Now.AddMinutes(-_topicModel.RetentationTime))
            {
                _logHelper.LogCreate(_clientId, false, methodName, "Kafka data is expired. kafkaDataTime: " + kafkaDataTime.ToString());
                return true;
            }

            return false;
        }

        private async Task MessageNotificationLogCreate
                                        (
                                            HttpResponseMessage notificationResponse,
                                            PostConsumerDetailRequestModel postConsumerDetailRequestModel,
                                            string phoneNumber,
                                            string email,
                                            object requestModel,
                                            NotificationTypeEnum notificationType,
                                            string templateName,
                                            bool isStaff
                                        )
        {
            object responseModel;
            string responseMessage;

            if (notificationResponse.IsSuccessStatusCode)
            {
                var sendMessageResponseModel = await notificationResponse.Content.ReadAsAsync<SendMessageResponseModel>();

                responseModel = sendMessageResponseModel;
                responseMessage = notificationResponse.StatusCode.ToString();
            }
            else
            {
                var errorResponseModel = new SendErrorResponseModel
                {
                    StatusCode = ((int)notificationResponse.StatusCode).ToString(),
                    ReasonPhrase = notificationResponse.ReasonPhrase ?? ""
                };

                var contentResult = await notificationResponse.Content.ReadAsStringAsync();
                errorResponseModel.Message = contentResult;

                responseModel = errorResponseModel;
                responseMessage = errorResponseModel.Message;
            }

            _logHelper.MessageNotificationLogCreate
                (
                    postConsumerDetailRequestModel.client,
                    postConsumerDetailRequestModel.sourceId,
                    phoneNumber,
                    email,
                    requestModel,
                    responseModel,
                    notificationType.GetHashCode(),
                    responseMessage,
                    templateName,
                    isStaff
                );
        }

        private static string GetReplacedJsonString(string str)
        {
            return str.Replace("\\\"", "\"").Replace("\"{", "{").Replace("}\"", "}");
        }

    }
}