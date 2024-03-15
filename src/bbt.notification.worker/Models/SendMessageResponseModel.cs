using bbt.notification.worker.Enum;

namespace bbt.notification.worker.Models
{
    public class SendMessageResponseModel
    {
        public Guid TxnId { get; set; }
        public dEngageResponseCodes Status { get; set; }
    }
}