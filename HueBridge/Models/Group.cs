using LiteDB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HueBridge.Models
{
    public class Group
    {
        [JsonIgnore]
        [BsonId]
        public int Id { get; set; }
        public List<string> Lights { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Class { get; set; }
        public GroupAction Action { get; set; }
        public GroupState State { get; set; }
    }

    public class GroupState
    {
        public bool Any_on { get; set; }
        public bool All_on { get; set; }
    }
}
