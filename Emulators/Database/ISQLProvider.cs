using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Emulators
{
    public interface ISQLiteProvider
    {
        string DatabasePath { get; set; }
        bool Init();
        SQLData Execute(string query);
        int LastInsertId { get; }
        void Close();
        void Dispose();
    }

    public class SQLData
    {
        public SQLData()
        {
            Columns = new List<string>();
            Rows = new List<SQLDataRow>();
        }
        public SQLData(List<string> columns)
        {
            Columns = columns;
            Rows = new List<SQLDataRow>();
        }

        public List<string> Columns { get; set; }
        public List<SQLDataRow> Rows { get; set; }
    }
    public class SQLDataRow
    {
        public List<string> fields { get; set; }
    }
}
