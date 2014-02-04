using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emulators.Import;
using Emulators.Database;

namespace Emulators
{
    public interface ISettingsProvider
    {
        ISQLiteProvider DataProvider { get; }
        ILog Logger { get; }
        string OptionsPath { get; }
        string DataPath { get; }
        string DefaultThumbDirectory { get; }
        string EmptySubGroupName { get; }
    }

    public class EmulatorsSettings : IDisposable
    {
        #region Wildcards
        public const string ROM_DIRECTORY_WILDCARD = "%ROMDIRECTORY%";
        public const string GAME_WILDCARD = "%ROM%";
        public const string GAME_WILDCARD_NO_EXT = "%ROM_WITHOUT_EXT%";
        #endregion
        
        public void Init(ISettingsProvider settings)
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = 100;
            Settings = settings;
            RomGroup.EmptySubGroupName = settings.EmptySubGroupName;
            DB.Instance.DataProvider = settings.DataProvider;
            DB.Instance.Init();
            initThumbDir(settings.DefaultThumbDirectory);
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

        public ISettingsProvider Settings
        {
            get;
            private set;
        }

        public ILog Logger
        {
            get { return Settings.Logger; }
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
        static EmulatorsSettings instance = null;
        public static EmulatorsSettings Instance
        {
            get
            {
                if (instance == null)
                    lock (instanceLock)
                        if (instance == null)
                            instance = new EmulatorsSettings();
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
