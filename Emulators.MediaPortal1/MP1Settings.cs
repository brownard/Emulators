using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Configuration;

namespace Emulators.MediaPortal1
{
    class MP1Settings : ISettingsProvider
    {
        MP1DataProvider dataProvider = new MP1DataProvider(Config.GetFile(Config.Dir.Database, "Emulators2_v2.db3"));
        public ISQLiteProvider DataProvider
        {
            get { return dataProvider; }
        }

        MP1Logger logger = new MP1Logger();
        public ILog Logger
        {
            get { return logger; }
        }

        string optionsPath = Config.GetFile(Config.Dir.Config, "Emulators_2.xml");
        public string OptionsPath
        {
            get { return optionsPath; }
        }

        string dataPath = System.IO.Path.Combine(Config.GetFolder(Config.Dir.Config), "Emulators");
        public string DataPath
        {
            get { return dataPath; }
        }

        string defaultThumbDirectory = Config.GetFolder(Config.Dir.Thumbs);
        public string DefaultThumbDirectory
        {
            get { return defaultThumbDirectory; }
        }

        public string EmptySubGroupName
        {
            get { return Translator.Instance.unknown; }
        }
    }
}
