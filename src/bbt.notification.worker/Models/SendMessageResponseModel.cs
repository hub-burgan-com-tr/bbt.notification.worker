using bbt.notification.worker.Enum;

namespace bbt.notification.worker.Models
{
    public class SendMessageResponseModel
    {
        public Guid TxnId { get; set; }
        public dEngageResponseCodes Status { get; set; }
    }

    public class SendErrorResponseModel
    {
        public string StatusCode { get; set; }
        public string ReasonPhrase { get; set; }
        public string Message { get; set; }
    }
}