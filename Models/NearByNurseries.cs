using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace igarden.Models
{
    public class NearByNurseries
    {
        public double locLat { get; set; }
        public double locLng { get; set; }
        public List<Nursery> Nurseries { get; set; }
    }

    public class Nursery
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public float Rating { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

}