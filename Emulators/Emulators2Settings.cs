using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emulators.Import;
using Emulators.Database;

namespace Emulators
{


    public class Emulators2Settings : IDisposable
    {
        #region Wildcards
        public const string ROM_DIRECTORY_WILDCARD = "%ROMDIRECTORY%";
        public const string GAME_WILDCARD = "%ROM%";
        public const string GAME_WILDCARD_NO_EXT = "%ROM_WITHOUT_EXT%";
        #endregion
        
        public void Init(ISQLiteProvider sqlProvider, ILog logger, string optionsPath, string emptySubGroupName, string defaultThumbDirectory)
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = 100;
            ILogger = logger;
            RomGroup.EmptySubGroupName = emptySubGroupName;
            Options.OptionsPath = optionsPath;
            DB.Instance.DataProvider = sqlProvider;
            DB.Instance.Init();
            initThumbDir(defaultThumbDirectory);
        }

        void initThumbDir(string defaultThumbDirectory)
        {
            string location = Options.Instance.GetStringOption("thumblocation");
            if (location == "")
            {
                location = defaultThumbDirectory;
                Options.Instance.UpdateOption("thumblocation", location);
            }
            else if (!System.IO.Directory.Exists(location))
            {
                Logger.LogError("Unable to locate thumb folder '{0}', reverting to default thumb location", location);
                location = defaultThumbDirectory;
                Options.Instance.UpdateOption("thumblocation", location);
            }

            location = location.TrimEnd('\\'); //remove any trailing '\'
            ThumbDirectory = location;            
        }

        public ILog ILogger
        {
            get;
            private set;
        }

        bool isConfig = false;
        public bool IsConfig
        {
            get { return isConfig; }
            set { isConfig = value; }
        }

        public string ThumbDirectory
        {
            get;
            private set;
        }

        static object instanceLock = new object();
        static Emulators2Settings instance = null;
        public static Emulators2Settings Instance
        {
            get
            {
                if (instance == null)
                    lock (instanceLock)
                        if (instance == null)
                            instance = new Emulators2Settings();
                return instance;
            }
        }

        Importer importer = null;
        public Importer Importer
        {
            get
            {
                if (importer == null)
                    importer = new Importer();
                return importer;
            }
        }

        public void Dispose()
        {
            DB.Instance.Dispose();
            Options.Instance.Save();
        }
    }
}
