using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;

namespace Emulators.PlatformImporter
{
    public class TheGamesDBImporter : IPlatformImporter
    {
        #region Consts

        const string PLATFORM_LIST_API_URL = "http://thegamesdb.net/api/GetPlatformsList.php";
        const string PLATFORM_API_URL = "http://thegamesdb.net/api/GetPlatform.php?id=";

        static readonly Dictionary<string, string> platformLoopup = new Dictionary<string, string>()
        {
            {"Macintosh", "Mac OS"},
            {"Xbox", "Microsoft Xbox"},
            {"Xbox 360", "Microsoft Xbox 360"},
            {"NES", "Nintendo Entertainment System (NES)"},
            {"Game Boy", "Nintendo Game Boy"},
            {"Game Boy Advance", "Nintendo Game Boy Advance"},
            {"Game Boy Color", "Nintendo Game Boy Color"},
            {"GameCube", "Nintendo GameCube"},
            {"Wii", "Nintendo Wii"},
            {"Wii U", "Nintendo Wii"}, 
            {"Windows", "PC"},
            {"Dreamcast", "Sega Dreamcast"},
            {"Game Gear", "Sega Game Gear"},
            {"Genesis", "Sega Genesis"},
            {"Playstation", "Sony Playstation"},
            {"Playstation 2", "Sony Playstation 2"},
            {"Playstation 3", "Sony Playstation 3"},
            {"Playstation Vita", "Sony Playstation Vita"},
            {"PSP", "Sony PSP"},
            {"SNES", "Super Nintendo (SNES)"}
        };

        #endregion

        #region Private Members

        object syncRoot = new object();
        ReadOnlyCollection<Platform> platformList;
        Dictionary<string, PlatformInfo> platformInfoDictionary;

        #endregion

        #region IPlatformImporter

        public ReadOnlyCollection<Platform> GetPlatformList()
        {
            lock (syncRoot)
            {
                if (platformList != null)
                    return platformList;

                List<Platform> platforms = new List<Platform>();
                XmlDocument doc = loadDocument(PLATFORM_LIST_API_URL);
                if (doc != null)
                    foreach (XmlNode node in doc.SelectNodes("//Platform"))
                        platforms.Add(platformFromXml(node));
                platformList = platforms.AsReadOnly();
                return platformList;
            }
        }

        public Platform GetPlatformByName(string platformName)
        {
            if (platformLoopup.ContainsKey(platformName))
                platformName = platformLoopup[platformName];
            platformName = platformName.ToLowerInvariant();
            return GetPlatformList().FirstOrDefault(p => p.Name.ToLowerInvariant() == platformName);
        }

        public PlatformInfo GetPlatformInfo(string id)
        {
            lock (syncRoot)
            {
                if (platformInfoDictionary == null)
                    platformInfoDictionary = new Dictionary<string, PlatformInfo>();
                else if (platformInfoDictionary.ContainsKey(id))
                    return platformInfoDictionary[id];
            }
            XmlDocument doc = loadDocument(PLATFORM_API_URL + id);
            if (doc != null)
            {
                XmlNode platformNode = doc.SelectSingleNode("/Data/Platform");
                if (platformNode != null)
                {
                    string baseImageUrl = null;
                    XmlNode imageNode = doc.SelectSingleNode("/Data/baseImgUrl");
                    if (imageNode != null)
                        baseImageUrl = imageNode.InnerText;

                    PlatformInfo platformInfo = platformInfoFromXml(platformNode, baseImageUrl);
                    lock (syncRoot)
                        platformInfoDictionary[id] = platformInfo;
                    return platformInfo;
                }
            }
            return null;
        }

        #endregion

        #region Static Methods

        static XmlDocument loadDocument(string url)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(url);
            }
            catch (Exception ex)
            {
                Logger.LogError("TheGamesDB: Error retrieving info from '{0}' - {1}", url, ex.Message);
                return null;
            }
            return doc;
        }

        static Platform platformFromXml(XmlNode node)
        {
            Platform platform = new Platform();
            XmlNode value = node.SelectSingleNode("./id");
            if (value != null)
                platform.Id = value.InnerText;
            value = node.SelectSingleNode("./name");
            if (value != null)
                platform.Name = value.InnerText;
            return platform;
        }

        static PlatformInfo platformInfoFromXml(XmlNode node, string baseImageUrl)
        {
            PlatformInfo platformInfo = new PlatformInfo();

            XmlNode value = node.SelectSingleNode("./Platform");
            if (value != null)
                platformInfo.Title = HttpUtility.HtmlDecode(value.InnerText);

            value = node.SelectSingleNode("./developer");
            if (value != null)
                platformInfo.Developer = HttpUtility.HtmlDecode(value.InnerText);

            value = node.SelectSingleNode("./Rating");
            if (value != null)
                platformInfo.Grade = HttpUtility.HtmlDecode(value.InnerText);

            value = node.SelectSingleNode("./overview");
            if (value != null)
                platformInfo.Overview = HttpUtility.HtmlDecode(value.InnerText);

            value = node.SelectSingleNode("./cpu");
            if (value != null)
                platformInfo.CPU = HttpUtility.HtmlDecode(value.InnerText);

            value = node.SelectSingleNode("./memory");
            if (value != null)
                platformInfo.Memory = HttpUtility.HtmlDecode(value.InnerText);

            value = node.SelectSingleNode("./graphics");
            if (value != null)
                platformInfo.Graphics = HttpUtility.HtmlDecode(value.InnerText);

            value = node.SelectSingleNode("./sound");
            if (value != null)
                platformInfo.Sound = HttpUtility.HtmlDecode(value.InnerText);

            value = node.SelectSingleNode("./display");
            if (value != null)
                platformInfo.Display = HttpUtility.HtmlDecode(value.InnerText);

            value = node.SelectSingleNode("./media");
            if (value != null)
                platformInfo.Media = HttpUtility.HtmlDecode(value.InnerText);

            value = node.SelectSingleNode("./maxcontrollers");
            if (value != null)
                platformInfo.MaxControllers = HttpUtility.HtmlDecode(value.InnerText);

            XmlNode images = node.SelectSingleNode("./Images");
            if (images != null)
            {
                addImagesToList(images.SelectNodes("./boxart"), platformInfo.ImageUrls, baseImageUrl);
                addImagesToList(images.SelectNodes("./consoleart"), platformInfo.ImageUrls, baseImageUrl);
                addImagesToList(images.SelectNodes("./controllerart"), platformInfo.ImageUrls, baseImageUrl);
                addImagesToList(images.SelectNodes("./banner"), platformInfo.ImageUrls, baseImageUrl);
                addImagesToList(images.SelectNodes("./fanart/original"), platformInfo.FanartUrls, baseImageUrl);
            }
            return platformInfo;
        }

        static void addImagesToList(XmlNodeList imageNodes, List<string> images, string baseImageUrl)
        {
            for (int x = 0; x < imageNodes.Count; x++)
                images.Add(baseImageUrl + imageNodes[x].InnerText);
        }

        #endregion
    }
}