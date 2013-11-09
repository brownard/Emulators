using MediaPortal.Configuration;
using SQLite.NET;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace Emulators.MediaPortal1
{
    class MP1DataProvider : ISQLiteProvider
    {
        SQLiteClient sqlClient = null;

        public string DatabasePath
        {
            get;
            set;
        }

        public MP1DataProvider(string databasePath)
        {
            DatabasePath = databasePath;  //"Emulators2_v2.db3"
        }

        public bool Init()
        {
            if (string.IsNullOrEmpty(DatabasePath))
            {
                Logger.LogError("No database path has been specified");
                return false;
            }
            sqlClient = new SQLiteClient(DatabasePath);
            return true;
        }        

        public SQLData Execute(string query)
        {
            SQLiteResultSet result = sqlClient.Execute(query);
            SQLData dt = new SQLData(result.ColumnNames);
            foreach (SQLiteResultSet.Row row in result.Rows)
                dt.Rows.Add(new SQLDataRow() { fields = row.fields });
            return dt;
        }

        public int LastInsertId
        {
            get { return sqlClient.LastInsertID(); }
        }

        public void Close()
        {
            if (sqlClient != null)
                sqlClient.Close();
        }

        public void Dispose()
        {
            if (sqlClient != null)
            {
                sqlClient.Dispose();
                sqlClient = null;
            }
        }
    }    
}
