using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class NotificationLog
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int SourceId { get; set; }
    public long Client { get; set; }
    public long User { get; set; }
    public string Filter { get; set; }
    public bool IsPushEnabled { get; set; }

    public bool IsSmsEnabled { get; set; }

    public bool IsEmailEnabled { get; set; }

}

