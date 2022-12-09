﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static bbt.notification.worker.Models.DengageRequestModel;

namespace bbt.notification.worker.Models
{
    public class PushNotificaitonRequestModel
    {
        public string TemplateParams { get; set; }
        public string Template { get; set; }
        public string CustomerNo { get; set; }
        public string ContactId { get; set; }
        public string CustomParameters { get; set; }
        public Guid Id { get; set; }

        public Process Process { get; set; }
    }
}
