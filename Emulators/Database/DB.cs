using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;
using System.Reflection;
using System.Collections;
using System.Threading;

namespace Emulators.Database
{
    public delegate void DatabaseChangedHandler(DBItem changedItem);
    public class DB : IDisposable
    {
        ISQLiteProvider sqlClient = null;
        public ISQLiteProvider DataProvider { get { return sqlClient; } set { sqlClient = value; } }

        public event DatabaseChangedHandler OnItemAdded;
        public event DatabaseChangedHandler OnItemDeleted;

        public const double DB_VERSION = 2.0;

        #region Singleton

        static object instanceSync = new object();
        static DB instance = null;
        public static DB Instance
        {
            get
            {
                if (instance == null)
                    lock (instanceSync)
                        if (instance == null)
                            instance = new DB();
                return instance;
            }
        }

        #endregion

        bool isInit = false;
        Dictionary<Type, bool> isVerified;
        Dictionary<Type, bool> doneFullRetrieval;
        DatabaseCache cache;
         
        public DB()
        {
            isVerified = new Dictionary<Type, bool>();
            doneFullRetrieval = new Dictionary<Type, bool>();
            cache = new DatabaseCache();
        }

        public void Init()
        {
            if (!isInit)
            {
                isInit = true;
                sqlClient.Init();

                if (Get<Emulator>(-1) == null)
                {
                    Emulator pc = new Emulator(EmulatorType.PcGame);
                    pc.Commit();
                }
            }
            //lock (syncRoot)
            //{

            //    if (isInit)
            //        return;

            //    if (string.IsNullOrEmpty(sqlClient.DatabasePath))
            //    {
            //        Logger.LogError("No database path has been specified");
            //        return;
            //    }

            //    //string dbFile = Config.GetFile(Config.Dir.Database, DB_FILE_NAME);
            //    bool exists = File.Exists(sqlClient.DatabasePath);
            //    sqlClient.Init();
            //    sqlClient.Execute(Emulator.DBTableString);
            //    sqlClient.Execute(EmulatorProfile.DBTableString);
            //    sqlClient.Execute(Game.DBTableString);
            //    sqlClient.Execute(GameDisc.DBTableString);
            //    sqlClient.Execute("CREATE TABLE IF NOT EXISTS Info(" +
            //            "name varchar(50)," +
            //            "value varchar(100)," +
            //            "PRIMARY KEY(name)" +
            //        ")");

            //    isInit = true;

            //    SQLData result = sqlClient.Execute("SELECT value FROM Info WHERE name='version'");
            //    if (result.Rows.Count == 0)
            //    {
            //        if (exists)
            //        {
            //            sqlClient.Execute("INSERT INTO Info VALUES('version','1.7')");
            //            isInit = false;
            //            sqlClient.Dispose();
            //            Init();
            //        }
            //        else
            //            UpdateDBVersion();
            //        return;
            //    }

            //    double currentVersion = double.Parse(result.Rows[0].fields[0], System.Globalization.CultureInfo.InvariantCulture);
            //    if (currentVersion == DB_VERSION)
            //        return;

            //    if (!backupDBFile())
            //    {
            //        isInit = false;
            //        return;
            //    }

            //    if (updateDatabase(currentVersion))
            //    {
            //        UpdateDBVersion();
            //        return;
            //    }

            //    isInit = false;
            //    sqlClient.Dispose();
            //    Logger.LogInfo("Deleting incompatible database (backup has been created)");
            //    try
            //    {
            //        File.Delete(sqlClient.DatabasePath);
            //    }
            //    catch (Exception ex)
            //    {
            //        Logger.LogError("Failed to delete database file, try deleting {0} manually - {1}", sqlClient.DatabasePath, ex.Message);
            //        return;
            //    }
            //    Init();
            //}
        }

