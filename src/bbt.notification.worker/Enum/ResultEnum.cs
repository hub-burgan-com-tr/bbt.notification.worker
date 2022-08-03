using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bbt.notification.worker.Enum
{
    public enum ResultEnum
    {
        [Description("ERROR")]
        ERROR = 1,
        [Description("SUCCESS")]
        SUCCESS = 2,
    }
}
