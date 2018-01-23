using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HueBridge.Controllers
{
    [Produces("application/json")]
    [Route("api/{user?}/config")]
    public class ConfigController : Controller
    {
        private IGlobalResourceProvider _grp;
        private IOptions<AppOptions> _option;

        public ConfigController(
            IGlobalResourceProvider grp,
            IOptions<AppOptions> optionsAccessor)
        {
            _grp = grp;
            _option = optionsAccessor;
        }

        [HttpGet]
        public JsonResult Get(string user)
        {
            // authentication
            if (!_grp.AuthenticatorInstance.IsValidUser(user))
            {
                return Json(_grp.AuthenticatorInstance.ErrorResponse(Request.Path.ToString()));
            }
            var config = new Models.Config();

            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            var found = false;
            foreach (var i in interfaces)
            {
                foreach (var addr in i.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.ToString() == _option.Value.NetworkInterface)
                    {
                        found = true;
                        break;
                    }
                }
                if (found)
                {
                    config.DHCP = i.GetIPProperties().GetIPv4Properties().IsDhcpEnabled;
                    config.MAC = string.Join(":", i.GetPhysicalAddress().GetAddressBytes().Select(x => BitConverter.ToString(new byte[] {x})));
                    config.IPAddress = _option.Value.NetworkInterface;
                    config.NetMask = i.GetIPProperties().UnicastAddresses.Where(x => (x.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                                                                                      x.Address.ToString() == config.IPAddress))
                                                                         .Select(x => x.IPv4Mask)
                                                                         .FirstOrDefault()
                                                                         .ToString();
                    try
                    {
                        config.Gateway = i.GetIPProperties().GatewayAddresses?[0].ToString();
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        config.Gateway = "0.0.0.0";
                    }
                    config.ProxyAddress = "";
                    config.ProxyPort = 0;
                    break;
                }
            }

            config.Name = Environment.MachineName;
            config.ZigbeeChannel = 15;
            config.UTC = DateTime.UtcNow.ToString();
            config.LocalTime = DateTime.Now.ToString();
            config.TimeZone = TimeZoneInfo.Local.ToString();

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
            config.SWVersion = "1.0.0 revision 2";
            config.APIVersion = "1.19.0";
            config.LinkButton = true;
            config.PortalConnection = "";
            config.PortalServices = false;
            config.PortalState = "none";

            return Json(config);
        }
    }
}