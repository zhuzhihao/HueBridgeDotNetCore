using LiteDB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace HueBridge.Models
{
    public class Schedule
    {
        [JsonIgnore]
        [BsonId]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ScheduleCommand Command { get; set; }
        public string LocalTime { get; set; }
        public DateTime Created { get; set; }
        public string Status { get; set; }
        public bool AutoDelete { get; set; }
        public DateTime StartTime { get; set; }
        public bool Recycle { get; set; }
    }

    public class ScheduleCommand
    {
        public string Address { get; set; }
        public Dictionary<string, string> Body { get; set; }
        public string Method { get; set; }
    }
}
