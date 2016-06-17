using System.Collections.Generic;

namespace EDIOptions.AppCenter.Database
{
    /// <summary>
    /// A class used to store options for queries
    /// </summary>
    public class DBOptions
    {
        /// <summary>
        /// A class used to store options for queries
        /// </summary>
        public DBOptions()
        {
            _MaxLinesInsert = 1000;
            _Replaces = new Dictionary<string, string>();
            _AutoIncFields = new Dictionary<string, int>();
            //_CurrIncrement = new Dictionary<string, int>();
        }

        private int _MaxLinesInsert;

        /// <summary>
        /// Gets or sets the max number of lines to insert at once
        /// </summary>
        public int MaxLinesInsert
        {
            get { return _MaxLinesInsert; }
            set { _MaxLinesInsert = value; }
        }

        private Dictionary<string, string> _Replaces;

        /// <summary>
        /// List of replacements
        /// </summary>
        public Dictionary<string, string> Replaces
        {
            get { return _Replaces; }
            set { _Replaces = value; }
        }

        private Dictionary<string, int> _AutoIncFields;

        /// <summary>
        /// List of fields to auto increment
        /// </summary>
        public Dictionary<string, int> AutoIncFields
        {
            get { return _AutoIncFields; }
            set { _AutoIncFields = value; }
        }

        /// <summary>
        /// Add a field for auto incrementing (should be used for 1 key at a time transfers)
        /// </summary>
        /// <param name="fieldName">Name of the field</param>
        /// <param name="startVal">Start value</param>
        public void AddAutoIncrement(string fieldName, int startVal = 1)
        {
            _AutoIncFields[fieldName] = startVal;
        }
    }
}