using Emulators.Database;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace Emulators
{
    public partial class DBItem
    {
        object syncRoot = new object();
        public object SyncRoot 
        { 
            get { return syncRoot; } 
        }

        public int? Id
        { 
            get; 
            set; 
        }

        bool exists = false;
        internal bool Exists
        {
            get { return Id != null && exists; }
            set { exists = value; }
        }

        bool commitNeeded = true;
        public bool CommitNeeded
        {
            get 
            { 
                return commitNeeded; 
            }
            set 
            { 
                commitNeeded = value;
                if (!commitNeeded)
                    exists = true;
            }
        }

        public bool CommitInProgress
        {
            get;
            set;
        }

        public virtual void Commit()
        {
            EmulatorsCore.Database.Commit(this);
        }

        public virtual void Delete()
        {
            EmulatorsCore.Database.Delete(this);
        }

        public void LoadFromRow(SQLDataRow row)
        {
            ReadOnlyCollection<DBField> fieldList = DBField.GetFieldList(this.GetType());

            // load each field one at a time. they should have been retrieved in the
            // ordering in FieldList
            int i;
            for (i = 0; i < fieldList.Count; i++)
            {
                if (row.fields[i] == "")
                    fieldList[i].SetValue(this, null);
                else
                    fieldList[i].SetValue(this, row.fields[i]);
            }

            // id is always at the end, assign that too
            Id = int.Parse(row.fields[i]);
            CommitNeeded = false;
        }

        public virtual void AfterCommit() { }
        public virtual void BeforeDelete() { }
        public virtual void AfterDelete() { }
    }
}
