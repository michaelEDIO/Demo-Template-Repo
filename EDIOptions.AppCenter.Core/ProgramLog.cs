using EDIOptions.AppCenter.Database;
using EDIOptions.AppCenter.Session;
using System;
using System.Collections.Generic;

namespace EDIOptions.AppCenter
{
    public static class ProgramLog
    {
        private const string tableLog = "oc_log_apps";

        private const string columnLogTime = "time";
        private const string columnSource = "source";
        private const string columnMethod = "method";
        private const string columnStatus = "status";
        private const string columnMessage = "description";
        private const string columnIsTest = "istest";

        public static void LogError(User user, string source, string method, string message)
        {
            if (user != null)
            {
                _Log(user.UserName, user.Customer, user.ActivePartner, source, method, "ERROR", message);
            }
            else
            {
                _Log("", "EDIO", "EDIO", source, method, "ERROR", message);
            }
        }

        public static void LogError(string username, string customer, string partner, string source, string method, string message)
        {
            _Log(username, customer, partner, source, method, "ERROR", message);
        }

        private static void _Log(string username, string customer, string partner, string source, string method, string type, string message)
        {
            if (customer == "")
            {
                customer = "EDIO";
            }
            if (partner == "")
            {
                partner = "EDIO";
            }
            string sCustomer = TruncateAndEscape(customer, 4);
            string sPartner = TruncateAndEscape(partner, 4);
            string sUsername = TruncateAndEscape(username, 40);
            string sSource = TruncateAndEscape(source, 32);
            string sMethod = TruncateAndEscape(method, 32);
            string sMessage = (string.IsNullOrWhiteSpace(message) ? "" : message).SQLEscape();
            bool isTest = false;
#if DEBUG
            isTest = true;
#endif
            var connection = new DBConnect();
            try
            {
                connection.Connect(ConnectionsMgr.GetAdminConnInfo());
                Dictionary<string, string> insertVals = new Dictionary<string, string>()
                {
                    {_Column.UniqueKey, connection.GetNewKey()},
                    {columnLogTime, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},
                    {columnSource, sSource},
                    {columnMethod, sMethod},
                    {columnStatus, type},
                    {columnMessage, sMessage},
                    {_Column.UserName, sUsername},
                    {_Column.Customer, sCustomer},
                    {_Column.Partner, sPartner},
                    {columnIsTest, isTest ? "1" : "0"}
                };
                connection.Insert(tableLog, insertVals.ToNameValueCollection());
            }
            catch { }
            connection.Close();
        }

        private static string TruncateAndEscape(string str, int maxLen)
        {
            if (str == null)
            {
                return "";
            }
            string temp = str;
            if (temp.Length > maxLen)
            {
                temp = temp.Substring(0, maxLen);
            }
            temp = temp.SQLEscape();
            if (temp.Length > maxLen)
            {
                return "";
            }
            else
            {
                return temp;
            }
        }
    }
}