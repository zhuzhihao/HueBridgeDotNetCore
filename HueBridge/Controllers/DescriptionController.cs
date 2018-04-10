using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace HueBridge.Controllers
{
    /// <summary>
    /// serve a static xml file for SSDP device discovery service
    /// </summary>

    [Produces("application/xml")]
    public class DescriptionController : Controller
    {
        private string macAddr;
        private string ipAddr;

        public DescriptionController(IGlobalResourceProvider grp)
        {
            macAddr = grp.CommInterface.NativeInfo.GetPhysicalAddress().ToString();
            ipAddr = grp.CommInterface.SocketLiteInfo.IpAddress;
        }

        [ResponseCache(Duration = 7200)]
        [Route("/description.xml")]
        [HttpGet]
        public Description GetDescription() => new Description
        {
            SpecVersion = new Version { Major = 1, Minor = 0 },
            URLBase = $"http://{ipAddr}",
            Device = new Device
            {
                DeviceType = "urn:schemas-upnp-org:device:Basic:1",
                FriendlyName = $"Philips hue ({ipAddr})",
                Manufacturer = "Royal Philips Electronics",
                ManufacturerURL = "http://www.philips.com",
                ModelDescription = "Philips hue Personal Wireless Lighting",
                ModelName = "Philips hue bridge 2015",
                ModelNumber = "BSB002",
                ModelURL = "http://www.meethue.com",
                SerialNumber = macAddr.ToUpper(),
                UDN = $"uuid:2f402f80-da50-11e1-9b23-{macAddr.ToLower()}",
                PresentationURL = "index.html",
                IconList = new List<Icon>
                    {
                        new Icon
                        {
                            MimeType = "image/png",
                            Height = 48,
                            Width = 48,
                            Depth = 24,
                            URL = "hue_logo_0.png"
                        }
                    }
            }
        };
    }

    [XmlRoot("root", Namespace = "urn:schemas-upnp-org:device-1-0")]
    public class Description
    {
        [XmlElement("specVersion")]
        public Version SpecVersion { get; set; }
        public string URLBase { get; set; }
        [XmlElement("device")]
        public Device Device { get; set; }
    }

    public class Version
    {
        [XmlElement("major")]
        public int Major { get; set; }
        [XmlElement("minor")]
        public int Minor { get; set; }
    }

    public class Icon
    {
        [XmlElement("mimetype")]
        public string MimeType { get; set; }
        [XmlElement("height")]
        public int Height { get; set; }
        [XmlElement("width")]
        public int Width { get; set; }
        [XmlElement("depth")]
        public int Depth { get; set; }
        [XmlElement("url")]
        public string URL { get; set; }
    }

    public class Device
    {
        [XmlElement("deviceType")]
        public string DeviceType { get; set; }
        [XmlElement("friendlyName")]
        public string FriendlyName { get; set; }
        [XmlElement("manufacturer")]
        public string Manufacturer { get; set; }
        [XmlElement("manufacturerURL")]
        public string ManufacturerURL { get; set; }
        [XmlElement("modelDescription")]
        public string ModelDescription { get; set; }
        [XmlElement("modelName")]
        public string ModelName { get; set; }
        [XmlElement("modelNumber")]
        public string ModelNumber { get; set; }
        [XmlElement("modelURL")]
        public string ModelURL { get; set; }
        [XmlElement("serialNumber")]
        public string SerialNumber { get; set; }
        [XmlElement("UDN")]
        public string UDN { get; set; }
        [XmlElement("presentationURL")]
        public string PresentationURL { get; set; }
        [XmlArray("iconList"), XmlArrayItem(typeof(Icon), ElementName = "icon")]
        public List<Icon> IconList { get; set; }
    }
}
