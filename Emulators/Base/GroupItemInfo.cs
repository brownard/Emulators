using Emulators.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators
{
    public enum GroupItemType { SQL, DYNAMIC, EMULATOR, GAME }
    [DBTable("GroupItems")]
    public class GroupItemInfo : DBItem, IComparable<GroupItemInfo>
    {
        #region Consts

        public const int ALL_ITEMS_ID = -2;

        #endregion

        #region Ctor

        /// <summary>
        /// Creates a GroupItemInfo that will contain games selected by the specified sql statement
        /// </summary>
        /// <param name="sql">The WHERE clause used to select games from the database</param>
        /// <param name="order">The ORDERBY clause</param>
        public static GroupItemInfo CreateSQLGroup(string sql, string order)
        {
            GroupItemInfo item = new GroupItemInfo();
            item.ItemType = GroupItemType.SQL;
            item.sql = sql;
            item.order = order;
            return item;
        }

        /// <summary>
        /// Creates a GroupItemInfo that will contain the unique values of the specified column
        /// </summary>
        /// <param name="column">The name of the column in the sql table</param>
        /// <param name="order">The ORDERBY clause</param>
        public static GroupItemInfo CreateDynamicGroup(string column, string order)
        {
            GroupItemInfo item = new GroupItemInfo();
            item.ItemType = GroupItemType.DYNAMIC;
            item.column = column;
            item.order = order;
            return item;
        }

        /// <summary>
        /// Creates a GroupItemInfo that will contain the emulator with the specified id
        /// </summary>
        /// <param name="id">The id of the emulator. Use GroupItemInfo.ALL_ITEMS_ID to select all emulators</param>
        public static GroupItemInfo CreateEmulatorGroup(int id)
        {
            GroupItemInfo item = new GroupItemInfo();
            item.ItemType = GroupItemType.EMULATOR;
            item.itemId = id;
            return item;
        }

        /// <summary>
        /// Creates a GroupItemInfo that will contain the game with the specified id
        /// </summary>
        /// <param name="id">The id of the game. Use GroupItemInfo.ALL_ITEMS_ID to select all games</param>
        public static GroupItemInfo CreateGameGroup(int id)
        {
            GroupItemInfo item = new GroupItemInfo();
            item.ItemType = GroupItemType.GAME;
            item.itemId = id;
            return item;
        }

        #endregion

        #region Properties

        GroupItemType itemType = GroupItemType.SQL;
        /// <summary>
        /// Determines how this GroupItemInfo will retrieve items
        /// </summary>
        [DBField]
        public GroupItemType ItemType
        {
            get { return itemType; }
            set
            {
                itemType = value;
                CommitNeeded = true;
            }
        }

        string sql = null;
        /// <summary>
        /// If ItemType is set to SQL, the WHERE clause when selecting games from the database
        /// </summary>
        [DBField]
        public string SQL
        {
            get { return sql; }
            set
            {
                sql = value;
                CommitNeeded = true;
            }
        }

        string column = null;
        /// <summary>
        /// If ItemType is set to DYNAMIC, the name of the column to select unique values from
        /// </summary>
        [DBField]
        public string Column
        {
            get { return column; }
            set
            {
                column = value;
                CommitNeeded = true;
            }
        }

        string order = null;
        /// <summary>
        /// If ItemType is set to SQL or DYNAMIC, the ORDERBY clause when selecting items from the database
        /// </summary>
        [DBField(FieldName="OrderStr")]
        public string Order
        {
            get { return order; }
            set
            {
                order = value;
                CommitNeeded = true;
            }
        }

        int itemId = -2;
        /// <summary>
        /// If ItemType is set to EMULATOR or GAME, the Id of the emulator/game
        /// </summary>
        [DBField]
        public int ItemId
        {
            get { return itemId; }
            set
            {
                itemId = value;
                CommitNeeded = true;
            }
        }

        int position = 0;
        /// <summary>
        /// The list position of this GroupItemInfo
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

        #endregion

        #region Methods

        /// <summary>
        /// Retrieves the DBItems encapsulated by this GroupItemInfo
        /// </summary>
        /// <param name="sortProperty">The sortProperty of the parent RomGroup</param>
        public List<DBItem> GetItems(ListItemProperty sortProperty)
        {
            switch (itemType)
            {
                case GroupItemType.SQL:
                    return EmulatorsCore.Database.Get(typeof(Game), new SimpleCriteria(sql, order));
                case GroupItemType.DYNAMIC:
                    return getSubGroups(sortProperty);
                case GroupItemType.EMULATOR:
                    if (itemId == ALL_ITEMS_ID)
                        return Emulator.GetAll(true).Select(e => (DBItem)e).ToList();
                    else
                        return new List<DBItem> { EmulatorsCore.Database.Get(typeof(Emulator), itemId) };
                case GroupItemType.GAME:
                    if (itemId == ALL_ITEMS_ID)
                        return EmulatorsCore.Database.GetAll(typeof(Game));
                    else
                        return new List<DBItem> { EmulatorsCore.Database.Get(typeof(Game), itemId) };
                default:
                    return new List<DBItem>();
            }
        }

        /// <summary>
        /// Creates sub-groups based on the unique values of the specified column
        /// </summary>
        /// <param name="sortProperty"></param>
        /// <returns></returns>
        List<DBItem> getSubGroups(ListItemProperty sortProperty)
        {
            List<DBItem> groups = new List<DBItem>();

            if (string.IsNullOrEmpty(column))
            {
                Logger.LogError("No column specified for dynamic group");
                return groups;
            }

            //special case for genres as a game may have multiple genres
            if (column == "Genre")
            {
                //get all unique genres
                List<string> genres = EmulatorsCore.Database.GetAllValues(DBField.GetField(typeof(Game), "Genre")).ToList();
                genres.Sort();
                //create groups that will return all games that contain the specified genre
                foreach (string genre in genres)
                    groups.Add(new RomGroup(genre, string.Format(@"'|' || Genre || '|' LIKE '%|{0}|%'", genre)));
                return groups;
            }

            //get all unique values of the specified column
            string order = string.IsNullOrEmpty(this.order) ? "" : " " + this.order;
            string sql = string.Format("SELECT DISTINCT {0} FROM {1} ORDER BY {0}{2}", column, DB.GetTableName(typeof(Game)), order);
            Logger.LogDebug("Created sql for dynamic group '{0}'", sql);
            SQLData results = EmulatorsCore.Database.Execute(sql);

            //create groups that will return all games with the specified column value
            foreach (SQLDataRow row in results.Rows)
            {
                string title = row.fields[0] == "" ? RomGroup.EmptySubGroupName : row.fields[0];
                string groupSql = string.Format("{0}='{1}'", column, row.fields[0]);
                RomGroup newGroup = new RomGroup(title, groupSql, "title") { SortProperty = sortProperty };
                groups.Add(newGroup);
            }

            return groups;
        }

        #endregion

        #region IComparable

        public int CompareTo(GroupItemInfo other)
        {
            return this.position.CompareTo(other.position);
        }

        #endregion
    }
}
