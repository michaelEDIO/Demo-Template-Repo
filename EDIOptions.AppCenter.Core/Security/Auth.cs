using System;
using System.Text;
using System.Web.Security;
using EDIOptions.AppCenter.Database;
using EDIOptions.AppCenter.Session;

namespace EDIOptions.AppCenter.Security
{
    public static class Auth
    {
        private const string tableOCAuth = "oc_asp_auth";

        private const string columnUserName = "username";
        private const string columnSalt = "salt";
        private const string columnPassword = "password";

        private const string columnSessionID = "sessionid";
        private const string columnCreateDate = "createdate";
        private const string columnActivePartner = "activepartner";
        private const string columnIsTest = "istest";

        private const int OCSessionTimeOutSeconds = 60; //10 TIMED OUT OFTEN (SENDING TO 404 PAGE), TRYING 30 SECONDS. CHANGED TO 60 SECS BECAUSE OF TEMPORARY TIME DIFF BETWEEN THE TWO SERVERS

        /// <summary>
        /// Authenticates the given user.
        /// </summary>
        /// <param name="usUserName">The username to check.</param>
        /// <param name="usPassword">The password to check.</param>
        /// <returns>True if the login info is valid, false otherwise.</returns>
        public static bool AuthenticateUser(string usUserName, string usPassword)
        {
            string sUserName = usUserName.SQLEscape();
            string sStoredHash = "";
            DBConnect connect = new DBConnect();
            try
            {
                connect.Connect(ConnectionsMgr.GetAuthConnInfo());
                using (var queryUserAuthInfo = connect.Select(columnPassword, _Table.Users, string.Format("WHERE {0}='{1}'", columnUserName, sUserName)))
                {
                    if (queryUserAuthInfo.AffectedRows <= 0)
                    {
                        connect.Close();
                        return false;
                    }
                    queryUserAuthInfo.Read();
                    sStoredHash = Encoding.UTF8.GetString((byte[])queryUserAuthInfo.Field2(0));
                }
                connect.Close();
                return MD5Crypt.Verify(usPassword, sStoredHash);
            }
            catch(Exception ex)
            {
                ProgramLog.LogError(null, "Auth", "AuthenticateUser", ex.Message + " "+ ex.StackTrace);
                connect.Close();
                return false;
            }
        }

        /// <summary>
        /// Checks if a given encrypted authentication ticket is valid.
        /// </summary>
        /// <param name="ecTicket">The ticket to check.</param>
        /// <param name="user">The user to check against.</param>
        /// <returns>True if the info is valid, false otherwise.</returns>
        public static bool VerifyFormAuthTicket(string ecTicket, string user)
        {
            if (string.IsNullOrEmpty(ecTicket) || ecTicket.Length > 4096)
            {
                return false;
            }
            try
            {
                FormsAuthenticationTicket dcTicket = FormsAuthentication.Decrypt(ecTicket);
                return !dcTicket.Expired && dcTicket.Name == user;
            }
            catch
            {
                return false;
            }
        }

        public static OCUserInfo GetOCRecord(string usToken)
        {
            OCUserInfo info = new OCUserInfo();
            if (string.IsNullOrEmpty(usToken))
            {
                return info;
            }
            string sToken = usToken.SQLEscape();
            DBConnect connection = new DBConnect();
            DateTime expTime = new DateTime();
            try
            {
                connection.Connect(ConnectionsMgr.GetAdminConnInfo());
                using (var queryUserAuthInfo = connection.Select(new[] { columnUserName, columnActivePartner, columnIsTest, columnCreateDate }, tableOCAuth, string.Format("WHERE {0}='{1}'", columnSessionID, sToken)))
                {
                    if (queryUserAuthInfo.AffectedRows <= 0)
                    {
                        connection.Close();
                        return info;
                    }
                    queryUserAuthInfo.Read();
                    info.UserName = queryUserAuthInfo.Field(0);
                    info.ActivePartner = queryUserAuthInfo.Field(1);
                    info.IsTest = queryUserAuthInfo.Field(2) == "1";
                    expTime = (DateTime)queryUserAuthInfo.Field2(3, DateTime.MinValue);
                    connection.Delete(tableOCAuth, string.Format("WHERE {0}='{1}'", columnSessionID, sToken));
                }
                connection.Close();
                var authLimit = DateTime.Now.AddSeconds(-OCSessionTimeOutSeconds);
                if (expTime >= authLimit) //expire date must be within the last OCSessionTimeOutSeconds seconds
                {
                    info.IsValid = true;
                }
                else
                {
                    throw new Exception("Authentication time is not within range allowed. Auth Time: " + expTime.ToString("yyyy-MM-dd HH:mm:ss") + ", Auth Limit: " + authLimit.ToString("yyyy-MM-dd HH:mm:ss"));
                }
                return info;
            }
            catch (Exception ex)
            {
                ProgramLog.LogError(null, "Auth", "GetOCRecord", ex.Message);
                connection.Close();
                return info;
            }
        }
    }
}