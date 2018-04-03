using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HueBridge.Models
{
    public class CreateGroupRequest
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Class { get; set; }
        public List<string> Lights { get; set; }
    }

    public class GroupActionRequest
    {
        public bool? On { get; set; }
        public uint? Bri { get; set; }
        public uint? Hue { get; set; }
        public uint? Sat { get; set; }
        public List<float> XY { get; set; }
        public uint? CT { get; set; }
        public string Alert { get; set; }
        public string Effect { get; set; }
        public int? TransitionTime { get; set; }
        public int? Bri_inc { get; set; }
        public int? Sat_inc { get; set; }
        public int? Hue_inc { get; set; }
        public int? CT_inc { get; set; }
        public List<float> XY_inc { get; set; }
        public string Scene { get; set; }
    }
}
