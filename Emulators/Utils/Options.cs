using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Windows.Forms;
using System.Threading;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Emulators
{
    #region Option MetaData

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class OptionAttribute : Attribute
    {
        public bool HasDefault { get; set; }
        object defaultValue;
        public object Default
        {
            get { return defaultValue; }
            set
            {
                HasDefault = true;
                defaultValue = value;
            }
        }
    }

    public class OptionMetadata
    {
        public PropertyInfo PropertyInfo { get; set; }
        public OptionAttribute OptionAttribute { get; set; }
    }

    #endregion

    public class Options
    {
        #region Options PropertyInfo

        static object propertiesSync = new object();
        static Dictionary<Type, Dictionary<string, OptionMetadata>> properties;
        static Dictionary<string, OptionMetadata> GetProperties(Type type)
        {
            lock (propertiesSync)
            {
                if (properties == null)
                    properties = new Dictionary<Type, Dictionary<string, OptionMetadata>>();
                else if (properties.ContainsKey(type))
                    return properties[type];

                Dictionary<string, OptionMetadata> typeProperties = new Dictionary<string, OptionMetadata>();
                foreach (PropertyInfo p in type.GetProperties())
                    foreach (object attr in p.GetCustomAttributes(true))
                    {
                        if (attr.GetType() == typeof(OptionAttribute))
                        {
                            typeProperties[p.Name] = new OptionMetadata() { PropertyInfo = p, OptionAttribute = (OptionAttribute)attr };
                            break;
                        }
                    }
                properties[type] = typeProperties;
                return typeProperties;
            }
        }

        #endregion

        #region Private Members
        
        object optionSync = new object();
        ReaderWriterLockSlim readWriteLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        List<string> ignoredFiles = null;

        #endregion

        #region Init

        public virtual void Init()
        {
            ignoredFiles = new List<string>();
            setDefaults();
            Load();
        }

        void setDefaults()
        {
            foreach (OptionMetadata meta in GetProperties(this.GetType()).Values)
                if (meta.OptionAttribute.HasDefault)
                    meta.PropertyInfo.GetSetMethod().Invoke(this, new[] { meta.OptionAttribute.Default });
        }

        #endregion

        #region Load/Save

        public string SavePath { get; set; }

        public virtual void Load()
        {
            string sourcePath = SavePath;
            if (string.IsNullOrEmpty(sourcePath))
                return;

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(sourcePath); //get the xml file

                XmlNodeList nodes = doc.GetElementsByTagName("option"); //select option node
                if (nodes.Count == 0)
                    return;
                var optionProperties = GetProperties(this.GetType());

                //loop through each option and determine the option Type
                foreach (XmlNode node in nodes)
                {
                    XmlNode attr = node.Attributes.GetNamedItem("property");
                    if (attr == null || string.IsNullOrEmpty(attr.Value) || !optionProperties.ContainsKey(attr.Value))
                        continue;

                    string name = attr.Value;
                    string strValue = node.InnerText;

                    PropertyInfo property = optionProperties[name].PropertyInfo;
                    Type type = property.PropertyType;

                    object value;
                    if (strValue == "")
                    {
                        if (type.IsValueType && Nullable.GetUnderlyingType(type) == null)
                            continue;
                        value = null;
                    }
                    else if (type == typeof(string))
                    {
                        if (strValue.Trim() == "")
                            strValue = strValue.Substring(1);
                        value = strValue;
                    }
                    else if (type == typeof(bool) || type == typeof(Nullable<bool>))
                    {
                        value = bool.Parse(strValue);
                    }
                    else if (type == typeof(int) || type == typeof(Nullable<int>))
                    {
                        value = int.Parse(strValue, CultureInfo.InvariantCulture);
                    }
                    else if (type == typeof(double) || type == typeof(Nullable<double>))
                    {
                        value = double.Parse(strValue, CultureInfo.InvariantCulture);
                    }
                    else if (type.IsEnum)
                    {
                        value = Enum.Parse(type, strValue);
                    }
                    else
                    {
                        continue;
                    }

                    property.GetSetMethod().Invoke(this, new[] { value });
                }

                foreach (XmlNode node in doc.GetElementsByTagName("ignorefile"))
                {
                    string path = node.Attributes.GetNamedItem("path").Value;
                    if (!ignoredFiles.Contains(path))
                        ignoredFiles.Add(path);
                }
            }
            catch (Exception ex) { Logger.LogError(ex); }
        }

        public virtual void Save()
        {
            string sourcePath = SavePath;
            if (string.IsNullOrEmpty(sourcePath))
                return;

            XmlDocument doc = new XmlDocument();
            XmlNode headNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(headNode);

            XmlNode options = doc.CreateElement("options");
            doc.AppendChild(options);

            var properties = GetProperties(this.GetType());
            readWriteLock.EnterReadLock();
            foreach (OptionMetadata meta in properties.Values)
            {
                XmlElement option = doc.CreateElement("option");
                XmlAttribute attr = doc.CreateAttribute("property");
                attr.Value = meta.PropertyInfo.Name;
                option.Attributes.Append(attr);
                object value = meta.PropertyInfo.GetGetMethod().Invoke(this, null);
                option.InnerText = createOptionString(value, meta.PropertyInfo.PropertyType);
                options.AppendChild(option);
            }
            readWriteLock.ExitReadLock();

            createIgnoreNodes(options, doc);
            try { doc.Save(sourcePath); }
            catch (Exception ex)
            {
                Logger.LogError("Options: Failed to save options to {0} - {1}", sourcePath, ex.Message);
            }
        }

        static string createOptionString(object option, Type type)
        {
            if (option == null)
                return "";
            else if (type == typeof(string))
            {
                string s = (string)option;
                if (s == "" || s.Trim() == "")
                    s += " ";
                return s;
            }
            else if (type == typeof(int) || type == typeof(Nullable<int>))
            {
                return ((int)option).ToString(CultureInfo.InvariantCulture);
            }
            else if (type == typeof(double) || type == typeof(Nullable<double>))
            {
                return ((double)option).ToString(CultureInfo.InvariantCulture);
            }
            return option.ToString();
        }

        void createIgnoreNodes(XmlNode parent, XmlDocument parentDoc)
        {
            foreach (string file in ignoredFiles)
            {
                XmlNode fileNode = parentDoc.CreateElement("ignorefile");
                XmlAttribute path = parentDoc.CreateAttribute("path");
                path.Value = file;
                fileNode.Attributes.Append(path);
                parent.AppendChild(fileNode);
            }
        }

        #endregion

        #region Read/Write Locking

        public void EnterReadLock()
        {
            readWriteLock.EnterReadLock();
        }

        public void ExitReadLock()
        {
            readWriteLock.ExitReadLock();
        }

        public void EnterWriteLock()
        {
            readWriteLock.EnterWriteLock();
        }

        public void ExitWriteLock()
        {
            readWriteLock.ExitWriteLock();
        }

        public T ReadOption<T>(Func<Options, T> reader)
        {
            readWriteLock.EnterReadLock();
            T value = reader(this);
            readWriteLock.ExitReadLock();
            return value;
        }

        public void WriteOption(Action<Options> writer)
        {
            readWriteLock.EnterWriteLock();
            writer(this);
            readWriteLock.ExitWriteLock();
        }

        #endregion

        #region Options
        /// <summary>
        /// The display name of the plugin
        /// </summary>
        [OptionAttribute(Default = "Emulators")]
        public string PluginDisplayName { get; set; }
        /// <summary>
        /// The language file to use
        /// </summary>
        [OptionAttribute(Default = "English")]
        public string Language { get; set; }
        /// <summary>
        /// The view to use when starting the plugin
        /// </summary>
        [OptionAttribute(Default = StartupState.GROUPS)]
        public StartupState StartupState { get; set; }
        /// <summary>
        /// The last used view
        /// </summary>
        [OptionAttribute(Default = StartupState.GROUPS)]
        public StartupState LastStartupState { get; set; }
        /// <summary>
        /// Whether to display the details view when a game is selected
        /// </summary>
        [OptionAttribute(Default = true)]
        public bool ClickToDetails { get; set; }
        /// <summary>
        /// The layout to use when viewing emulators
        /// </summary>
        [OptionAttribute(Default = 0)]
        public int EmulatorLayout { get; set; }
        /// <summary>
        /// The layout to use when viewing PC games
        /// </summary>
        [OptionAttribute(Default = 0)]
        public int PCGamesLayout { get; set; }
        /// <summary>
        /// The layout to use when viewing emulator roms
        /// </summary>
        [OptionAttribute(Default = 0)]
        public int GamesLayout { get; set; }
        /// <summary>
        /// The layout to use when viewing favourites
        /// </summary>
        [OptionAttribute(Default = 0)]
        public int FavouritesLayout { get; set; }
        /// <summary>
        /// Whether to display the sort value when sorting
        /// </summary>
        [OptionAttribute(Default = true)]
        public bool ShowSortValue { get; set; }
        /// <summary>
        /// Whether to display fanart
        /// </summary>
        [OptionAttribute(Default = true)]
        public bool ShowFanart { get; set; }
        /// <summary>
        /// Whether to display gameart
        /// </summary>
        [OptionAttribute(Default = true)]
        public bool ShowGameart { get; set; }
        /// <summary>
        /// The delay before fanart is displayed (ms)
        /// </summary>
        [OptionAttribute(Default = 500)]
        public int FanartDelay { get; set; }
        /// <summary>
        /// The delay before gameart is displayed (ms)
        /// </summary>
        [OptionAttribute(Default = 500)]
        public int GameartDelay { get; set; }
        /// <summary>
        /// Whether to auto refresh the database on plugin start
        /// </summary>
        [OptionAttribute(Default = true)]
        public bool AutoRefreshGames { get; set; }
        /// <summary>
        /// Whether to auto refresh the database on plugin start
        /// </summary>
        [OptionAttribute(Default = true)]
        public bool AutoImportGames { get; set; }
        /// <summary>
        /// Whether to auto select the top search result when a better match is not found
        /// </summary>
        [OptionAttribute(Default = false)]
        public bool ImportTop { get; set; }
        /// <summary>
        /// Whether to only approve exact title and platform match
        /// </summary>
        [OptionAttribute(Default = false)]
        public bool ImportExact { get; set; }
        /// <summary>
        /// Whether to resize gameart to the correct aspect ratio for the platform
        /// </summary>
        [OptionAttribute(Default = true)]
        public bool ResizeGameart { get; set; }
        /// <summary>
        /// Whether to search all scrapers recursively until all missing artwork is found
        /// </summary>
        [OptionAttribute(Default = true)]
        public bool TryAndFillMissingArt { get; set; }
        /// <summary>
        /// Directory to store artwork
        /// </summary>
        [OptionAttribute(Default = "")]
        public string ImageDirectory { get; set; }
        /// <summary>
        /// Whether to close the emulator when the mapped key is pressed
        /// </summary>
        [OptionAttribute(Default = false)]
        public bool StopOnMappedKey { get; set; }
        /// <summary>
        /// Mapped key for closing emulator
        /// </summary>
        [OptionAttribute(Default = 0)]
        public int MappedKey { get; set; }
        /// <summary>
        /// The file filters used to determine whether a rom is a Goodmerge archive
        /// </summary>
        [OptionAttribute(Default = "*.7z;*.rar;*.zip;*.tar")]
        public string GoodmergeFilters { get; set; }
        /// <summary>
        /// Whether to display the goodmerge select dialog only on first run of game
        /// </summary>
        [OptionAttribute(Default = true)]
        public bool ShowGoodmergeDialogOnFirstOpen { get; set; }
        /// <summary>
        /// Whether to always display the goodmerge select dialog
        /// </summary>
        [OptionAttribute(Default = false)]
        public bool AlwaysShowGoodmergeDialog { get; set; }
        /// <summary>
        /// The number of background threads to use when running the importer
        /// </summary>
        [OptionAttribute(Default = 5)]
        public int ImportThreads { get; set; }
        /// <summary>
        /// The maximum number of import threads allowed to hash files at the same time, a lower count helps limit cpu usage
        /// </summary>
        [OptionAttribute(Default = 2)]
        public int HashThreads { get; set; }
        /// <summary>
        /// Whether to stop any playing media in MediaPortal when starting a game
        /// </summary>
        [OptionAttribute(Default = true)]
        public bool StopMediaPlayback { get; set; }
        /// <summary>
        /// Whether to play preview videos
        /// </summary>
        [OptionAttribute(Default = false)]
        public bool ShowVideoPreview { get; set; }
        /// <summary>
        /// Whether to loop preview videos
        /// </summary>
        [OptionAttribute(Default = false)]
        public bool LoopVideoPreview { get; set; }
        /// <summary>
        /// Use emulator video if no game video
        /// </summary>
        [OptionAttribute(Default = false)]
        public bool FallBackToEmulatorVideo { get; set; }
        /// <summary>
        /// The delay before starting preview videos (ms)
        /// </summary>
        [OptionAttribute(Default = 2000)]
        public int PreviewVideoDelay { get; set; }
        /// <summary>
        /// Scraper Ids in order of priority 
        /// </summary>
        [OptionAttribute(Default = "1;2;4;3")]
        public string ScraperPriorities { get; set; }
        /// <summary>
        /// Ids of scrapers to ignore
        /// </summary>
        [OptionAttribute(Default = "3")] //GameFaqs, blocks IP address if detects you are scraping
        public string IgnoredScrapers { get; set; }
        /// <summary>
        /// The scraper to use first to find covers
        /// </summary>
        [OptionAttribute(Default = "1")] //TheGamesDB seems to be most consistent and best quality
        public string PriorityCoversScraper { get; set; }
        /// <summary>
        /// The scraper to use first to find screens
        /// </summary>
        [OptionAttribute(Default = "2")] //MobyGames has more screens than TheGamesDB
        public string PriorityScreensScraper { get; set; }
        /// <summary>
        /// The scraper to use first to find fanart
        /// </summary>
        [OptionAttribute(Default = "1")] //TheGamesDB only scraper with fanart
        public string PriorityFanartScraper { get; set; }
        /// <summary>
        /// The maximum width to resize gameart. Lower to save space at cost of quality
        /// </summary>
        [OptionAttribute(Default = 640)]
        public int MaxImageDimensions { get; set; }

        /// <summary>
        /// The path to save backup to
        /// </summary>
        [OptionAttribute(Default = "")]
        public string BackupFile { get; set; }
        /// <summary>
        /// Whether to backup images
        /// </summary>
        [OptionAttribute(Default = true)]
        public bool BackupImages { get; set; }
        /// <summary>
        /// The file to load backup from
        /// </summary>
        [OptionAttribute(Default = "")]
        public string RestoreFile { get; set; }
        /// <summary>
        /// Whether to restore images
        /// </summary>
        [OptionAttribute(Default = true)]
        public bool RestoreImages { get; set; }
        /// <summary>
        /// Whether to merge backup with existing database
        /// </summary>
        [OptionAttribute(Default = true)]
        public bool RestoreMerge { get; set; }
        /// <summary>
        /// How to handle merging emulators
        /// </summary>
        [OptionAttribute(Default = 0)]
        public int MergeEmulatorSetting { get; set; }
        /// <summary>
        /// How to handle merging profiles
        /// </summary>
        [OptionAttribute(Default = 0)]
        public int MergeProfileSetting { get; set; }
        /// <summary>
        /// How to handle merging games
        /// </summary>
        [OptionAttribute(Default = 1)]
        public int MergeGameSetting { get; set; }

        #endregion

        public static string GetKeyDisplayString(int keyData)
        {
            int ctrl = (int)Keys.Control;
            int alt = (int)Keys.Alt;
            int shift = (int)Keys.Shift;

            string retString = "";

            if ((keyData & shift) == shift)
            {
                keyData = keyData ^ shift;
                retString += "Shift + ";
            }
            if ((keyData & ctrl) == ctrl)
            {
                keyData = keyData ^ ctrl;
                retString += "Ctrl + ";
            }
            if ((keyData & alt) == alt)
            {
                keyData = keyData ^ alt;
                retString += "Alt + ";
            }

            return retString + Enum.GetName(typeof(Keys), keyData);
        }

        #region File Ignore List

        public List<string> IgnoredFiles()
        {
            List<string> files = new List<string>();

            lock (optionSync)
                files.AddRange(ignoredFiles);

            return files;
        }

        public bool ShouldIgnoreFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            lock (optionSync)
                return ignoredFiles.Contains(path);
        }

        public void AddIgnoreFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            lock (optionSync)
                if (!ignoredFiles.Contains(path))
                    ignoredFiles.Add(path);
        }

        public void RemoveIgnoreFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            lock (optionSync)
                if (ignoredFiles.Contains(path))
                    ignoredFiles.Remove(path);
        }

        #endregion
    }
}
