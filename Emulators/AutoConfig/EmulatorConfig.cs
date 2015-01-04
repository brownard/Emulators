using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Emulators.AutoConfig
{

    public class EmulatorConfig
    {
        public string Name { get; set; }
        public string Platform { get; set; }
        public double CaseAspect { get; set; }
        public string Filters { get; set; }
        public ProfileConfig ProfileConfig { get; set; }

        public EmulatorConfig(XmlNode parentNode, string platform, double caseAspect, string exeName)
        {
            Platform = platform;
            CaseAspect = caseAspect;

            XmlNode node = parentNode.SelectSingleNode("./@name");
            Name = node != null ? node.Value : exeName;

            node = parentNode.SelectSingleNode("./Filters");
            if (node != null)
                Filters = node.InnerText;

            node = parentNode.SelectSingleNode("./Profile");
            if (node != null)
                ProfileConfig = new ProfileConfig(node);
        }
    }

    public class ProfileConfig
    {
        public string WorkingDirectory { get; set; }
        public string Arguments { get; set; }
        public bool? UseQuotes { get; set; }
        public bool? SuspendMP { get; set; }
        public bool? MountImages { get; set; }
        public bool? EscapeToExit { get; set; }

        public ProfileConfig(XmlNode parentNode)
        {
            Type t = this.GetType();
            foreach (XmlNode profileNode in parentNode.SelectNodes("./*"))
            {
                if (string.IsNullOrEmpty(profileNode.InnerText))
                    continue;

                System.Reflection.PropertyInfo pi;
                try { pi = t.GetProperty(profileNode.Name); }
                catch { pi = null; }
                if (pi == null)
                    continue;

                if (pi.PropertyType == typeof(string))
                {
                    pi.SetValue(this, profileNode.InnerText, null);
                }
                else if (pi.PropertyType == typeof(bool?))
                {
                    bool result;
                    if (bool.TryParse(profileNode.InnerText, out result))
                        pi.SetValue(this, result, null);
                }
            }
        }
    }
}
