using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml;

namespace Emulators.PlatformImporter
{
    public class PlatformInfo
    {
        public string Title { get; set; }
        public string Developer { get; set; }
        public string Grade { get; set; }

        public string Overview { get; set; }
        public string CPU { get; set; }
        public string Memory { get; set; }
        public string Graphics { get; set; }
        public string Sound { get; set; }
        public string Display { get; set; }
        public string Media { get; set; }
        public string MaxControllers { get; set; }

        List<string> imageUrls = new List<string>();
        public List<string> ImageUrls { get { return imageUrls; } }
        public string LogoUrl
        {
            get { return imageUrls.Count != 0 ? imageUrls[0] : null; }
        }

        List<string> fanartUrls = new List<string>();
        public List<string> FanartUrls { get { return fanartUrls; } }
        public string FanartUrl 
        { 
            get { return fanartUrls.Count != 0 ? fanartUrls[0] : null; } 
        }

        public string GetDescription()
        {

            StringBuilder info = new StringBuilder();
            if (!string.IsNullOrEmpty(Overview))
                info.Append(Overview + "\r\n\r\n");

            string format = "{0}: {1}\r\n";
            if (!string.IsNullOrEmpty(CPU))
                info.AppendFormat(format, "CPU", CPU);

            if (!string.IsNullOrEmpty(Memory))
                info.AppendFormat(format, "Memory", Memory);

            if (!string.IsNullOrEmpty(Graphics))
                info.AppendFormat(format, "Graphics", Graphics);

            if (!string.IsNullOrEmpty(Sound))
                info.AppendFormat(format, "Sound", Sound);

            if (!string.IsNullOrEmpty(Display))
                info.AppendFormat(format, "Display", Display);

            if (!string.IsNullOrEmpty(Media))
                info.AppendFormat(format, "Media", Media);

            if (!string.IsNullOrEmpty(MaxControllers))
                info.AppendFormat(format, "Max Controllers", MaxControllers);

            return info.ToString().Trim();
        }
    }
}
