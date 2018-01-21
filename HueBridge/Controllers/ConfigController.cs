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
        [HttpGet]
        public Models.Config Get(string user)
        {
            var config = new Models.Config();
            return config;
        }
    }
}