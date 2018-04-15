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
    public class KonkeSmartDeviceHandler : ILightHandlerContract
    {
        public List<string> SupportedModels => new List<string> { "mini K" };

        public Task<bool> CheckReachable(Light light)
        {
            throw new NotImplementedException();
        }

        public Task<List<Light>> ScanLights(string hostIP)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetLightState(Light light)
        {
            throw new NotImplementedException();
        }

        public Task<Light> GetLightState(Light light)
        {
            throw new NotImplementedException();
        }
    }
}
