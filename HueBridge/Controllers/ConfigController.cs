using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HueBridge.Controllers
{
    [Produces("application/json")]
    [Route("api/{user?}/config")]
    public class ConfigController : Controller
    {
        private IGlobalResourceProvider _grp;

        public ConfigController(
            IGlobalResourceProvider grp)
        {
            _grp = grp;
        }

        [HttpGet]
        public JsonResult Get(string user)
        {
            var config = new Models.Config();

            var commInterface = _grp.CommInterface;
            try
            {
                config.DHCP = commInterface.NativeInfo.GetIPProperties().GetIPv4Properties().IsDhcpEnabled;
            }
            catch (PlatformNotSupportedException)
            {
                config.DHCP = true;
            }

            config.MAC = string.Join(":", commInterface.NativeInfo.GetPhysicalAddress().GetAddressBytes().Select(x => BitConverter.ToString(new byte[] {x})));
            config.IPAddress = commInterface.SocketLiteInfo.IpAddress;
            config.NetMask = commInterface.NativeInfo.GetIPProperties().UnicastAddresses.Where(x => (x.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                                                                                      x.Address.ToString() == config.IPAddress))
                                                                        .Select(x => x.IPv4Mask)
                                                                        .FirstOrDefault()
                                                                        .ToString();
            config.Gateway = commInterface.SocketLiteInfo.GatewayAddress;
            config.ProxyAddress = "";
            config.ProxyPort = 0;

            config.Name = Environment.MachineName;
            config.ZigbeeChannel = 15;
            config.UTC = DateTime.UtcNow.ToString();
            config.LocalTime = DateTime.Now.ToString();
            config.TimeZone = TimeZoneInfo.Local.ToString();

            // authentication
            if (_grp.AuthenticatorInstance.IsValidUser(user))
            {
                var users = _grp.DatabaseInstance.GetCollection<Models.User>("users");
                config.WhiteList = new Dictionary<string, Models.WhiteListItem>();
                foreach (var u in users.FindAll())
                {
                    config.WhiteList[u.Id] = new Models.WhiteListItem
                    {
                        LastUsedDate = u.LastUsedDate.ToString(),
                        CreateDate = u.CreateDate.ToString(),
                        Name = u.Name
                    };
                }
            }
            config.SWVersion = "1.0.0 revision 2";
            config.APIVersion = "1.19.0";
            config.LinkButton = true;
            config.PortalConnection = "";
            config.PortalServices = false;
            config.PortalState = "none";

            return Json(config);
        }

        [Route("whitelist/{user2}")]
        [HttpDelete]
        public JsonResult Delete(string user, string user2)
        {
            // authentication
            if (!_grp.AuthenticatorInstance.IsValidUser(user))
            {
                return Json(_grp.AuthenticatorInstance.ErrorResponse(Request.Path.ToString()));
            }
            var ret = new object[1];

            var users = _grp.DatabaseInstance.GetCollection<Models.User>("users");
            var nrOfDeletedUser = users.Delete(u => u.Id == user2);
            if (nrOfDeletedUser == 0)
            {

                ret[0] = new
                {
                    failure = $"user {user2} not found"
                };
                return Json(ret);
            }
            else
            {
                _grp.AuthenticatorInstance.RemoveUserFromCache(user2);
                ret[0] = new
                {
                    success = $"/config/whitelist/{user2} deleted."
                };
            }
            return Json(ret);
        }

        [HttpPut]
        public JsonResult ModifyConfig(string user)
        {
            var buffer = new byte[1024];
            var body =  Request.Body.Read(buffer, 0, 1024);

            return Json("");
        }
    }
}