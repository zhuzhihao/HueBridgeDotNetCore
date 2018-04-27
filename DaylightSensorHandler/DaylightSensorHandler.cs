using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using HueBridge.Models;
using Innovative.SolarCalculator;

namespace HueBridge.Utilities
{
    [Export(typeof(ISensorHandlerContract))]
    public class DaylightSensorHandler : ISensorHandlerContract
    {
        public List<string> SupportedModels => new List<string> {"PHDL00"};

        public bool ConfigSensor(Sensor sensor)
        {
            throw new NotImplementedException();
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<Sensor> GetSensorState(Sensor sensor)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var now = DateTime.Now;

            double latitude = 31.209633, longtitude = 121.469279;

            var solarTimes = new SolarTimes(now.Date, latitude, longtitude);
            if (now >= solarTimes.Sunrise && now <= solarTimes.Sunset)
            {
                sensor.State["daylight"] = true;
            }
            else
            {
                sensor.State["daylight"] = false;
            }
            sensor.State["lastupdated"] = now;

            return sensor;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<List<Sensor>> ScanSensors(string hostIP)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var now = DateTime.Now;

            return new List<Sensor>
            {
                new Sensor
                {
                    State = new Dictionary<string, object>
                    {
                        ["lastupdated"] = now
                    },
                    Config = new Dictionary<string, object>
                    {
                        ["on"] = true,
                        ["long"] = "none",
                        ["lat"] = "none",
                        ["sunriseoffset"] = 0,
                        ["sunsetoffset"] = 0
                    },
                    Name = "Daylight",
                    Type = "Daylight",
                    ModelId = "PHDL00",
                    ManufacturerName = "Philips",
                    SWVersion = "1.0",
                    UniqueId = "42"
                }
            };
        }
    }
}
