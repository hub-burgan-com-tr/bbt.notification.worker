using System.ComponentModel;

namespace bbt.notification.worker.Enum
{
    [Flags]
    public enum MessageDataFieldType
    {
        [Description("Json")]
        Json = 0,
        [Description("String")]
        String = 1
    }
}