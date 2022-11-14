using bbt.notification.worker.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bbt.notification.worker.Models
{
    public class SendMessageResponseModel
    {
        public Guid TxnId { get; set; }
        public dEngageResponseCodes Status { get; set; }
    }
}
