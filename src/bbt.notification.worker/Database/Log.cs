using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Log
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string ProjectName { get; set; }
    public string ServiceName { get; set; }
    public string ResponseData { get; set; }
    public string RequestData { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime ErrorDate { get; set; }
}