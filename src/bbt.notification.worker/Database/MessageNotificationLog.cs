using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
public class MessageNotificationLog
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public long CustomerNo { get; set; }
    public int SourceId { get; set; }
    public string Content { get; set; }
    public string PhoneNumber { get; set; }
    public string Email { get; set; }
    public string ResponseData { get; set; }
    public string RequestData { get; set; }
    public int NotificationType { get; set; }
    public string ResponseMessage { get; set; }
    public DateTime CreateDate { get; set; }
    public bool IsRead { get; set; }
    public bool IsDelete { get; set; }
    public DateTime? ReadTime { get; set; }
    public DateTime? DeleteTime { get; set; }
    public bool IsStaff { get; set; }
}