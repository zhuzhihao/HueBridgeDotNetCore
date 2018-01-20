using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HueBridge.Controllers.Default
{
    [Produces("application/json")]
    [Route("api")]
    public class DefaultController : Controller
    {
        private IGlobalResourceProvider _grp;

        public DefaultController(IGlobalResourceProvider grp)
        {
            _grp = grp;
        }

        // POST: api
        [HttpPost]
        public IEnumerable<Result> Post([FromBody] Device content)
        {
            Result ret = new Result();
            if (content.devicetype.Length > 0)
            {
                // check for existing user in database
                var db = _grp.DatabaseInstance;
                var users = db.GetCollection<Models.User>("users");
                var res = users.Find(x => x.Name.Equals(content.devicetype));
                if (res.Count() > 0)
                {
                    ret.success.username = res.First().Id;
                }
                else
                {
                    // create new user
                    var user = new Models.User();
                    user.CreateDate = DateTime.Now;
                    user.Name = content.devicetype;
                    user.Id = Guid.NewGuid().ToString().Replace("-", "");
                    users.Insert(user);
                    ret.success.username = user.Id;
                }
            }

            // format return object according to spec
            return new List<Result>
            {
                ret
            };
        }
    }

    public class Device
    {
        public string devicetype { get; set; }
    }

    public class Result
    {
        public User success { get; set; } = new User();
    }

    public class User
    {
        public string username { get; set; }
    }

}