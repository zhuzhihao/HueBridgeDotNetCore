using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HueBridge.Controllers
{
    using Utilities;

    [Produces("application/json")]
    [Route("api/{user?}/lights")]
    public class LightController : Controller
    {
        private IGlobalResourceProvider _grp;
        private static DateTime _lastScan;

        public LightController(
            IGlobalResourceProvider grp)
        {
            _grp = grp;
        }

        #region /lights
        [HttpGet]
        public Dictionary<string, Models.Light> Get(string user)
        {
            var lights = _grp.DatabaseInstance.GetCollection<Models.Light>("lights");
            var ret = new Dictionary<string, Models.Light>();
            foreach (var l in lights.FindAll())
            {
                ret[l.Id.ToString()] = l;
            }

            return ret;
        }

        [HttpPost]
        public JsonResult Post(string user)
        {
            // authentication
            if (!_grp.AuthenticatorInstance.IsValidUser(user))
            {
                return Json(_grp.AuthenticatorInstance.ErrorResponse(Request.Path.ToString()));
            }

            _lastScan = DateTime.Now;
            // begin scanning all kinds of devices
            foreach (var s in _grp.ScannerInstances)
            {
                if (s.State == ScannerState.IDLE)
                {
                    s.Begin();
                }
            }

            return Json(new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    ["success"] = new Dictionary<string, string>
                    {
                        ["/lights"] = "Searching for new devices"
                    }
                }
            });
        }
        #endregion

        #region /lights/new
        [Route("new")] // api/{user?}/lights/new
        [HttpGet]
        public JsonResult GetNewLights(string user)
        {
            // authentication
            if (!_grp.AuthenticatorInstance.IsValidUser(user))
            {
                return Json(_grp.AuthenticatorInstance.ErrorResponse(Request.Path.ToString()));
            }

            var lights = _grp.DatabaseInstance.GetCollection<Models.Light>("lights");
            var newLights = lights.Find(x => x.CreateDate > _lastScan);

            var ret = new Dictionary<string, object>();
            ret["lastscan"] = _lastScan.ToString();
            foreach (var l in newLights)
            {
                ret[l.Id.ToString()] = new
                {
                    Name = l.Name,
                };
            }

            return Json(ret);
        }
        #endregion

        #region /lights/id
        [Route("{id}")]
        [HttpGet]
        public JsonResult GetLightAttributesAndState(string user, string id)
        {
            // authentication
            if (!_grp.AuthenticatorInstance.IsValidUser(user))
            {
                return Json(_grp.AuthenticatorInstance.ErrorResponse(Request.Path.ToString()));
            }
            var lights = _grp.DatabaseInstance.GetCollection<Models.Light>("lights");
            return Json(lights.FindOne(l => l.Id.ToString() == id));
        }

        [Route("{id}")]
        [HttpDelete]
        public JsonResult DeleteLight(string user, string id)
        {
            // authentication
            if (!_grp.AuthenticatorInstance.IsValidUser(user))
            {
                return Json(_grp.AuthenticatorInstance.ErrorResponse(Request.Path.ToString()));
            }
            var ret = new object[1];

            var lights = _grp.DatabaseInstance.GetCollection<Models.Light>("lights");
            var nrOfDeletedLights = lights.Delete(l => l.Id.ToString() == id);
            if (nrOfDeletedLights == 0)
            {

                ret[0] = new
                {
                    failure = $"light {id} not found"
                };
                return Json(ret);
            }
            else
            {
                ret[0] = new
                {
                    success = $"/lights/{id} deleted."
                };
            }
            return Json(ret);
        }


        [Route("{id}")]
        [HttpPut]
        public JsonResult RenameLight(string user, string id, [FromBody]ChangeNameRequest newName)
        {
            // authentication
            if (!_grp.AuthenticatorInstance.IsValidUser(user))
            {
                return Json(_grp.AuthenticatorInstance.ErrorResponse(Request.Path.ToString()));
            }
            var ret = new object[1];

            // check request
            if (newName != null && newName.Name.Length > 0)
            {
                var lights = _grp.DatabaseInstance.GetCollection<Models.Light>("lights");
                var light = lights.FindOne(l => l.Id.ToString() == id);
                if (light == null)
                {
                    // id not exist
                    ret[0] = new
                    {
                        failure = $"light {id} not found"
                    };
                }
                else
                {
                    light.Name = newName.Name;
                    lights.Update(light);
                    ret[0] = new
                    {
                        success = new Dictionary<string, string>
                        {
                            [$"/lights/{id}/name"] = light.Name
                        }
                    };
                }
            }
            else
            {
                // invalid request
                ret[0] = new
                {
                    failure = $"invalid request"
                };
            }
            return Json(ret);
        }
        #endregion
    }

    public class ChangeNameRequest
    {
        public string Name { get; set; }
    }
}