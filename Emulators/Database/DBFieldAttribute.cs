using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators.Database
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    class DBFieldAttribute : System.Attribute
    {
        #region Private
        private string fieldName = string.Empty;
        private string defaultValue = string.Empty;
        #endregion

        #region Properties
        // if unassigned, the name of the parameter should be used for the field name
        public string FieldName
        {
            get { return fieldName; }
            set { fieldName = value; }
        }

        public string Default
        {
            get { return defaultValue; }
            set { defaultValue = value; }
        }



        #endregion

        public DBFieldAttribute()
        {
        }
    }
}
