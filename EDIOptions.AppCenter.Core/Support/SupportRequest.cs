using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using EDIOptions.AppCenter.Database;

namespace EDIOptions.AppCenter.Support
{
    public static class SupportRequest
    {
        private const string databaseEmailRequest = "admin";

        private const string tableEmailReq = "emailreq";

        private const string columnUniqueKey = "unique_key";
        private const string columnRequestDate = "reqdate";
        private const string columnToEmail = "to_email";
        private const string columnFromEmail = "from_email";
        private const string columnSubject = "subject";
        private const string columnMessage = "message";
        private const string columnProcessed = "processed";
        private const string columnCustomer = "customer";
        private const string columnPartner = "partner";
        private const string columnSendAfter = "sendafter";

        public static bool Submit(HttpRequest request, ReportDetail req, string usUserName, string usCustomer, string usPartner)
        {
            if (request == null || req == null)
            {
                return false;
            }
            DBConnect connectionAdmin = new DBConnect();
            try
            {
                connectionAdmin.Connect(new DatabaseInfo(ConnectionsMgr.GetAdminConnInfo()) { Database = databaseEmailRequest });

                string defaultToEmail = "support@edioptions.com";
                string defaultFromEmail = "optcenter@edioptions.com";

                StringBuilder builtMessage = new StringBuilder();
                builtMessage.AppendLine("From: " + req.Name);
                builtMessage.AppendLine("Company: " + req.Company);
                builtMessage.AppendLine("Email: " + req.Email);
                builtMessage.AppendLine("Message: " + req.Message);
                builtMessage.AppendLine();
                builtMessage.AppendLine("Additional Info");
                builtMessage.AppendLine("IP Address: " + request.UserHostAddress);
                builtMessage.AppendLine("Browser Info: " + request.UserAgent);
                builtMessage.AppendLine("Referral: " + request.UrlReferrer.ToString());
                DateTime requestTime = DateTime.Now;
                var vals = new Dictionary<string, string>()
                {
                    {columnUniqueKey, connectionAdmin.GetNewKey()},
                    {columnCustomer, (usCustomer ?? "").SQLEscape()},
                    {columnPartner, (usPartner ?? "").SQLEscape()},
                    {columnRequestDate, requestTime.ToString("yyyy-MM-dd HH:mm:ss")},
                    {columnToEmail, defaultToEmail},
                    {columnFromEmail, defaultFromEmail},
                    {columnSubject, "EDIOC- Support Submission"},
                    {columnMessage, WrapTextTo70(builtMessage.ToString().SQLEscape())},
#if DEBUG
                    {columnProcessed, "Y"},
#else
                    {columnProcessed, ""},
#endif
                    {columnSendAfter, requestTime.ToString("yyyy-MM-dd HH:mm:ss")}
                };
                var result = connectionAdmin.Insert(tableEmailReq, vals.ToNameValueCollection());
                return result.AffectedRows > 0;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(usUserName, usCustomer, usPartner, "SupportRequest", "Submit", e.Message);
                return false;
            }
        }

        private static string WrapTextTo70(string text)
        {
            string[] lines = text.Split(new[] { '\n' });
            StringBuilder ret = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                ret.AppendLine(WrapLineTo70(lines[i]));
            }
            return ret.ToString();
        }

        private static string WrapLineTo70(string input)
        {
            string[] words = input.Split(' ');
            StringBuilder ret = new StringBuilder();
            string tempLine = "";
            for (int i = 0; i < words.Length; i++)
            {
                if (tempLine.Length + words[i].Length <= 70)
                {
                    tempLine += " " + words[i];
                }
                else
                {
                    ret.AppendLine(tempLine);
                    tempLine = words[i];
                }
            }
            if (tempLine.Length > 0)
            {
                ret.Append(tempLine);
            }
            return ret.ToString();
        }
    }
}