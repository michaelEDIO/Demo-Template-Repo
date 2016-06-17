using System;

namespace EDIOptions.AppCenter.Database
{
    /// <summary>
    /// A class used to throw database exceptions
    /// </summary>
    public class DBEx : Exception
    {
        /// <summary>
        /// A class used to throw database exceptions
        /// </summary>
        public DBEx(string msg, string query)
            : base(msg)
        {
            _Query = query;
        }

        private string _Query;

        /// <summary>
        /// Gets the error query
        /// </summary>
        public string Query
        {
            get { return _Query; }
        }

        /// <summary>
        /// Gets the error message
        /// </summary>
        public string Msg
        {
            get { return base.Message; }
        }
    }
}