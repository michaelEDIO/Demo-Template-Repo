using EDIOptions.AppCenter.Security;
using EDIOptions.AppCenter.Session;
using System;
using System.Web.UI.WebControls;

namespace EDIOptCenterNet
{
    public partial class Login : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            loginControl.Authenticate += new AuthenticateEventHandler(loginControl_Authenticate);
            loginControl.LoggedIn += new EventHandler(loginControl_LoggedIn);
            Page.ClientScript.RegisterOnSubmitStatement(typeof(Login), "val", "OnUpdateValidators()");
        }

        private void loginControl_LoggedIn(object sender, EventArgs e)
        {
            string sUser = loginControl.UserName;
            SessionHandler.ClearSession();
            SessionHandler.BeginSession(sUser, Request);

            var master = Master as SiteMaster;
            if (master != null)
            {
                master.GenerateAuthTicket(Session, sUser);
            }
        }

        private void loginControl_Authenticate(object sender, AuthenticateEventArgs e)
        {
            var session = Session;
            e.Authenticated = Auth.AuthenticateUser(loginControl.UserName, loginControl.Password);
        }
    }
}