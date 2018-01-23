using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace HueBridge.Models
{
    public class Config
    {
        public string Name { get; set; }            // Name of the bridge. This is also its uPnP name, so will reflect the actual uPnP name after any conflicts have been resolved.
        public int ZigbeeChannel { get; set; } = 15;// The current wireless frequency channel used by the bridge. It can take values of 11, 15, 20,25 or 0 if undefined (factory new).
        public string MAC { get; set; }
        public bool DHCP { get; set; } = true;
        public string IPAddress { get; set; }
        public string NetMask { get; set; }
        public string Gateway { get; set; }
        public string ProxyAddress { get; set; }
        public int ProxyPort { get; set; }
        public string UTC { get; set; }
        public string LocalTime { get; set; }
        public string TimeZone { get; set; }
        public Dictionary<string, WhiteListItem> WhiteList { get; set; }
        public string SWVersion { get; set; }
        public string APIVersion { get; set; }
        public string SWUpdate2 { get; set; }       // Contains information related to software updates.
        public bool LinkButton { get; set; }
        public bool PortalServices { get; set; }
        public string PortalConnection { get; set; }
        public string PortalState { get; set; }
    }

    public class WhiteListItem
    {
        [JsonProperty("last use date")]
        public string LastUsedDate { get; set; }
        [JsonProperty("create date")]
        public string CreateDate { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
