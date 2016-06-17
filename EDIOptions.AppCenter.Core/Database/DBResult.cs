using System;
using System.Data.Common;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace EDIOptions.AppCenter.Database
{
    /// <summary>
    /// A class to display database results
    /// </summary>
    public class DBResult : IDisposable
    {
        private bool _IsDisposed = false;
        private string[] _FNames;

        /// <summary>
        /// A class to display database results
        /// </summary>
        /// <param name="data">Data returned by the query</param>
        /// <param name="affectedRows">Number of affected rows</param>
        /// <param name="elapsed">Time elapsed by the query</param>
        /// <param name="query">Query used to obtain results</param>
        /// <param name="nulldateyr">Sets the maximum year where dates should be set to NULL</param>
        /// <param name="nullval">Sets the null value to return when data is null</param>
        public DBResult(DbDataReader data, int affectedRows, TimeSpan elapsed, string query, int nulldateyr, string nullval)
        {
            Data = data;
            AffectedRows = affectedRows;
            ElapsedSecs = elapsed.TotalSeconds;
            Query = query;
            FieldCount = (Data == null ? 0 : Data.FieldCount);
            NullDateYr = nulldateyr;
            NullValue = nullval;
            string[] names = new string[FieldCount];
            for (int i = 0; i < names.Count(); i++)
            {
                names[i] = Data.GetName(i).ToUpper();
            }
            _FNames = names;
            //System.Data.DataTable dt = data.GetSchemaTable();
            //bool isCaseSensitive = dt.CaseSensitive;
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~DBResult()
        {
            Dispose(false);
        }

        #region IDisposable Implementation

        /// <summary>
        /// Releases any resources used by DBResult
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases any resources used by DBResult
        /// </summary>
        /// <param name="isDisposing">(Optional) Set to true to dispose of resources</param>
        protected virtual void Dispose(bool isDisposing)
        {
            if (!_IsDisposed)
            {
                if (isDisposing)
                {
                    Data.Dispose();
                    Data = null;
                }
                _IsDisposed = true;
            }
        }

        #endregion IDisposable Implementation

        /// <summary>
        /// Gets the query used to obtain these results
        /// </summary>
        public string Query { get; private set; }

        /// <summary>
        /// Gets the number of rows affected by the query
        /// </summary>
        public int AffectedRows { get; private set; }

        /// <summary>
        /// Gets the amount of time it took the query to execute
        /// </summary>
        public double ElapsedSecs { get; private set; }

        /// <summary>
        /// Gets or sets the maximum year where dates should be set to NULL (anything after this year is a valid date, defaulted to 1900)
        /// </summary>
        public int NullDateYr { get; set; }

        /// <summary>
        /// Gets or sets the value to be returned when data is null (defaults to "NULL")
        /// </summary>
        public string NullValue { get; set; }

        /// <summary>
        /// Gets a data reader containing the data returned
        /// </summary>
        public DbDataReader Data { get; private set; }

        /// <summary>
        /// Gets the data in XML format
        /// </summary>
        /// <returns></returns>
        public XElement ToXML()
        {
            return ToXML(NullValue);
        }

        /// <summary>
        /// Gets the data in XML format
        /// </summary>
        /// <param name="nullval">String to use as null.</param>
        /// <returns></returns>
        public XElement ToXML(string nullval)
        {
            if (Data == null) { throw new Exception("This query did not return any data."); }
            XElement xml = new XElement("records");
            while (Data.Read())
            {
                string field, val;
                XElement rec = new XElement("record");
                for (int i = 0; i < Data.FieldCount; i++)
                {
                    field = Data.GetName(i);
                    //val = (Data[i] == DBNull.Value ? nullval : Data[i].ToString());
                    val = GetFieldValue(i, nullval);
                    rec.Add(new XElement(XmlConvert.EncodeName(field), val.Trim()));
                }
                xml.Add(rec);
            }
            return xml;
        }

        /// <summary>
        /// Gets a 'elDelim' separated string of values, with records separated by 'recDelim'
        /// </summary>
        /// <param name="elDelim">Delimiter to use to separate fields</param>
        /// <param name="recDelim">(Optional) Delimiter use use to separate records (defaulted to \r\n)</param>
        /// <param name="nullval">(Optional) Value to use to represent NULL (defaults to "NULL" string)</param>
        /// <returns></returns>
        public string ToDelim(string elDelim, string recDelim = "\r\n", string nullval = "NULL")
        {
            if (Data == null) { throw new Exception("This query did not return any data."); }
            string res = "", val = "";
            while (Data.Read())
            {
                for (int i = 0; i < Data.FieldCount; i++)
                {
                    val = Field(i, nullval);
                    res += val.Trim() + elDelim;
                }
                if (res.Length > 0) { res = res.Substring(0, res.Length - 1); }
                res += recDelim;
            }
            return res;
        }

        /// <summary>
        /// Gets the number of fields in each row
        /// </summary>
        public int FieldCount { get; private set; }

        /// <summary>
        /// Reads a record. Returns true until there are no more records to read
        /// </summary>
        public bool Read()
        {
            return Data.Read();
        }

        /// <summary>
        /// Gets an array with the field names returned (IN UPPERCASE LETTERS).
        /// </summary>
        public string[] FieldNames
        {
            get { return _FNames; }
            set { _FNames = value; }
        }

        /// <summary>
        /// Gets the value of a field with the name requested as an object type
        /// </summary>
        /// <param name="fName">Field name to look for</param>
        /// <param name="nullval">Value to use to represent NULL</param>
        /// <returns></returns>
        public string FieldByName(string fName, string nullval = null)
        {
            nullval = (nullval == null ? NullValue : nullval);
            fName = fName.ToUpper();
            int i = Array.IndexOf<string>(_FNames, fName);
            if (i == -1) { throw new Exception("Field '" + fName + "' not found in the results."); }
            return this.Field(i, nullval);
        }

        /// <summary>
        /// Gets the value of a field with the name requested as an object type
        /// </summary>
        /// <param name="fName">Field name to look for</param>
        /// <param name="nullObj"(Optional) Onject to return if data comes back null (defaults to null)></param>
        /// <returns></returns>
        public object FieldByName2(string fName, object nullObj = null)
        {
            fName = fName.ToUpper();
            int i = Array.IndexOf<string>(_FNames, fName);
            if (i == -1) { throw new Exception("Field '" + fName + "' not found in the results."); }
            return this.Field2(i, nullObj);
        }

        /// <summary>
        /// Gets the value of the ith field of the record
        /// </summary>
        /// <param name="i">An integer greater or equal to 0.</param>
        /// <returns></returns>
        public string Field(int i)
        {
            return Field(i, NullValue);
        }

        /// <summary>
        /// Gets the value of the ith field of the record as a string
        /// </summary>
        /// <param name="i">An integer greater or equal to 0.</param>
        /// <param name="nullval">Value to use to represent NULL</param>
        /// <returns></returns>
        public string Field(int i, string nullval)
        {
            return GetFieldValue(i, nullval);
        }

        /// <summary>
        /// Gets the value of theith field of the record as an object, which can be casted to it's original type.
        /// </summary>
        /// <param name="i">An integer greater or equal to 0.</param>
        /// <param name="nullObj">(Optional) Onject to return if data comes back null (defaults to null)</param>
        /// <returns></returns>
        public object Field2(int i, object nullObj = null)
        {
            if (Data[i] == DBNull.Value) { return nullObj; }
            return Data[i];
        }

        /// <summary>
        /// Returns the field type of the ith field of the record
        /// </summary>
        /// <param name="i">An integer greater than or equal to 0</param>
        /// <returns></returns>
        public Type GetFieldType(int i)
        {
            Type t = Data.GetFieldType(i);
            return t;
        }

        private string GetFieldValue(int i, string nullval = "NULL")
        {
            Type t = Data.GetFieldType(i);
            DateTime dt;
            //ENTER NULL FIELD
            if (Data[i] == DBNull.Value) { return nullval; }
            //HANDLE DATE AND DATETIME
            else if (t.FullName == "System.DateTime")
            {
                dt = DateTime.Parse(Data[i].ToString());
                if (dt.Year <= NullDateYr) { return nullval; }
                if (dt.ToString("HH:mm:ss") == "00:00:00")
                {
                    return dt.ToString("yyyy-MM-dd");
                }
                else
                {
                    return dt.ToString("yyyy-MM-dd HH:mm:ss");
                }
            }
            //HANDLE NORMAL FIELDS
            else
            {
                return Data[i].ToString();
            }
        }
    }
}