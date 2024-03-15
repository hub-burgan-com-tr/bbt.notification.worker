namespace bbt.notification.worker.Helper
{
    public interface ILogHelper
    {
        public bool LogCreate(object requestModel,object responseModel,string methodName,string ErrorMessage);
        public bool MessageNotificationLogCreate(long CustomerNo, int SourceId, string PhoneNumber, string Email, object requestModel, object responseModel, int NotificationType, string ResponseMessage,string Content,bool isStaff);

    }
}