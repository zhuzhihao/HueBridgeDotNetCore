using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HueBridge.Models;

namespace HueBridge
{
    public interface ISensorHandlerContract
    {
        List<string> SupportedModels { get; }
        Task<List<Sensor>> ScanSensors(string hostIP);
        Task<Sensor> GetSensorState(Sensor sensor);
        bool ConfigSensor(Sensor sensor);
    }
}
