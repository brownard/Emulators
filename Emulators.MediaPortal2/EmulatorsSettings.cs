using MediaPortal.Common;
using MediaPortal.Common.PathManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.MediaPortal2
{
    class EmulatorsSettings : ISettingsProvider
    {
        public EmulatorsSettings()
        {
            dataPath = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\Emulators");

            string databasePath = Path.Combine(dataPath, "Emulators2_v2.db3");
            string optionsPath = Path.Combine(dataPath, "Emulators_2.xml");
            defaultThumbDirectory = Path.Combine(dataPath, "Images");

            options = new Emulators.Options() { SavePath = optionsPath };
            dataProvider = new SQLProvider(databasePath);
            logger = new MP2Logger();
        }

        ISQLiteProvider dataProvider;
        public ISQLiteProvider DataProvider
        {
            get { return dataProvider; }
        }

        ILog logger;
        public ILog Logger
        {
            get { return logger; }
        }

        Options options;
        public Options Options
        {
            get { return options; }
        }

        string dataPath;
        public string DataPath
        {
            get { return dataPath; }
        }

        string defaultThumbDirectory;
        public string DefaultThumbDirectory
        {
            get { return defaultThumbDirectory; }
        }

        public string EmptySubGroupName
        {
            get { return "Unknown"; }
        }
    }
}