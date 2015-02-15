using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Emulators.Database
{
    public enum DBDataType { INTEGER, REAL, TEXT, STRING_OBJECT, BOOL, TYPE, ENUM, DATE_TIME, DB_OBJECT, DB_FIELD, DB_RELATION }
    public class DBField
    {
        PropertyInfo propertyInfo;
        DBFieldAttribute attribute;
        DBDataType type;

        private DBField(PropertyInfo propertyInfo, DBFieldAttribute attribute)
        {
            this.propertyInfo = propertyInfo;
            this.attribute = attribute;

            // determine how this shoudl be stored in the DB
            type = DBDataType.TEXT;

            if (propertyInfo.PropertyType == typeof(string))
                type = DBDataType.TEXT;
            else if (propertyInfo.PropertyType == typeof(int))
                type = DBDataType.INTEGER;
            else if (propertyInfo.PropertyType == typeof(int?))
                type = DBDataType.INTEGER;
            else if (propertyInfo.PropertyType == typeof(float))
                type = DBDataType.REAL;
            else if (propertyInfo.PropertyType == typeof(float?))
                type = DBDataType.REAL;
            else if (propertyInfo.PropertyType == typeof(double))
                type = DBDataType.REAL;
            else if (propertyInfo.PropertyType == typeof(double?))
                type = DBDataType.REAL;
            else if (propertyInfo.PropertyType == typeof(bool))
                type = DBDataType.BOOL;
            else if (propertyInfo.PropertyType == typeof(bool?))
                type = DBDataType.BOOL;
            else if (propertyInfo.PropertyType == typeof(Boolean))
                type = DBDataType.BOOL;
            else if (propertyInfo.PropertyType == typeof(DateTime))
                type = DBDataType.DATE_TIME;
            else if (propertyInfo.PropertyType == typeof(DateTime?))
                type = DBDataType.DATE_TIME;
            else if (propertyInfo.PropertyType == typeof(Type))
                type = DBDataType.TYPE;
            else if (propertyInfo.PropertyType.IsEnum)
                type = DBDataType.ENUM;
            // nullable enum
            else if (Nullable.GetUnderlyingType(propertyInfo.PropertyType) != null ? Nullable.GetUnderlyingType(propertyInfo.PropertyType).IsEnum : false)
                type = DBDataType.ENUM;
            else if (DB.IsDatabaseTableType(propertyInfo.PropertyType))
                type = DBDataType.DB_OBJECT;
            else if (propertyInfo.PropertyType == typeof(DBField))
                type = DBDataType.DB_FIELD;
            //else if (propertyInfo.PropertyType == typeof(DBRelation))
            //    type = DBDataType.DB_RELATION;
            //else {
            //    // check for string object types
            //    foreach (Type currInterface in propertyInfo.PropertyType.GetInterfaces())
            //        if (currInterface == typeof(IStringSourcedObject)) {
            //            type = DBDataType.STRING_OBJECT;
            //            return;
            //        }
            //}
        }

        // Returns the name of this attribute.
        public string Name
        {
            get { return propertyInfo.Name; }
        }

        public string FriendlyName
        {
            get
            {
                if (_friendlyName == null)
                    _friendlyName = DBField.MakeFriendlyName(Name);

                return _friendlyName;
            }
        } private string _friendlyName = null;

        // Returns the name of this field in the database. Generally the same as Name,
        // but this is not gauranteed.
        public string FieldName
        {
            get
            {
                if (attribute.FieldName == string.Empty)
                    return Name.ToLower();
                else
                    return attribute.FieldName;
            }
        }

        // Returns the Type of database object this field belongs to.
        public Type OwnerType
        {
            get
            {
                return propertyInfo.DeclaringType;
            }
        }

        // Returns the type the field will be stored as in the database.
        public DBDataType DBType
        {
            get { return type; }
        }

        // Returns the C# type for the field.
        public Type Type
        {
            get { return propertyInfo.PropertyType; }
        }

        public bool IsNullable
        {
            get
            {
                //if (Type == typeof(StringList)) return false;
                if (!Type.IsValueType) return true;
                if (Nullable.GetUnderlyingType(Type) != null) return true; // Nullable<T>
                return false;
            }
        }

        // Returns the default value for the field. Currently always returns in type string.
        public object Default
        {
            get
            {
                if (attribute.Default == null)
                    return null;

                switch (DBType)
                {
                    case DBDataType.INTEGER:
                        if (attribute.Default == "")
                            return 0;
                        else
                            return int.Parse(attribute.Default);
                    case DBDataType.REAL:
                        if (attribute.Default == "")
                            return (float)0.0;
                        else
                            return float.Parse(attribute.Default, CultureInfo.InvariantCulture);
                    case DBDataType.BOOL:
                        if (attribute.Default == "")
                            return false;
                        else
                            return attribute.Default.ToLower() == "true" || attribute.Default.ToString() == "1";
                    case DBDataType.DATE_TIME:
                        if (attribute.Default == "")
                            return DateTime.Now;
                        else
                        {
                            try
                            {
                                return DateTime.Parse(attribute.Default);
                            }
                            catch { }
                        }
                        return DateTime.Now;
                    //case DBDataType.STRING_OBJECT:
                    //    IStringSourcedObject newObj = (IStringSourcedObject)propertyInfo.PropertyType.GetConstructor(System.Type.EmptyTypes).Invoke(null);
                    //    newObj.LoadFromString(attribute.Default);
                    //    return newObj;
                    case DBDataType.DB_OBJECT:
                        if (propertyInfo.PropertyType == typeof(DBItem))
                            return null;

                        DBItem newDBObj = (DBItem)propertyInfo.PropertyType.GetConstructor(System.Type.EmptyTypes).Invoke(null);
                        return newDBObj;
                    default:
                        if (attribute.Default == "")
                            return " ";
                        else
                            return attribute.Default;
                }
            }
        }

        #region Public Methods
        // Sets the value of this field for the given object.
        public void SetValue(DBItem owner, object value)
        {
            try
            {
                // if we were passed a null value, try to set that. 
                if (value == null)
                {
                    propertyInfo.GetSetMethod().Invoke(owner, new object[] { null });
                    return;
                }

                // if we were passed a matching object, just set it
                if (value.GetType() == propertyInfo.PropertyType)
                {
                    string strVal = value as string;
                    if (strVal != null)
                        value = strVal.Trim();
                    propertyInfo.GetSetMethod().Invoke(owner, new object[] { value });
                    return;
                }
                
                if (value is string)
                    propertyInfo.GetSetMethod().Invoke(owner, new object[] { ConvertString((string)value) });

            }
            catch (Exception e)
            {
                Logger.LogError("Error writing to {0}.{1} Property: {2}", owner.GetType().Name, this.Name, e.Message);
            }
        }

        // Returns the value of this field for the given object.
        public object GetValue(DBItem owner)
        {
            try
            {
                return propertyInfo.GetGetMethod().Invoke(owner, null);
            }
            catch (Exception)
            {
                throw new Exception("DBField does not belong to the Type of the supplied Owner.");
            }
        }

        public object ConvertString(string strVal)
        {
            try
            {
                if (string.IsNullOrEmpty(strVal.Trim()) && IsNullable)
                    return null;

                switch (DBType)
                {
                    case DBDataType.INTEGER:
                        string tmp = strVal.ToString();
                        while (tmp.Contains(","))
                            tmp = tmp.Remove(tmp.IndexOf(','), 1);

                        return int.Parse(tmp);

                    case DBDataType.REAL:
                        if (propertyInfo.PropertyType == typeof(double))
                            return double.Parse(strVal, new CultureInfo("en-US", false));
                        else
                            return float.Parse(strVal, new CultureInfo("en-US", false));

                    case DBDataType.BOOL:
                        return (strVal.ToString() == "true" || strVal.ToString() == "1");

                    //case DBDataType.STRING_OBJECT:
                    //    // create a new object and populate it
                    //    IStringSourcedObject newObj = (IStringSourcedObject)propertyInfo.PropertyType.GetConstructor(System.Type.EmptyTypes).Invoke(null);
                    //    newObj.LoadFromString(strVal);
                    //    return newObj;

                    case DBDataType.TYPE:
                        return Type.GetType(strVal);

                    case DBDataType.ENUM:
                        if (strVal.Trim().Length != 0)
                        {
                            Type enumType = propertyInfo.PropertyType;
                            if (Nullable.GetUnderlyingType(enumType) != null)
                                enumType = Nullable.GetUnderlyingType(enumType);

                            return Enum.Parse(enumType, strVal);
                        }
                        break;

                    case DBDataType.DATE_TIME:
                        DateTime newDateTimeObj = DateTime.Now;
                        if (strVal.Trim().Length != 0)
                            try
                            {
                                newDateTimeObj = DateTime.Parse(strVal);
                            }
                            catch { }

                        return newDateTimeObj;

                    case DBDataType.DB_OBJECT:
                        if (strVal.Trim().Length == 0)
                            return null;

                        string[] objectValues = strVal.Split(new string[] { "|||" }, StringSplitOptions.None);
                        if (objectValues.Length > 1)
                        {
                            return EmulatorsCore.Database.Get(Type.GetType(objectValues[1]), int.Parse(objectValues[0]));
                        }
                        else
                            return EmulatorsCore.Database.Get(propertyInfo.PropertyType, int.Parse(strVal));

                    case DBDataType.DB_FIELD:
                        string[] fieldValues = strVal.Split(new string[] { "|||" }, StringSplitOptions.None);
                        if (fieldValues.Length != 2)
                            break;

                        return DBField.GetFieldByDBName(Type.GetType(fieldValues[0]), fieldValues[1]);

                    //case DBDataType.DB_RELATION:
                    //    string[] relationValues = strVal.Split(new string[] { "|||" }, StringSplitOptions.None);
                    //    if (relationValues.Length != 3)
                    //        break;

                    //    return DBRelation.GetRelation(Type.GetType(relationValues[0]),
                    //                                  Type.GetType(relationValues[1]),
                    //                                  relationValues[2]);

                    default:
                        return strVal;
                }
            }
            catch (Exception e)
            {
                Logger.LogError("Error parsing {0}.{1} Property: {2}", propertyInfo.DeclaringType.Name, this.Name, e.Message);
            }

            return null;
        }

        // sets the default value based on the datatype.
        public void InitializeValue(DBItem owner)
        {
            SetValue(owner, Default);
        }

        public override string ToString()
        {
            return FriendlyName;
        }

        #endregion

        static object fieldListSync = new object();
        static Dictionary<Type, ReadOnlyCollection<DBField>> fieldLists;
        public static ReadOnlyCollection<DBField> GetFieldList(Type itemType)
        {
            lock (fieldListSync)
            {
                if (fieldLists == null)
                    fieldLists = new Dictionary<Type, ReadOnlyCollection<DBField>>();
                else if (fieldLists.ContainsKey(itemType))
                    return fieldLists[itemType];

                List<DBField> fields = new List<DBField>();
                foreach(PropertyInfo property in itemType.GetProperties())
                    foreach (object attr in property.GetCustomAttributes(true))
                    {
                        if (attr.GetType() == typeof(DBFieldAttribute))
                        {
                            fields.Add(new DBField(property, (DBFieldAttribute)attr));
                            break;
                        }
                    }
                fieldLists[itemType] = fields.AsReadOnly();
                return fields.AsReadOnly();
            }
        }


        // Returns the DBField with the specified name for the specified table.
        public static DBField GetField(Type tableType, string fieldName)
        {
            if (tableType == null)
            {
                return null;
            }

            ReadOnlyCollection<DBField> fieldList = GetFieldList(tableType);
            foreach (DBField currField in fieldList)
            {
                if (currField.Name.Equals(fieldName))
                    return currField;
            }

            return null;
        }

        // Returns the DBField with the specified name for the specified table.
        public static DBField GetFieldByDBName(Type tableType, string fieldName)
        {
            if (tableType == null)
            {
                return null;
            }

            ReadOnlyCollection<DBField> fieldList = GetFieldList(tableType);
            foreach (DBField currField in fieldList)
            {
                if (currField.FieldName.Equals(fieldName))
                    return currField;
            }

            return null;
        }

        public static string MakeFriendlyName(string input)
        {
            string friendlyName = "";

            char prevChar = char.MinValue;
            foreach (char currChar in input)
            {
                if (prevChar != char.MinValue && char.IsLower(prevChar) && char.IsUpper(currChar))
                    friendlyName += " ";

                friendlyName += currChar;
                prevChar = currChar;
            }

            return friendlyName;
        }
    }
}
