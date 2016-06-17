using EDIOptions.AppCenter;
using EDIOptions.AppCenter.POAcknowledge;
using EDIOptions.AppCenter.Security;
using EDIOptions.AppCenter.Session;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Services;

namespace EDIOptCenterNet
{
    public partial class POAcknowledge : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            SiteMaster.SetAppID(AppID.POAcknowledge);
        }

        [WebMethod]
        public static string GetTemplateLink(string version, string data)
        {
            if (SiteMaster.VerifyRequest(HttpContext.Current.Session))
            {
                var user = HttpContext.Current.Session[SKeys.User] as User;
                var isTest = (bool?)HttpContext.Current.Session[SKeys.IsTest] == true;
                string tplPath = SiteFileSystem.GetTemplateDocFilePath(user, isTest, "855.xlsx");
                string linkReturn = "";
                if (tplPath != "")
                {
                    linkReturn = "doc/template/855.xlsx";
                }
                else
                {
                    linkReturn = "";
                }
                return ApiResponse.JSONSuccess(linkReturn);
            }
            else
            {
                return ApiResponse.JSONError(ResponseType.ErrorAuth);
            }
        }

        [WebMethod]
        public static string GetPastRequests(string version, string data)
        {
            if (SiteMaster.VerifyRequest(HttpContext.Current.Session))
            {
                var user = HttpContext.Current.Session[SKeys.User] as User;
                var trxDict = HttpContext.Current.Session[SKeys.TrxDict] as Dictionary<string, string>;
                var recs = ProcessQueue.GetPreviousRecords(user);
                foreach (var item in recs)
                {
                    if (trxDict.ContainsKey(item.Type))
                    {
                        item.Type = trxDict[item.Type];
                    }
                }
                return ApiResponse.JSONSuccess(recs);
            }
            else
            {
                return ApiResponse.JSONError(ResponseType.ErrorAuth);
            }
        }

        [WebMethod]
        public static string CheckUpload(string version, string data)
        {
            if (SiteMaster.VerifyRequest(HttpContext.Current.Session))
            {
                var user = HttpContext.Current.Session[SKeys.User] as User;
                var session = HttpContext.Current.Session;
                if (session[SKeys.UploadResponse] == null)
                {
                    // No upload response.
                    return ApiResponse.JSONSuccess();
                }
                var respStr = session[SKeys.UploadResponse] as string;
                if (string.IsNullOrWhiteSpace(respStr))
                {
                    // Key set, but no content.
                    return ApiResponse.JSONSuccess();
                }
                session.Remove(SKeys.UploadResponse);

                ApiResponse response = JsonConvert.DeserializeObject<ApiResponse>(respStr);
                if (!response.success)
                {
                    // Error happened, so return that.
                    return respStr;
                }

                // Get upload data.
                UploadDetail up = ((JObject)response.data).ToObject<UploadDetail>();
                if (up == null || string.IsNullOrWhiteSpace(up.Extension) || string.IsNullOrWhiteSpace(up.Token) || !SiteFileSystem.IsExtensionAllowed(up.Extension))
                {
                    // Response data was bad for some reason.
                    return ApiResponse.JSONSuccess();
                }
                // Check upload data.
                if (Crypt.IsTokenGood(up.Token))
                {
                    string outFile = Path.Combine(SiteFileSystem.GetTempFileDirectory(), Path.GetRandomFileName());
                    string ecFile = Path.Combine(SiteFileSystem.GetTempFileDirectory(), up.Token.Substring(32));
                    if (Crypt.DecryptTempFileToFile(user, outFile, up.Token))
                    {
                        // Remove old file.
                        File.Delete(ecFile);
                        // Then do verification.
                        ResponseType resp = POAcknowledgeManager.VerifyFile(user, outFile);
                        if (resp == ResponseType.SuccessAPO || resp == ResponseType.WarningAPOUnverifiedAccept)
                        {
                            // Good response, move the file
                            var isTest = (HttpContext.Current.Session[SKeys.IsTest] as bool?) == true;
                            try
                            {
                                var uploadFilePath = SiteFileSystem.GetUploadFileName(user, isTest, "855", up.Extension);
                                File.Move(outFile, uploadFilePath);
                                ProcessQueue.CreateUploadRecord(user, DateTime.Now, "855", Path.GetFileName(uploadFilePath));
                            }
                            catch
                            {
                                return ApiResponse.JSONError(ResponseType.ErrorAPOUnknown);
                            }
                            if (resp == ResponseType.SuccessAPO)
                            {
                                return ApiResponse.JSONSuccess(ResponseDescription.Get(resp));
                            }
                            else
                            {
                                return ApiResponse.JSONWarning(ResponseDescription.Get(resp));
                            }
                        }
                        else
                        {
                            // Fail response, delete the file.
                            File.Delete(outFile);
                            return ApiResponse.JSONError(resp);
                        }
                    }
                    else
                    {
                        return ApiResponse.JSONError(ResponseType.ErrorAPOUnknown);
                    }
                }
                else
                {
                    // Bad token.
                    return ApiResponse.JSONError(ResponseType.ErrorAPOUnknown);
                }
            }
            else
            {
                return ApiResponse.JSONError(ResponseType.ErrorAuth);
            }
        }
    }
}