using Elastic.Apm.Api;
using Newtonsoft.Json;

namespace bbt.notification.worker.Helper
{
    public class LogHelper : ILogHelper
    {
        private readonly ITracer _tracer;
        private readonly IConfiguration _configuration;
        public LogHelper(ITracer tracer, IConfiguration configuration)
        {
            _tracer = tracer;
            _configuration = configuration;
        }
        public bool LogCreate(object requestModel, object responseModel, string methodName, string errorMessage)
        {
            var span = _tracer.CurrentTransaction?.StartSpan("LogCreateSpan", "LogCreate");

            using (var db = new DatabaseContext())
            {
                try
                {
                    var topicId = CommonHelper.GetWorkerTopicId(_configuration);

                    db.Add(new Log
                    {
                        ServiceName = methodName,
                        ProjectName = System.Reflection.Assembly.GetEntryAssembly().GetName().Name + "." + topicId,
                        ErrorDate = DateTime.Now,
                        ErrorMessage = errorMessage,
                        RequestData = JsonConvert.SerializeObject(requestModel),
                        ResponseData = JsonConvert.SerializeObject(responseModel)
                    });

                    db.SaveChanges();

                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("DB ERROR =>" + e.Message);
                    span?.CaptureException(e);
                    return false;
                }
            }
        }

        public bool MessageNotificationLogCreate(long CustomerNo, int SourceId, string PhoneNumber, string Email, object requestModel, object responseModel, int NotificationType, string ResponseMessage, string Content, bool isStaff)
        {
            var span = _tracer.CurrentTransaction?.StartSpan("MessageNotificationLogCreateSpan", "MessageNotificationLogCreate");
            using (var db = new DatabaseContext())
            {
                try
                {
                    db.Add(new MessageNotificationLog
                    {
                        CustomerNo = CustomerNo,
                        SourceId = SourceId,
                        PhoneNumber = PhoneNumber,
                        Email = Email,
                        ResponseMessage = ResponseMessage,
                        CreateDate = DateTime.Now,
                        NotificationType = NotificationType,
                        RequestData = JsonConvert.SerializeObject(requestModel),
                        ResponseData = JsonConvert.SerializeObject(responseModel),
                        IsStaff = isStaff,
                        Content = Content
                    });

                    db.SaveChanges();
                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("DB ERROR =>" + e.Message);
                    span?.CaptureException(e);
                    return false;
                }
            }
        }
    }
}