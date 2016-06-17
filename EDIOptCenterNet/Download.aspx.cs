using EDIOptions.AppCenter;
using EDIOptions.AppCenter.Security;
using EDIOptions.AppCenter.Session;
using System;
using System.IO;
using System.Web;
using System.Web.UI;

namespace EDIOptCenterNet
{
    public partial class Download : System.Web.UI.Page
    {
        private const string typeDownload = "download";
        private const string typeTemporary = "temp";
        private const string typeGeneral = "general";
        private const string typeTemplate = "template";
        private const string typeSpec = "spec";

        private const string qsType = "type";
        private const string qsFile = "file";
        private const string qsToken = "token";

        protected void Page_Load(object sender, EventArgs e)
        {
            SiteMaster.SetAppID(AppID.None);
            if (SiteMaster.VerifyRequest(HttpContext.Current.Session))
            {
                User user = Session[SKeys.User] as User;
                switch (Page.RouteData.Values[qsType] as string)
                {
                    case typeDownload:
                        DownloadFile(user, Page.RouteData.Values[qsFile] as string);
                        break;

                    case typeTemporary:
                        DownloadTemp(user, Page.RouteData.Values[qsFile] as string, Page.RouteData.Values[qsToken] as string);
                        break;

                    case typeGeneral:
                        DownloadDoc(user, Page.RouteData.Values[qsFile] as string);
                        break;

                    case typeTemplate:
                        DownloadTemplate(user, Page.RouteData.Values[qsFile] as string);
                        break;

                    case typeSpec:
                        DownloadSpec(user, Page.RouteData.Values[qsFile] as string);
                        break;

                    default:
                        Response.Redirect(SiteMaster.Path404);
                        break;
                }
            }
            else
            {
                Response.Redirect(SiteMaster.Path404);
            }
        }

        protected void DownloadTemp(User user, string usFileRename, string token)
        {
            string sFileName = GetSafeFile(usFileRename);

            string contentType = SiteFileSystem.GetContentType(sFileName);
            if (sFileName != "" && contentType != "" && Crypt.IsTokenGood(token))
            {
                string eTempFile = Path.Combine(SiteFileSystem.GetTempFileDirectory(), token.Substring(32));
                byte[] file = Crypt.DecryptFileToArray(user, eTempFile, token);
                if (file.Length > 0)
                {
                    File.Delete(eTempFile);
                    WriteFileToResponse(sFileName, contentType, file);
                }
                else
                {
                    Response.Redirect(SiteMaster.Path404);
                }
            }
            else
            {
                Response.Redirect(SiteMaster.Path404);
            }
        }

        protected void DownloadFile(User user, string usFileName)
        {
            Response.Redirect(SiteMaster.Path404);
        }

        protected void DownloadDoc(User user, string usFileName)
        {
            Response.Redirect(SiteMaster.Path404);
        }

        protected void DownloadTemplate(User user, string usFileName)
        {
            string sFileName = GetSafeFile(usFileName);
            string contentType = SiteFileSystem.GetContentType(sFileName);
            if (!SiteFileSystem.IsExtensionAllowed(sFileName))
            {
                Response.Redirect(SiteMaster.Path404);
                return;
            }
            var isTest = (bool?)HttpContext.Current.Session[SKeys.IsTest] == true;
            string tplPath = SiteFileSystem.GetTemplateDocFilePath(user, isTest, sFileName);
            if (tplPath != "")
            {
                byte[] file = File.ReadAllBytes(tplPath);
                WriteFileToResponse(sFileName, contentType, file);
            }
            else
            {
                Response.Redirect(SiteMaster.Path404);
            }
        }

        protected void DownloadSpec(User user, string usFileName)
        {
            Response.Redirect(SiteMaster.Path404);
        }

        private string GetSafeFile(string usFileName)
        {
            if (string.IsNullOrWhiteSpace(usFileName) || usFileName.IndexOfAny(Path.GetInvalidFileNameChars()) > 0)
            {
                return "";
            }
            return Path.GetFileName(usFileName);
        }

        private void WriteFileToResponse(string filename, string contentType, byte[] file)
        {
            Response.Clear();
            Response.Buffer = true;
            Response.ContentType = contentType;
            if (contentType == "text/csv")
            {
                Response.ContentEncoding = System.Text.Encoding.UTF8;
            }
            Response.AddHeader("content-disposition", "attachment; filename=" + filename);
            Response.AddHeader("content-length", file.Length.ToString());
            Response.BinaryWrite(file);
            Response.End();
        }
    }
}