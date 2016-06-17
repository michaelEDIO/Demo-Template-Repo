using EDIOptions.AppCenter;
using EDIOptions.AppCenter.SalesRequest;
using EDIOptions.AppCenter.Session;
using Newtonsoft.Json;
using System;
using System.Web;
using System.Web.Services;
using System.Web.SessionState;
using System.Web.UI;

namespace EDIOptCenterNet
{
    public partial class SalesRequest : System.Web.UI.Page
    {
        protected delegate void fetchItemsDelegate(HttpSessionState state);

        protected string storeList { get; set; }

        protected string retailWeekList { get; set; }

        protected string partnerEmail { get; set; }

        protected fetchItemsDelegate _fetchList;

        protected static JsonSerializerSettings serializerSettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };

        protected void Page_Load(object sender, EventArgs e)
        {
            SiteMaster.SetAppID(AppID.SalesRequest);
            _fetchList = (state) =>
            {
                var user = Session[SKeys.User] as User;

                storeList = JsonConvert.SerializeObject(SalesRequestManager.GetStoreList(user));
                retailWeekList = JsonConvert.SerializeObject(SalesRequestManager.GetRetailWeeks(user));
                partnerEmail = user.Email;

                var pastReqs = SalesRequestManager.GetPastOrders(user);
                pastOrderGrid.DataSource = pastReqs;
                pastOrderGrid.DataBind();
                if (pastReqs.Count > 0)
                {
                    pastOrderGrid.HeaderRow.TableSection = System.Web.UI.WebControls.TableRowSection.TableHeader;
                }
            };
            Page.RegisterAsyncTask(new PageAsyncTask((sndr, args, callback, extraData) => { return _fetchList.BeginInvoke(HttpContext.Current.Session, callback, extraData); }, empty => { }, empty => { }, null));
        }

        [WebMethod]
        public static string SendRequest(string version, string data)
        {
            if (SiteMaster.VerifyRequest(HttpContext.Current.Session))
            {
                var updateData = JsonConvert.DeserializeObject<ReportRequestData>(data);
                var user = HttpContext.Current.Session[SKeys.User] as User;
                try
                {
                    SalesRequestManager.SendRequest(user, updateData);
                }
                catch
                {
                    return JsonConvert.SerializeObject(ApiResponse.Error(ResponseType.ErrorSalesReqUnknown), serializerSettings);
                }
                return JsonConvert.SerializeObject(ApiResponse.Success(SalesRequestManager.GetPastOrders(user)), serializerSettings);
            }
            else
            {
                return JsonConvert.SerializeObject(ApiResponse.Error(ResponseType.ErrorAuth), serializerSettings);
            }
        }

        [WebMethod]
        public static string GenerateReport(string version, string data)
        {
            if (SiteMaster.VerifyRequest(HttpContext.Current.Session))
            {
                var updateData = JsonConvert.DeserializeObject<ReportRequestData>(data);
                var user = HttpContext.Current.Session[SKeys.User] as User;
                switch (updateData.RequestType)
                {
                    case "ESS":
                    default:
                        {
                            return JsonConvert.SerializeObject(ApiResponse.Success(SalesRequestManager.GenerateSalesReportList(user, updateData)), serializerSettings);
                        }
                    case "EST":
                        {
                            return JsonConvert.SerializeObject(ApiResponse.Success(SalesRequestManager.GenerateShipmentReportList(user, updateData)), serializerSettings);
                        }
                }
            }
            else
            {
                return JsonConvert.SerializeObject(ApiResponse.Error(ResponseType.ErrorAuth), serializerSettings);
            }
        }
    }
}