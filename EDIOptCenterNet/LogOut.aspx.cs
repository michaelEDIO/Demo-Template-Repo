using System;

namespace EDIOptCenterNet.Scripts
{
    public partial class LogOut : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Session.Abandon();
        }
    }
}