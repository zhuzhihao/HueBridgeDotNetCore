using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HueBridge.Models
{
    public class Sensor
    {
        public Dictionary<string, object> State { get; set; }
        public Dictionary<string, object> Config { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string ModelId { get; set; }
        public string ManufacturerName { get; set; }
        public string SWVersion { get; set; }
        public string UniqueId { get; set; }
    }
}
