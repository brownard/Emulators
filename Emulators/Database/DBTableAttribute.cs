using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators.Database
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    class DBTableAttribute : System.Attribute
    {
        string tableName;
        public string TableName
        {
            get { return tableName; }
            set { tableName = value; }
        }

        public DBTableAttribute(string tableName)
        {
            this.tableName = tableName;
        }
    }
}
