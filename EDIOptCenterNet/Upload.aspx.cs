using EDIOptions.AppCenter;
using EDIOptions.AppCenter.Security;
using EDIOptions.AppCenter.Session;
using System;
using System.IO;
using System.Web;
using System.Web.UI;

namespace EDIOptCenterNet
{
    public partial class Upload : System.Web.UI.Page
    {
        private const int MaxFileSize = 2 * 1024 * 1024; // 2 MB

        protected void Page_Load(object sender, EventArgs e)
        {
            if (SiteMaster.VerifyRequest(Session))
            {
                if (!IsPostBack)
                {
                    if (!Page.RouteData.Values.ContainsKey("key"))
                    {
                        Response.Redirect(SiteMaster.Path404);
                    }
                    if (!SiteMaster.IsKeyGood(Session, Page.RouteData.Values["key"] as string))
                    {
                        Response.Redirect(SiteMaster.Path404);
                    }
                }
                submitButton.ServerClick += new EventHandler(Upload_Click);
            }
            else
            {
                Response.Redirect(SiteMaster.Path404);
            }
        }

        private void Upload_Click(object sender, EventArgs e)
        {
            if (SiteMaster.VerifyRequest(HttpContext.Current.Session))
            {
                try
                {
                    User user = Session[SKeys.User] as User;
                    if (fileUpload.HasFile)
                    {
                        if (fileUpload.PostedFile.ContentLength >= MaxFileSize)
                        {
                            Session[SKeys.UploadResponse] = ApiResponse.JSONError(ResponseType.ErrorUploadFileTooLarge);
                            return;
                        }
                        if (!(SiteFileSystem.IsExtensionAllowed(fileUpload.FileName) && SiteFileSystem.IsContentTypeAllowed(fileUpload.PostedFile.ContentType)))
                        {
                            Session[SKeys.UploadResponse] = ApiResponse.JSONError(ResponseType.ErrorUploadFileFormatNotSupported);
                            return;
                        }
                        string token = Crypt.EncryptStreamToTempFile(user, fileUpload.FileContent);
                        if (string.IsNullOrWhiteSpace(token))
                        {
                            Session[SKeys.UploadResponse] = ApiResponse.JSONError(ResponseType.ErrorUploadUnknown);
                            return;
                        }
                        else
                        {
                            UploadDetail detail = new UploadDetail() { Extension = Path.GetExtension(fileUpload.FileName), Token = token };
                            Session[SKeys.UploadResponse] = ApiResponse.JSONSuccess(detail);
                        }
                    }
                }
                catch
                {
                    Session[SKeys.UploadResponse] = ApiResponse.JSONError(ResponseType.ErrorUploadUnknown);
                }
            }
        }
    }
}