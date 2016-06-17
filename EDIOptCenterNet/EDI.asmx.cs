using EDIOptions.AppCenter;
using EDIOptions.AppCenter.Session;
using EDIOptions.AppCenter.Support;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web;
using System.Web.Script.Services;
using System.Web.Services;
using System.Web.SessionState;

namespace EDIOptCenterNet
{
    [WebService(Namespace = "https://www.edi-optcenter.com/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    [ScriptService]
    public class EDI1 : System.Web.Services.WebService
    {
        [WebMethod(true)]
        public string SubmitSupportRequest(string version, string data)
        {
            HttpSessionState session = HttpContext.Current.Session;
            var requestInfo = HttpContext.Current.Request;

            var report = JsonConvert.DeserializeObject<ReportDetail>(data);
            if (report == null)
            {
                return JsonConvert.SerializeObject(ApiResponse.Error(ResponseType.ErrorInvalidData), new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            }
            else if (string.IsNullOrEmpty(report.Email))
            {
                return JsonConvert.SerializeObject(ApiResponse.Error(ResponseType.ErrorSupportEmailRequired), new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            }
            else if (string.IsNullOrEmpty(report.Message))
            {
                return JsonConvert.SerializeObject(ApiResponse.Error(ResponseType.ErrorSupportMessageRequired), new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            }

            string username = "";
            string customer = "EDIO";
            string partner = "EDIO";
            var user = session[SKeys.User] as User;
            if (user != null)
            {
                username = user.UserName;
                customer = user.Customer;
                partner = user.ActivePartner;
            }
            if (SupportRequest.Submit(requestInfo, report, username, customer, partner))
            {
                return JsonConvert.SerializeObject(ApiResponse.Success(ResponseDescription.Get(ResponseType.SuccessSupportRequest)), new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            }
            else
            {
                return JsonConvert.SerializeObject(ApiResponse.Error(ResponseType.ErrorSupportUnknownError), new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            }
        }

        [WebMethod(true)]
        public string ChangePartner(string version, string data)
        {
            HttpSessionState session = HttpContext.Current.Session;
            if (SiteMaster.VerifyRequest(session))
            {
                var user = session[SKeys.User] as User;
                var attrList = JsonConvert.DeserializeObject<Dictionary<string, string>>(data);
                if (attrList.Count == 0 || !attrList.ContainsKey("PartnerIndex"))
                {
                    return JsonConvert.SerializeObject(ApiResponse.Error(ResponseType.ErrorGeneralNoPartnerIndex), new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
                }
                int index = -1;
                if (!int.TryParse(attrList["PartnerIndex"], out index))
                {
                    return JsonConvert.SerializeObject(ApiResponse.Error(ResponseType.ErrorGeneralInvalidPartnerIndex), new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
                }
                if (index < 0 || index >= user.PartnerList.Count)
                {
                    return JsonConvert.SerializeObject(ApiResponse.Error(ResponseType.ErrorGeneralPartnerIndexOutOfRange), new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
                }
                user.PartnerIndex = index;
                return JsonConvert.SerializeObject(ApiResponse.Success(), new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            }
            else
            {
                return JsonConvert.SerializeObject(ApiResponse.Error(ResponseType.ErrorAuth), new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            }
        }

        [WebMethod(true)]
        public string GetKey(string version, string data)
        {
            HttpSessionState session = HttpContext.Current.Session;
            if (SiteMaster.VerifyRequest(session))
            {
                string key = SiteMaster.AddKey(session);
                return ApiResponse.JSONSuccess(key);
            }
            else
            {
                return JsonConvert.SerializeObject(ApiResponse.Error(ResponseType.ErrorAuth), new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            }
        }
    }
}