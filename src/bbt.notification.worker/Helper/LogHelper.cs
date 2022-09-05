using Newtonsoft.Json;

namespace bbt.notification.worker.Helper
{
    public class LogHelper : ILogHelper
    {
        public bool LogCreate(object requestModel, object responseModel, string methodName, string errorMessage)
        {

            using (var db = new DatabaseContext())
            {
                //using (var transaction = db.Database.BeginTransaction())
                //{
                try
                {
                    db.Add(new Log
                    {
                        ServiceName = methodName,
                        ProjectName = System.Reflection.Assembly.GetEntryAssembly().GetName().Name,
                        ErrorDate = DateTime.Now,
                        ErrorMessage = errorMessage,
                        RequestData = JsonConvert.SerializeObject(requestModel),
                        ResponseData = JsonConvert.SerializeObject(responseModel)
                    });

                    db.SaveChanges();

                    //int logId = db.Logs.OrderByDescending(u => u.Id).FirstOrDefault().Id;

                    //db.Add(new MessageNotificationLog
                    //{
                    //    LogId = logId,
                    //    RequestData = System.Text.Json.JsonSerializer.Serialize(requestModel),
                    //    ResponseData = System.Text.Json.JsonSerializer.Serialize(responseModel),
                    //    RequestDate=DateTime.Now,
                    //    ErrorMessage = errorMessage
                    //});
                    //db.SaveChanges();

                    //transaction.Commit();
                    return true;
                }

                catch (Exception e)
                {
                    //  transaction.Rollback();
                    return false;
                }
            }
        }

        public bool MessageNotificationLogCreate(long CustomerNo, int SourceId, string PhoneNumber, string Email, object requestModel, object responseModel, int NotificationType, string ResponseMessage)
        {

            using (var db = new DatabaseContext())
            {
                //using (var transaction = db.Database.BeginTransaction())
                //{
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
                        ResponseData = JsonConvert.SerializeObject(responseModel)
                    });

                    db.SaveChanges();
                    return true;
                }

                catch (Exception e)
                {
                    //  transaction.Rollback();
                    return false;
                }
            }
        }
    }
}
