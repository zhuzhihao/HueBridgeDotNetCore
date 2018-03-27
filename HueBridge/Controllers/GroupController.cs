using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// APIs for groups https://developers.meethue.com/documentation/groups-api
/// </summary>

namespace HueBridge.Controllers
{
    [Produces("application/json")]
    [Route("api/{user?}/groups")]
    public class GroupController : Controller
    {
        private IGlobalResourceProvider _grp;

        public GroupController(
            IGlobalResourceProvider grp)
        {
            _grp = grp;
        }

        [HttpGet]
        public JsonResult GetAllGroups(string user)
        {
            // authentication
            if (!_grp.AuthenticatorInstance.IsValidUser(user))
            {
                return Json(_grp.AuthenticatorInstance.ErrorResponse(Request.Path.ToString()));
            }

            var groups = _grp.DatabaseInstance.GetCollection<Models.Group>("groups");
            var groups_dict = new Dictionary<string, Models.Group>();
            foreach (var g in groups.FindAll())
            {
                groups_dict[g.Id.ToString()] = g;
            }

            return Json(groups_dict);
        }


        [HttpPost]
        // create a new group
        public JsonResult AddNewGroup(string user, [FromBody]CreateGroupRequest newGroup)
        {
            // authentication
            if (!_grp.AuthenticatorInstance.IsValidUser(user))
            {
                return Json(_grp.AuthenticatorInstance.ErrorResponse(Request.Path.ToString()));
            }

            // parameter check
            var parametersOK = false;
            if (newGroup != null)
            {
                if (newGroup.Name != null && newGroup.Name.Length > 0)
                {
                    if (newGroup.Lights != null && newGroup.Lights.Count > 0)
                    {
                        parametersOK = true;
                        if (newGroup.Class == null || newGroup.Class.Length == 0)
                        {
                            newGroup.Class = "Other";
                        }
                        if (newGroup.Type == null || newGroup.Type.Length == 0)
                        {
                            newGroup.Type = "LightGroup";
                        }
                    }
                }
            }
            if (!parametersOK)
            {
                return Json(new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        ["failure"] = "Invalid parameter"
                    }
                });
            }

            // verify that lights given by parameters are all known to us
            var lights = _grp.DatabaseInstance.GetCollection<Models.Light>("lights");
            var lightsInGroup = new LiteDB.BsonArray();

            var count = lights.Count(x => newGroup.Lights.Contains(x.Id.ToString()));
            if (count != newGroup.Lights.Count)
            {
                return Json(new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        ["failure"] = "Trying to add a non-existent light id into group"
                    }
                });
            }

            // all good, add new group into database
            var group = new Models.Group
            {
                Name = newGroup.Name,
                Class = newGroup.Class,
                Type = newGroup.Type,
                Lights = newGroup.Lights,
                Action = new Models.GroupAction(),
                State = new Models.GroupState()
            };

            var groups = _grp.DatabaseInstance.GetCollection<Models.Group>("groups");
            var id = groups.Insert(group);

            return Json(new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    ["success"] = new Dictionary<string, string>
                    {
                        ["id"] = $"{id}"
                    }
                }
            });
        }

        [Route("{id}")]
        [HttpDelete]
        public JsonResult DeleteGroup(string user, string id)
        {
            // authentication
            if (!_grp.AuthenticatorInstance.IsValidUser(user))
            {
                return Json(_grp.AuthenticatorInstance.ErrorResponse(Request.Path.ToString()));
            }
            var ret = new object[1];

            var groups = _grp.DatabaseInstance.GetCollection<Models.Group>("groups");
            var nrOfDeletedGroups = groups.Delete(g => g.Id.ToString() == id);
            if (nrOfDeletedGroups == 0)
            {

                ret[0] = new
                {
                    failure = $"group {id} not found"
                };
                return Json(ret);
            }
            else
            {
                ret[0] = new
                {
                    success = $"/groups/{id} deleted."
                };
            }
            return Json(ret);
        }
    }

    public class CreateGroupRequest
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Class { get; set; }
        public List<string> Lights { get; set; }

    }
}