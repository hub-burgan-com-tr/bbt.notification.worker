namespace bbt.notification.worker.Models
{

    public class DengageRequestModel
    {

        public DengageRequestModel()
        {
            phone = new Phone();
            process = new Process();
        }
        public string templateParams { get; set; }
        public string template { get; set; }
        public string customerNo { get; set; }
        public Phone phone { get; set; }
        public Process process { get; set; }

        public class Phone
        {
            public int countryCode { get; set; }
            public int prefix { get; set; }
            public int number { get; set; }
        }

        public class Process
        {
            public string name { get; set; }
        }

    }
}