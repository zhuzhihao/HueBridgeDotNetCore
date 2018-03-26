using LiteDB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HueBridge.Models
{
    public class Scene
    {
        [BsonId]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public List<string> Lights { get; set; }
        [JsonIgnore]
        public Dictionary<string, GroupAction> LightStates { get; set; }
        public string Owner { get; set; }
        public bool Recycle { get; set; }
        public bool Locked { get; set; }
        public SceneAppData Appdata { get; set; }
        public string Picture { get; set; }
        public DateTime Lastupdated { get; set; }
        public int Version { get; set; }
        [JsonIgnore]
        public bool StoreLightState { get; set; }
    }

    public class SceneAppData
    {
        public int Version { get; set; }
        public string Data { get; set; }
    }
}
