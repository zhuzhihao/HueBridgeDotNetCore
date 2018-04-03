using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HueBridge.Models
{
    public class CreateSceneRequest
    {
        public string Name { get; set; }
        public bool Recycle { get; set; }
        public string Picture { get; set; }
        public List<string> Lights { get; set; }
        public SceneAppData AppData { get; set; }
        public string Type { get; set; }
    }

    public class ModifySceneRequest
    {
        public string Name { get; set; }
        public List<string> Lights { get; set; }
        public bool? StoreLightState { get; set; }

        public bool? On { get; set; }
        public uint? Bri { get; set; }
        public uint? Hue { get; set; }
        public uint? Sat { get; set; }
        public string Effect { get; set; }
        public float?[] XY { get; set; }
        public uint? CT { get; set; }
        public string Alert { get; set; }
        public string ColorMode { get; set; }
    }
}
