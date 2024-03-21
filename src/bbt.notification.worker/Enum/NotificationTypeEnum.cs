using System.ComponentModel;

namespace bbt.notification.worker.Enum
{
    public enum NotificationTypeEnum
    {
        [Description("SMS")]
        SMS = 1,
        [Description("EMAIL")]
        EMAIL = 2,
        [Description("PUSHNOTIFICATION")]
        PUSHNOTIFICATION = 3,
    }
}