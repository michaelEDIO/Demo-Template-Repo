using System;
using System.Collections.Generic;
using System.Net;
using EDIOptions.AppCenter.Session;

namespace EDIOptions.AppCenter.Database
{
    public static class ConnectionsMgr
    {
#if DEBUG
        private const string _DefaultDriver = "MySQL ODBC 5.1 Driver";
#else
        private const string _DefaultDriver = "MySQL ODBC 5.2 ANSI Driver";
#endif

        private const string databaseHome = "home";

        private const string tableConnectionIDInfo = "connectcomp";
        private const string tableAllPorts = "netadmin.allconnports";

        private const string columnOCConnID = "oc_connid";
        private const string columnNPConnID = "np_connid";
        private const string columnSHConnID = "sh_connid";
        private const string columnSLConnID = "sl_connid";

        private const string columnCustomer = "customer";
        private const string columnServer = "connectserver";
        private const string columnPort = "connectport";
        private const string columnIsTest = "istest";
        private const string columnConnectID = "connectid";

        private const string idTestOC = "ATIT";
        private const string idTestNP = "TNET";
        private const string idTestSH = "TSHR";
        private const string idTestSL = "TSLS";

        private static Dictionary<string, NetworkCredential> cred = new Dictionary<string, NetworkCredential>()
            {
                {"TSLS", new NetworkCredential("WebTest14","42xZ#8Edi6")},
                {"TSHR", new NetworkCredential("WebTest14","42xZ#8Edi6")},
                {"ATIT", new NetworkCredential("WebTest14","42xZ#8Edi6")},
                {"TPRE", new NetworkCredential("WebTest14","42xZ#8Edi6")},
                {"ALTS", new NetworkCredential("WebPHP88","3OpC#+Edi3")},
                {"OPT2", new NetworkCredential("WebPHP88","3OpC#+Edi3")},
                {"SLS1", new NetworkCredential("WebPHP88","3OpC#+Edi3")},
                {"ESIC", new NetworkCredential("WebPHP88","3OpC#+Edi3")},
            };

        private static NetworkCredential defaultCred = new NetworkCredential("AutomationUser", "12ImnW$Q");

        private static DatabaseInfo _defaultAdmin = new DatabaseInfo()
        {
            Id = "ADMN",
            Server = "sql.rm.edioptions.com",
            Port = 3309,
            Username = "AutomationUser",
            Password = "12ImnW$Q",
            Database = "admin",
            Driver = _DefaultDriver
        };

        private static DatabaseInfo _defaultAuth = new DatabaseInfo()
        {
            Id = "ADMN",
            Server = "edi-optcenter.com",
            Port = 3306,
            Username = "edioptio_ASPUsr",
            Password = "4a[^ezK",
            Database = "edioptio_ecgbp",
            Driver = _DefaultDriver
        };

        public static string GetDefaultDriver()
        {
            return _DefaultDriver;
        }

        public static bool SetConnIDs(User user, bool isTest)
        {
            if (isTest)
            {
                // Use default connections when debugging
                user.OCConnID = idTestOC;
                user.NPConnID = idTestNP;
                user.SHConnID = idTestSH; // Use OC ID
                user.SLConnID = idTestSL; // Use OC ID
                return true;
            }
            DBConnect connection = new DBConnect();
            if (!connection.Connect(ConnectionsMgr.GetAdminConnInfo()))
            {
                return false;
            }
            try
            {
                using (var res = connection.Select(new[] { columnOCConnID, columnNPConnID, columnSHConnID, columnSLConnID }, tableConnectionIDInfo, string.Format("WHERE {0}='{1}'", columnCustomer, user.Customer)))
                {
                    if (!res.Read())
                    {
                        // No info for customer?
                        ProgramLog.LogError(user.UserName, user.Customer, "EDIO", "ConnectionsMgr", "SetConnIDs", string.Format("Unable to find connection info in {0} for customer {1}", tableConnectionIDInfo, user.Customer));
                        connection.Close();
                        return false;
                    }
                    user.OCConnID = res.Field(0);
                    user.NPConnID = res.Field(1);
                    user.SHConnID = res.Field(2);
                    user.SLConnID = res.Field(3);
                }
                connection.Close();
                return true;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, "ConnectionsMgr", "SetConnIDs", e.Message);
                connection.Close();
                return false;
            }
        }

        #region Fetching opened connections