        bool updateDatabase(double currentVersion)
        {
            if (currentVersion > DB_VERSION)
            {
                Logger.LogError("Database is from a newer version of the plugin and cannot be used");
                return false;
            }

            Logger.LogInfo("Updating database from v{0} to v{1}", currentVersion, DB_VERSION);
            if (currentVersion < 1.3)
            {
                Logger.LogError("Database is from a pre-beta version and cannot be upgraded");
                return false;
            }
            
            if (new DB_Updater().Update())
            {
                return true;
            }
            else
            {
                Logger.LogError("Database update failed");
            }
            return false;
        }

        bool backupDBFile()
        {
            FileInfo dbFile = new FileInfo(sqlClient.DatabasePath);
            if (!dbFile.Exists)
                return true;

            Logger.LogInfo("Backing up current database");
            string backupFolder = Path.Combine(dbFile.DirectoryName, string.Format("Emulators2_backup_{0}", DateTime.Now.ToString("yyyy_MM_dd_HHmm")));
            string backupPath = Path.Combine(backupFolder, dbFile.Name);
            try
            {
                DirectoryInfo dir = new DirectoryInfo(backupFolder);
                if (!dir.Exists)
                    dir.Create();
                dbFile.CopyTo(backupPath, true);
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to create backup of database at {0} - {1}, {2}\n{3}", backupPath, ex, ex.Message, ex.StackTrace);
                return false;
            }
            Logger.LogInfo("Successfully created backup of database at {0}", backupPath);
            return true;
        }

        readonly object syncRoot = new object();
        public object SyncRoot
        {
            get { return syncRoot; }
        }

        bool supressExceptions = true;
        public bool SupressExceptions
        { 
            get { return supressExceptions; } 
            set { supressExceptions = value; } 
        }

        public SQLData Execute(string query, params object[] args)
        {
            lock (syncRoot)
                return ExecuteWithoutLock(query, args);
        }

        public SQLData ExecuteWithoutLock(string query, params object[] args)
        {
            if (!isInit)
            {
                Logger.LogError("Database has not initialised correctly");
                return new SQLData();
            }
            try
            {
                string exeQuery = string.Format(System.Globalization.CultureInfo.InvariantCulture, query, args);
                SQLData result = sqlClient.Execute(exeQuery);
                return result;
            }
            catch (Exception ex)
            {
                if (supressExceptions)
                {
                    Logger.LogError(ex);
                    return new SQLData();
                }
                else
                    throw;
            }
        }

        int currentTransactions = 0;
        public void BeginTransaction()
        {
            Monitor.Enter(syncRoot);
            if (currentTransactions < 1)
                Execute("BEGIN");
            currentTransactions++;
        }

        public void EndTransaction()
        {
            currentTransactions--;
            try
            {
                if (currentTransactions < 1)
                    Execute("COMMIT");
            }
            finally
            {
                Monitor.Exit(syncRoot);
            }
        }

        public void ExecuteTransaction<T>(IEnumerable<T> items, Action<T> sqlHandler)
        {
            BeginTransaction();
            foreach (T item in items)
                sqlHandler(item);
            EndTransaction();
        }

