using System;
using System.Web;
using System.Web.Routing;

namespace EDIOptCenterNet
{
    public class Global : HttpApplication
    {
        private void Application_Start(object sender, EventArgs e)
        {
            // Code that runs on application startup
            SetupRoutes(RouteTable.Routes);
        }

        private void Application_End(object sender, EventArgs e)
        {
            //  Code that runs on application shutdown
        }

        private void Application_Error(object sender, EventArgs e)
        {
            // Code that runs when an unhandled error occurs
            var serverError = Server.GetLastError() as HttpException;
            EventArgs ex = e;

            if (serverError != null)
            {
                int errorCode = serverError.GetHttpCode();
                switch (errorCode)
                {
                    case 404:
                    default:
                        Response.Redirect(SiteMaster.Path404);
                        break;
                }
            }
        }

        private void Session_Start(object sender, EventArgs e)
        {
            Session["StartDate"] = DateTime.Now;
            // Code that runs when args new session is started
            //Session["Started"] = true;
        }

        private void Session_End(object sender, EventArgs e)
        {
            // Code that runs when args session ends.
            // Note: The Session_End event is raised only when the sessionstate mode
            // is set to InProc in the Web.config file. If session mode is set to StateServer
            // or SQLServer, the event is not raised.
        }

        private void SetupRoutes(RouteCollection routes)
        {
            // Route types:
            // 1: Documents
            // 2: Downloads
            // 3: Temporary
            // 4: Templates
            routes.MapPageRoute("download", "download/{file}", "~/Download.aspx", true, new RouteValueDictionary() { { "type", "download" } });
            routes.MapPageRoute("temp", "download/{file}/{token}", "~/Download.aspx", true, new RouteValueDictionary() { { "type", "temp" } });
            routes.MapPageRoute("doc", "doc/{file}", "~/Download.aspx", true, new RouteValueDictionary() { { "type", "general" } });
            routes.MapPageRoute("template", "doc/template/{file}", "~/Download.aspx", true, new RouteValueDictionary() { { "type", "template" } });
            routes.MapPageRoute("spec", "doc/spec/{file}", "~/Download.aspx", true, new RouteValueDictionary() { { "type", "spec" } });
            routes.MapPageRoute("upload", "upload/{key}", "~/Upload.aspx", true);
        }
    }
}