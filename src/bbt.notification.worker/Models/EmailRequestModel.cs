using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static bbt.notification.worker.Models.DengageRequestModel;

namespace bbt.notification.worker.Models
{
    public class EmailRequestModel
    {
        public Guid Id { get; set; }

        public string Email { get; set; }

        public List<Attachment> Attachments { get; set; }

        public Process Process { get; set; }

        public HeaderInfo HeaderInfo { get; set; }
        public string TemplateParams { get; set; }
        public long? CustomerNo { get; set; }
        public string ContactId { get; set; }
        public string Template { get; set; }
    }
    public class Attachment
    {
        public string Name { get; set; }
        public string Data { get; set; }
    }
}
