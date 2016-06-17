using EDIOptions.AppCenter;
using EDIOptions.AppCenter.Database;
using EDIOptions.AppCenter.Security;
using EDIOptions.AppCenter.Session;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Web.UI.HtmlControls;

namespace EDIOptCenterNet
{
    public partial class SiteMaster : System.Web.UI.MasterPage
    {
        public const string Path404 = "~/404.aspx";
        public const string PathDefault = "~/Default.aspx";
        public const string PathLogin = "~/Login.aspx";
        public const string PathLogout = "~/Logout.aspx";
        public const string QSToken = "token";

        private static List<string> listNoRedirect = new List<string>()
        {
            "login",
            "logout",
            "404",
        };

        private static bool IsAppPage = false;

        private static AppID PageAppID = AppID.None;

        protected string appList { get; set; }

        protected string userName { get; set; }

        protected string userCompany { get; set; }

        protected string userPartner { get; set; }

        protected string userEmail { get; set; }

        protected int challenge1 { get; set; }

        protected int challenge2 { get; set; }

        protected int challengeSolution { get; set; }

        protected string LoggedIn { get; set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            InitDefaultPage();
            if (listNoRedirect.Contains(Path.GetFileNameWithoutExtension(Request.FilePath).ToLower()))
            {
                SetTopMenuVisible(false);
                return;
            }

            var ocToken = Request.QueryString[QSToken];
            if (Session == null)
            {
                // No existing session, check if request from OC or ASP
                if (!string.IsNullOrWhiteSpace(ocToken))
                {
                    // OC request, create or 404.
                    var userInfo = Auth.GetOCRecord(ocToken);
                    if (userInfo.IsValid)
                    {
                        SessionHandler.BeginSession(userInfo.UserName, Request, userInfo);
                        InitOCPage(Session[SKeys.User] as User, userInfo);
                    }
                    else
                    {
                        ProgramLog.LogError(null, "SiteMaster", "Page_Load", "Unable to authenticate user.");
                        Response.Redirect(Path404);
                    }
                }
                else
                {
                    // ASP request, redirect to login
                    RedirectToLogin("A");
                }
            }
            else
            {
                if (!IsExistingSessionValid())
                {
                    if (!string.IsNullOrWhiteSpace(ocToken))
                    {
                        // OC request, create or 404.
                        var userInfo = Auth.GetOCRecord(ocToken);
                        if (userInfo.IsValid)
                        {
                            SessionHandler.BeginSession(userInfo.UserName, Request, userInfo);
                            InitOCPage(Session[SKeys.User] as User, userInfo);
                        }
                        else
                        {
                            Response.Redirect(Path404);
                        }
                    }
                    else
                    {
                        RedirectToLogin("S");
                    }
                }
                else
                {
                    var user = Session[SKeys.User] as User;
                    var isOCSession = Session[SKeys.IsOCSession] as bool?;
                    if (isOCSession == true)
                    {
                        if (!string.IsNullOrWhiteSpace(ocToken))
                        {
                            // OC session + OC request -> maintain existing session
                            var userInfo = Auth.GetOCRecord(ocToken);
                            if (userInfo.IsValid)
                            {
                                // Check the userinfo against the existing session info. If match, then good.
                                if (user.UserName == userInfo.UserName)
                                {
                                    InitOCPage(user, userInfo);
                                }
                                else
                                {
                                    // Remove OC session, redirect to 404.
                                    Session.Abandon();
                                    Response.Redirect(Path404);
                                }
                            }
                            else
                            {
                                // Invalid info, Remove OC session, redirect to 404.
                                Session.Abandon();
                                Response.Redirect(Path404);
                            }
                        }
                        else
                        {
                            // OC Session + ASP request -> Remove OC session, redirect to login.
                            Session.Abandon();
                            RedirectToLogin("OA");
                        }
                    }
                    else
                    {
                        // ASP session, token doesn't matter, setup page as usual.
                        RedirectAppIfNotAllowed(user, PathDefault);
                        SetupUserPage(user);
                    }
                }
            }
        }

        private bool IsExistingSessionValid()
        {
            var user = Session[SKeys.User] as User;
            var sessionID = Session[SKeys.SessionID] as string;
            return user != null && !string.IsNullOrWhiteSpace(sessionID) && Auth.VerifyFormAuthTicket(sessionID, user.UserName);
        }

        #region Page Setup

        public static void SetAppID(AppID appID)
        {
            IsAppPage = appID != AppID.None;
            PageAppID = appID;
        }

        protected void InitDefaultPage()
        {
            CheckHTTPS();
            InitializeSupportChallenge();
            SetLoggedIn(false);
            SetUserDetail();
            SetNavigateLinks();
            SetAppList();
        }

        protected void InitOCPage(User user, OCUserInfo userInfo)
        {
            SetOCInfo(user, userInfo);
            RedirectAppIfNotAllowed(user, Path404);
            SetupUserPage(user);
            SetTopMenuVisible(false);
        }

        protected void SetupUserPage(User user)
        {
            bool isTest = ((bool?)this.Session[SKeys.IsTest] == true);
            GenerateAuthTicket(Session, user.UserName);
            SetAppList(user);
            if (isTest)
            {
                SetDevelopmentInfo(user);
            }
            SetUserDetail(user);
            InitializePartnerList(user);
            SetLoggedIn(true);
            SetNavigateLinks();
        }

        protected void SetTopMenuVisible(bool isShown)
        {
            FixedHeadFrame.Visible = isShown;
        }

        protected void SetAppList(User user = null)
        {
            if (user == null)
            {
                appList = "{}";
                return;
            }
            Dictionary<string, string> apps = AppManagement.GetAllowedApps(user);
            if (apps == null)
            {
                appList = "{}";
                return;
            }
            appList = JsonConvert.SerializeObject(apps);
        }

