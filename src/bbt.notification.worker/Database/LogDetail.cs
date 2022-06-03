using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


    public class LogDetail
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int LogId { get; set; }
        public string ResponseData { get; set; }
        public string RequestData { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime RequestDate { get; set; }

    }

