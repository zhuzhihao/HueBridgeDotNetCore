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
        public List<string> SupportedModels => new List<string> { "LST001" };

        public async Task<List<Light>> ScanLights(string hostIP)
        {
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
            Task.WaitAll(portScanTasks.ToArray(), 500); // wait maximum 500ms

            devices = portScanTasks.Where(t => t.IsCompleted)
                                    .Select(x => x.Result)
                                    .Where(x => x != "").ToList();

            var checkLightTasks = devices.Select(dev => CheckAndAddLights(dev))
                                         .ToArray();
            try
            {
                var lights = await Task.WhenAll(checkLightTasks);
                return lights.Where(x => x != null).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return null;
        }

        public async Task<bool> CheckReachable(Light light)
        {
            return (await TestHttpPort(light.IPAddress)) == light.IPAddress;
        }

        public Task<Light> SyncLightState(Light light)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> SetLightState(Light light)
        {
            var newState = light.State;
            HttpClient client = new HttpClient();
            var light_request_url = $"http://{light.IPAddress}/set?light=1";
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

            var response = await client.GetAsync(light_request_url.ToLower());
            return response.IsSuccessStatusCode;
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

        private async Task<Light> CheckAndAddLights(string ip)
        {
            Console.WriteLine($"Checking {ip}");

            var url = $"http://{ip}/detect";
            HttpClient client = new HttpClient();
            HttpResponseMessage response;
            client.Timeout = TimeSpan.FromMilliseconds(1000);

            try
            {
                response = await client.GetAsync(url);
            }
            catch (TaskCanceledException)
            {
                // request time out
                return null;
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
                        var newLight = new Light()
                        {
                            CreateDate = DateTime.Now,
                            IPAddress = ip,
                            Type = "Extended color light",
                            Name = $"{r.modelid}",
                            ModelId = r.modelid,
                            UniqueId = r.mac + "-01",
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
                        };
                        return newLight;
                    }
                }
                catch (JsonReaderException)
                {
                }
            }
            return null;
        }
    }
}
