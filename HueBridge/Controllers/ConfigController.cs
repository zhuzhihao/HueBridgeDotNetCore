using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HueBridge.Controllers
{
    [Produces("application/json")]
    [Route("api/{user?}/config")]
    public class ConfigController : Controller
    {
        private IGlobalResourceProvider _grp;

        public ConfigController(IGlobalResourceProvider grp)
        {
            _grp = grp;
        }

        [HttpGet]
        public JsonResult Get(string user)
        {
            var config = new Models.Config();

            // authentication
            if (!_grp.AuthenticatorInstance.IsValidUser(user))
            {
                return Json(_grp.AuthenticatorInstance.ErrorResponse(Request.Path.ToString()));
            }
            return Json(config);
        }
    }
}