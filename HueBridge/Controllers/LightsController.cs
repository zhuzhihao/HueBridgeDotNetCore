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
    public class LightsController : Controller
    {
        private IGlobalResourceProvider _grp;

        public LightsController(
            IGlobalResourceProvider grp)
        {
            _grp = grp;
        }

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

        [Route("new")] // api/{user?}/lights/new
        [HttpGet]
        public JsonResult GetNewLights(string user)
        {
            return Json(new Dictionary<string, string>
            {
                ["lastscan"] = DateTime.Now.ToString()
            });
        }
    }
}