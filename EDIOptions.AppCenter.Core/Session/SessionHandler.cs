using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.SessionState;
using EDIOptions.AppCenter.Database;

namespace EDIOptions.AppCenter.Session
{
    public static class SessionHandler
    {
        private const string tableUserInfo = "users";
        private const string tablePartnerInfo = "partner";
        private const string tableCustomerInfo = "company";

        private const string columnUserName = "username";
        private const string columnEmail = "email";
        private const string columnOrgID = "orgid";
        private const string columnFirstName = "firstname";
        private const string columnLastName = "lastname";
        private const string columnLevel = "level";
        private const string columnPartnerList = "partners";
        private const string columnCustomer = "customer";
        private const string columnPartner = "partner";

        private const string columnCompanyName = "customername";
        private const string columnPartnerName = "partnername";

        /// <summary>
        /// Begin a session. Must be called whenever a page loads.
        /// </summary>
        /// <param name="usUserName">The username to initialize a session for.</param>
        /// <param name="request">The request from the user's browser.</param>
        /// <returns>True if the session was initialized successfully, false otherwise.</returns>
        public static bool BeginSession(string usUserName, HttpRequest request, OCUserInfo userInfo = null)
        {
            try
            {
                // Check existence of session
                HttpSessionState session = HttpContext.Current.Session;
                if (session == null)
                {
                    return false;
                }

                User user = new User();
                bool isTest = false;

                //CHECK DEVELOPMENT/TEST ENVIRONMENT
                string hostname = request.Url.Authority;
                if (hostname == "10.0.0.245:30658")
                {
                    isTest = true;
                }
#if DEBUG
                isTest = true;
#endif
                if (userInfo != null)
                {
                    isTest = userInfo.IsTest;
                }
                if (!GetUserInfo(user, usUserName, isTest))
                {
                    return false;
                }
                if (userInfo != null)
                {
                    for (int i = 0; i < user.PartnerList.Count; i++)
                    {
                        if (user.PartnerList[i].ID == userInfo.ActivePartner)
                        {
                            user.PartnerIndex = i;
                            break;
                        }
                    }
                }

                session[SKeys.User] = user;
                session[SKeys.IsTest] = isTest;
                session[SKeys.LandingPg] = "Default.aspx";
                session[SKeys.TokenSet] = new HashSet<string>();
                session[SKeys.TrxDict] = ProcessQueue.GetReqTypeDict(user);
                session[SKeys.IsOCSession] = userInfo != null;

                return true;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(usUserName, "EDIO", "EDIO", "SessionHandler", "BeginSession", e.Message);
                return false;
            }
        }

        public static bool BeginGuestSession()
        {
            try
            {
                // Check existence of session
                HttpSessionState session = HttpContext.Current.Session;
                HttpRequest request = HttpContext.Current.Request;
                if (session == null) { return false; }

                User user = new User();
                user.UserName = "Guest";
                user.Email = "guest@edioptions.com";
                user.FirstName = "Guest";
                user.LastName = "";
                user.Level = 1;
                user.Customer = "GST1";
                user.PartnerList = new List<PartnerDetail>();
                user.IsGuest = true;
                List<string> partnerList = new List<string>() { "PART" };
                bool isTest = false;
                //CHECK DEVELOPMENT/TEST ENVIRONMENT
                string hostname = request.Url.Authority;
                if (hostname == "10.0.0.245:30658")
                {
                    isTest = true;
                }
#if DEBUG
                isTest = true;
#endif

                // Set connection IDs
                if (!ConnectionsMgr.SetConnIDs(user, isTest))
                {
                    // No Conn IDs?
                    ProgramLog.LogError(user, "SessionHandler", "GetUserInfo", string.Format("Unable to get connection IDs for customer {0} and partner {1}.", user.Customer, user.ActivePartner));
                    return false;
                }
                DBConnect connection = new DBConnect();
                // Set partner info
                connection.Connect(ConnectionsMgr.GetAdminConnInfo());
                using (var res = connection.Select(new[] { columnPartner, columnPartnerName }, tablePartnerInfo, string.Format("WHERE {0} IN ({1})", columnPartner, string.Join(",", partnerList.Select(p => "'" + p + "'")))))
                {
                    while (res.Read())
                    {
                        user.PartnerList.Add(new PartnerDetail() { ID = res.Field(0), FullName = res.Field(1) });
                    }
                }
                user.PartnerIndex = 0;
                connection.Close();
                // Set extra company info.
                connection.Connect(ConnectionsMgr.GetOCConnInfo(user));
                using (var res = connection.Select(columnCompanyName, tableCustomerInfo, string.Format("WHERE {0}='{1}'", columnCustomer, user.Customer)))
                {
                    if (res.AffectedRows == 0)
                    {
                        // No company name?
                        ProgramLog.LogError(user, "SessionHandler", "GetUserInfo", string.Format("Unable to find company name in {0} for customer {1}", tableCustomerInfo, user.Customer));
                        connection.Close();
                        return false;
                    }
                    res.Read();
                    user.CompanyName = res.Field(0);
                }
                connection.Close();

                session[SKeys.User] = user;
                session[SKeys.IsTest] = isTest;
                session[SKeys.LandingPg] = "Default.aspx";
                session[SKeys.TokenSet] = new HashSet<string>();
                session[SKeys.TrxDict] = ProcessQueue.GetReqTypeDict(user);
                session[SKeys.IsOCSession] = false;
                return true;
            }
            catch (Exception e)
            {
                ProgramLog.LogError("Guest", "EDIO", "EDIO", "SessionHandler", "BeginGuestSession", e.Message);
                return false;
            }
        }

