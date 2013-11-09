using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Emulators.Database
{
    class DatabaseCache
    {
        private Dictionary<Type, Dictionary<int, DBItem>> cache;

        public DatabaseCache()
        {
            cache = new Dictionary<Type, Dictionary<int, DBItem>>();
        }

        public bool Contains(DBItem obj)
        {
            if (obj == null || cache[obj.GetType()] == null)
                return false;

            return cache[obj.GetType()].ContainsValue(obj);
        }

        public DBItem Get(Type type, int id)
        {
            if (cache.ContainsKey(type) && cache[type].ContainsKey(id))
                return cache[type][id];
            else return null;
        }


        public ICollection<DBItem> GetAll(Type type)
        {
            if (cache.ContainsKey(type))
                return cache[type].Values;

            return new List<DBItem>();
        }

        // Adds the given element to the cacheing system
        public DBItem Add(DBItem obj)
        {
            if (obj == null || obj.Id == null)
                return obj;

            if (!cache.ContainsKey(obj.GetType()))
                cache[obj.GetType()] = new Dictionary<int, DBItem>();

            if (!cache[obj.GetType()].ContainsKey((int)obj.Id))
                cache[obj.GetType()][(int)obj.Id] = obj;

            return cache[obj.GetType()][(int)obj.Id];
        }

        // Goes through the list and if any elements reference an object already in
        // memory, it updates the reference in the list with the in memory version.
        public void Sync(IList<DBItem> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                DBItem currObj = list[i];

                if (currObj == null || currObj.Id == null)
                    continue;

                try
                {
                    list[i] = (DBItem)cache[currObj.GetType()][(int)currObj.Id];
                }
                catch (Exception e)
                {
                    if (e.GetType() == typeof(ThreadAbortException))
                        throw e;
                    Add(currObj);
                }
            }
        }

        // Shoudl really only be called if an item has been deleted from the database
        public void Remove(DBItem obj)
        {
            if (obj == null || obj.Id == null)
                return;

            cache[obj.GetType()].Remove((int)obj.Id);
        }

        // remove the existing object with the same ID from the cache and store this one instead.
        public void Replace(DBItem obj)
        {
            if (obj == null || obj.Id == null)
                return;

            if (!cache.ContainsKey(obj.GetType()))
                cache[obj.GetType()] = new Dictionary<int, DBItem>();

            cache[obj.GetType()][(int)obj.Id] = obj;
        }


    }
}
