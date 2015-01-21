using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Emulators.PlatformImporter;
using System.Xml;

namespace Emulators
{
    public class Dropdowns
    {
        public static List<ComboBoxItem> GetEmuComboBoxItems()
        {
            List<ComboBoxItem> items = new List<ComboBoxItem>();
            items.Add(new ComboBoxItem(null) { Name = "All Systems", ID = -2 });
            foreach (Emulator emu in Emulator.GetAll(true))
                items.Add(new ComboBoxItem(emu));

            return items;
        }

        public static List<ComboBoxItem> GetNewRomComboBoxItems()
        {
            List<ComboBoxItem> items = new List<ComboBoxItem>();
            foreach (Emulator emu in Emulator.GetAll())
                items.Add(new ComboBoxItem(emu));

            return items;
        }

        static object platformSync = new object();
        static List<Platform> platformList;
        public static List<Platform> GetPlatformList()
        {
            lock (platformSync)
            {
                if (platformList != null)
                    return platformList;

                platformList = new List<Platform>();
                XmlDocument doc = new XmlDocument();
                doc.Load(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Emulators.Data.PlatformList.xml"));
                foreach (XmlNode node in doc.SelectNodes("//option"))
                    platformList.Add(new Platform() { Name = node.InnerText, Id = node.Attributes["value"].Value });
                return platformList;
            }
        }
    }
}
