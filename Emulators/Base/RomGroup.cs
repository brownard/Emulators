using Emulators.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Emulators
{
    [DBTable("Groups")]
    public class RomGroup : DBItem, IComparable<RomGroup>
    {
        public static string EmptySubGroupName 
        { 
            get; 
            set; 
        }

        public static List<RomGroup> GetAll()
        {
            List<RomGroup> groups = DB.Instance.GetAll<RomGroup>();
            if (groups.Count < 1)
            {
                RomGroup group = new RomGroup("Favourites", "favourite=1", "LOWER(title), title");
                group.Favourite = true;
                groups.Add(group);

                group = new RomGroup("Emulators");
                group.GroupItemInfos.Add(GroupItemInfo.CreateEmulatorGroup(-2));
                groups.Add(group);

                group = new RomGroup("All Games", "LOWER(title), title");
                groups.Add(group);

                group = new RomGroup("Top Rated", null, "grade DESC, LOWER(title), title LIMIT 10")
                {
                    SortProperty = ListItemProperty.GRADE,
                    SortDescending = true
                };
                groups.Add(group);

                group = new RomGroup("Recently Played", "playcount > 0", "latestplay DESC LIMIT 10")
                {
                    SortProperty = ListItemProperty.LASTPLAYED,
                    SortDescending = true
                };
                groups.Add(group);

                group = new RomGroup("Developer");
                group.GroupItemInfos.Add(GroupItemInfo.CreateDynamicGroup("Developer", null));
                groups.Add(group);

                group = new RomGroup("Year");
                group.GroupItemInfos.Add(GroupItemInfo.CreateDynamicGroup("Year", "DESC"));
                groups.Add(group);

                group = new RomGroup("Genre");
                group.GroupItemInfos.Add(GroupItemInfo.CreateDynamicGroup("Genre", null));
                groups.Add(group);

                DB.Instance.BeginTransaction();
                for (int x = 0; x < groups.Count; x++)
                {
                    groups[x].Position = x;
                    groups[x].Commit();
                }
                DB.Instance.EndTransaction();
            }
            return groups;
        }

        public RomGroup() { }
        public RomGroup(string title, string sql = null, string orderBy = null)
        {
            if (title == null)
                title = "";
            Title = title;
            if (sql != null || orderBy != null)
                GroupItemInfos.Add(GroupItemInfo.CreateSQLGroup(sql, orderBy));
        }

        #region Public Properties

        string title = "";
        [DBField]
        public string Title 
        { 
            get { return title; } 
            set 
            { 
                title = value;
                CommitNeeded = true;
            } 
        }

        int position = 0;
        [DBField]
        public int Position
        {
            get { return position; }
            set
            {
                position = value;
                CommitNeeded = true;
            }
        }

        bool favourite = false;
        [DBField]
        public bool Favourite 
        {
            get { return favourite; }
            set
            {
                favourite = value;
                CommitNeeded = true;
            }
        }

        int layout = -1;
        [DBField]
        public int Layout 
        { 
            get { return layout; } 
            set 
            { 
                layout = value;
                CommitNeeded = true;
            } 
        }

        ListItemProperty sortProperty = ListItemProperty.DEFAULT;
        [DBField]
        public ListItemProperty SortProperty
        {
            get { return sortProperty; }
            set
            {
                sortProperty = value;
                CommitNeeded = true;
            }
        }

        bool sortDesc = false;
        [DBField]
        public bool SortDescending 
        { 
            get { return sortDesc; } 
            set 
            { 
                sortDesc = value;
                CommitNeeded = true;
            } 
        }

        DBRelationList<GroupItemInfo> groupItemInfos = null;
        [DBRelation(AutoRetrieve = true)]
        public DBRelationList<GroupItemInfo> GroupItemInfos
        {
            get 
            {
                if (groupItemInfos == null)
                    groupItemInfos = new DBRelationList<GroupItemInfo>(this);
                return groupItemInfos;
            }
        }

        List<DBItem> groupItems = null;
        public List<DBItem> GroupItems
        {
            get
            {
                if (groupItems == null)
                {
                    groupItems = new List<DBItem>();
                    foreach (GroupItemInfo info in GroupItemInfos)
                        groupItems.AddRange(info.GetItems(sortProperty));
                }
                return groupItems;
            }
        }

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
                List<GroupItemInfo> thumbInfos = GroupItemInfos.Where(g => g.ItemType != GroupItemType.DYNAMIC).ToList();
                Random r = new Random();
                int tries = 0;
                while (thumbItem == null && tries < thumbInfos.Count)
                {
                    GroupItemInfo info = thumbInfos[r.Next(thumbInfos.Count)];
                    List<DBItem> infoItems = info.GetItems(sortProperty);
                    if (infoItems.Count > 0)
                        thumbItem = (ThumbItem)infoItems[new Random().Next(infoItems.Count)];
                    else
                        tries++;
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

        #endregion

        public int CompareTo(RomGroup other)
        {
            return this.position.CompareTo(other.position);
        }
    }    
}
