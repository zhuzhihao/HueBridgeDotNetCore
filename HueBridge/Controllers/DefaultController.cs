using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace HueBridge.Controllers.Default
{

    [Produces("application/json")]
    public class DefaultController : Controller
    {
        private static HttpClient _client = new HttpClient();
        private IGlobalResourceProvider _grp;

        public DefaultController(IGlobalResourceProvider grp)
        {
            _grp = grp;

            try
            {
                _client.Timeout = TimeSpan.FromMilliseconds(5000);
            }
            catch (InvalidOperationException ex)
            {
                
            }
        }

        [Route("api")]
        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IEnumerable<Result>> CreateNewUserFromForm([FromForm] Device content)
        {
            if (content.Devicetype == null)
            {
                var ms = new MemoryStream();
                await Request.Body.CopyToAsync(ms);
                ms.Seek(0, SeekOrigin.Begin);
                var body = await new StreamReader(ms).ReadToEndAsync();

                content = JsonConvert.DeserializeObject<Device>(body);
            }

            return CreateNewUser(content);
        }

        [Route("api")]
        [HttpPost]
        [Consumes("application/json")]
        public IEnumerable<Result> CreateNewUserFromBody([FromBody] Device content)
        {
            return CreateNewUser(content);
        }

        private IEnumerable<Result> CreateNewUser(Device content)
        {
            Result ret = new Result();
            if (content.Devicetype?.Length > 0)
            {
                // check for existing user in database
                var db = _grp.DatabaseInstance;
                var users = db.GetCollection<Models.User>("users");
                var res = users.Find(x => x.Name.Equals(content.Devicetype));
                if (res.Count() > 0)
                {
                    ret.Success.Username = res.First().Id;
                }
                else
                {
                    // create new user
                    var user = new Models.User();
                    user.CreateDate = DateTime.Now;
                    user.Name = content.Devicetype;
                    user.Id = Guid.NewGuid().ToString().Replace("-", "");
                    users.Insert(user);
                    ret.Success.Username = user.Id;
                }
            }

            // format return object according to spec
            return new List<Result>
            {
                ret
            };
        }

        [Route("api/{user?}")]
        [HttpGet]
        public async Task<JsonResult> GetAllStatus(string user)
        {
            // authentication
            if (!_grp.AuthenticatorInstance.IsValidUser(user))
            {
                return Json(_grp.AuthenticatorInstance.ErrorResponse(Request.Path.ToString()));
            }
            HttpResponseMessage response;

            var ret = new Dictionary<string, object>();

            var lights = _grp.DatabaseInstance.GetCollection<Models.Light>("lights");
            var lights_dict = new Dictionary<string, Models.Light>();
            foreach (var l in lights.FindAll())
            {
                lights_dict[l.Id.ToString()] = l;
            }

            object groups_dict = new Dictionary<string, string>();
            var groups_request_url = $"{Request.Scheme}://{Request.Host.ToString()}/api/{user}/groups";
            try
            {
                response = await _client.GetAsync(groups_request_url);
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    groups_dict = JsonConvert.DeserializeObject(body);
                }
            }
            catch (TaskCanceledException)
            {
                // request time out
            }

            object config_dict = new Dictionary<string, string>();
            var config_request_url = $"{Request.Scheme}://{Request.Host.ToString()}/api/{user}/config";
            try
            {
                response = await _client.GetAsync(config_request_url);
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    config_dict = JsonConvert.DeserializeObject(body);
                }
            }
            catch (TaskCanceledException)
            {
                // request time out
            }

            object scenes_dict = new Dictionary<string, string>();
            var scenes_request_url = $"{Request.Scheme}://{Request.Host.ToString()}/api/{user}/scenes";
            try
            {
                response = await _client.GetAsync(scenes_request_url);
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    scenes_dict = JsonConvert.DeserializeObject(body);
                }
            }
            catch (TaskCanceledException)
            {
                // request time out
            }

            object sensors_dict = new Dictionary<string, string>();
            var sensors_request_url = $"{Request.Scheme}://{Request.Host.ToString()}/api/{user}/sensors";
            try
            {
                response = await _client.GetAsync(sensors_request_url);
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    sensors_dict = JsonConvert.DeserializeObject(body);
                }
            }
            catch (TaskCanceledException)
            {
                // request time out
            }

            ret["lights"] = lights_dict;
            ret["groups"] = groups_dict;
            ret["config"] = config_dict;
            ret["scenes"] = scenes_dict;
            ret["sensors"] = sensors_dict;
            ret["rules"] = new Dictionary<string, string>();
            ret["resourcelinks"] = new Dictionary<string, string>();
            ret["schedules"] = new Dictionary<string, string>(); ;

            return Json(ret);
        }
    }

    public class Device
    {
        public string Devicetype { get; set; }
    }

    public class Result
    {
        public User Success { get; set; } = new User();
    }

    public class User
    {
        public string Username { get; set; }
    }

}