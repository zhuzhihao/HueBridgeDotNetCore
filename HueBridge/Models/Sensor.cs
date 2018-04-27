using LiteDB;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace HueBridge.Models
{
    public class Sensor
    {
        [JsonIgnore]
        [BsonId]
        public int Id { get; set; }
        [JsonIgnore]
        public string IPAddress { get; set; }
        public Dictionary<string, object> State { get; set; }
        public Dictionary<string, object> Config { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string ModelId { get; set; }
        public string ManufacturerName { get; set; }
        public string SWVersion { get; set; }
        public string UniqueId { get; set; }
        public bool Recycle { get; set; }
    }
}
