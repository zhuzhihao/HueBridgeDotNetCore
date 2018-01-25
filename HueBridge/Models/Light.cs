using LiteDB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HueBridge.Models
{
    public class Light
    {
        [JsonIgnore]
        [BsonId]
        public int Id { get; set; }
        [JsonIgnore]
        public DateTime CreateDate { get; set; }
        [JsonIgnore]
        public string IPAddress { get; set; }
        public LightState State { get; set; } = new LightState();
        public string Type { get; set; }        // A fixed name describing the type of light e.g. “Extended color light”.
        public string Name { get; set; }        // A unique, editable name given to the light.
        public string ModelId { get; set; }     // The hardware model of the light.
        public string UniqueId { get; set; }    // Unique id of the device. The MAC address of the device with a unique endpoint id in the form: AA:BB:CC:DD:EE:FF:00:11-XX
        public string ManufacturerName { get; set; }
        public string LuminaireUniqueId { get; set; }   // Unique ID of the luminaire the light is a part of in the format: AA:BB:CC:DD-XX-YY.  AA:BB:, ... represents the hex of the luminaireid,
                                                        // XX the lightsource position (incremental but may contain gaps) and YY the lightpoint position (index of light in luminaire group). 
                                                        // A gap in the lightpoint position indicates an incomplete luminaire (light search required to discover missing light points in this case).
        public StreamingCapability Streaming { get; set; } = new StreamingCapability();
        public string SWVersion { get; set; }
    }

    public class LightState
    {
        public bool On { get; set; }            // On/Off state of the light. On=true, Off=false
        public uint Bri { get; set; }           // Brightness of the light. This is a scale from the minimum brightness the light is capable of, 1, to the maximum capable brightness, 254.
        public uint Hue { get; set; }           // Hue of the light. This is a wrapping value between 0 and 65535. Both 0 and 65535 are red, 25500 is green and 46920 is blue.
        public uint Sat { get; set; }           // Saturation of the light. 254 is the most saturated (colored) and 0 is the least saturated (white).
        public float[] XY { get; set; } = new float[] { 0.0f, 0.0f }; 
                                                // The x and y coordinates of a color in CIE color space.
        public uint CT { get; set; }            // The Mired Color temperature of the light. 2012 connected lights are capable of 153 (6500K) to 500 (2000K).
        public string Alert { get; set; } = "none";
                                                // “none” – The light is not performing an alert effect.
                                                // “select” – The light is performing one breathe cycle.
                                                // “lselect” – The light is performing breathe cycles for 15 seconds or until an "alert": "none" command is received.
        public string Effect { get; set; } = "none";
        // The dynamic effect of the light, can either be “none” or “colorloop”.
        public string ColorMode { get; set; } = "";
                                                // Values are “hs” for Hue and Saturation, “xy” for XY and “ct” for Color Temperature. This parameter is only present when the light supports at least one of the values.
        public bool Reachable { get; set; }     // Indicates if a light can be reached by the bridge.
    }

    public class StreamingCapability
    {
        public bool Renderer { get; set; }      // Indicates if a lamp can be used for entertainment streaming as renderer
        public bool Proxy { get; set; }         // Indicates if a lamp can be used for entertainment streaming as a proxy node
    }
}
