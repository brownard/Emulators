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
        public static GroupItemInfo CreateSQLGroup(string sql, string order)
        {
            GroupItemInfo item = new GroupItemInfo();
            item.ItemType = GroupItemType.SQL;
            item.sql = sql;
            item.order = order;
            return item;
        }

        public static GroupItemInfo CreateDynamicGroup(string column, string order)
        {
            GroupItemInfo item = new GroupItemInfo();
            item.ItemType = GroupItemType.DYNAMIC;
            item.column = column;
            item.order = order;
            return item;
        }

        public static GroupItemInfo CreateEmulatorGroup(int id)
        {
            GroupItemInfo item = new GroupItemInfo();
            item.ItemType = GroupItemType.EMULATOR;
            item.itemId = id;
            return item;
        }

        public static GroupItemInfo CreateGameGroup(int id)
        {
            GroupItemInfo item = new GroupItemInfo();
            item.ItemType = GroupItemType.GAME;
            item.itemId = id;
            return item;
        }

        GroupItemType itemType = GroupItemType.SQL;
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

        public List<DBItem> GetItems(ListItemProperty sortProperty)
        {
            switch (itemType)
            {
                case GroupItemType.SQL:
                    return DB.Instance.Get(typeof(Game), new SimpleCriteria(sql, order));
                case GroupItemType.DYNAMIC:
                    return getSubGroups(sortProperty);
                case GroupItemType.EMULATOR:
                    if (itemId == -2)
                        return Emulator.GetAll(true).Select(e => (DBItem)e).ToList();
                    else
                        return new List<DBItem> { DB.Instance.Get(typeof(Emulator), itemId) };
                case GroupItemType.GAME:
                    if (itemId == -2)
                        return DB.Instance.GetAll(typeof(Game));
                    else
                        return new List<DBItem> { DB.Instance.Get(typeof(Game), itemId) };
                default:
                    return new List<DBItem>();
            }
        }

        List<DBItem> getSubGroups(ListItemProperty sortProperty)
        {
            List<DBItem> groups = new List<DBItem>();

            if (string.IsNullOrEmpty(column))
            {
                Logger.LogError("No column specified for dynamic group");
                return groups;
            }

            if (column == "Genre")
            {
                List<string> genres = DB.Instance.GetAllValues(DBField.GetField(typeof(Game), "Genre")).ToList();
                genres.Sort();
                foreach (string genre in genres)
                    groups.Add(new RomGroup(genre, string.Format(@"'|' || Genre || '|' LIKE '%|{0}|%'", genre)));
                return groups;
            }

            string order = string.IsNullOrEmpty(this.order) ? "" : this.order;
            string sql = string.Format("SELECT DISTINCT {0} FROM {1} ORDER BY {0} {2}", column, DB.GetTableName(typeof(Game)), order).Trim();
            Logger.LogDebug("Created sql for dynamic group '{0}'", sql);

            SQLData results = DB.Instance.Execute(sql);
            foreach (SQLDataRow row in results.Rows)
            {
                string title = row.fields[0] == "" ? RomGroup.EmptySubGroupName : row.fields[0];
                string groupSql = string.Format("{0}='{1}'", column, row.fields[0]);
                RomGroup newGroup = new RomGroup(title, groupSql, "title")
                {
                    SortProperty = sortProperty
                };
                groups.Add(newGroup);
            }

            return groups;
        }

        public int CompareTo(GroupItemInfo other)
        {
            return this.position.CompareTo(other.position);
        }
    }
}
