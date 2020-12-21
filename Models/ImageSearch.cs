using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace igarden.Models
{
    public class ImageSearch
    {
        public string Plant_Scientific_Name { get; set; }
        public string Common_Name { get; set; }
        public string Family { get; set; }
        public double Score { get; set; }
        public string Plant_ID { get; set; }
    }
}