using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
