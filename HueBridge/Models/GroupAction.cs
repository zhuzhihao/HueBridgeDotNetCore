using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HueBridge.Models
{
    public class GroupAction
    {
        public bool On { get; set; }
        public uint Bri { get; set; }
        public uint Hue { get; set; }
        public uint Sat { get; set; }
        public string Effect { get; set; } = "none";
        public List<float> XY { get; set; } = new List<float> { 0.0f, 0.0f };
        public uint CT { get; set; }
        public string Alert { get; set; } = "none";
        public string ColorMode { get; set; } = "";
        public uint TransitionTime { get; set; }
    }
}
