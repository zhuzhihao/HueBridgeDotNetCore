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

        public Task<bool> CheckReachable(Light light)
        {
            throw new NotImplementedException();
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
                                var data = resp.Split('%');
                                if (data.Length == 5)
                                {
                                    // data[0] lan_device
                                    // data[1] mac address
                                    // data[2] password
                                    // data[3] action
                                    // data[4] message type
                                    string name, model;
                                    switch (data[4])
                                    {
                                        // relay
                                        case "rack":
                                            name = "KRelay";
                                            model = "ZLL Light";
                                            break;
                                        // kbulb
                                        case "kback":
                                            name = "KBulb";
                                            model = "LTW001";
                                            break;
                                        default:
                                            throw new ArgumentException($"Unkown device type: {data[4]}");
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
                                            SWVersion = "2.1.8",
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
            string msg, action;
            try
            {
                string device;
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
                msg = $"lan_phone%{meta["PhysicalAddress"]}%{meta["Password"]}%{action}%{device}";
            }
            catch (KeyNotFoundException ex)
            {
                DebugOutput($"Invalid device data: {ex.Message}");
                return false;
            }

            using (var udp = new UdpClient())
            {
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
                    if (respData[3].StartsWith(action))
                    {
                        return true;
                    }

                }
                catch (Exception ex)
                {
                    DebugOutput($"Cannot send command to {light.IPAddress}: {ex.Message}");
                    return false;
                }
            }
            return false;
        }

        public Task<Light> GetLightState(Light light)
        {
            throw new NotImplementedException();
        }

    }
}
