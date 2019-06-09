using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using HueBridge.Models;
using System.Threading.Tasks;

namespace HueBridge
{
    public interface ILightHandlerContract
    {
        List<string> SupportedModels { get; }
        Task<List<Light>> ScanLights(string hostIP);
        Task<bool> CheckReachable(Light light);
        Task<bool> SetLightState(Light light);
        Task<Light> GetLightState(Light light);
    }
}
