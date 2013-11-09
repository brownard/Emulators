using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SQLite;
using System.Reflection;

namespace Emulators.MediaPortal2
{
    class SQLProvider : ISQLiteProvider
    {
        string dbConnection = @"Data Source={0};Version=3;";
        SQLiteConnection connection = null;
        public string DatabasePath
        {
            get;
            set;
        }

        public SQLProvider(string databasePath)
        {
            DatabasePath = databasePath;
        }

        public bool Init()
        {
            connection = new SQLiteConnection(string.Format(dbConnection, DatabasePath));
            connection.Open();            
            return true;
        }

        public SQLData Execute(string query)
        {
            SQLData dt = new SQLData();
            if (connection != null)
            {
                using (SQLiteCommand mycommand = new SQLiteCommand(connection))
                {
                    mycommand.CommandText = query;
                    using (SQLiteDataReader reader = mycommand.ExecuteReader())
                    {
                        if (reader.FieldCount > 0)
                            dt.Columns.AddRange(reader.GetValues().AllKeys);

                        while (reader.Read())
                        {
                            List<string> fields = new List<string>();
                            var values = reader.GetValues();
                            for (int i = 0; i < values.Count; i++)
                                fields.Add(values[i]);
                            dt.Rows.Add(new SQLDataRow() { fields = fields });
                        }
                    }
                }
            }
            return dt;
        }

        public int LastInsertId
        {
            get { return (int)connection.LastInsertRowId; }
        }

        public void Close()
        {
            if (connection != null)
                connection.Close();
        }

        public void Dispose()
        {
            if (connection != null)
            {
                connection.Dispose();
                connection = null;
            }
        }
    }
}
