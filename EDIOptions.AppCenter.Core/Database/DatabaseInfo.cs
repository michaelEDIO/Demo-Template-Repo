using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EDIOptions.AppCenter.Database
{
    /// <summary>
    /// A class that holds database connection information.
    /// </summary>
    public class DatabaseInfo
    {
        /// <summary>
        /// A class that holds database connection information.
        /// </summary>
        public DatabaseInfo()
        {
            Server = "";
            Port = 0;
            Username = "";
            Password = "";
            Database = "";
            IsTest = false;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="copy">DatabaseInfo object to copy</param>
        public DatabaseInfo(DatabaseInfo copy)
        {
            Id = copy.Id;
            Server = copy.Server;
            Port = copy.Port;
            Username = copy.Username;
            Password = copy.Password;
            Database = copy.Database;
            IsTest = copy.IsTest;
            Driver = copy.Driver;
        }

        /// <summary>
        /// Name given to this database connection.
        /// </summary>
        /// <remarks>public;COM</remarks>
        /// <returns>string</returns>
        public string Id { get; set; }

        /// <summary>
        /// Server ip used for this connection.
        /// </summary>
        /// <remarks>public;COM</remarks>
        /// <returns>string</returns>
        public string Server { get; set; }

        /// <summary>
        /// Port used for this connection.
        /// </summary>
        /// <remarks>public;COM</remarks>
        /// <returns>string</returns>
        public int Port { get; set; }

        /// <summary>
        /// Username used to access connection.
        /// </summary>
        /// <remarks>public;COM</remarks>
        /// <returns>string</returns>
        public string Username { get; set; }

        /// <summary>
        /// Password used to access connection.
        /// </summary>
        /// <remarks>public;COM</remarks>
        /// <returns>string</returns>
        public string Password { get; set; }

        /// <summary>
        /// Database to be accessed by connection.
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Gets or sets whether this is a test connection
        /// </summary>
        public bool IsTest { get; set; }

        /// <summary>
        /// Gets or sets the ODBC Driver to use
        /// </summary>
        public string Driver { get; set; }

        /// <summary>
        /// Gets connection information as string to be passed into program as arguments (server port database username password)
        /// </summary>
        /// <returns></returns>
        public string GetDiArgs()
        {
            return Server + " " + Port.ToString() + " " + Database + " " + Username + " " + Password;
        }
    }
}
