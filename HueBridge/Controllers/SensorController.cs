using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HueBridge.Controllers
{
    [Produces("application/json")]
    [Route("api/{user?}/sensors")]
    public class SensorController : Controller
    {
    }
}