using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HueBridge.Controllers
{
    [Produces("application/json")]
    [Route("api/{user?}/lights")]
    public class LightsController : Controller
    {
        [HttpGet]
        public Dictionary<string, Models.Light> Get(string user)
        {
            var lights = new Dictionary<string, Models.Light>();
            lights["1"] = new Models.Light();

            return lights;
        }
    }
}