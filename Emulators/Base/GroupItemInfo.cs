using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators
{
    public enum GroupItemType { SQL, DYNAMIC, EMULATOR, GAME }
    public class GroupItemInfo
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
            item.id = id;
            return item;
        }

        public static GroupItemInfo CreateGameGroup(int id)
        {
            GroupItemInfo item = new GroupItemInfo();
            item.ItemType = GroupItemType.GAME;
            item.id = id;
            return item;
        }

        private GroupItemInfo() { }

        public GroupItemType ItemType
        {
            get;
            private set;
        }

        string sql = null;
        public string SQL
        {
            get { return sql; }
            set { sql = value; }
        }

        string column = null;
        public string Column
        {
            get { return column; }
            set { column = value; }
        }

        string order = null;
        public string Order
        {
            get { return order; }
            set { order = value; }
        }

        int id = -2;
        public int Id
        {
            get { return id; }
        }
    }
}
