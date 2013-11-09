using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators.Database
{
    public interface ICriteria
    {
        string GetWhereClause();
        string GetClause();
        bool IsOrdered { get; }
    }

    public class GroupedCriteria : ICriteria
    {
        public enum Operator { AND, OR }

        private ICriteria critA;
        private ICriteria critB;
        private Operator op;

        public GroupedCriteria(ICriteria critA, Operator op, ICriteria critB)
        {
            this.critA = critA;
            this.critB = critB;
            this.op = op;
        }

        public string GetWhereClause()
        {
            return " where " + GetClause();
        }

        public string GetClause()
        {
            return " (" + critA.GetClause() + " " + op.ToString() + " " + critB.GetClause() + ") ";
        }

        public override string ToString()
        {
            return GetWhereClause();
        }

        public bool IsOrdered
        {
            get { return false; }
        }
    }

    public class BaseCriteria : ICriteria
    {
        private DBField field;
        private object value;
        private string op;

        public BaseCriteria(DBField field, string op, object value)
        {
            this.field = field;
            this.op = op;
            this.value = value;
        }

        public string GetWhereClause()
        {
            if (field == null)
                return "";

            return " where " + GetClause();
        }

        public string GetClause()
        {
            return " (" + field.FieldName + " " + op + " " + DB.GetSQLString(field, value) + ") ";
        }

        public override string ToString()
        {
            return GetWhereClause();
        }
        
        public bool IsOrdered
        {
            get { return false; }
        }
    }

    public class ListCriteria : ICriteria
    {
        List<int> list;
        private bool exclude;

        public ListCriteria(List<DBItem> list, bool exclude = false)
        {
            this.list = list.Where(item => item.Id.HasValue).Select(item => item.Id.Value).ToList();
            this.exclude = exclude;
        }

        public ListCriteria(List<int> list, bool exclude = false)
        {
            this.list = list;
            this.exclude = exclude;
        }

        public string GetWhereClause()
        {
            return " where " + GetClause();
        }

        public string GetClause()
        {
            if (list == null) return "1=1";

            string rtn = " Id" + (exclude ? " not " : " ") + "in ( ";
            bool first = true;
            foreach (int currId in list)
            {
                //if (currId == null) continue;

                if (first) first = false;
                else rtn += ", ";

                rtn += currId;
            }

            rtn += ")";

            return rtn;
        }

        public override string ToString()
        {
            return GetWhereClause();
        }

        public bool IsOrdered
        {
            get { return false; }
        }
    }

    public class SimpleCriteria : ICriteria
    {
        string clause;
        string orderBy;

        public SimpleCriteria(string clause, string orderBy)
        {
            if (!string.IsNullOrEmpty(clause))                
                this.clause = clause;
            else
                this.clause = "1=1";
            this.orderBy = orderBy;
        }

        public string GetWhereClause()
        {
            return " where " + GetClause() + (!string.IsNullOrEmpty(orderBy) ? " order by " + orderBy : "");
        }

        public string GetClause()
        {
            return clause;
        }

        public override string ToString()
        {
            return GetWhereClause();
        }
        
        public bool IsOrdered
        {
            get { return !string.IsNullOrEmpty(orderBy); }
        }
    }
}
