using Cornerstone.Database.CustomTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Emulators.Database
{
    public class DBRelationList<T> : DynamicList<T>, IRelationList where T : DBItem
    {
        DBItem owner;
        public DBRelationList(DBItem owner)
        {
            this.owner = owner;
        }

        public DBItem Owner
        {
            get { return owner; }
        }

        public DBRelation MetaData
        {
            get
            {
                ReadOnlyCollection<DBRelation> metadataList = DBRelation.GetRelations(owner.GetType());
                foreach (DBRelation currData in metadataList)
                    if (currData.GetRelationList(owner) == this)
                    {
                        _metaData = currData;
                        break;
                    }
                return _metaData;
            }
        } private DBRelation _metaData = null;

        public bool Populated { get; set; }
        public bool CommitNeeded { get; set; }
        public void Commit()
        {
            EmulatorsCore.Database.Commit(this);
        }

        protected override void OnChanged(EventArgs e)
        {
            base.OnChanged(e);
            CommitNeeded = true;
        }

        public void AddDBItem(DBItem item)
        {
            if (item != null && item.GetType() == typeof(T))
                base.Add((T)item);
        }

        public void RemoveDBItem(DBItem item)
        {
            if (item != null && item.GetType() == typeof(T))
                base.Remove((T)item);
        }
    }

    public interface IRelationList : IList
    {
        DBItem Owner { get; }
        DBRelation MetaData { get; }
        bool Populated { get; set; }
        bool CommitNeeded { get; set; }
        void AddDBItem(DBItem item);
        void RemoveDBItem(DBItem item);
        void Sort();
    }
}
