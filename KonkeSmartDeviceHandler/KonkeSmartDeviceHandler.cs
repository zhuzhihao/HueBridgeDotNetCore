using HueBridge;
using HueBridge.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Composition;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace KonkeSmartDeviceHandler
{
    [Export(typeof(ILightHandlerContract))]
    public class KonkeSmartDeviceHandler : ILightHandlerContract
    {
        private static readonly int KonkePort = 27431;
        private static object mylock = new object();

        private static void DebugOutput(string msg)
        {
            lock (mylock)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.BackgroundColor = ConsoleColor.White;
                Console.Write("[KonkeSmartDeviceHandler] ");
                Console.ResetColor();
                Console.WriteLine(msg);
            }
        }

        public List<string> SupportedModels => new List<string> { "LTW001", "ZLL Light" };

        public async Task<bool> CheckReachable(Light light)
        {
            return true;
        }

        public async Task<List<Light>> ScanLights(string hostIP)
        {
            DebugOutput("Start scanning...");

            var lights = new List<Light>();
            // create a scan request string, encrypt with AES key and broadcast
            var msg = $"lan_phone%mac%nopassword%{DateTime.Now: yyyy-MM-dd-HH:mm:ss}%heart";
            var encrypted = AES.EncryptMessage(msg);
            
            // listen for udp diagrams from chosen interface
            var localEP = new IPEndPoint(IPAddress.Parse(hostIP), KonkePort);
            using (var udp = new UdpClient(localEP))
            {
                for (var retries = 3; retries > 0; retries--)
                {
                    DebugOutput($"Sending search device broadcast, retries remaining:{retries}");
                    await udp.SendAsync(encrypted, encrypted.Length, hostname: "255.255.255.255", port: KonkePort);
                    while (true)
                    {
                        var udpReceiveTask = udp.ReceiveAsync();
                        if (await Task.WhenAny(udpReceiveTask, Task.Delay(TimeSpan.FromSeconds(5))) != udpReceiveTask)
                        {
                            // no message received in 5 seconds
                            break;
                        }
                        var result = udpReceiveTask.Result;
                        if (!result.RemoteEndPoint.Equals(localEP))
                        {
                            try
                            {
                                var resp = AES.DecryptMessage(result.Buffer);
                                DebugOutput($"From:{result.RemoteEndPoint.ToString()} Data:{resp}");

                                // parse % separated data
                                var data = resp.TrimEnd('\0').Split('%');
                                if (data.Length == 5 && data[4] == "hack")
                                {
                                    // data[0] lan_device
                                    // data[1] mac address
                                    // data[2] password
                                    // data[3] action
                                    // data[4] message type
                                    string name, model, swversion;
                                    var action = data[3].Split('#');
                                    // use hardware version to tell the model
                                    if (action.Length >= 3)
                                    {
                                        if (action[1].StartsWith("hv"))
                                        {
                                            name = "KRelay";
                                            model = "ZLL Light";
                                        }
                                        else if (action[1].StartsWith("kbulb_hv"))
                                        {
                                            name = "KBulb";
                                            model = "LTW001";
                                        }
                                        else
                                        {
                                            throw new ArgumentException($"Unkown device type: {action[1]}");
                                        }
                                        swversion = action[2].Substring(action[2].IndexOf("sv") + 2);
                                    }
                                    else
                                    {
                                        throw new ArgumentException($"Unkown device type: {data[1]}");
                                    }
                                    
                                    // avoid adding same device twice
                                    if (lights.Count(x => x.UniqueId == data[1] + "-01") == 0)
                                    {
                                        lights.Add(new Light
                                        {
                                            CreateDate = DateTime.Now,
                                            IPAddress = result.RemoteEndPoint.Address.ToString(),
                                            Type = "Konke light or relay",
                                            Name = name,
                                            ModelId = model,
                                            UniqueId = data[1] + "-01",
                                            ManufacturerName = "Konke",
                                            LuminaireUniqueId = "",
                                            Streaming = new StreamingCapability
                                            {
                                                Renderer = false,
                                                Proxy = false
                                            },
                                            SWVersion = swversion,
                                            State = new LightState
                                            {
                                                ColorMode = "ct",
                                                Reachable = true
                                            },
                                            MetaData = JsonConvert.SerializeObject(new Dictionary<string, string>
                                            {
                                                ["Model"] = name,
                                                ["PhysicalAddress"] = data[1],
                                                ["Password"] = data[2]
                                            })
                                        });
                                    }
                                }
                            }
                            catch (ArgumentException ex)
                            {
                                DebugOutput($"Failed to add device: {ex.Message}");
                            }
                            catch (CryptographicException ex)
                            {
                                DebugOutput($"Decryption error: {ex.Message}");
                            }
                        }
                    }
                }
            }
            DebugOutput($"Scanning finished, found {lights.Count} devices");
            return lights;
        }

        public async Task<bool> SetLightState(Light light)
        {
            // only implement on/off for now

            var meta = JsonConvert.DeserializeObject<Dictionary<string, string>>(light.MetaData);
            if (meta == null) return false;
            // build request string
            var msgList = new List<string>();
            try
            {
                string device, action;
                switch (meta["Model"])
                {
                    case "KRelay":
                        device = "relay";
                        break;
                    case "KBulb":
                        device = "kbulb";
                        break;
                    default:
                        return false;
                }
                action = light.State.On ? "open" : "close";
                msgList.Add($"lan_phone%{meta["PhysicalAddress"]}%{meta["Password"]}%{action}%{device}");

                if (device == "kbulb" && action == "open")
                {
                    // brightness and color temperature
                    msgList.Add($"lan_phone%{meta["PhysicalAddress"]}%{meta["Password"]}%set#lum#{Math.Floor(light.State.Bri * 100D / 255)}%{device}");
                    msgList.Add($"lan_phone%{meta["PhysicalAddress"]}%{meta["Password"]}%set#ctp#{Math.Floor(1000000D / light.State.CT)}%{device}");
                }
            }
            catch (KeyNotFoundException ex)
            {
                DebugOutput($"Invalid device data: {ex.Message}");
                return false;
            }

            using (var udp = new UdpClient())
            {
                foreach (var msg in msgList)
                {
                    DebugOutput(msg);
                    try
                    {
                        var ep = new IPEndPoint(IPAddress.Parse(light.IPAddress), KonkePort);
                        var encrypted = AES.EncryptMessage(msg);
                        await udp.SendAsync(encrypted, encrypted.Length, ep);
                        var udpReceiveTask = udp.ReceiveAsync();
                        if (await Task.WhenAny(udpReceiveTask, Task.Delay(TimeSpan.FromSeconds(5))) != udpReceiveTask)
                        {
                            // no message received in 5 seconds
                            DebugOutput($"Wait for device {light.IPAddress} response timeout");
                            return false;
                        }

                        var result = udpReceiveTask.Result;
                        // parse response
                        var respData = AES.DecryptMessage(result.Buffer).Split('%');
                        if (respData[3].StartsWith(msg.Split('%')[3]))
                        {
                            continue;
                        }

                    }
                    catch (Exception ex)
                    {
                        DebugOutput($"Cannot send command to {light.IPAddress}: {ex.Message}");
                        return false;
                    }
                }
            }
            return true;
        }

        public async Task<Light> GetLightState(Light light)
        {
            return light;
        }

    }
}
