using EDIOptions.AppCenter;
using System;

namespace EDIOptCenterNet
{
    public partial class _Default : System.Web.UI.Page
    {
        protected string WelcomeMsg;

        protected void Page_Load(object sender, EventArgs e)
        {
            SiteMaster.SetAppID(AppID.None);
            WelcomeMsg = "Welcome to the Apps Center. For available applications, use the top-left menu.";
        }
    }
}