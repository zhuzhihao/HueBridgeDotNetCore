using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using HueBridge.Models;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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
        private static HttpClient _client = new HttpClient();

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

            var groups = _grp.DatabaseInstance.GetCollection<Group>("groups");
            var groups_dict = new Dictionary<string, Group>();
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
            var lights = _grp.DatabaseInstance.GetCollection<Light>("lights");
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
            var group = new Group
            {
                Name = newGroup.Name,
                Class = newGroup.Class,
                Type = newGroup.Type,
                Lights = newGroup.Lights,
                Action = new GroupAction(),
                State = new GroupState()
            };

            var groups = _grp.DatabaseInstance.GetCollection<Group>("groups");
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

            var groups = _grp.DatabaseInstance.GetCollection<Group>("groups");
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

        [Route("{id}/action")]
        [HttpPut]
        public async Task<JsonResult> SetGroupState(string user, string id, [FromBody]GroupActionRequest newGroupAction)
        {
            // authentication
            if (!_grp.AuthenticatorInstance.IsValidUser(user))
            {
                return Json(_grp.AuthenticatorInstance.ErrorResponse(Request.Path.ToString()));
            }

            var lights = _grp.DatabaseInstance.GetCollection<Light>("lights");
            var groups = _grp.DatabaseInstance.GetCollection<Group>("groups");
            var group = Convert.ToInt64(id) > 0 ? groups.FindById(Convert.ToInt64(id)) : new Group
            {
                Id = 0, // special group that contains all lights: 0
                Class = "Other",
                Name = "All lights",
                Lights = lights.FindAll().Select(x => Convert.ToString(x.Id)).ToList(),
                State = new GroupState(),
                Action = new GroupAction()
            };
            if (group == null)
            {
                return Json(new
                {
                    failure = $"group {id} not found"
                });
            }

            var ret = new List<Dictionary<string, object>>();

            if (newGroupAction.Scene != null)
            {
                // change scene on group
                var scenes = _grp.DatabaseInstance.GetCollection<Scene>("scenes");
                var scene = scenes.FindById(Convert.ToInt64(newGroupAction.Scene));
                if (scene == null)
                {
                    return Json(new
                    {
                        failure = $"scene {newGroupAction.Scene} not found"
                    });
                }

                // find the lights and their lightstate in the scene
                var newLightStates = scene.LightStates.Where(x => group.Lights.Contains(x.Key));
                var tasks = new List<Task<HttpResponseMessage>>();
                var settings = new JsonSerializerSettings();
                settings.ContractResolver = new DefaultContractResolver { NamingStrategy = new Utilities.LowercaseNamingStrategy() };
                foreach (var l in newLightStates)
                {
                    var lightstate_request_url = $"{Request.Scheme}://{Request.Host.ToString()}/api/{user}/lights/{l.Key}/state";
                    var content = new StringContent(JsonConvert.SerializeObject(l.Value, settings), Encoding.UTF8, "application/json");
                    tasks.Add(_client.PutAsync(lightstate_request_url, content));
                }

                try
                {
                    await Task.WhenAll(tasks.ToArray());
                    ret.Add(new Dictionary<string, object>
                    {
                        ["success"] = new Dictionary<string, object>
                        {
                            [$"/groups/{id}/state/scene"] = newGroupAction.Scene
                        }
                    });
                }
                catch (Exception ex)
                {
                    return Json(new
                    {
                        failure = $"group {id} failed to change to new scene {ex.Message}"
                    });
                }
            }
            else
            {
                var pp = typeof(GroupAction).GetProperties().ToList();
                var ppp = typeof(SetLightStateRequest).GetProperties().ToList();
                var colormode = "";
                var req = new SetLightStateRequest();

                foreach (var p in typeof(GroupActionRequest).GetProperties())
                {
                    var pv = p.GetValue(newGroupAction, null);
                    if (pv != null)
                    {
                        if (p.Name.EndsWith("_inc"))
                        {
                            var pName = p.Name.Replace("_inc", "");
                            var ppOrgValue = pp.Find(x => x.Name == pName).GetValue(group.Action, null);
                            if (ppOrgValue != null)
                            {
                                pp.Find(x => x.Name == pName).SetValue(group.Action, Convert.ToUInt32((uint)ppOrgValue + (int)pv));
                                ppp.Find(x => x.Name == pName).SetValue(req, Convert.ToUInt32((uint)ppOrgValue + (int)pv));
                            }

                            ret.Add(new Dictionary<string, object>
                            {
                                ["success"] = new Dictionary<string, object>
                                {
                                    [$"/groups/{id}/state/{pName.ToLower()}"] = Convert.ToUInt32((uint)ppOrgValue + (int)pv)
                                }
                            });
                        }
                        else
                        {
                            try
                            {
                                pp.Find(x => x.Name == p.Name).SetValue(group.Action, pv);
                                ppp.Find(x => x.Name == p.Name).SetValue(req, pv);
                                ret.Add(new Dictionary<string, object>
                                {
                                    ["success"] = new Dictionary<string, object>
                                    {
                                        [$"/groups/{id}/state/{p.Name.ToLower()}"] = pv
                                    }
                                });
                            }
                            catch
                            {
                                Console.WriteLine($"Cannot set group state: {p.Name}");
                            }

                        }

                        // colormode priority system: xy > ct > hs
                        switch (p.Name)
                        {
                            case nameof(SetLightStateRequest.Hue):
                            case nameof(SetLightStateRequest.Hue_inc):
                            case nameof(SetLightStateRequest.Sat):
                            case nameof(SetLightStateRequest.Sat_inc):
                                colormode = colormode == "" ? "hs" : colormode;
                                break;
                            case nameof(SetLightStateRequest.XY):
                            case nameof(SetLightStateRequest.XY_inc):
                                colormode = "xy";
                                break;
                            case nameof(SetLightStateRequest.CT):
                            case nameof(SetLightStateRequest.CT_inc):
                                colormode = colormode != "xy" ? "ct" : colormode;
                                break;
                            default:
                                break;
                        }
                    }
                }
                if (colormode != "")
                {
                    group.Action.ColorMode = colormode;
                }

                // find the lights and their lightstate in the scene
                var tasks = new List<Task<HttpResponseMessage>>();
                var settings = new JsonSerializerSettings();
                settings.ContractResolver = new DefaultContractResolver { NamingStrategy = new Utilities.LowercaseNamingStrategy() };
                foreach (var l in group.Lights)
                {
                    var lightstate_request_url = $"{Request.Scheme}://{Request.Host.ToString()}/api/{user}/lights/{l}/state";
                    var content = new StringContent(JsonConvert.SerializeObject(req, settings), Encoding.UTF8, "application/json");
                    tasks.Add(_client.PutAsync(lightstate_request_url, content));
                }
            }
            groups.Update(group);

            return Json(ret);
        }

        [Route("{id}")]
        [HttpPut]
        // create a new group
        public JsonResult ModifyGroup(string user, string id, [FromBody]ModifyGroupRequest modifiedGroup)
        {
            // authentication
            if (!_grp.AuthenticatorInstance.IsValidUser(user))
            {
                return Json(_grp.AuthenticatorInstance.ErrorResponse(Request.Path.ToString()));
            }

            var groups = _grp.DatabaseInstance.GetCollection<Group>("groups");
            var group = groups.FindById(Convert.ToInt64(id));
            if (group == null)
            {
                return Json(new
                {
                    failure = $"group {id} not found"
                });
            }

            group.Name = modifiedGroup.Name ?? group.Name;
            group.Class = modifiedGroup.Class ?? group.Class;
            group.Lights = modifiedGroup.Lights ?? group.Lights;

            groups.Update(group);

            var ret = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    ["success"] = new Dictionary<string, object>
                    {
                        [$"/groups/{id}/name"] = group.Name
                    }
                },
                new Dictionary<string, object>
                {
                    ["success"] = new Dictionary<string, object>
                    {
                        [$"/groups/{id}/class"] = group.Class
                    }
                },
                new Dictionary<string, object>
                {
                    ["success"] = new Dictionary<string, object>
                    {
                        [$"/groups/{id}/lights"] = group.Lights
                    }
                }
            };

            return Json(ret);
        }
    }
}