        public double CurrentDBVersion
        {
            get
            {
                SQLData result = Execute("SELECT value FROM Info WHERE name='version'");
                if (result.Rows.Count == 0)
                    return DB_VERSION; //Fresh install
                else
                    return double.Parse(result.Rows[0].fields[0], System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        public void UpdateDBVersion()
        {
            lock (syncRoot)
            {
                SQLData result = ExecuteWithoutLock("SELECT value FROM Info WHERE name='version'");
                string query;
                if (result.Rows.Count == 0)
                    query = "INSERT INTO Info VALUES('version','{0}')";
                else
                    query = "UPDATE Info SET value='{0}' WHERE name='version'";

                ExecuteWithoutLock(query, DB_VERSION);
            }
        }

        public Dictionary<int, DBItem> GetAllAsDictionary(Type tableType)
        {
            Dictionary<int, DBItem> dict = new Dictionary<int, DBItem>();
            foreach (DBItem item in GetAll(tableType))
                dict[item.Id.Value] = item;
            return dict;
        }

        public List<T> GetAll<T>() where T : DBItem
        {
            return Get<T>(null);
        }

        public List<DBItem> GetAll(Type tableType)
        {
            verifyTable(tableType);
            lock (syncRoot)
            {
                if (doneFullRetrieval.ContainsKey(tableType) && doneFullRetrieval[tableType])
                    return new List<DBItem>(cache.GetAll(tableType));
                doneFullRetrieval[tableType] = true;
                string query = getSelectQuery(tableType);
                SQLData resultSet = Execute(query);
                return createItems(resultSet, tableType);
            }
        }

        public List<T> Get<T>(ICriteria criteria) where T : DBItem
        {
            List<T> result = new List<T>();
            foreach (DBItem item in Get(typeof(T), criteria))
                result.Add((T)item);

            if (result.Count > 1 && (criteria == null || !criteria.IsOrdered) && result[0] is IComparable<T>)
                result.Sort();

            return result;
        }

        public List<DBItem> Get(Type tableType, ICriteria criteria)
        {
            if (criteria == null)
                return GetAll(tableType);

            verifyTable(tableType);
            string query = getSelectQuery(tableType);
            query += criteria.GetWhereClause();
            SQLData resultSet = Execute(query);
            return createItems(resultSet, tableType);
        }

        public T Get<T>(int id) where T : DBItem
        {
            return (T)Get(typeof(T), id);
        }

        public DBItem Get(Type tableType, int id)
        {
            lock (syncRoot)
            {
                // if we have already pulled this record down, don't query the DB
                DBItem cachedObj = cache.Get(tableType, id);
                if (cachedObj != null)
                    return cachedObj;
            }

            verifyTable(tableType);

            // build and execute the query
            string query = getSelectQuery(tableType);
            query += "where id = " + id;
            SQLData resultSet = Execute(query);
            // if the given id doesn't exist, create a new uncommited record 
            if (resultSet.Rows.Count == 0)
            {
                //newRecord.Clear();
                return null;
            }

            // otherwise load it into the object
            return createItems(resultSet, tableType)[0];
        }

        List<DBItem> createItems(SQLData resultSet, Type tableType)
        {
            List<DBItem> result = new List<DBItem>();
            ConstructorInfo constructor = tableType.GetConstructor(System.Type.EmptyTypes);
            lock (syncRoot)
            {
                foreach (SQLDataRow row in resultSet.Rows)
                {
                    int id = int.Parse(row.fields.Last());
                    DBItem cachedOb = cache.Get(tableType, id);
                    if (cachedOb != null)
                    {
                        result.Add(cachedOb);
                    }
                    else
                    {
                        DBItem newItem = (DBItem)constructor.Invoke(null);
                        newItem.Id = id;
                        cache.Add(newItem);
                        newItem.LoadFromRow(row);
                        getAllRelationData(newItem);
                        result.Add(newItem);
                    }
                }
            }
            return result;
        }

        public void Commit(DBItem dbItem)
        {
            if (dbItem == null || dbItem.CommitInProgress)
                return;

            lock (dbItem.SyncRoot)
            {
                if (dbItem.CommitInProgress)
                    return;
                dbItem.CommitInProgress = true;
                if (dbItem.CommitNeeded)
                {
                    verifyTable(dbItem.GetType());
                    if (!dbItem.Exists)
                        insert(dbItem);
                    else
                        update(dbItem);
                }
                commitRelations(dbItem);
                dbItem.CommitInProgress = false;
                dbItem.CommitNeeded = false;
            }
            dbItem.AfterCommit();
        }

        public void Commit(IRelationList relationList)
        {
            updateRelationTable(relationList.Owner, relationList.MetaData);
        }

        void commitRelations(DBItem dbObject)
        {
            if (dbObject == null) return;

            foreach (DBRelation currRelation in DBRelation.GetRelations(dbObject.GetType()))
            {
                if (currRelation.AutoRetrieve)
                {
                    foreach (DBItem subObj in currRelation.GetRelationList(dbObject))
                        Commit(subObj);

                    updateRelationTable(dbObject, currRelation);
                }
            }

            foreach (DBField currField in DBField.GetFieldList(dbObject.GetType()))
            {
                if (currField.DBType == DBDataType.DB_OBJECT)
                {
                    Commit((DBItem)currField.GetValue(dbObject));
                }
            }

        }

        public void Delete(DBItem dbItem)
        {
            if (dbItem == null || dbItem.Id == null)
                return;
            lock (dbItem.SyncRoot)
            {
                if (dbItem.Id == null)
                    return;
                dbItem.BeforeDelete();
                deleteAllRelationData(dbItem);
                Execute("DELETE FROM {0} WHERE id={1}", GetTableName(dbItem), dbItem.Id);
                cache.Remove(dbItem);
                dbItem.Id = null;
                dbItem.AfterDelete();
            }
            if (OnItemDeleted != null)
                OnItemDeleted(dbItem);
        }

        void insert(DBItem dbItem)
        {
            string paramString = "";
            string valueString = "";
            foreach (DBField currField in DBField.GetFieldList(dbItem.GetType()))
            {
                if (paramString != "")
                {
                    paramString += ", ";
                    valueString += ", ";
                }

                // if we dont have an ID, commit as needed
                DBItem propertyItem;
                if (currField.DBType == DBDataType.DB_OBJECT && currField.GetValue(dbItem) != null &&
                    !(propertyItem = (DBItem)currField.GetValue(dbItem)).Exists)
                {
                    Commit(propertyItem);
                    propertyItem.CommitNeeded = true;
                }

                paramString += currField.FieldName;
                valueString += GetSQLString(currField, currField.GetValue(dbItem));
            }
            if (paramString == "")
                return;

            if (dbItem.Id != null)
            {
                paramString += ", id";
                valueString += ", " + dbItem.Id;
            }

            lock (syncRoot)
            {
                ExecuteWithoutLock("INSERT INTO {0} ({1}) VALUES ({2})", GetTableName(dbItem), paramString, valueString);
                if (dbItem.Id == null)
                    dbItem.Id = sqlClient.LastInsertId;
                cache.Add(dbItem);
            }

            // loop through the fields and commit attached objects as needed
            foreach (DBField currField in DBField.GetFieldList(dbItem.GetType()))
            {
                if (currField.DBType == DBDataType.DB_OBJECT)
                    Commit((DBItem)currField.GetValue(dbItem));
            }

            if (OnItemAdded != null)
                OnItemAdded(dbItem);
        }

        void update(DBItem dbItem)
        {
            string sql = "";
            foreach (DBField field in DBField.GetFieldList(dbItem.GetType()))
            {
                if (sql != "")
                    sql += ", ";

                // if this is a linked db object commit it as needed
                if (field.DBType == DBDataType.DB_OBJECT)
                    Commit((DBItem)field.GetValue(dbItem));

                sql += field.FieldName + "=" + GetSQLString(field, field.GetValue(dbItem));
            }
            if (sql == "")
                return;

            Execute("UPDATE {0} SET {1} WHERE id={2}", GetTableName(dbItem), sql, dbItem.Id);
            updateRelationTables(dbItem);
        }

        /// <summary>
        /// Inserts into the database all relation information. Dependent objects will be commited.
        /// </summary>
        /// <param name="dbObject">The primary object owning the RelationList to be populated.</param>
        /// <param name="forceRetrieval">Determines if ALL relations will be retrieved.</param>
        private void updateRelationTables(DBItem dbObject) {
            foreach (DBRelation currRelation in DBRelation.GetRelations(dbObject.GetType())) {
                updateRelationTable(dbObject, currRelation);
            }            
        }

        private void updateRelationTable(DBItem dbObject, DBRelation currRelation)
        {
            if (!currRelation.GetRelationList(dbObject).CommitNeeded)
                return;

            // clear out old values then insert the new
            deleteRelationData(dbObject, currRelation);

            // insert all relations to the database
            foreach (object currObj in (IList)currRelation.GetRelationList(dbObject)) {
                DBItem currDBObj = (DBItem)currObj;
                Commit(currDBObj);
                string insertQuery = "insert into " + currRelation.TableName + "(" +
                    currRelation.PrimaryColumnName + ", " +
                    currRelation.SecondaryColumnName + ") values (" +
                    dbObject.Id + ", " + currDBObj.Id + ")";

                sqlClient.Execute(insertQuery);
            }

            currRelation.GetRelationList(dbObject).CommitNeeded = false;
        }

        // deletes all subtable data for the given object.
        private void deleteAllRelationData(DBItem dbObject)
        {
            foreach (DBRelation currRelation in DBRelation.GetRelations(dbObject.GetType()))
                deleteRelationData(dbObject, currRelation);
        }

        private void deleteRelationData(DBItem dbObject, DBRelation relation)
        {
            if (relation.PrimaryType != dbObject.GetType())
                return;

            string deleteQuery = "delete from " + relation.TableName + " where " + relation.PrimaryColumnName + "=" + dbObject.Id;

            sqlClient.Execute(deleteQuery);
        }

        private void getAllRelationData(DBItem dbObject)
        {
            foreach (DBRelation currRelation in DBRelation.GetRelations(dbObject.GetType())) {
                if (currRelation.AutoRetrieve)
                    getRelationData(dbObject, currRelation);
            }
        }

        private void getRelationData(DBItem dbObject, DBRelation relation)
        {
            IRelationList list = relation.GetRelationList(dbObject);

            if (list.Populated)
                return;

            bool oldCommitNeededFlag = dbObject.CommitNeeded;
            list.Populated = true;

            // build query
            string selectQuery = "select " + relation.SecondaryColumnName + " from " +
                       relation.TableName + " where " + relation.PrimaryColumnName + "=" + dbObject.Id;

            // and retireve relations
            SQLData resultSet = sqlClient.Execute(selectQuery);

            // parse results and add them to the list
            list.Clear();
            foreach (SQLDataRow currRow in resultSet.Rows)
            {
                int objID = int.Parse(currRow.fields[0]);
                DBItem newObj = Get(relation.SecondaryType, objID);
                list.AddDBItem(newObj);
            }

            list.Sort();
            // update flags as needed
            list.CommitNeeded = false;
            dbObject.CommitNeeded = oldCommitNeededFlag;
        }

        public void Populate(IRelationList relationList)
        {
            if (relationList != null)
                getRelationData(relationList.Owner, relationList.MetaData);
        }

        public static string GetSQLString(DBField ownerField, object value)
        {
            string strVal = GetString(ownerField, value);
            if (strVal == null)
                return "NULL";

            // if we ended up with an empty string, save a space. an empty string is interpreted
            // as null by SQLite, and thats not what we want.
            if (strVal == "")
                strVal = " ";

            // escape all quotes
            strVal = strVal.Replace("'", "''");

            return "'" + strVal + "'";
        }

        public static string GetString(DBField ownerField, object value)
        {
            if (value == null)
                return null;

            string strVal = "";

            // handle boolean types
            if (value.GetType() == typeof(bool) || value.GetType() == typeof(Boolean))
            {
                if ((Boolean)value == true)
                    strVal = "1";
                else
                    strVal = "0";
            }
            // handle double types
            else if (value.GetType() == typeof(double) || value.GetType() == typeof(Double))
                strVal = ((double)value).ToString(new CultureInfo("en-US", false));

            // handle float types
            else if (value.GetType() == typeof(float) || value.GetType() == typeof(Single))
                strVal = ((float)value).ToString(new CultureInfo("en-US", false));

            // handle database table types
            else if (IsDatabaseTableType(value.GetType()))
            {
                if (ownerField != null && ownerField.Type != value.GetType())
                    strVal = ((DBItem)value).Id.ToString() + "|||" + value.GetType().AssemblyQualifiedName;
                else
                    strVal = ((DBItem)value).Id.ToString();

            }

            // if field represents metadata about another dbfield
            else if (value is DBField)
            {
                DBField field = (DBField)value;
                strVal = field.OwnerType.AssemblyQualifiedName + "|||" + field.FieldName;
            }

            // if field represents metadata about a relation (subtable)
            //else if (value is DBRelation)
            //{
            //    DBRelation relation = (DBRelation)value;
            //    strVal = relation.PrimaryType.AssemblyQualifiedName + "|||" +
            //             relation.SecondaryType.AssemblyQualifiedName + "|||" +
            //             relation.Identifier;
            //}

            // handle C# Types, Need full qualified name to load types from other aseemblies
            else if (value is Type)
                strVal = ((Type)value).AssemblyQualifiedName;

            else if (value is DateTime)
            {
                strVal = ((DateTime)value).ToUniversalTime().ToString("u");
            }
            // everythign else just uses ToString()
            else
                strVal = value.ToString();

            return strVal;
        }

        public HashSet<string> GetAllValues(DBField field)
        {
            Type t = field.OwnerType;
            ICollection items = Get(field.OwnerType, null);

            // loop through all items in the DB and grab all existing values for this field
            HashSet<string> uniqueStrings = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            foreach (DBItem currItem in items)
            {
                List<string> values = getValues(field.GetValue(currItem));
                foreach (string currStr in values)
                    uniqueStrings.Add(currStr);
            }

            return uniqueStrings;
        }

        public HashSet<string> GetAllValues<T>(DBField field, DBRelation relation, ICollection<T> items) where T : DBItem
        {
            // loop through all items in the DB and grab all existing values for this field
            HashSet<string> uniqueStrings = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            foreach (T currItem in items)
            {
                if (relation == null)
                {
                    List<string> values = getValues(field.GetValue(currItem));
                    foreach (string currStr in values)
                        uniqueStrings.Add(currStr);
                }
                else
                {
                    foreach (DBItem currSubItem in relation.GetRelationList(currItem))
                    {
                        List<string> values = getValues(field.GetValue(currSubItem));
                        foreach (string currStr in values)
                            uniqueStrings.Add(currStr);
                    }
                }
            }

            return uniqueStrings;
        }

        private static List<string> getValues(object obj)
        {
            List<string> results = new List<string>();

            if (obj == null)
                return results;

            if (obj is string)
            {
                string s = (string)obj;
                if (s.Trim().Length != 0)
                {
                    string[] split = s.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    if (split.Length < 1)
                    {
                        results.Add((string)obj);
                    }
                    else
                    {
                        foreach (string t in split)
                            results.Add(t);
                    }
                }
            }
            //else if (obj is StringList)
            //{
            //    foreach (string currValue in (StringList)obj)
            //    {
            //        if (currValue != null && currValue.Trim().Length != 0)
            //            results.Add(currValue);
            //    }
            //}
            else if (obj is bool || obj is bool?)
            {
                results.Add("true");
                results.Add("false");
            }
            else
            {
                results.Add(obj.ToString());
            }

            return results;
        }

        public static bool IsDatabaseTableType(Type t)
        {
            Type currType = t;
            while (currType != null)
            {
                if (currType == typeof(DBItem))
                {
                    return true;
                }
                currType = currType.BaseType;
            }

            return false;
        }

        public static string GetTableName(DBItem tableItem)
        {
            return GetTableName(tableItem.GetType());
        }

        public static string GetTableName(Type tableType)
        {
            return getDBTableAttribute(tableType).TableName;
        }
        
        // Returns the table attribute information for the given type.
        static DBTableAttribute getDBTableAttribute(Type tableType)
        {
            // loop through the custom attributes of the type, if one of them is the type
            // we want, return it.
            object[] customAttrArray = tableType.GetCustomAttributes(true);
            foreach (object currAttr in customAttrArray)
            {
                if (currAttr.GetType() == typeof(DBTableAttribute))
                    return (DBTableAttribute)currAttr;
            }

            throw new Exception("Table class " + tableType.Name + " not tagged with DBTable attribute.");
        }

        // Returns a select statement retrieving all fields ordered as defined by FieldList
        // for the given Table Type. A where clause can be appended
        private static string getSelectQuery(Type tableType)
        {
            string query = "select ";
            foreach (DBField currField in DBField.GetFieldList(tableType))
            {
                if (query != "select ")
                    query += ", ";

                query += currField.FieldName;
            }
            query += ", id from " + GetTableName(tableType) + " ";
            return query;
        }

        // Checks that the table coorisponding to this type exists, and if it is missing, it creates it.
        // Also verifies all columns represented in the class are also present in the table, creating 
        // any missing. Needs to be enhanced to allow for changed defaults.
        private void verifyTable(Type tableType)
        {
            // check that we haven't already verified this table
            if (isVerified.ContainsKey(tableType))
                return;
            lock (syncRoot)
            {
                // check that we haven't already verified this table
                if (isVerified.ContainsKey(tableType))
                    return;

                // attempt to grab table info for the type. if none exists, it's not tagged to be a table
                DBTableAttribute tableAttr = getDBTableAttribute(tableType);
                if (tableAttr == null)
                    return;

                try
                {
                    // check if the table exists in the database, if not, create it
                    SQLData resultSet = sqlClient.Execute("select * from sqlite_master where type='table' and name = '" + tableAttr.TableName + "'");
                    if (resultSet.Rows.Count == 0)
                    {
                        resultSet = sqlClient.Execute("create table " + tableAttr.TableName + " (id INTEGER primary key )");
                        Logger.LogDebug("Created " + tableAttr.TableName + " table.");
                    }

                    // grab existing table info from the DB
                    resultSet = sqlClient.Execute("PRAGMA table_info(" + tableAttr.TableName + ")");

                    // loop through the CLASS DEFINED fields, and verify each is contained in the result set
                    foreach (DBField currField in DBField.GetFieldList(tableType))
                    {
                        // loop through all defined columns in DB to ensure this col exists 
                        bool exists = false;
                        foreach (SQLDataRow currRow in resultSet.Rows)
                        {
                            if (currField.FieldName == currRow.fields[1])
                            {
                                exists = true;
                                break;
                            }
                        }

                        // if we couldn't find the column create it
                        if (!exists)
                        {
                            string defaultValue;
                            if (currField.Default == null)
                                defaultValue = "NULL";
                            else
                                defaultValue = GetSQLString(currField, currField.Default);

                            sqlClient.Execute("alter table " + tableAttr.TableName + " add column " + currField.FieldName + " " +
                                             currField.DBType.ToString() + " default " + defaultValue);
                            // logger.Debug("Added " + tableAttr.TableName + "." + currField.FieldName + " column.");
                        }
                    }

                    verifyRelationTables(tableType);
                    isVerified[tableType] = true;
                }
                catch (Exception e)
                {
                    Logger.LogError("Internal error verifying " + tableAttr.TableName + " (" + tableType.ToString() + ") table. - {0}", e);
                    throw;
                }
            }
        }

        void verifyRelationTables(Type primaryType)
        {
            foreach (DBRelation currRelation in DBRelation.GetRelations(primaryType))
            {
                try
                {
                    // check if the table exists in the database, if not, create it
                    SQLData resultSet = sqlClient.Execute("select * from sqlite_master where type='table' and name = '" + currRelation.TableName + "'");
                    if (resultSet.Rows.Count == 0)
                    {
                        // create table
                        string createQuery =
                            "create table " + currRelation.TableName + " (id INTEGER primary key, " +
                            currRelation.PrimaryColumnName + " INTEGER, " +
                            currRelation.SecondaryColumnName + " INTEGER)";

                        resultSet = sqlClient.Execute(createQuery);

                        // create index1
                        resultSet = sqlClient.Execute("create index " + currRelation.TableName + "__index1 on " +
                            currRelation.TableName + " (" + currRelation.PrimaryColumnName + ")");

                        // create index2
                        resultSet = sqlClient.Execute("create index " + currRelation.TableName + "__index2 on " +
                            currRelation.TableName + " (" + currRelation.SecondaryColumnName + ")");

                        Logger.LogDebug("Created " + currRelation.TableName + " sub-table.");
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError("Error verifying " + currRelation.TableName + " subtable. - {0}", e);
                }
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (sqlClient != null)
            {
                sqlClient.Close();
                sqlClient.Dispose();
                sqlClient = null;
            }
        }

        #endregion
    }
}
