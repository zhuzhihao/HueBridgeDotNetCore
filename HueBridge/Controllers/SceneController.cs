using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;


/// <summary>
/// APIs for groups https://developers.meethue.com/documentation/scenes-api
/// </summary>
namespace HueBridge.Controllers
{
    [Produces("application/json")]
    [Route("api/{user?}/scenes")]
    public class SceneController : Controller
    {
        private IGlobalResourceProvider _grp;

        public SceneController(
            IGlobalResourceProvider grp)
        {
            _grp = grp;
        }

        [HttpGet]
        public JsonResult GetAllScenes(string user)
        {
            // authentication
            if (!_grp.AuthenticatorInstance.IsValidUser(user))
            {
                return Json(_grp.AuthenticatorInstance.ErrorResponse(Request.Path.ToString()));
            }

            var scenes = _grp.DatabaseInstance.GetCollection<Models.Scene>("scenes");
            var scenes_dict = new Dictionary<string, Models.Scene>();
            foreach (var s in scenes.FindAll())
            {
                scenes_dict[s.Id.ToString()] = s;
            }

            return Json(scenes_dict);
        }

        [Route("{sceneId}")]
        [HttpGet]
        public JsonResult GetScene(string user, string sceneId)
        {
            // authentication
            if (!_grp.AuthenticatorInstance.IsValidUser(user))
            {
                return Json(_grp.AuthenticatorInstance.ErrorResponse(Request.Path.ToString()));
            }

            var scenes = _grp.DatabaseInstance.GetCollection<Models.Scene>("scenes");
            var s = scenes.FindById(Convert.ToInt64(sceneId));
            s.SerializeLightStates(true);
            return Json(s);
        }

        [HttpPost]
        // create a new scene
        public JsonResult AddNewScene(string user, [FromBody]CreateSceneRequest newScene)
        {
            // authentication
            if (!_grp.AuthenticatorInstance.IsValidUser(user))
            {
                return Json(_grp.AuthenticatorInstance.ErrorResponse(Request.Path.ToString()));
            }

            // parameter check
            // verify that lights given by parameters are all known to us
            var lights = _grp.DatabaseInstance.GetCollection<Models.Light>("lights");
            var lightsInGroup = new LiteDB.BsonArray();

            var count = lights.Count(x => newScene.Lights.Contains(x.Id.ToString()));
            if (count != newScene.Lights.Count)
            {
                return Json(new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        ["failure"] = "Trying to add a non-existent light id into group"
                    }
                });
            }

            // insert new scene
            var scene = new Models.Scene
            {
                Name = newScene.Name,
                Recycle = newScene.Recycle,
                Picture = newScene.Picture ?? "",
                Lights = newScene.Lights,
                Appdata = newScene.AppData,
                Type = newScene.Type ?? "LightScene",
                Version = 2,
                Owner = user,
                Lastupdated = DateTime.UtcNow,
                LightStates = new Dictionary<string, Models.GroupAction>()
            };

            var scenes = _grp.DatabaseInstance.GetCollection<Models.Scene>("scenes");
            var id = scenes.Insert(scene);

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

        [Route("{sceneId}/lightstates/{lightId}")]
        [HttpPut]
        public JsonResult ModifyScene(string user, [FromBody]ModifySceneRequest newScene, string sceneId, string lightId)
        {
            // authentication
            if (!_grp.AuthenticatorInstance.IsValidUser(user))
            {
                return Json(_grp.AuthenticatorInstance.ErrorResponse(Request.Path.ToString()));
            }

            // caller want to modify scene name/lights/storelightstate only
            var scenes = _grp.DatabaseInstance.GetCollection<Models.Scene>("scenes");
            var scene = scenes.FindById(Convert.ToInt64(sceneId));
            if (scene == null)
            {
                return Json(new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        ["failure"] = "scene not exist"
                    }
                });
            }

            if (newScene.Name != null || newScene.Lights != null || newScene.StoreLightState != null)
            {
                return ModifySceneProperty(newScene, scene);
            }
            // caller want to modify light state
            else
            {
                // check if lightId is valid
                if (scene.Lights.Count(x => x == lightId) > 0)
                {
                    if (scene.LightStates.Count(x => x.Key == lightId) == 0)
                    {
                        scene.LightStates[lightId] = new Models.GroupAction();
                    }
                    var pp = typeof(Models.GroupAction).GetProperties().ToList();
                    var lightstate = scene.LightStates[lightId];

                    foreach (var p in typeof(ModifySceneRequest).GetProperties())
                    {
                        switch (p.Name)
                        {
                            case "Name":
                            case "Lights":
                            case "StoreLightState":
                                break;
                            case "XY": // have to make a special case for nullable array
                                if (newScene.XY != null)
                                {
                                    lightstate.XY[0] = newScene.XY[0] ?? lightstate.XY[0];
                                    lightstate.XY[1] = newScene.XY[1] ?? lightstate.XY[1];
                                }
                                break;
                            default:
                                var pv = p.GetValue(newScene, null);
                                if (pv != null)
                                {
                                     pp.Find(x => x.Name == p.Name).SetValue(lightstate, pv);
                                }
                                break;
                        }
                    }
                    scenes.Update(scene);
                }
            }

            return Json(new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    ["success"] = new Dictionary<string, string>
                    {
                        ["id"] = $"0"
                    }
                }
            });
        }

        private JsonResult ModifySceneProperty(ModifySceneRequest newScene, Models.Scene scene)
        {
            scene.Name = newScene.Name ?? scene.Name;
            scene.Lights = newScene.Lights ?? scene.Lights;
            scene.StoreLightState = newScene.StoreLightState ?? scene.StoreLightState;
            var scenes = _grp.DatabaseInstance.GetCollection<Models.Scene>("scenes");
            scenes.Update(scene);
            return Json(new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    ["success"] = new Dictionary<string, string>
                    {
                        [$"/scenes/{scene.Id}/name"] = $"{newScene.Name}",
                        [$"/scenes/{scene.Id}/lights"] = $"{newScene.Lights}",
                        [$"/scenes/{scene.Id}/storelightstate"] = $"{newScene.StoreLightState}"
                    }
                }
            });
        }

        public class CreateSceneRequest
        {
            public string Name { get; set; }
            public bool Recycle { get; set; }
            public string Picture { get; set; }
            public List<string> Lights { get; set; }
            public Models.SceneAppData AppData { get; set; }
            public string Type { get; set; }
        }

        public class ModifySceneRequest 
        {
            public string Name { get; set; }
            public List<string> Lights { get; set; }
            public bool? StoreLightState { get; set; }

            public bool? On { get; set; }
            public uint? Bri { get; set; }
            public uint? Hue { get; set; }
            public uint? Sat { get; set; }
            public string Effect { get; set; }
            public float?[] XY { get; set; }
            public uint? CT { get; set; }
            public string Alert { get; set; }
            public string ColorMode { get; set; }
        }
    }
}