using bbt.notification.worker.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bbt.notification.worker.Models
{
    public class HeaderInfo
    {
       
            public SenderType Sender { get; set; }
            public string SmsPrefix { get; set; }
            public string SmsSuffix { get; set; }
            public string EmailTemplatePrefix { get; set; }
            public string EmailTemplateSuffix { get; set; }
            public string SmsTemplatePrefix { get; set; }
            public string SmsTemplateSuffix { get; set; }
        }

    }

