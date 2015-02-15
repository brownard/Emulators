using Emulators.Database;
using Emulators.ImageHandlers;
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
        /// <summary>
        /// The name to use for any sub-groups that would otherwise have an empty name, e.g 'Unknown'
        /// </summary>
        public static string EmptySubGroupName
        {
            get;
            set;
        }

        #region Default Groups

        static List<RomGroup> createDefaultGroups()
        {
            List<RomGroup> groups = new List<RomGroup>();

            RomGroup group = new RomGroup("Favourites", "favourite=1", "LOWER(title), title");
            group.Favourite = true;
            groups.Add(group);

            group = new RomGroup("Emulators");
            group.GroupItemInfos.Add(GroupItemInfo.CreateEmulatorGroup(GroupItemInfo.ALL_ITEMS_ID));
            groups.Add(group);

            group = new RomGroup("All Games", null, "LOWER(title), title");
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

            EmulatorsCore.Database.BeginTransaction();
            for (int x = 0; x < groups.Count; x++)
            {
                groups[x].Position = x;
                groups[x].Commit();
            }
            EmulatorsCore.Database.EndTransaction();

            return groups;
        }

        #endregion

        #region Get Groups

        /// <summary>
        /// Retrieves all RomGroups currently stored in the database
        /// </summary>
        /// <returns></returns>
        public static List<RomGroup> GetAll()
        {
            List<RomGroup> groups = EmulatorsCore.Database.GetAll<RomGroup>();
            if (groups.Count < 1)
                groups = createDefaultGroups();
            return groups;
        }

        #endregion

        #region Ctor

        public RomGroup() { }

        public RomGroup(string title, string sql = null, string orderBy = null)
        {
            if (title == null)
                title = "";
            Title = title;
            if (sql != null || orderBy != null)
                GroupItemInfos.Add(GroupItemInfo.CreateSQLGroup(sql, orderBy));
        }

        #endregion

        #region Public Properties

        string title = "";
        /// <summary>
        /// The display name of the group
        /// </summary>
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
        /// <summary>
        /// The list position of the group
        /// </summary>
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
        /// <summary>
        /// Whether the group holds user favourites
        /// </summary>
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
        /// <summary>
        /// The last used layout of the group
        /// </summary>
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
        /// <summary>
        /// The property of the underlying items used to sort
        /// </summary>
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
        /// <summary>
        /// Whether to sort in descending order
        /// </summary>
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

        bool checkedThumbs = false;
        ThumbGroup thumbs = null;
        /// <summary>
        /// The ThumbGroup of a randomly selected sub-item
        /// </summary>
        public ThumbGroup ThumbGroup
        {
            get
            {
                if (checkedThumbs)
                    return thumbs;
                checkedThumbs = true;

                //get all GroupItemInfos that may contain ThumbItems
                List<GroupItemInfo> thumbInfos = GroupItemInfos.Where(g => g.ItemType != GroupItemType.DYNAMIC).ToList();
                if (thumbInfos.Count == 0)
                    return null;

                ThumbItem thumbItem = null;
                Random r = new Random();
                int startIndex = r.Next(thumbInfos.Count);
                int tries = 0;
                while (thumbItem == null && tries < thumbInfos.Count)
                {
                    GroupItemInfo info = thumbInfos[startIndex % thumbInfos.Count];
                    List<DBItem> infoItems = info.GetItems(sortProperty);
                    if (infoItems.Count > 0)
                    {
                        thumbItem = (ThumbItem)infoItems[r.Next(infoItems.Count)];
                    }
                    else
                    {
                        startIndex++;
                        tries++;
                    }
                }

                if (thumbItem != null)
                    thumbs = new ThumbGroup(thumbItem);
                return thumbs;
            }
        }

        #endregion

        #region Group Items

        DBRelationList<GroupItemInfo> groupItemInfos = null;
        /// <summary>
        /// The GroupItemInfos used to populate the GroupItems
        /// </summary>
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
        /// <summary>
        /// The items contained in this group
        /// </summary>
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

        #endregion

        #region Public Methods

        /// <summary>
        /// Discards the currently selected ThumbGroup
        /// </summary>
        public void RefreshThumbs()
        {
            checkedThumbs = false;
            if (thumbs != null)
            {
                thumbs.Dispose();
                thumbs = null;
            }
        }

        /// <summary>
        /// Discards the current GroupItems, they will be re-populated when next accessed
        /// </summary>
        public void Refresh()
        {
            groupItems = null;
            RefreshThumbs();
        }

        #endregion

        #region IComparable

        public int CompareTo(RomGroup other)
        {
            return this.position.CompareTo(other.position);
        }

        #endregion
    }
}
