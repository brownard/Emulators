using Emulators.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators
{
    public class GroupHandler
    {
        public static string GroupsFile { get; set; }
        public static string EmptySubGroupName { get; set; }

        static object instanceSync = new object();
        object syncRoot = new object();

        static GroupHandler instance = null;
        public static GroupHandler Instance
        {
            get
            {
                if (instance == null)
                    lock (instanceSync)
                        if (instance == null)
                            instance = new GroupHandler();
                return instance;
            }
        }

        List<RomGroup> groups = null;
        Dictionary<string, RomGroup> groupNameDict = null;

        public void Init()
        {
            lock (syncRoot)
                init();
        }

        public List<RomGroup> Groups
        {
            get
            {
                lock (syncRoot)
                {
                    if (groups == null)
                        init();
                    return groups;
                }
            }
        }

        public Dictionary<string, RomGroup> GroupNames
        {
            get
            {
                lock (syncRoot)
                {
                    if (groupNameDict == null)
                        init();
                    return groupNameDict;
                }
            }
        }

        void init()
        {
            groups = LoadGroups();
            groupNameDict = new Dictionary<string, RomGroup>();

            foreach (RomGroup group in groups)
            {
                if (!groupNameDict.ContainsKey(group.Title))
                    groupNameDict.Add(group.Title.ToLower(), group);
            }
        }

        public List<RomGroup> LoadGroups()
        {
            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            string xmlPath = GroupsFile;

            bool loaded = false;
            bool error = false;

            if (System.IO.File.Exists(xmlPath))
            {
                try
                {
                    doc.Load(xmlPath);
                    loaded = true;
                }
                catch (Exception ex)
                {
                    loaded = false;
                    error = true;
                    Logger.LogError("Error loading Groups xml from location '{0}' - {1}", xmlPath, ex.Message);
                }
            }

            if (!loaded)
            {
                using (System.IO.Stream stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Emulators.Data.Emulators2Groups.xml"))
                {
                    doc.Load(stream);
                }
                if (!error)
                {
                    try
                    {
                        doc.Save(xmlPath);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Unable to save Group xml to location '{0}' - {1}", xmlPath, ex.Message);
                    }
                }
            }

            List<RomGroup> groups = new List<RomGroup>();
            foreach (System.Xml.XmlNode node in doc.GetElementsByTagName("Group"))
            {
                RomGroup group = new RomGroup(node);
                if (group.IsReady) 
                    groups.Add(group);
            }
            return groups;
        }

        public void SaveGroups(List<RomGroup> groups)
        {
            if (groups == null)
                return;

            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            System.Xml.XmlElement groupsEl = doc.CreateElement("Groups");
            doc.AppendChild(groupsEl);
            foreach (RomGroup group in groups)
                groupsEl.AppendChild(group.GetXML(doc));

            string xmlPath = GroupsFile;

            try
            {
                doc.Save(xmlPath);
            }
            catch (Exception ex)
            {
                Logger.LogError("Unable to save Group xml to location '{0}' - {1}", xmlPath, ex.Message);
            }
        }

        #region Random Artwork

        object artworkCacheSync = new object();
        Dictionary<string, DBItem> sqlRandomArtwork = new Dictionary<string, DBItem>();
        Dictionary<int, DBItem> emuRandomArtwork = new Dictionary<int, DBItem>();
        Dictionary<int, DBItem> gameRandomArtwork = new Dictionary<int, DBItem>();
        static System.Text.RegularExpressions.Regex orderByRegEx = new System.Text.RegularExpressions.Regex(@"\bORDER BY\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        public void ResetThumbCache()
        {
            lock (artworkCacheSync)
            {
                sqlRandomArtwork.Clear();
                emuRandomArtwork.Clear();
                gameRandomArtwork.Clear();
            }
        }

        public DBItem GetRandomThumbItem(GroupItemInfo info)
        {
            if (info == null || info.ItemType == GroupItemType.DYNAMIC)
                return null;

            DBItem thumbItem = null;
            switch (info.ItemType)
            {
                case GroupItemType.SQL:
                    SimpleCriteria criteria = new SimpleCriteria(info.SQL, info.Order);
                    string clause = criteria.GetWhereClause();
                    lock (artworkCacheSync)
                    {
                        if (sqlRandomArtwork.ContainsKey(clause))
                            return sqlRandomArtwork[clause];
                    }
                    List<Game> games = DB.Instance.Get<Game>(criteria);
                    if (games.Count > 0)
                    {
                        thumbItem = games[new Random().Next(games.Count)];
                        lock (artworkCacheSync)
                            sqlRandomArtwork[clause] = thumbItem;
                    }
                    break;
                case GroupItemType.EMULATOR:
                    lock (artworkCacheSync)
                        if (emuRandomArtwork.ContainsKey(info.Id))
                            return emuRandomArtwork[info.Id];

                    if (info.Id == -2)
                    {
                        List<Emulator> emus = DB.Instance.Get<Emulator>(new SimpleCriteria(null, "RANDOM() LIMIT 1"));
                        if (emus.Count > 0)
                            thumbItem = emus[0];
                    }
                    else
                    {
                        thumbItem = DB.Instance.Get<Emulator>(info.Id);
                    }
                    if (thumbItem != null)
                        lock (artworkCacheSync)
                            emuRandomArtwork[info.Id] = thumbItem;
                    break;
                case GroupItemType.GAME:
                    lock (artworkCacheSync)
                        if (gameRandomArtwork.ContainsKey(info.Id))
                            return gameRandomArtwork[info.Id];

                    if (info.Id == -2)
                    {
                        games = DB.Instance.Get<Game>(new SimpleCriteria(null, "RANDOM() LIMIT 1"));
                        if (games.Count > 0)
                            thumbItem = games[0];
                    }
                    else
                    {
                        thumbItem = DB.Instance.Get<Game>(info.Id);
                    }
                    if (thumbItem != null)
                        lock (artworkCacheSync)
                            gameRandomArtwork[info.Id] = thumbItem;
                    break;
            }

            return thumbItem;
        }

        #endregion
    }
}