        /// <summary>
        /// Clears a session.
        /// </summary>
        public static void ClearSession()
        {
            HttpContext.Current.Session.Clear();
        }

        /// <summary>
        /// Fetches user information based on the username.
        /// </summary>
        /// <param name="user">The user object.</param>
        /// <param name="usUserName">The username of the user.</param>
        /// <returns>True if successful, false otherwise.</returns>
        private static bool GetUserInfo(User user, string usUserName, bool isTest)
        {
            string sUserName = usUserName.SQLEscape();
            List<string> partnerList = new List<string>();
            List<PartnerDetail> partnerDetailList = new List<PartnerDetail>();
            DBConnect connection = new DBConnect();
            try
            {
                connection.Connect(ConnectionsMgr.GetAuthConnInfo());
                using (var res = connection.Select(new[] { columnEmail, columnFirstName, columnLastName, columnLevel, columnOrgID, columnPartnerList }, tableUserInfo, string.Format("WHERE {0}='{1}'", columnUserName, sUserName)))
                {
                    if (!res.Read())
                    {
                        connection.Close();
                        return false;
                    }
                    user.UserName = sUserName;
                    user.Email = res.Field(0);
                    user.FirstName = res.Field(1);
                    user.LastName = res.Field(2);
                    user.Level = (int)double.Parse(res.Field(3));
                    user.Customer = res.Field(4).ToUpper();
                    partnerList.AddRange(res.Field(5).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(pt => pt.Trim()));
                }
                connection.Close();

                // Set partner info
                connection.Connect(ConnectionsMgr.GetAdminConnInfo());
                using (var res = connection.Select(new[] { columnPartner, columnPartnerName }, tablePartnerInfo, string.Format("WHERE {0} IN ({1})", columnPartner, string.Join(",", partnerList.Select(p => "'" + p + "'")))))
                {
                    while (res.Read())
                    {
                        partnerDetailList.Add(new PartnerDetail() { ID = res.Field(0), FullName = res.Field(1) });
                    }
                }
                connection.Close();

                if (partnerDetailList.Count == 0)
                {
                    // No partners?
                    ProgramLog.LogError(user.UserName, user.Customer, "EDIO", "SessionHandler", "GetUserInfo", string.Format("Unable to find partner list in {0} for user {1}.", tablePartnerInfo, user.UserName));
                    return false;
                }
                user.PartnerList = partnerDetailList;
                user.PartnerIndex = 0;

                // Set connection IDs
                if (!ConnectionsMgr.SetConnIDs(user, isTest))
                {
                    // No Conn IDs?
                    ProgramLog.LogError(user, "SessionHandler", "GetUserInfo", string.Format("Unable to get connection IDs for customer {0} and partner {1}.", user.Customer, user.ActivePartner));
                    return false;
                }

                // Set extra company info.
                connection.Connect(ConnectionsMgr.GetOCConnInfo(user));
                using (var res = connection.Select(columnCompanyName, tableCustomerInfo, string.Format("WHERE {0}='{1}'", columnCustomer, user.Customer)))
                {
                    if (res.AffectedRows == 0)
                    {
                        // No company name?
                        ProgramLog.LogError(user, "SessionHandler", "GetUserInfo", string.Format("Unable to find company name in {0} for customer {1}", tableCustomerInfo, user.Customer));
                        connection.Close();
                        return false;
                    }
                    res.Read();
                    user.CompanyName = res.Field(0);
                }
                connection.Close();

                return true;
            }
            catch (Exception e)
            {
                ProgramLog.LogError("", "EDIO", "EDIO", "SessionHandler", "GetUserInfo", e.Message);
                return false;
            }
        }
    }
}