using System.Collections.Generic;

namespace EDIOptions.AppCenter.Database
{
    /// <summary>
    /// A class used to hold information about a table
    /// </summary>
    public class DBTable
    {
        /// <summary>
        /// A class used to hold information about a table
        /// </summary>
        /// <param name="dbname">Database name</param>
        /// <param name="tablename">Table name</param>
        public DBTable(string dbname, string tablename)
        {
            _Database = dbname;
            _Table = tablename;
            _Fields = new List<DBField>();
        }

        private string _Table;

        /// <summary>
        /// Gets the table name
        /// </summary>
        public string Table
        {
            get { return _Table; }
        }

        private string _Database;

        /// <summary>
        /// Gets the database name
        /// </summary>
        public string Database
        {
            get { return _Database; }
        }

        private List<DBField> _Fields;

        /// <summary>
        /// Gets a list of fields for this table and attributes associated with them
        /// </summary>
        public List<DBField> Fields
        {
            get { return _Fields; }
        }

        /// <summary>
        /// Gets the DBField for the field name specified
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public DBField this[string fieldName]
        {
            get
            {
                string uField = fieldName.ToUpper();
                foreach (DBField f in _Fields)
                {
                    if (f.Name == uField) { return f; }
                }
                return null;
            }
        }

        /// <summary>
        /// Gets a dictionary containing an empty record to be used for inserts.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetEmptyRecord()
        {
            Dictionary<string, string> temp = new Dictionary<string, string>();
            foreach (DBField f in Fields)
            {
                if (f.DefaultValue != null)
                {
                    temp[f.Name] = f.DefaultValue;
                }
                else
                {
                    //IF NULL ISN'T ALLOWED AND NO DEFAULT VALUE SPECIFIED
                    if (!f.AllowNull)
                    {
                        temp[f.Name] = "";
                    }
                    else
                    {
                        temp[f.Name] = "NULL";
                    }
                }
            }
            return temp;
        }

        /// <summary>
        /// Gets the field names (IN UPPERCASE LETTERS)
        /// </summary>
        public string[] FieldNames
        {
            get
            {
                string[] names = new string[_Fields.Count];
                for (int i = 0; i < _Fields.Count; i++)
                {
                    names[i] = _Fields[i].Name;
                }
                return names;
            }
        }
    }
}