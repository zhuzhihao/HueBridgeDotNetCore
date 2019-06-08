using HueBridge.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace HueBridge.Utilities
{
    [Export(typeof(ILightHandlerContract))]
    public class ESP8266LightHandler : ILightHandlerContract
    {
        #region hue devices
        // https://github.com/nsheldon/Hue-Lights-Indigo-plugin/blob/master/Hue%20Lights.indigoPlugin/Contents/Server%20Plugin/supportedDevices.py
        // List of compatible device IDs that may be associated with a Hue hub.
        //
        // LCT001	=	Hue bulb (color gamut B)
        // LCT002	=	Hue Downlight/Spot BR30 bulb (color gamut B)
        // LCT003	=	Hue Spot Light GU10 bulb (color gamut B)
        // LCT007	=	Hue bulb (800 lumen version, color gamut B)
        // LCT010	=	Hue bulb (A19 version 3, color gamut C)
        // LCT011	=	Hue bulb (BR30 version 3, color gamut C)
        // LCT014	=	Hue bulb (alternate A19 version 3)
        // LLC001	=	LivingColors light (generic)
        // LLC006	=	LivingColors Gen3 Iris
        // LLC007	=	LivingColors Gen3 Bloom Aura
        // LLC010	=	LivingColors Iris (Europe)
        // LLC011	=	Bloom (European?)
        // LLC012	=	Bloom
        // LLC013	=	Disney StoryLight
        // LLC014	=	LivingColors Aura
        // LLC020	=	Hue Go
        // LLM001	=	Hue Luminaire Color Light Module
        // LLM010	=	Hue Color Temperature Module (2200K - 6500K)
        // LLM011	=	" " "
        // LLM012	=	" " "
        // LST001	=	LED LightStrip
        // LST002	=	LED LightStrip Plus (RGB + color temperature)
        // The LightStrip Plus is temporarily in the kHueBulbDeviceIDs
        // list because it supports color temperature and more code will
        // need to change before it can be added to the kLightStripsDeviceIDs list.
        // LTW001	=	Hue White Ambiance bulb (color temperature only bulb).
        // LTW004	=	Another Hue White Ambiance bulb (color temperature only bulb).
        // LTW013	=	Hue Ambiance Spot GU10 spotlight bulb.
        // LTW014	=	" " "
        // LWB001	=	LivingWhites bulb
        // LWB003	=	" " "
        // LWB004	=	Hue A19 Lux
        // LWB006	=	Hue White A19 extension bulb
        // LWB007	=	Hue Lux (alternate version)
        // LWB010	=	Hue White (version 2)
        // LWB014	=	Hue White (version 3)
        // LWL001	=	LivingWhites light socket
        // HML004	=	Phoenix wall lights
        // HML006	=	Phoenix white LED lights
        // ZLL Light	=	Generic ZigBee Light (e.g. GE Link LEDs)
        // FLS-PP3	=	Dresden Elektronik FLS-PP lp LED light strip, color LED segment
        // FLS-PP3 White = Dresden Elektronik FLS-PP lp LED light strip, white light segment
        // Classic A60 TW = Osram Lightify CLA60 Tunable White bulb (color temp. only)
        #endregion
        public List<string> SupportedModels => new List<string> { "LST001", "LWB001", "LWB010" };
        private static object mylock = new object();
        private HttpClient _client;

        [ImportingConstructor]
        public ESP8266LightHandler(IHttpClientFactory clientFactory)
        {
            _client = clientFactory.CreateClient();
            _client.Timeout = TimeSpan.FromMilliseconds(2000);
        }

        private static void DebugOutput(string msg)
        {
            lock (mylock)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.BackgroundColor = ConsoleColor.White;
                Console.Write("[ESP8266LightHandler] ");
                Console.ResetColor();
                Console.WriteLine(msg);
            }
        }

        public async Task<List<Light>> ScanLights(string hostIP)
        {
            DebugOutput("Start scanning...");

            // scan devices in class C subnet
            var ip = string.Join(".", hostIP.Split('.').Take(3));
            var ipList = new List<string>();
            for (var i = 1; i < 255; i++)
            {
                ipList.Add(ip + "." + i.ToString());
            }

            var devices = new List<string>();
            // first scan 80 port in local network
            var portScanTasks = ipList.Select(i => TestHttpPort(i)).ToList();
            Task.WaitAll(portScanTasks.ToArray(), 2000); // wait maximum 2000ms

            devices = portScanTasks.Where(t => t.IsCompleted)
                                    .Select(x => x.Result)
                                    .Where(x => x != "").ToList();

            var checkLightTasks = devices.Select(dev => CheckAndAddLights(dev))
                                         .ToArray();
            try
            {
                var lights = await Task.WhenAll(checkLightTasks);
                var ret = lights.Where(x => x != null && x.Count > 0)
                                .SelectMany(x => x)
                                .ToList();
                DebugOutput($"Scanning finished, found {ret.Count} devices");
                return ret;
            }
            catch (Exception ex)
            {
                DebugOutput(ex.Message);
            }

            return null;
        }

        public async Task<bool> CheckReachable(Light light)
        {
            return (await TestHttpPort(light.IPAddress)) == light.IPAddress;
        }

        class LightStateResponse
        {
            public bool? On { get; set; }
            public uint? Bri { get; set; }
            public uint? Hue { get; set; }
            public uint? Sat { get; set; }
            public List<float> XY { get; set; }
            public uint? CT { get; set; }
            public string ColorMode { get; set; }
        }
        public async Task<Light> GetLightState(Light light)
        {
            DebugOutput($"Requesting light state: {light.IPAddress}");

            var lightstate_request_url = $"http://{light.IPAddress}/get?light={GetLightId(light)}";
            try
            {
                var response = _client.GetAsync(lightstate_request_url);
                var responseText = await response.Result.Content.ReadAsStringAsync();
                var newState = JsonConvert.DeserializeObject<LightStateResponse>(responseText);

                light.State.Reachable = true;
                light.State.On = newState.On ?? light.State.On;
                light.State.Bri = newState.Bri ?? light.State.Bri;
                light.State.XY = newState.XY ?? light.State.XY;
                light.State.CT = newState.CT ?? light.State.CT;
                light.State.Sat = newState.Sat ?? light.State.Sat;
                light.State.Hue = newState.Hue ?? light.State.Hue;
                light.State.ColorMode = newState.ColorMode ?? light.State.ColorMode;
            }
            catch
            {
                light.State.Reachable = false;
            }

            return light;
        }

        public async Task<bool> SetLightState(Light light)
        {
            var newState = light.State;
            var light_request_url = $"http://{light.IPAddress}/set?light={GetLightId(light)}";
            if (newState.Alert != "none")
            {
                light_request_url += $"&alert={newState.Alert}";
                newState.Alert = "none";
            }
            else
            {
                light_request_url += $"&colormode={light.State.ColorMode}&on={light.State.On}";
                light_request_url += light.State.On ? $"&bri={light.State.Bri}" : "";
                switch (light.State.ColorMode)
                {
                    case "xy":
                        light_request_url += $"&x={light.State.XY[0]}&y={light.State.XY[1]}";
                        break;
                    case "ct":
                        light_request_url += $"&ct={light.State.CT}";
                        break;
                    case "hs":
                        light_request_url += $"&hue={light.State.Hue}&sat={light.State.Sat}";
                        break;
                }
            }

            try
            {
                var response = await _client.GetAsync(light_request_url.ToLower());
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> TestHttpPort(string ip)
        {
            using (var tcpClient = new TcpClient())
            {
                try
                {
                    await tcpClient.ConnectAsync(ip, 80);
                }
                catch (Exception)
                {

                }

                if (tcpClient.Connected)
                {
                    return ip;
                }
                return "";
            }
        }

        private async Task<List<Light>> CheckAndAddLights(string ip)
        {
            DebugOutput($"Checking {ip}");
            var ret = new List<Light>();
            var url = $"http://{ip}/detect";
            HttpResponseMessage response;

            try
            {
                response = await _client.GetAsync(url);
            }
            catch (TaskCanceledException)
            {
                // request time out
                return ret;
            }

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                var valid_response = new
                {
                    hue = "",
                    lights = 0,
                    modelid = "",
                    mac = ""
                };
                try
                {
                    var r = JsonConvert.DeserializeAnonymousType(body, valid_response);
                    if (r.hue.Length > 0)
                    {
                        // found a new light
                        foreach (var i in Enumerable.Range(1, r.lights))
                        {
                            ret.Add(new Light()
                            {
                                CreateDate = DateTime.Now,
                                IPAddress = ip,
                                Type = "Extended color light",
                                Name = $"{r.modelid}",
                                ModelId = r.modelid,
                                UniqueId = $"{r.mac}-{i:00}",
                                ManufacturerName = "HomeMade",
                                LuminaireUniqueId = "",
                                Streaming = new StreamingCapability
                                {
                                    Renderer = false,
                                    Proxy = false
                                },
                                SWVersion = "66010400",
                                State = new LightState
                                {
                                    ColorMode = "xy",
                                    Reachable = true
                                }
                            });
                        }
                        return ret;
                    }
                }
                catch (JsonReaderException)
                {
                }
            }
            return ret;
        }

        private static int GetLightId(Light light)
        {
            int id;
            try
            {
                id = Convert.ToInt32(light.UniqueId.Split('-').Last());
            }
            catch
            {
                id = 1;
            }

            return id;
        }
    }
}
