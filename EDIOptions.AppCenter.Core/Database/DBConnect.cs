using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace EDIOptions.AppCenter.Database
{
    /// <summary>
    /// A class used to make queries to databases
    /// </summary>
    [ProgId("DBConnect")]
    [ComVisible(true)]
    public class DBConnect
    {
        private OdbcConnection _ODBC;
        private OleDbConnection _OLE;

        /// <summary>
        /// A class used to make queries to databases
        /// </summary>
        public DBConnect()
        {
            _ODBC = null;
            _OLE = null;
            SourceFolder = null;
            DBInfo = null;
            ThrowExceptions = true;
            IsReadOnly = false;
            ErrMsg = "";
            NullDateYr = 1900;
            NullValue = "NULL";
        }

        /// <summary>
        /// A flag to indicate whether to throw exceptions or store them in ErrMsg only.
        /// </summary>
        public bool ThrowExceptions { get; set; }

        /// <summary>
        /// Gets or sets whether this connection should be read only
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// Gets the last error message
        /// </summary>
        public string ErrMsg { get; private set; }

        /// <summary>
        /// Gets the last query used when an error occured.
        /// </summary>
        public string ErrQuery { get; private set; }

        /// <summary>
        /// Gets the database connection information being used
        /// </summary>
        public DatabaseInfo DBInfo { get; private set; }

        /// <summary>
        /// Gets the source folder used for a VFP connection
        /// </summary>
        public string SourceFolder { get; private set; }

        /// <summary>
        /// Gets or sets the maximum year where dates should be set to NULL (anything after this year is a valid date, defaulted to 1900)
        /// </summary>
        public int NullDateYr { get; set; }

        /// <summary>
        /// Gets or sets the value to be returned when data is null (defaults to "NULL")
        /// </summary>
        public string NullValue { get; set; }

        /// <summary>
        /// Gets the connection string used to connect
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// Gets the ODBC driver being used.
        /// </summary>
        public string Driver { get; private set; }

        #region CONNECTION

        private void TryConnect(string connString, int attempts = 0, int maxAttempts = 3)
        {
            try
            {
                _ODBC = new OdbcConnection(connString);
                _ODBC.Open();
            }
            catch (Exception ex)
            {
                string msg = ex.Message.ToUpper();
                if ((msg.Contains("CAN'T CONNECT TO MYSQL SERVER") || msg.Contains("UNKNOWN MYSQL SERVER HOST")) && attempts != maxAttempts)
                {
                    attempts++;
                    int sleepSecs = attempts * attempts * 5;
                    Thread.Sleep(sleepSecs * 1000);
                    TryConnect(connString, attempts, maxAttempts);
                }
                else
                {
                    throw;
                }
                //Can't connect to MySQL server
                //Unknown MySQL server host
            }
        }

        /// <summary>
        /// Creates an ODBC connection to the selected database. Returns false if a connection cannot be made.
        /// </summary>
        /// <param name="info">A DatabaseInfo object containing database connection information.</param>
        /// <remarks>public</remarks>
        /// <returns>bool</returns>
        public bool Connect(DatabaseInfo info)
        {
            try
            {
                Close();
                DBInfo = new DatabaseInfo(info);
                Driver = info.Driver;
                if (DBInfo != null)
                {
                    string MyConString = GetConnectionString(info, info.Driver);
                    _ODBC = new OdbcConnection(MyConString);
                    _ODBC.Open();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        /// <summary>
        /// Connect to a FoxPro database
        /// </summary>
        /// <param name="tablesfolder">Path to folder containing tables.</param>
        /// <returns></returns>
        public bool Connect(string tablesfolder)
        {
            try
            {
                Close();
                string MyConString = "Provider=VFPOLEDB.1;Data Source=" + tablesfolder + ";Collating Sequence=general;";
                _OLE = new OleDbConnection(MyConString);
                _OLE.Open();
                SourceFolder = tablesfolder;
                return true;
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        /// <summary>
        /// Attemps to reconnect to the database. Returns true on success
        /// </summary>
        /// <returns></returns>
        public bool Reconnect()
        {
            try
            {
                if (_ODBC != null)
                {
                    DatabaseInfo di = new DatabaseInfo(DBInfo);
                    try { Close(); }
                    catch { }
                    return Connect(di);
                }
                else
                {
                    string tbl = SourceFolder;
                    try { Close(); }
                    catch { }
                    return Connect(tbl);
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        #endregion CONNECTION

        #region Pre-set Queries

        public static string GenerateUniqueKey()
        {
            string key = DateTime.UtcNow.Ticks.ToBase36() + StringHelper.GetRandomAlphaNumeric(8);
            //int l = key.Length;
            return key;
        }

        /// <summary>
        /// Gets a new uniquekey.
        /// </summary>
        /// <returns></returns>
        public string GetNewKey()
        {
            return GenerateUniqueKey();
        }

        /// <summary>
        /// Gets the connection string used to connect to the database
        /// </summary>
        /// <param name="di">DatabaseInfo object containing connection information</param>
        /// <param name="driver">Name of ODBC drive to use</param>
        /// <returns></returns>
        public string GetConnectionString(DatabaseInfo di, string driver)
        {
            driver = (driver == "" ? ConnectionsMgr.GetDefaultDriver() : driver);
            ConnectionString = String.Format("DRIVER={{{0}}};SERVER={1};PORT={2};DATABASE={3};UID={4};PASSWORD={5};OPTION=3", driver, di.Server, di.Port, di.Database, di.Username, di.Password);
            return ConnectionString;
        }

        /// <summary>
        /// Creates a database (only works with MySQL)
        /// </summary>
        /// <param name="database">Name of database to create</param>
        /// <param name="checkIfExists">(Optional) Set to true to create only if database does not already exist</param>
        /// <returns></returns>
        public DBResult CreateDB(string database, bool checkIfExists = false)
        {
            string query = "";
            try
            {
                query = "CREATE DATABASE";
                if (checkIfExists) { query += " IF NOT EXISTS"; }
                query += " " + database;
                //query += " CHARACTER SET='LATIN1' COLLATE='LATIN1_SWEDISH_CI'";
                return Query(query);
            }
            catch (Exception ex)
            {
                ErrQuery = query;
                HandleException(ex);
                return null;
            }
        }

        /// <summary>
        /// Creates a temporary table (only works with MySQL at the moment)
        /// </summary>
        /// <param name="table"></param>
        /// <param name="fields"></param>
        /// <param name="primarykey"></param>
        /// <param name="checkIfExists"></param>
        /// <param name="indexes"></param>
        /// <returns></returns>
        public DBResult CreateTemp(string table, List<DBField> fields, string primarykey = null, bool checkIfExists = false, List<string> indexes = null)
        {
            string query = "";
            try
            {
                query = "CREATE TEMPORARY TABLE";
                if (checkIfExists) { query += " IF NOT EXISTS"; }
                query += " " + table + " (";
                foreach (DBField f in fields)
                {
                    query += f.QueryString + ",";
                }
                query = query.Substring(0, query.Length - 1);
                if (primarykey != null)
                {
                    query += ",PRIMARY KEY (" + primarykey + ")";
                }
                if (indexes != null)
                {
                    for (int i = 0; i < indexes.Count(); i++)
                    {
                        query += ",INDEX IDX" + i + " (" + indexes[i] + ")";
                    }
                }
                query += ")";
                return Query(query);
            }
            catch (Exception ex)
            {
                ErrQuery = query;
                HandleException(ex);
                return null;
            }
        }

        /// <summary>
        /// Creates a table (only works with MySQL at the moment)
        /// </summary>
        /// <param name="table">Name of the table to create (ex. mytable or mydb.mytable)</param>
        /// <param name="fields">A list of field to add to the table</param>
        /// <param name="primarykey">(Optional) A comma-separated string of fields to set as primary keys (ex. uniquekey or uniquekey,uniqueitem)</param>
        /// <param name="checkIfExists">(Optional) Set to true to create only if table does not already exist</param>
        /// <param name="indexes">(Optional) A list of indexes to add to the table (ex. A sample index is uniquekey or uniquekey,uniqueitem)</param>
        /// <returns></returns>
        public DBResult Create(string table, List<DBField> fields, string primarykey = null, bool checkIfExists = false, List<string> indexes = null)
        {
            string query = "";
            try
            {
                query = "CREATE TABLE";
                if (checkIfExists) { query += " IF NOT EXISTS"; }
                query += " " + table + " (";
                foreach (DBField f in fields)
                {
                    query += f.QueryString + ",";
                }
                query = query.Substring(0, query.Length - 1);
                if (primarykey != null)
                {
                    query += ",PRIMARY KEY (" + primarykey + ")";
                }
                if (indexes != null)
                {
                    for (int i = 0; i < indexes.Count(); i++)
                    {
                        query += ",INDEX IDX" + i + " (" + indexes[i] + ")";
                    }
                }
                query += ")";
                return Query(query);
            }
            catch (Exception ex)
            {
                ErrQuery = query;
                HandleException(ex);
                return null;
            }
        }

        /// <summary>
        /// Creates a table (only works with MySQL at the moment)
        /// </summary>
        /// <param name="table">Name of the table to create (ex. mytable or mydb.mytable)</param>
        /// <param name="fields">A list of field to add to the table</param>
        /// <param name="primarykey">(Optional) A comma-separated string of fields to set as primary keys (ex. uniquekey or uniquekey,uniqueitem)</param>
        /// <param name="checkIfExists">(Optional) Set to true to create only if table does not already exist</param>
        /// <param name="index">(Optional) An index to add to the table (ex. A sample index is uniquekey or uniquekey,uniqueitem)</param>
        /// <returns></returns>
        public DBResult Create(string table, List<DBField> fields, string primarykey, bool checkIfExists, string index)
        {
            if (index == null)
            {
                return Create(table, fields, primarykey, checkIfExists);
            }
            else
            {
                List<string> indexes = new List<string>();
                indexes.Add(index);
                return Create(table, fields, primarykey, checkIfExists, indexes);
            }
        }

        /// <summary>
        /// Deletes a table
        /// </summary>
        /// <param name="table">Table to delete</param>
        /// <param name="isTemporary">(Optional) Flag to indicate if table was a temporary table (defaults to false)</param>
        /// <returns></returns>
        public DBResult DropTable(string table, bool isTemporary = false)
        {
            string query = "";
            try
            {
                query = "DROP" + (isTemporary ? " TEMPORARY" : "") + " TABLE IF EXISTS " + table;
                return Query(query);
            }
            catch (Exception ex)
            {
                ErrQuery = query;
                HandleException(ex);
                return null;
            }
        }

        /// <summary>
        /// Performs a select statment
        /// </summary>
        /// <param name="fields">A comma-separated list of fields to select. Clause after SELECT keyword</param>
        /// <param name="from">Table(s) to select from. Clause after FROM keyword</param>
        /// <param name="options">(Optional) Any additional parts to add to the statement (ex. WHERE clause)</param>
        /// <returns></returns>
        public DBResult Select(string fields, string from, string options = null)
        {
            string query = "";
            try
            {
                query = "SELECT " + fields + " FROM " + from;
                if (options != null)
                {
                    query += " " + options;
                }
                return Query(query);
            }
            catch (Exception ex)
            {
                ErrQuery = query;
                HandleException(ex);
                return null;
            }
        }

        /// <summary>
        /// Performs a select statment
        /// </summary>
        /// <param name="fields">An array of fields to select, * for all. Clause after SELECT keyword</param>
        /// <param name="from">Table(s) to select from. Clause after FROM keyword</param>
        /// <param name="options">(Optional) Any additional parts to add to the statement (ex. WHERE clause)</param>
        /// <returns></returns>
        public DBResult Select(string[] fields, string from, string options = null)
        {
            string query = "";
            try
            {
                query = "SELECT ";
                foreach (string f in fields) { query += f + ","; }
                query = query.Substring(0, query.Length - 1);
                query += " FROM " + from;
                if (options != null)
                {
                    query += " " + options;
                }
                return Query(query);
            }
            catch (Exception ex)
            {
                ErrQuery = query;
                HandleException(ex);
                return null;
            }
        }

        /// <summary>
        /// Performs an insert statement
        /// </summary>
        /// <param name="fieldvals">Collection of fields and values</param>
        /// <param name="table">Table to insert into</param>
        /// <param name="addQuotes">(Optional) Automatically add quotes around values. Defaults to true.</param>
        /// <returns></returns>
        public DBResult Insert(string table, NameValueCollection fieldvals, bool addQuotes = true)
        {
            string query = "";
            try
            {
                query = "INSERT INTO " + table + " (";
                string fields = "";
                string vals = "";
                if (addQuotes)
                {
                    foreach (string key in fieldvals.Keys)
                    {
                        fields += "`" + key + "`,";
                        vals += (fieldvals[key] == "NULL" ? "NULL" : "'" + EncodeValue(fieldvals[key]) + "'") + ",";
                    }
                }
                else
                {
                    foreach (string key in fieldvals.Keys)
                    {
                        fields += key + ",";
                        vals += EncodeValuesInQuotes(fieldvals[key]) + ",";
                    }
                }
                query += fields.Substring(0, fields.Length - 1) + ") VALUES (" + vals.Substring(0, vals.Length - 1) + ")";
                return Query(query);
            }
            catch (Exception ex)
            {
                ErrQuery = query;
                HandleException(ex);
                return null;
            }
        }

        /// <summary>
        /// Performs an insert statement
        /// </summary>
        /// <param name="fieldvals">Collection of fields and values</param>
        /// <param name="table">Table to insert into</param>
        /// <param name="addQuotes">(Optional) Automatically add quotes around values. Defaults to true.</param>
        /// <returns></returns>
        public DBResult Insert(string table, Dictionary<string, string> fieldvals, bool addQuotes = true)
        {
            NameValueCollection nv = new NameValueCollection();
            foreach (string k in fieldvals.Keys)
            {
                nv[k] = fieldvals[k];
            }
            return Insert(table, nv, addQuotes);
        }

        /// <summary>
        /// Insert multiple records at once.
        /// </summary>
        /// <param name="table">Name of table to insert</param>
        /// <param name="fields">A comma-separated string of fields to insert (ex. UNIQUEKEY,UNIQUEITEM,CUSTOMER,PARTNER)</param>
        /// <param name="records">A multidimentional list of strings where the 1st is a list of records, and the 2nd is a list of fields in each record</param>
        /// <param name="addQuotes">(Optional) A flag indicating whether to add single quotes around each value (defaults to true)</param>
        /// <returns></returns>
        public DBResult InsertMulti(string table, string fields, List<List<string>> records, bool addQuotes = true)
        {
            string query = "";
            try
            {
                if (records.Count == 0) { throw new Exception("No values have been chosen to insert."); }
                query = "INSERT INTO " + table + " (" + fields + ") VALUES ";
                string vals = "";
                if (addQuotes)
                {
                    for (int row = 0; row < records.Count; row++)
                    {
                        vals = "(";
                        for (int col = 0; col < records[row].Count; col++)
                        {
                            vals += "'" + EncodeValue(records[row][col]) + "',";
                        }
                        vals = vals.Substring(0, vals.Length - 1) + "),";
                        query += vals;
                    }
                }
                else
                {
                    for (int row = 0; row < records.Count; row++)
                    {
                        vals = "(";
                        for (int col = 0; col < records[row].Count; col++)
                        {
                            vals += EncodeValuesInQuotes(records[row][col]) + ",";
                        }
                        vals = vals.Substring(0, vals.Length - 1) + "),";
                        query += vals;
                    }
                }
                query = query.Substring(0, query.Length - 1);
                return Query(query);
            }
            catch (Exception ex)
            {
                ErrQuery = query;
                HandleException(ex);
                return null;
            }
        }

        /// <summary>
        /// Insert multiple records at once.
        /// </summary>
        /// <param name="table">Name of table to insert</param>
        /// <param name="fields">A comma-separated string of fields to insert (ex. UNIQUEKEY,UNIQUEITEM,CUSTOMER,PARTNER)</param>
        /// <param name="records">A multidimentional array of strings where the 1st index refers to records, and the 2nd to fields in each record</param>
        /// <param name="addQuotes">(Optional) A flag indicating whether to add single quotes around each value (defaults to true)</param>
        /// <returns></returns>
        public DBResult InsertMultiA(string table, string fields, string[,] records, bool addQuotes = true)
        {
            List<List<string>> recs = new List<List<string>>();
            List<string> temp;
            int maxRows = records.GetLength(0);
            int maxCols = records.GetLength(1);
            for (int i = 0; i < maxRows; i++)
            {
                temp = new List<string>();
                for (int j = 0; j < maxCols; j++)
                {
                    temp.Add(records[i, j]);
                }
                recs.Add(temp);
            }
            return InsertMulti(table, fields, recs, addQuotes);
        }

        /// <summary>
        /// Performs an update statement
        /// </summary>
        /// <param name="table">Table to update</param>
        /// <param name="fieldvals">Collection of fields and values to update</param>
        /// <param name="options">(Optional) Any additional parts to add to the statement (ex. WHERE clause)</param>
        /// <param name="addQuotes">(Optional) Automatically add quotes around values. Defaults to true.</param>
        /// <returns></returns>
        public DBResult Update(string table, NameValueCollection fieldvals, string options = null, bool addQuotes = true)
        {
            string query = "";
            try
            {
                query = "UPDATE " + table + " SET ";
                if (addQuotes)
                {
                    foreach (string key in fieldvals.Keys)
                    {
                        query += key + "=" + "'" + fieldvals[key].Replace("'", "''").Replace(@"\", @"\\") + "',";
                    }
                }
                else
                {
                    foreach (string key in fieldvals.Keys)
                    {
                        query += key + "=" + fieldvals[key].Replace(@"\", @"\\") + ",";
                    }
                }
                query = query.Substring(0, query.Length - 1);
                if (options != null)
                {
                    query += " " + options;
                }
                return Query(query);
            }
            catch (Exception ex)
            {
                ErrQuery = query;
                HandleException(ex);
                return null;
            }
        }

        /// <summary>
        /// Performs an update statement
        /// </summary>
        /// <param name="table">Table to update</param>
        /// <param name="fieldvals">Collection of fields and values to update</param>
        /// <param name="options">(Optional) Any additional parts to add to the statement (ex. WHERE clause)</param>
        /// <param name="addQuotes">(Optional) Automatically add quotes around values. Defaults to true.</param>
        /// <returns></returns>
        public DBResult Update(string table, Dictionary<string, string> fieldvals, string options = null, bool addQuotes = true)
        {
            NameValueCollection nv = new NameValueCollection();
            foreach (string k in fieldvals.Keys)
            {
                nv[k] = fieldvals[k];
            }
            return Update(table, nv, options, addQuotes);
        }

        /// <summary>
        /// Performs a delete statement for records in a table
        /// </summary>
        /// <param name="table">Table to delete from</param>
        /// <param name="options">(Optional) Any additional parts to add to the statement (ex. WHERE clause)</param>
        /// <returns></returns>
        public DBResult Delete(string table, string options = null)
        {
            string query = "";
            try
            {
                query = "DELETE FROM " + table;
                if (options != null)
                {
                    query += " " + options;
                }
                return Query(query);
            }
            catch (Exception ex)
            {
                ErrQuery = query;
                HandleException(ex);
                return null;
            }
        }

        /// <summary>
        /// Performs a truncate statement for a table
        /// </summary>
        /// <param name="table">Table to truncate</param>
        /// <returns></returns>
        public DBResult Truncate(string table)
        {
            string query = "TRUNCATE " + table;
            try
            {
                return Query(query);
            }
            catch (Exception ex)
            {
                ErrQuery = query;
                HandleException(ex);
                return null;
            }
        }

        /// <summary>
        /// Performs a call statement
        /// </summary>
        /// <param name="procedure">Procedure (including parameters) to call.</param>
        /// <returns></returns>
        public DBResult Call(string procedure)
        {
            string query = "";
            try
            {
                query = "CALL " + procedure;
                return Query(query);
            }
            catch (Exception ex)
            {
                ErrQuery = query;
                HandleException(ex);
                return null;
            }
        }

        /// <summary>
        /// Gets a list of the field names of a table
        /// </summary>
        /// <param name="table">Table to read</param>
        /// <param name="uppercase">(Optional) Set to true to get uppercase fields</param>
        /// <returns></returns>
        public List<string> GetColumns(string table, bool uppercase = false)
        {
            string[] fnames = GetFieldNames(table);
            if (uppercase)
            {
                List<string> fields = new List<string>();
                foreach (string f in fnames) { fields.Add(f.ToUpper()); }
                return fields;
            }
            return fnames.ToList();
        }

        /// <summary>
        /// Checks if a table exists in a database and returns true on success
        /// </summary>
        /// <param name="table">Table to check</param>
        /// <param name="includeViews">(Optional) Set to true to include views in the results</param>
        /// <param name="db">(Optional) Database to check in if different from default</param>
        /// <returns></returns>
        public bool TableExists(string table, bool includeViews = false, string db = null)
        {
            try
            {
                string query = "SHOW FULL TABLES FROM " + (db == null ? DBInfo.Database : db) + (!includeViews ? " WHERE TABLE_TYPE!='VIEW'" : "");
                DBResult res = Query(query);
                string uTable = table.ToUpper();
                if (res.AffectedRows == 0) { return false; }
                else
                {
                    string tbl;
                    while (res.Read())
                    {
                        tbl = res.Field(0).ToUpper();
                        if (tbl == uTable) { return true; }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return false;
            }
        }

        /// <summary>
        /// Gets the fieldnames of the table specified
        /// </summary>
        /// <param name="tbl">Table to check</param>
        /// <returns></returns>
        public string[] GetFieldNames(string tbl)
        {
            string query = "SELECT * FROM " + tbl + " LIMIT 1";
            if (_OLE != null)
            {
                query = "SELECT * FROM " + tbl + " WHERE RECNO() < 2";
            }
            try
            {
                DBResult res = Query(query);
                return res.FieldNames;
            }
            catch (Exception ex)
            {
                ErrQuery = query;
                HandleException(ex);
                return null;
            }
        }

        /// <summary>
        /// Gets the number of fields of the table specified
        /// </summary>
        /// <param name="tbl">Table to check</param>
        /// <returns></returns>
        public int GetFieldCount(string tbl)
        {
            string query = "SELECT * FROM " + tbl + " LIMIT 1";
            try
            {
                DBResult res = Query(query);
                return res.FieldCount;
            }
            catch (Exception ex)
            {
                ErrQuery = query;
                HandleException(ex);
                return 0;
            }
        }

        /// <summary>
        /// Gets information about a table
        /// </summary>
        /// <param name="dbname">Database name</param>
        /// <param name="tablename">Table name</param>
        /// <returns></returns>
        public DBTable GetTableInfo(string dbname, string tablename)
        {
            string query = "SELECT COLUMN_NAME,ORDINAL_POSITION,COLUMN_DEFAULT,IS_NULLABLE,DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='" + dbname + "' AND TABLE_NAME='" + tablename + "' ORDER BY ORDINAL_POSITION";
            try
            {
                DBTable t = new DBTable(dbname, tablename);
                DBField f;
                DBResult res = Query(query);
                bool allownull;
                int position;
                string name, defaultval, datatype;
                while (res.Read())
                {
                    name = res.Field(0);
                    position = Int32.Parse(res.Field(1));
                    defaultval = res.Field(2, null);
                    allownull = (res.Field(3) == "YES" ? true : false);
                    datatype = res.Field(4);
                    f = new DBField(name, datatype, allownull, defaultval, position);
                    t.Fields.Add(f);
                }
                return t;
            }
            catch (Exception ex)
            {
                ErrQuery = query;
                HandleException(ex);
                return null;
            }
        }

        #endregion Pre-set Queries

        #region QUERYING

        /// <summary>
        /// Performs a query to on the database
        /// </summary>
        /// <param name="query">Query to perform</param>
        /// <returns></returns>
        public DBResult Query(string query)
        {
            try
            {
                query = query.Replace("'NULL'", "NULL");
                //DETERMINE IF CONNECTION IS OPEN
                IsConnectionOpen();
                //CHECK IF DATA NEEDS TO BE RETURNED
                if (query.Substring(0, 6).ToUpper() == "SELECT" || query.Substring(0, 4).ToUpper() == "SHOW")
                {
                    return ExecuteQuery(QueryType.SELECT, query);
                }
                return ExecuteQuery(QueryType.OTHER, query);
            }
            catch (Exception ex)
            {
                ErrQuery = query;
                HandleException(ex);
                return null;
            }
        }

        /// <summary>
        /// Executes a query and returns appriate data
        /// </summary>
        /// <param name="type">Type of query to execute</param>
        /// <param name="query">Query to execute</param>
        /// <param name="tries"># of attempts to execute query</param>
        /// <returns></returns>
        private DBResult ExecuteQuery(QueryType type, string query, int tries = 0)
        {
            try
            {
                DateTime start = DateTime.Now;
                DBResult result = null;
                if (_ODBC != null) //ODBC CONNECTION IS OPEN
                {
                    if (_ODBC.State == System.Data.ConnectionState.Broken || _ODBC.State == System.Data.ConnectionState.Closed)
                    {
                        //RECONNECT
                        Reconnect();
                    }
                    OdbcCommand command = _ODBC.CreateCommand();
                    command.CommandText = query;
                    if (type == QueryType.SELECT) //RETURNS DATA
                    {
                        OdbcDataReader data = command.ExecuteReader();
                        result = new DBResult(data, data.RecordsAffected, DateTime.Now.Subtract(start), query, NullDateYr, NullValue);
                    }
                    else //NO DATA RETURNED
                    {
                        if (IsReadOnly) { throw new Exception("This connection is read only. Only select statements are allowed."); }
                        int affectedRows = command.ExecuteNonQuery();
                        result = new DBResult(null, affectedRows, DateTime.Now.Subtract(start), query, NullDateYr, NullValue);
                    }
                }
                else //OLE CONNECTION IS OPEN
                {
                    if (_OLE.State == System.Data.ConnectionState.Broken || _OLE.State == System.Data.ConnectionState.Closed)
                    {
                        //RECONNECT
                        Reconnect();
                    }
                    OleDbCommand command = _OLE.CreateCommand();
                    command.CommandText = query;
                    if (type == QueryType.SELECT) //RETURNS DATA
                    {
                        OleDbDataReader data = command.ExecuteReader();
                        result = new DBResult(data, data.RecordsAffected, DateTime.Now.Subtract(start), query, NullDateYr, NullValue);
                    }
                    else //NO DATA RETURNED
                    {
                        if (IsReadOnly) { throw new Exception("This connection is read only. Only select statements are allowed."); }
                        int affectedRows = command.ExecuteNonQuery();
                        result = new DBResult(null, affectedRows, DateTime.Now.Subtract(start), query, NullDateYr, NullValue);
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                string temp = ex.Message.ToUpper();
                if (temp.Contains("CURRENT STATE IS CONNECTING"))
                {
                    if (tries < 5)
                    {
                        Thread.Sleep(50);
                        return ExecuteQuery(type, query, ++tries);
                    }
                }
                else if (temp.Contains("BEEN DISABLED") || temp.Contains("OPEN AND AVAILABLE") || temp.Contains("UNDERLYING CONNECTION WAS CLOSED"))
                {
                    if (tries == 0)
                    {
                        Reconnect();
                        return ExecuteQuery(type, query, ++tries);
                    }
                }
                throw ex;
            }
        }

        /// <summary>
        /// Duplicates a record and returns the new primary key. Returns null on failure.
        /// </summary>
        /// <param name="table">Table to create record in</param>
        /// <param name="query">Query used to select items to duplicate</param>
        /// <param name="primary">(Optional) Name of the primary key field, if different from uniquekey</param>
        /// <param name="destDB">(Optional) An open connection to another database. If provided, duplicate data will be inserted into this database, instead of the current.</param>
        /// <returns></returns>
        public string Duplicate(string table, string query, string primary = "uniquekey", DBConnect destDB = null)
        {
            try
            {
                string newQuery;
                DBResult res = Query(query);
                NameValueCollection nv = new NameValueCollection();
                if (res == null) throw new Exception(ErrMsg);
                List<string> fields = null;
                if (destDB == null)
                {
                    nv[primary] = Query("SELECT UUID_SHORT() AS UNIQUEKEY").ToXML().Element("record").Element("UNIQUEKEY").Value;
                    fields = GetColumns(table);
                    //READ EACH RECORD TO COPY
                    while (res.Data.Read())
                    {
                        newQuery = GetDuplicateQuery(this, table, res, fields, nv);
                        if (Query(newQuery) == null) { throw new Exception(ErrMsg); }
                    }
                }
                else
                {
                    nv[primary] = destDB.Query("SELECT UUID_SHORT() AS UNIQUEKEY").ToXML().Element("record").Element("UNIQUEKEY").Value;
                    fields = destDB.GetColumns(table);
                    //READ EACH RECORD TO COPY
                    while (res.Data.Read())
                    {
                        newQuery = GetDuplicateQuery(destDB, table, res, fields, nv);
                        if (destDB.Query(newQuery) == null) { throw new Exception(destDB.ErrMsg); }
                    }
                }

                return nv[primary];
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return null;
            }
        }

        /// <summary>
        /// Copies data from one table to another. Returns true on success
        /// </summary>
        /// <param name="fromTable">Table to copy from</param>
        /// <param name="toTable">Table to copy to</param>
        /// <param name="options">(Optional) Any additional parts to add to the statement (ex. WHERE clause) </param>
        /// <param name="destDB">(Optional) Destination database (defaults to current db)</param>
        /// <param name="replace">(Optional) A collection of fields and values to replace in the query</param>
        /// <param name="groupInsertMax">(Optional) The maximum number of records to insert in a single insert statement (defaulted to 1000)</param>
        /// <returns></returns>
        public bool Copy(string fromTable, string toTable, string options = null, DBConnect destDB = null, NameValueCollection replace = null, int groupInsertMax = 1000)
        {
            string query = null;
            try
            {
                if (destDB == null) { destDB = this; }
                query = "SELECT * FROM " + fromTable + (options == null ? "" : " " + options);
                List<string> fields = destDB.GetColumns(toTable, true);
                DBResult res = Query(query);
                if (res == null) throw new Exception(ErrMsg);
                if (res.AffectedRows == 0) { throw new Exception("No records found to copy."); }
                query = "";
                string insTemplate = "INSERT INTO " + toTable + " (`" + string.Join("`,`", fields) + "`) VALUES ";
                List<string> vals = new List<string>();
                while (res.Read())
                {
                    vals.Add(GetDuplicateQuery(destDB, toTable, res, fields, replace, true));
                    if (vals.Count() == groupInsertMax)
                    {
                        query = insTemplate + string.Join(",", vals);
                        if (destDB.Query(query) == null) { throw new Exception(destDB.ErrMsg); }
                        vals.Clear();
                        query = "";
                    }
                    //query = GetDuplicateQuery(toTable, res.Data, fields, replace);
                    //if (destDB.Query(query) == null) { throw new Exception(destDB.ErrMsg); }
                }
                if (vals.Count() > 0)
                {
                    query = insTemplate + string.Join(",", vals);
                    if (destDB.Query(query) == null) { throw new Exception(destDB.ErrMsg); }
                    vals.Clear();
                }
                return true;
            }
            catch (Exception ex)
            {
                ErrQuery = query;
                HandleException(ex);
                return false;
            }
        }

        /// <summary>
        /// Copies data from a query into a table
        /// </summary>
        /// <param name="toTable">Table to copy to</param>
        /// <param name="query">Query to perform to get data</param>
        /// <param name="destDB">(Optional) Destination database (defaults to current db)</param>
        /// <param name="replace">(Optional) A collection of fields and values to replace in the query</param>
        /// <param name="groupInsertMax">(Optional) The maximum number of records to insert in a single insert statement (defaulted to 1000)</param>
        /// <returns></returns>
        public bool CopyFromQuery(string toTable, string query, DBConnect destDB = null, NameValueCollection replace = null, int groupInsertMax = 1000)
        {
            try
            {
                DBOptions opt = new DBOptions();
                opt.MaxLinesInsert = groupInsertMax;
                if (replace != null)
                {
                    foreach (string k in replace.AllKeys) { opt.Replaces.Add(k, replace[k]); }
                }
                return CopyFromQuery(toTable, query, destDB, opt);
            }
            catch (Exception ex)
            {
                ErrQuery = query;
                HandleException(ex);
                return false;
            }
        }

        /// <summary>
        /// Copy data from query with additional options for updating
        /// </summary>
        /// <param name="toTable"></param>
        /// <param name="query"></param>
        /// <param name="destDB"></param>
        /// <param name="replace"></param>
        /// <param name="groupInsertMax"></param>
        /// <returns></returns>
        public bool CopyFromQuery(string toTable, string query, DBConnect destDB, DBOptions options)
        {
            try
            {
                if (destDB == null) { destDB = this; }
                List<string> fields = destDB.GetColumns(toTable, true);
                DBResult res = Query(query);
                if (res == null) throw new Exception(ErrMsg);
                if (res.AffectedRows == 0) { throw new Exception("No records found to copy."); }
                query = "";
                string insTemplate = "INSERT INTO " + toTable + " (`" + string.Join("`,`", fields) + "`) VALUES ";
                List<string> vals = new List<string>();
                while (res.Read())
                {
                    vals.Add(GetDuplicateQuery(destDB, toTable, res, fields, options.Replaces.ToNameValueCollection(), true, options.AutoIncFields));
                    if (vals.Count() == options.MaxLinesInsert)
                    {
                        query = insTemplate + string.Join(",", vals);
                        if (destDB.Query(query) == null) { throw new Exception(destDB.ErrMsg); }
                        vals.Clear();
                        query = "";
                    }
                }
                if (vals.Count() > 0)
                {
                    query = insTemplate + string.Join(",", vals);
                    if (destDB.Query(query) == null) { throw new Exception(destDB.ErrMsg); }
                    vals.Clear();
                }
                return true;
            }
            catch (Exception ex)
            {
                ErrQuery = query;
                HandleException(ex);
                return false;
            }
        }

        /// <summary>
        /// Creates an insert statement to duplicate a record
        /// </summary>
        /// <param name="table">Name of the table to insert into</param>
        /// <param name="destDb">Destination database</param>
        /// <param name="res">A DBResult object containing results from a select statement</param>
        /// <param name="fields">A list of fields to insert in the destination table</param>
        /// <param name="replaces">(Optional) Collection of fields to apply replaces</param>
        /// <param name="valsOnly">(Optional) If set to true, returns only the values portion of the query (defaults to false)</param>
        /// <returns></returns>
        private string GetDuplicateQuery(DBConnect destDb, string table, DBResult res, List<string> fields, NameValueCollection replaces = null, bool valsOnly = false, Dictionary<string, int> autoIncrements = null)
        {
            string flist = "", vlist = "", newQuery;
            string col, val;

            string dbName = destDb.DBInfo.Database;
            string tbName = table;
            string[] dbTbSplit = table.Split('.');
            if (dbTbSplit.Length > 1) { dbName = dbTbSplit[0]; tbName = dbTbSplit[1]; }
            DBTable tbInfo = destDb.GetTableInfo(dbName, tbName);
            Dictionary<string, string> nv = tbInfo.GetEmptyRecord();
            for (int i = 0; i < res.FieldCount; i++)
            {
                col = res.Data.GetName(i).ToUpper();
                if (fields.Contains(col))
                {
                    val = res.Field(i);
                    nv[col] = EncodeValue(val);
                }
            }
            string keyupper = null;
            if (replaces != null)
            {
                foreach (string key in replaces.AllKeys)
                {
                    keyupper = key.ToUpper();
                    foreach (string nvkey in nv.Keys)
                    {
                        if (nvkey.ToUpper() == keyupper)
                        {
                            nv[nvkey] = replaces[key];
                            break;
                        }
                    }
                }
            }

            if (autoIncrements != null)
            {
                List<string> updKeys = new List<string>();
                foreach (string key in autoIncrements.Keys)
                {
                    keyupper = key.ToUpper();
                    foreach (string nvkey in nv.Keys)
                    {
                        if (nvkey.ToUpper() == keyupper)
                        {
                            nv[nvkey] = autoIncrements[key].ToString();
                            updKeys.Add(key);
                            break;
                        }
                    }
                }
                updKeys.ForEach(k => autoIncrements[k]++);
            }

            foreach (string key in nv.Keys)
            {
                vlist += (nv[key] == "NULL" ? "NULL," : "'" + nv[key] + "',");
            }
            //REMOVE LAST COMMA
            vlist = vlist.Substring(0, vlist.Length - 1);
            if (valsOnly)
            {
                newQuery = "(" + vlist + ")";
            }
            else
            {
                flist = string.Join("`,`", nv.Keys);
                newQuery = "INSERT INTO " + table + " (`" + flist + "`) VALUES (" + vlist + ")";
            }
            return newQuery;
        }

        #endregion QUERYING

        #region CLOSING

        /// <summary>
        /// Close any open connections
        /// </summary>
        public bool Close()
        {
            if (_ODBC != null)
            {
                _ODBC.Close();
                _ODBC = null;
                DBInfo = null;
            }
            if (_OLE != null)
            {
                _OLE.Close();
                _OLE = null;
                SourceFolder = null;
            }
            return true;
        }

        #endregion CLOSING

        /// <summary>
        /// Determines if a connection is open
        /// </summary>
        /// <returns></returns>
        private void IsConnectionOpen()
        {
            if (_ODBC == null && _OLE == null) { throw new Exception("No open connection found. Connect to a database and try again."); }
        }

        /// <summary>
        /// Handles exceptions
        /// </summary>
        /// <param name="ex">Exception to handle</param>
        private bool HandleException(Exception ex)
        {
            ErrMsg = ex.Message;
            try
            {
                int bracket = 0;
                for (int i = 0; i < 4; i++)
                {
                    bracket = ErrMsg.IndexOf(']', bracket + 1);
                    if (bracket == -1) { break; }
                }
                if (bracket != -1)
                {
                    ErrMsg = ErrMsg.Substring(bracket + 1, ErrMsg.Length - bracket - 1);
                }
            }
            catch { }
            if (ThrowExceptions) { throw new DBEx(ErrMsg, ErrQuery); }
            return false;
        }

        private string GetFieldValue(DbDataReader data, int i)
        {
            Type t = data.GetFieldType(i);
            DateTime dt;
            //ENTER NULL FIELD
            if (data[i] == DBNull.Value) { return "NULL"; }
            //HANDLE DATE AND DATETIME
            else if (t.FullName == "System.DateTime")
            {
                dt = DateTime.Parse(data[i].ToString());
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
                return data[i].ToString();
            }
        }

        /// <summary>
        /// Encodes values for a query (single quotes and back slashes)
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        private string EncodeValue(string val)
        {
            return val.Replace("'", "''").Replace(@"\", @"\\");
        }

        /// <summary>
        /// Encodes values for a query (back slashes)
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        private string EncodeValuesInQuotes(string val)
        {
            if (val == "''") { return val; }
            bool hasQuotes = false;
            if (val.Length > 2)
            {
                if (val[0] == '\'')
                {
                    hasQuotes = true;
                    val = val.Substring(1, val.Length - 2);
                }
            }
            if (hasQuotes)
            {
                return "'" + EncodeValue(val) + "'";
            }
            return EncodeValue(val);
        }

        /// <summary>
        /// Gets the type of query to execute
        /// </summary>
        private enum QueryType
        {
            /// <summary>
            /// Query that returns data
            /// </summary>
            SELECT,

            /// <summary>
            /// Query that does not return data
            /// </summary>
            OTHER
        }
    }
}