        protected void SetDevelopmentInfo(User user)
        {
            DatabaseInfo di = null;
            string sFormat = "{1} ({0})";
            List<string> conns = new List<string>();

            di = ConnectionsMgr.GetOCConnInfo(user);
            conns.Add(string.Format(sFormat, di.Port, di.Id));
            di = ConnectionsMgr.GetNPConnInfo(user);
            conns.Add(string.Format(sFormat, di.Port, di.Id));
            di = ConnectionsMgr.GetSHConnInfo(user);
            conns.Add(string.Format(sFormat, di.Port, di.Id));
            di = ConnectionsMgr.GetSLConnInfo(user);
            conns.Add(string.Format(sFormat, di.Port, di.Id));

            lblDeNotice.Text = string.Format("Development Environment: {0}", string.Join(", ", conns));
            divDeNotice.Visible = true;
        }

        protected void SetUserDetail(User user = null)
        {
            if (user == null)
            {
                userName = "";
                userCompany = "";
                userPartner = "";
                userEmail = "";
            }
            else
            {
                userName = user.FirstName + " " + user.LastName;
                userEmail = user.Email;
                userCompany = user.CompanyName;
                userPartner = user.ActivePartnerName + " (" + user.ActivePartner + ")";
            }
        }

        protected void SetNavigateLinks()
        {
            string homeUrl = "~/";
            if (Session == null)
            {
                homeUrl += "Login.aspx";
                linkHomepage.NavigateUrl = homeUrl;
            }
            else
            {
                bool sessionType = (Session[SKeys.IsOCSession] as bool?) == true;
                if ((Session[SKeys.IsOCSession] as bool?) == true)
                {
                    homeUrl = "https://edi-optcenter.com";
                    bool isTest = (Session[SKeys.IsTest] as bool?) == true;
                    if (isTest)
                    {
                        homeUrl += "/dev-len";
                    }
                }
                else
                {
                    homeUrl += this.Session[SKeys.LandingPg] as string ?? "Default.aspx";
                }
                linkHomepage.NavigateUrl = "#";
                linkHomepage.Attributes.Add("data-url", homeUrl);
            }
            linkBlog.NavigateUrl = "https://edi-optcenter.com/blog";
        }

        protected void SetLoggedIn(bool isLoggedIn)
        {
            LoggedIn = isLoggedIn ? "true" : "false";
        }

        protected void InitializeSupportChallenge()
        {
            Rand r = new Rand();
            challenge1 = r.Next(1, 10);
            challenge2 = r.Next(1, 10);
            challengeSolution = challenge1 + challenge2;
        }

        protected void InitializePartnerList(User user)
        {
            FixedPartnersList.Controls.Clear();
            for (int i = 0; i < user.PartnerList.Count; i++)
            {
                HtmlGenericControl li = new HtmlGenericControl("li");
                if (i == user.PartnerIndex)
                {
                    li.Attributes.Add("class", "ActivePartner");
                }
                li.Attributes.Add("data-id", i.ToString());
                li.InnerText = user.PartnerList[i].FullName + " (" + user.PartnerList[i].ID + ")";
                FixedPartnersList.Controls.Add(li);
            }
        }

        protected void RedirectAppIfNotAllowed(User user, string path)
        {
            if (IsAppPage && !AppManagement.IsAllowed(user, PageAppID))
            {
                Response.Redirect(path);
            }
        }

        protected void SetOCInfo(User user, OCUserInfo userInfo)
        {
            if (user.ActivePartner != userInfo.ActivePartner)
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
            Session[SKeys.IsTest] = userInfo.IsTest;
        }

        #endregion Page Setup

        #region Authentication

        public static bool IsValid()
        {
            return VerifyRequest(HttpContext.Current.Session);
        }

        public static bool VerifyRequest(HttpSessionState session)
        {
            if (session != null)
            {
                var usUser = session[SKeys.User] as User;
                var usTicket = session[SKeys.SessionID] as string;
                return usUser != null && usTicket != null && Auth.VerifyFormAuthTicket(usTicket, usUser.UserName);
            }
            else
            {
                return false;
            }
        }

        public void GenerateAuthTicket(HttpSessionState session, string user)
        {
            FormsAuthenticationTicket ucAuthTicket = new FormsAuthenticationTicket(user, true, 180);
            session[SKeys.SessionID] = FormsAuthentication.Encrypt(ucAuthTicket);
        }

        public static string AddKey(HttpSessionState session)
        {
            try
            {
                string key = Crypt.Get128BitKey();
                HashSet<string> keySet = session[SKeys.TokenSet] as HashSet<string>;
                keySet.Add(key);
                return key;
            }
            catch
            {
                return "";
            }
        }

        public static bool IsKeyGood(HttpSessionState session, string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }
            try
            {
                HashSet<string> keySet = session[SKeys.TokenSet] as HashSet<string>;
                if (keySet.Contains(key))
                {
                    keySet.Remove(key);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        #endregion Authentication

        protected void CheckHTTPS()
        {
            string http = this.Request.Url.Scheme.ToUpper();
            if (http != "HTTPS" && this.Request.Url.Host != "10.0.0.245")
            {
                string securePath = "https://" + this.Request.Url.Host;
#if !DEBUG
                //this.Response.Redirect(securePath);
#endif
            }
        }

        protected void RedirectToLogin(string code)
        {
            if (Path.GetFileNameWithoutExtension(Request.FilePath).ToLower() != "login")
            {
                this.Response.Redirect("~/Login.aspx?r=" + code);
            }
        }
    }
}