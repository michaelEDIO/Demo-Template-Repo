using EDIOptions.AppCenter;
using EDIOptions.AppCenter.ChangePO;
using EDIOptions.AppCenter.Session;
using Newtonsoft.Json;
using System;
using System.Web;
using System.Web.Services;
using System.Web.SessionState;
using System.Web.UI;

namespace EDIOptCenterNet
{
    public partial class ChangePO : System.Web.UI.Page
    {
        protected delegate void fetchItemsDelegate(HttpSessionState state);

        protected string CPODetails { get; set; }

        protected fetchItemsDelegate _fetchList;

        protected void Page_Load(object sender, EventArgs e)
        {
            SiteMaster.SetAppID(AppID.ChangePO);
            _fetchList = (state) =>
            {
                CPODetails = JsonConvert.SerializeObject(ChangePOTracker.GetChangeList(Session[SKeys.User] as User));
            };
            Page.RegisterAsyncTask(new PageAsyncTask((sndr, args, callback, extraData) => { return _fetchList.BeginInvoke(HttpContext.Current.Session, callback, extraData); }, empty => { }, empty => { }, null));
        }

        [WebMethod]
        public static string DoAction(string version, string data)
        {
            if (SiteMaster.VerifyRequest(HttpContext.Current.Session))
            {
                var updateData = JsonConvert.DeserializeObject<CPOAction>(data);
                var user = HttpContext.Current.Session[SKeys.User] as User;
                ResponseType result = ResponseType.ErrorCPOUnknown;
                try
                {
                    result = ChangePOTracker.PerformAction(user, updateData);
                }
                catch
                {
                    return ApiResponse.JSONError(ResponseType.ErrorCPOUnknown);
                }
                if (result == ResponseType.SuccessCPO)
                {
                    return ApiResponse.JSONSuccess(ResponseDescription.Get(result));
                }
                else
                {
                    return ApiResponse.JSONError(result);
                }
            }
            else
            {
                return ApiResponse.JSONError(ResponseType.ErrorAuth);
            }
        }
    }
}