using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using HueBridge.Models;

namespace HueBridge.Controllers
{
    [Produces("application/json")]
    [Route("api/{user?}/schedules")]
    public class ScheduleController : Controller
    {
        private IGlobalResourceProvider _grp;

        public ScheduleController(
            IGlobalResourceProvider grp)
        {
            _grp = grp;
        }

        [HttpGet]
        public JsonResult GetAllSchedules(string user)
        {
            // authentication
            if (!_grp.AuthenticatorInstance.IsValidUser(user))
            {
                return Json(_grp.AuthenticatorInstance.ErrorResponse(Request.Path.ToString()));
            }
            var schedules = _grp.DatabaseInstance.GetCollection<Schedule>("schedules");
            var schedules_dict = new Dictionary<string, Schedule>();
            foreach (var g in schedules.FindAll())
            {
                schedules_dict[g.Id.ToString()] = g;
            }

            return Json(schedules_dict);
        }
    }
}