        public static DBConnect GetAdminConnection(string database = "")
        {
            DBConnect db = new DBConnect();
            db.Connect(GetAdminConnInfo(database));
            return db;
        }

        public static DBConnect GetAuthConnection(string database = "")
        {
            DBConnect db = new DBConnect();
            db.Connect(GetAuthConnInfo(database));
            return db;
        }

        public static DBConnect GetOCConnection(User user, string database = "")
        {
            DBConnect db = new DBConnect();
            db.Connect(GetOCConnInfo(user, database));
            return db;
        }

        public static DBConnect GetSharedConnection(User user, string database = "")
        {
            DBConnect db = new DBConnect();
            db.Connect(GetSHConnInfo(user, database));
            return db;
        }

        public static DBConnect GetSalesConnection(User user, string database = "")
        {
            DBConnect db = new DBConnect();
            db.Connect(GetSLConnInfo(user, database));
            return db;
        }

        #endregion Fetching opened connections

        #region Fetching connection info

        public static DatabaseInfo GetAdminConnInfo(string database = "")
        {
            DatabaseInfo di = new DatabaseInfo();
            di.Driver = _defaultAdmin.Driver;
            di.Server = _defaultAdmin.Server;
            di.Database = string.IsNullOrWhiteSpace(database) ? _defaultAdmin.Database : database;
            di.Port = _defaultAdmin.Port;
            di.Username = _defaultAdmin.Username;
            di.Password = _defaultAdmin.Password;
            return di;
        }

        public static DatabaseInfo GetAuthConnInfo(string database = "")
        {
#if DEBUG
            return GetAdminConnInfo(database);
#else
            DatabaseInfo di = new DatabaseInfo();
            di.Driver = _defaultAuth.Driver;
            di.Server = _defaultAuth.Server;
            di.Database = string.IsNullOrWhiteSpace(database) ? _defaultAuth.Database : database;
            di.Port = _defaultAuth.Port;
            di.Username = _defaultAuth.Username;
            di.Password = _defaultAuth.Password;
            return di;
#endif
        }

        public static DatabaseInfo GetOCConnInfo(User user, string database = "")
        {
            return _GetConnInfo(user, user.OCConnID, database);
        }

        public static DatabaseInfo GetNPConnInfo(User user, string database = "")
        {
            return _GetConnInfo(user, user.NPConnID, database);
        }

        public static DatabaseInfo GetSHConnInfo(User user, string database = "")
        {
            return _GetConnInfo(user, user.SHConnID, database);
        }

        public static DatabaseInfo GetSLConnInfo(User user, string database = "")
        {
            return _GetConnInfo(user, user.SLConnID, database);
        }

        private static DatabaseInfo _GetConnInfo(User user, string connID, string database = "")
        {
            DatabaseInfo dbInfo = new DatabaseInfo();

            DBConnect connection = new DBConnect();
            if (!connection.Connect(ConnectionsMgr.GetAdminConnInfo()))
            {
                ProgramLog.LogError(user, "ConnectionInfo", "_GetConnInfo", "Unable to connect to admin database.");
                return dbInfo;
            }

            using (var res = connection.Select(new[] { columnServer, columnPort, columnIsTest }, tableAllPorts, string.Format("WHERE {0}='{1}'", columnConnectID, connID.SQLEscape())))
            {
                if (!res.Read())
                {
                    ProgramLog.LogError(user, "ConnectionInfo", "_GetConnInfo", string.Format("Unable to find info in table \"{0}\" for connection ID \"{1}\".", tableConnectionIDInfo, connID.SQLEscape()));
                    connection.Close();
                    return dbInfo;
                }
                dbInfo.Server = res.Field(0);
                dbInfo.Port = (int)res.Field2(1);
                dbInfo.IsTest = (int)res.Field2(2) != 0;
            }
            connection.Close();

            dbInfo.Id = connID;
            dbInfo.Database = string.IsNullOrWhiteSpace(database) ? databaseHome : database;
            dbInfo.Driver = _DefaultDriver;
            if (cred.ContainsKey(dbInfo.Id))
            {
                dbInfo.Username = cred[dbInfo.Id].UserName;
                dbInfo.Password = cred[dbInfo.Id].Password;
            }
            else
            {
                dbInfo.Username = defaultCred.UserName;
                dbInfo.Password = defaultCred.Password;
            }
            return dbInfo;
        }

        #endregion Fetching connection info
    }
}