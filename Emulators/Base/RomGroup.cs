using Emulators.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Emulators
{
    public class RomGroup : DBItem
    {
        List<int> gameIds = new List<int>();
        List<int> emuIds = new List<int>();
        List<DBItem> groupItems = null;
                
        public RomGroup(string title, string sql = null, string orderBy = null)
        {
            if (title == null)
                title = "";
            Title = title;

            if (sql != null || orderBy != null)
                groupItemInfos.Add(GroupItemInfo.CreateSQLGroup(sql, orderBy));
        }

        public RomGroup(XmlNode groupNode)
        {
            initFromXml(groupNode);
        }

        #region Init

        void initFromXml(XmlNode groupNode)
        {
            if (groupNode.Attributes == null)
            {
                Logger.LogError("No attributes found for group xml\r\n{0}", groupNode.InnerText);
                return;
            }

            XmlAttribute title = groupNode.Attributes["title"];
            if (title == null)
            {
                Logger.LogError("No title attribute found for group xml\r\n{0}", groupNode.InnerText);
                return;
            }

            isReady = true;
            Title = title.Value;

            XmlAttribute fav = groupNode.Attributes["favourite"];
            if (fav != null)
            {
                bool favourite;
                if (bool.TryParse(fav.Value, out favourite))
                {
                    if (favourite)
                    {
                        layout = Options.Instance.GetIntOption("viewfavourites");
                        Favourite = favourite;
                    }
                }
                else
                    Logger.LogError("Unable to parse favourite attribute '{0}' to boolean", fav.Value);
            }

            if (groupNode.Attributes["sort"] != null)
                getSortProperty(groupNode.Attributes["sort"].Value);
            if (groupNode.Attributes["desc"] != null)
                bool.TryParse(groupNode.Attributes["desc"].Value, out sortDesc);

            if (layout < 0 && groupNode.Attributes["layout"] != null)
                if (!int.TryParse(groupNode.Attributes["layout"].Value, out layout))
                    layout = -1;

            foreach (XmlNode childNode in groupNode.ChildNodes)
            {
                addQuery(childNode);
            }
        }

        void getSortProperty(string p)
        {
            if (string.IsNullOrEmpty(p))
                return;
            try
            {
                sortProperty = (ListItemProperty)Enum.Parse(typeof(ListItemProperty), p, true);
            }
            catch
            {
                Logger.LogError("Error parsing sort property for group '{0}'", Title);
            }
        }

        void addQuery(XmlNode selectedNode)
        {
            if (selectedNode == null || selectedNode.Name != "item" || selectedNode.Attributes == null)
                return;

            XmlAttribute attr = selectedNode.Attributes["type"];
            if (attr == null)
                return;

            int id;
            switch (attr.Value)
            {
                case "SQL":
                    string where = null;
                    string orderBy = null;
                    foreach (XmlNode childNode in selectedNode.ChildNodes)
                    {
                        if (!string.IsNullOrEmpty(childNode.InnerText))
                        {
                            if (childNode.Name == "where")
                                where = childNode.InnerText;
                            else if (childNode.Name == "orderby")
                                orderBy = childNode.InnerText;
                        }
                    }
                    if (where != null || orderBy != null)
                        groupItemInfos.Add(GroupItemInfo.CreateSQLGroup(where, orderBy));
                    break;
                case "Dynamic":
                    XmlAttribute column = selectedNode.Attributes["column"];
                    if (column == null)
                        break;
                    XmlAttribute orderAttr = selectedNode.Attributes["order"];
                    string order = null;
                    if (orderAttr != null)
                        order = orderAttr.Value;
                    groupItemInfos.Add(GroupItemInfo.CreateDynamicGroup(column.Value, order));
                    break;
                case "Emulator":
                    if (int.TryParse(selectedNode.InnerText, out id))
                    {
                        groupItemInfos.Add(GroupItemInfo.CreateEmulatorGroup(id));
                        if (id > -2 && !emuIds.Contains(id))
                            emuIds.Add(id);
                    }
                    break;
                case "Game":
                    if (int.TryParse(selectedNode.InnerText, out id))
                    {
                        groupItemInfos.Add(GroupItemInfo.CreateGameGroup(id));
                        if (id > -2 && !gameIds.Contains(id))
                            gameIds.Add(id);
                    }
                    break;
            }
        }

        void getItems()
        {
            groupItems = new List<DBItem>();
            Dictionary<int?, Emulator> emus = DB.Instance.Get<Emulator>(new ListCriteria(emuIds)).ToDictionary(item => item.Id);
            Dictionary<int?, Game> games = DB.Instance.Get<Game>(new ListCriteria(gameIds)).ToDictionary(item => item.Id);
            foreach (GroupItemInfo info in groupItemInfos)
            {
                switch (info.ItemType)
                {
                    case GroupItemType.SQL:
                        foreach (Game game in DB.Instance.Get<Game>(new SimpleCriteria(info.SQL, info.Order)))
                            groupItems.Add(game);
                        break;
                    case GroupItemType.DYNAMIC:
                        groupItems.AddRange(GetSubGroups(info));
                        break;
                    case GroupItemType.EMULATOR:
                        if (info.Id == -2)
                            groupItems.AddRange(Emulator.GetAll(true));
                        else if (emus.ContainsKey(info.Id))
                            groupItems.Add(emus[info.Id]);
                        break;
                    case GroupItemType.GAME:
                        if (info.Id == -2)
                            groupItems.AddRange(Game.GetAll());
                        else if (games.ContainsKey(info.Id))
                            groupItems.Add(games[info.Id]);
                        break;
                }
            }
        }

        public List<DBItem> GetSubGroups(GroupItemInfo info)
        {
            List<DBItem> groups = new List<DBItem>();

            if (string.IsNullOrEmpty(info.Column))
            {
                Logger.LogError("No column specified for dynamic group");
                return groups;
            }
            if (info.Column == "Genre")
            {
                List<string> genres = DB.Instance.GetAllValues(DBField.GetField(typeof(Game), "Genre")).ToList();
                genres.Sort();
                foreach (string genre in genres)
                {
                    groups.Add(new RomGroup(genre, string.Format(@"'|' || Genre || '|' LIKE '%|{0}|%'", genre)));
                }
                return groups;
            }

            string order = string.IsNullOrEmpty(info.Order) ? "" : info.Order;
            string sql = string.Format("SELECT DISTINCT {0} FROM {1} ORDER BY {0} {2}", info.Column, DB.GetTableName(typeof(Game)), order).Trim();
            Logger.LogDebug("Created sql for dynamic group '{0}' - {1}", Title, sql);

            SQLData results = DB.Instance.Execute(sql);
            foreach (SQLDataRow row in results.Rows)
            {
                try
                {
                    RomGroup newGroup = new RomGroup(row.fields[0] == "" ? GroupHandler.EmptySubGroupName : row.fields[0], string.Format("{0}='{1}'", info.Column, row.fields[0]), "title");
                    newGroup.SortProperty = SortProperty;
                    groups.Add(newGroup);
                }
                catch (Exception ex)
                {
                    Logger.LogError("Error adding subgroup - {0}\r\n{1}", ex.Message, ex.StackTrace);
                }
            }

            return groups;
        }

        #endregion

        #region Public Properties

        bool isReady = false;
        public bool IsReady
        {
            get { return isReady; }
        }

        string title = "";
        public string Title 
        { 
            get { return title; } 
            set { title = value; } 
        }

        public bool Favourite { get; set; }

        int layout = -1;
        public int Layout 
        { 
            get { return layout; } 
            set { layout = value; } 
        }

        ListItemProperty sortProperty = ListItemProperty.DEFAULT;
        public ListItemProperty SortProperty
        {
            get { return sortProperty; }
            set { sortProperty = value; }
        }

        bool sortDesc = false;
        public bool SortDescending 
        { 
            get { return sortDesc; } 
            set { sortDesc = value; } 
        }

        List<GroupItemInfo> groupItemInfos = new List<GroupItemInfo>();
        public List<GroupItemInfo> GroupItemInfos
        {
            get { return groupItemInfos; }
        }

        public List<DBItem> GroupItems
        {
            get
            {
                if (groupItems == null)
                    getItems();
                return groupItems;
            }
        }

        static System.Text.RegularExpressions.Regex orderByRegEx = new System.Text.RegularExpressions.Regex(@"\bORDER BY\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        bool checkedThumbs = false;
        ThumbGroup thumbs = null;
        public ThumbGroup ThumbGroup
        {
            get
            {
                if (checkedThumbs)
                    return thumbs;
                checkedThumbs = true;

                ThumbItem thumbItem = null;
                Random r = new Random();
                int index = r.Next(groupItemInfos.Count);
                int tries = 0;
                while (tries < groupItemInfos.Count)
                {
                    if (index == groupItemInfos.Count)
                        index = 0;
                    tries++;
                    thumbItem = GroupHandler.Instance.GetRandomThumbItem(groupItemInfos[index]);
                    if (thumbItem != null)
                        break;
                }

                if (thumbItem != null)
                    thumbs = new ThumbGroup(thumbItem);
                return thumbs;
            }
        }

        #endregion

        #region Public Methods

        public void RefreshThumbs()
        {
            checkedThumbs = false;
            if (thumbs != null)
            {
                thumbs.Dispose();
                thumbs = null;
            }
        }

        public void Refresh()
        {
            groupItems = null;
            RefreshThumbs();
        }

        public XmlElement GetXML(XmlDocument doc)
        {
            XmlElement group = doc.CreateElement("Group");
            XmlAttribute attr = doc.CreateAttribute("title");
            attr.Value = Title;
            group.Attributes.Append(attr);

            if (Favourite)
            {
                attr = doc.CreateAttribute("favourite");
                attr.Value = "true";
                group.Attributes.Append(attr);
            }

            if (SortProperty != ListItemProperty.NONE)
            {
                attr = doc.CreateAttribute("sort");
                attr.Value = SortProperty.ToString();
                group.Attributes.Append(attr);
            }

            attr = doc.CreateAttribute("desc");
            attr.Value = SortDescending.ToString();
            group.Attributes.Append(attr);

            attr = doc.CreateAttribute("layout");
            attr.Value = layout.ToString();
            group.Attributes.Append(attr);

            foreach (GroupItemInfo info in groupItemInfos)
            {
                XmlElement item = doc.CreateElement("item");
                XmlAttribute typeAttr = doc.CreateAttribute("type");
                item.Attributes.Append(typeAttr);

                switch (info.ItemType)
                {
                    case GroupItemType.SQL:
                        typeAttr.Value = "SQL";
                        if (!string.IsNullOrEmpty(info.SQL))
                        {
                            XmlElement whereNode = doc.CreateElement("where");
                            whereNode.AppendChild(doc.CreateCDataSection(info.SQL));
                            item.AppendChild(whereNode);
                        }
                        if (!string.IsNullOrEmpty(info.Order))
                        {
                            XmlElement orderNode = doc.CreateElement("orderby");
                            orderNode.AppendChild(doc.CreateCDataSection(info.Order));
                            item.AppendChild(orderNode);
                        }
                        break;
                    case GroupItemType.DYNAMIC:
                        typeAttr.Value = "Dynamic";
                        XmlAttribute columnAttr = doc.CreateAttribute("column");
                        columnAttr.Value = info.Column;
                        item.Attributes.Append(columnAttr);
                        if (!string.IsNullOrEmpty(info.Order))
                        {
                            XmlAttribute orderAttr = doc.CreateAttribute("order");
                            orderAttr.Value = info.Order;
                            item.Attributes.Append(orderAttr);
                        }
                        break;
                    case GroupItemType.EMULATOR:
                        typeAttr.Value = "Emulator";
                        item.AppendChild(doc.CreateTextNode(info.Id.ToString()));
                        break;
                    case GroupItemType.GAME:
                        typeAttr.Value = "Game";
                        item.AppendChild(doc.CreateTextNode(info.Id.ToString()));
                        break;
                }
                group.AppendChild(item);
            }
            return group;
        }

        #endregion
    }    
}
