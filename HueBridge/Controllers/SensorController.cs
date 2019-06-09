using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using HueBridge.Models;

/// <summary>
/// References:
/// https://developers.meethue.com/documentation/sensors-api
/// https://developers.meethue.com/documentation/supported-sensors
/// 
/// </summary>

namespace HueBridge.Controllers
{
    [Produces("application/json")]
    [Route("api/{user?}/sensors")]
    public class SensorController : Controller
    {
        private IGlobalResourceProvider _grp;
        private static DateTime _lastScan;

        public SensorController(
            IGlobalResourceProvider grp)
        {
            _grp = grp;
        }

        [HttpGet]
        public Dictionary<string, Sensor> GetAllSensors(string user)
        {
            var sensors = _grp.DatabaseInstance.GetCollection<Sensor>("sensors");
            var ret = new Dictionary<string, Sensor>();
            foreach (var l in sensors.FindAll())
            {
                ret[l.Id.ToString()] = l;
            }

            return ret;
        }

        [HttpPost]
        public JsonResult ScanForNewSensors(string user)
        {
            // authentication
            if (!_grp.AuthenticatorInstance.IsValidUser(user))
            {
                return Json(_grp.AuthenticatorInstance.ErrorResponse(Request.Path.ToString()));
            }

            _lastScan = DateTime.Now;
            // begin scanning all kinds of sensors
            Task.Factory.StartNew(async () =>
            {
                var tasks = new List<Task<List<Sensor>>>();
                foreach (var h in _grp.SensorHandlers)
                {
                    try
                    {
                        tasks.Add(h.ScanSensors(_grp.CommInterface.SocketLiteInfo.IpAddress));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Cannot initiate scan sensors task for {h.GetType().Name}:{ex.Message}");
                    }
                }
                try
                {
                    var l = await Task.WhenAll(tasks);
                    // each sensor handler returns a list of sensor found, put all sensors in a flat list "expanded"
                    var expanded = new List<Sensor>();
                    foreach (var ll in l)
                    {
                        ll.ForEach(x => expanded.Add(x));
                    }

                    var sensors = _grp.DatabaseInstance.GetCollection<Sensor>("sensors");
                    foreach (var ll in expanded)
                    {
                        // check that the sensor is not in db
                        var sensor_in_db = sensors.FindOne(x => x.UniqueId == ll.UniqueId);
                        if (sensor_in_db == null)
                        {
                            try
                            {
                                sensors.EnsureIndex(x => x.UniqueId, true);
                                sensors.Insert(ll);
                                ll.Name = $"{ll.ModelId} {ll.Id}";
                                sensors.Update(ll);
                            }
                            catch (LiteDB.LiteException ex)
                            {
                                Console.WriteLine(ex);
                            }
                        }
                        else if (sensor_in_db.IPAddress != ll.IPAddress)
                        {
                            // update ip address in database
                            try
                            {
                                sensor_in_db.IPAddress = ll.IPAddress;
                                sensors.Update(sensor_in_db);
                            }
                            catch (LiteDB.LiteException ex)
                            {
                                Console.WriteLine(ex);
                            }
                        }
                    }
                }
                catch
                {

                }
            });

            return Json(new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    ["success"] = new Dictionary<string, string>
                    {
                        ["/sensors"] = "Searching for new devices"
                    }
                }
            });
        }
    }
}