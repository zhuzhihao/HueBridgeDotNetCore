using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace HueBridge.Utilities
{
    public class ESP8266LightScanner : IScanner
    {
        private IGlobalResourceProvider _grp;
        private ScannerState _state;
        private IOptions<AppOptions> _option;
        private Task _scanningTask;

        public ESP8266LightScanner(
            IOptions<AppOptions> optionsAccessor,
            IGlobalResourceProvider grp)
        {
            _state = ScannerState.IDLE;
            _option = optionsAccessor;
            _grp = grp;
        }

        public ScannerState State => _state;

        private List<string> _devicesAlive;
        private async Task TestHttpPort(string ip)
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
                    lock (_devicesAlive)
                    {
                        _devicesAlive?.Add(ip);
                    }
                }
            }
        }

        private async Task CheckAndAddLights(string ip)
        {
            var url = $"http://{ip}/detect";
            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromMilliseconds(500);
            var response = await client.GetAsync(url);
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
                        var db = _grp.DatabaseInstance;
                        var lights = db.GetCollection<Models.Light>("lights");

                        if (lights.FindOne(l => l.UniqueId.StartsWith(r.mac)) == null)
                        {
                            // found a new light
                            var newLight = new Models.Light()
                            {
                                Type = "Extended color light",
                                Name = $"{r.modelid}",
                                ModelId = r.modelid,
                                UniqueId = r.mac + "-01",
                                ManufacturerName = "HomeMade",
                                LuminaireUniqueId = "",
                                Streaming = new Models.StreamingCapability
                                {
                                    Renderer = false,
                                    Proxy = false
                                },
                                SWVersion = "66010400",
                                State = new Models.LightState
                                {
                                    Reachable = true
                                }
                            };

                            try
                            {
                                lights.EnsureIndex(x => x.UniqueId, true);
                                lights.Insert(newLight);
                            }
                            catch (LiteDB.LiteException ex)
                            {
                                Console.WriteLine(ex);
                            }
                        }
                    }
                }
                catch (JsonReaderException)
                {

                }
            }
        }

        private async Task DetectLights(IEnumerable<string> deviceList)
        {
            _devicesAlive = new List<string>();
            // first check how many devices alive in local network
            var portScanTasks = deviceList.Select(dev => TestHttpPort(dev))
                                             .ToArray();
            Task.WaitAll(portScanTasks, 150); // wait maximum 150ms

            var checkLightTasks = _devicesAlive.Select(dev => CheckAndAddLights(dev))
                                               .ToArray();
            await Task.WhenAll(checkLightTasks);

            _state = ScannerState.IDLE;
        }

        public void Begin()
        {
            // scan subnet xxx.xxx.xxx.1 ~ xxx.xxx.xxx.254 for ESP8266 lights
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            var found = false;
            foreach (var i in interfaces)
            {
                foreach (var addr in i.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.ToString() == _option.Value.NetworkInterface)
                    {
                        found = true;
                        break;
                    }
                }
                if (found) break;
            }

            if (found)
            {
                _state = ScannerState.SCANNING;

                var ip = string.Join(".", _option.Value.NetworkInterface.Split('.').Take(3));
                var ipList = new List<string>();
                for (var i = 1; i < 255; i++)
                {
                    ipList.Add(ip + "." + i.ToString());
                }

                _scanningTask = Task.Run(async () => { await DetectLights(ipList); });
            }
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
