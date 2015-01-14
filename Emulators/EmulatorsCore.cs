using Emulators.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators
{
    public interface ISettingsProvider
    {
        ISQLiteProvider DataProvider { get; }
        ILog Logger { get; }
        Options Options { get; }
        string DataPath { get; }
        string DefaultThumbDirectory { get; }
        string EmptySubGroupName { get; }
    }

    public static class EmulatorsCore
    {
        static volatile bool optionsInit;
        static volatile bool dbInit;
        static object syncRoot = new object();

        public static void Init(ISettingsProvider settings)
        {
            //max concurrent web requests, set to 2 by default which throttles the importer
            //set to a more sensible limit for the modern world :)
            System.Net.ServicePointManager.DefaultConnectionLimit = 100;

            logger = settings.Logger;
            dataPath = settings.DataPath;
            RomGroup.EmptySubGroupName = settings.EmptySubGroupName;

            options = settings.Options ?? new Options();
            database = new DB();
            database.DataProvider = settings.DataProvider;

            if (!string.IsNullOrEmpty(settings.DefaultThumbDirectory))
                initThumbDir(settings.DefaultThumbDirectory);
        }

        public static void DeInit()
        {
            lock (syncRoot)
            {
                database.Dispose();
                options.Save();
            }
        }

        static DB database;
        public static DB Database
        {
            get
            {
                if (!dbInit)
                    lock (syncRoot)
                        if (!dbInit)
                        {
                            database.Init();
                            dbInit = true;
                        }
                return database;
            }
        }

        static Options options;
        public static Options Options
        {
            get
            {
                if (!optionsInit)
                    lock (syncRoot)
                        if (!optionsInit)
                        {
                            options.Init();
                            optionsInit = true;
                        }
                return options;
            }
        }

        static ILog logger;
        public static ILog Logger
        {
            get { return logger; }
        }

        static string dataPath;
        public static string DataPath
        {
            get { return dataPath; }
        }

        static void initThumbDir(string defaultThumbDirectory)
        {
            string location = EmulatorsCore.Options.ReadOption(o => o.ImageDirectory);
            if (location == "")
            {
                location = defaultThumbDirectory;
            }
            else if (!System.IO.Directory.Exists(location))
            {
                Logger.LogError("Unable to locate thumb folder '{0}', reverting to default thumb location", location);
                location = defaultThumbDirectory;
            }
            else
            {
                return;
            }

            location = location.TrimEnd('\\'); //remove any trailing '\'
            EmulatorsCore.Options.WriteOption(o => o.ImageDirectory = location);
        }
    }
}
