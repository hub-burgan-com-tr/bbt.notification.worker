namespace bbt.notification.worker.Helper
{
    public class LogHelper : ILogHelper
    {
        public bool LogCreate(object requestModel, object responseModel, string methodName, string errorMessage)
        {

            using (var db = new DatabaseContext())
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        db.Add(new Log
                        {
                            ServiceName = methodName,
                            ProjectName = System.Reflection.Assembly.GetEntryAssembly().GetName().Name
                        });

                        db.SaveChanges();

                        int logId = db.Logs.OrderByDescending(u => u.Id).FirstOrDefault().Id;

                        db.Add(new LogDetail
                        {
                            LogId = logId,
                            RequestData = System.Text.Json.JsonSerializer.Serialize(requestModel),
                            ResponseData = System.Text.Json.JsonSerializer.Serialize(responseModel),
                            RequestDate=DateTime.Now,
                            ErrorMessage = errorMessage
                        });
                        db.SaveChanges();

                        transaction.Commit();
                    }

                    catch (Exception e)
                    {
                        transaction.Rollback();
                        return false;
                    }
                }
            }

            return true;

        }
    }
}
