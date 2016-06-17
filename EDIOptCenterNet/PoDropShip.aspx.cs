using EDIOptions.AppCenter;
using EDIOptions.AppCenter.PoDropShip;
using EDIOptions.AppCenter.Session;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;

namespace EDIOptCenterNet
{
    public partial class PoDropShip : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            SiteMaster.SetAppID(AppID.PoDropShip);
        }

        private static FilterInfo CreateFilter()
        {
            FilterInfo filter = new FilterInfo();

            filter.Refinements.AddRange(new[]{
                RefineInfo.CreateDateRefine("Po Date", "podate"),
                RefineInfo.CreateDateRefine("Ship Date", "shipdate"),
                RefineInfo.CreateDateRefine("Cancel Date", "canceldate")
            });

            filter.SortColumn = "podate";
            filter.SortIsDesc = true;

            return filter;
        }

        private static TableState CreateState(FilterInfo filter, PdsOptions options)
        {
            TableState tState = new TableState();
            tState.Filter = filter;

            tState.Columns.AddRange(new[]{
                new ColumnInfo("PO Number", "ponumber", true, true),
                new ColumnInfo("PO Date", "podate", false, true),
                new ColumnInfo("Ship Date", "shipdate", false, true),
                new ColumnInfo("Cancel Date", "canceldate", false, true),
                new ColumnInfo("Type", "potype", true, true),
            });

            if (options.IsInvoiceEnabled)
            {
                tState.Columns.Add(new ColumnInfo("Invoiced", "invtotal", false, true));
            }
            if (options.IsPackingEnabled)
            {
                tState.Columns.Add(new ColumnInfo("Shipped", "asntotal", false, true));
            }

            return tState;
        }

        [WebMethod]
        public static string InitPoList(string version, string data)
        {
            if (!SiteMaster.VerifyRequest(HttpContext.Current.Session))
            {
                return ApiResponse.JSONError(ResponseType.ErrorAuth);
            }

            var user = HttpContext.Current.Session[SKeys.User] as User;
            var opt = PdsManager.GetOptions(user);
            var filter = CreateFilter();

            return ApiResponse.JSONSuccess(new
            {
                TableState = CreateState(filter, opt),
                TableData = PdsManager.GetCurrentPoList(user, filter)
            });
        }

        [WebMethod]
        public static string GetPoList(string version, string data)
        {
            if (!SiteMaster.VerifyRequest(HttpContext.Current.Session))
            {
                return ApiResponse.JSONError(ResponseType.ErrorAuth);
            }

            var user = HttpContext.Current.Session[SKeys.User] as User;
            var opt = PdsManager.GetOptions(user);
            var filter = JsonConvert.DeserializeObject<FilterInfo>(data);

            return ApiResponse.JSONSuccess(new
            {
                TableState = CreateState(filter, opt),
                TableData = PdsManager.GetCurrentPoList(user, filter)
            });
        }

        [WebMethod]
        public static string GetDownloadReports(string version, string data)
        {
            if (!SiteMaster.VerifyRequest(HttpContext.Current.Session))
            {
                return ApiResponse.JSONError(ResponseType.ErrorAuth);
            }
            var user = HttpContext.Current.Session[SKeys.User] as User;
            var keyList = JsonConvert.DeserializeObject<List<string>>(data);
            return ApiResponse.JSONSuccess(PdsManager.GetReportList(user, keyList));
        }

        [WebMethod]
        public static string VerifyPoList(string version, string data)
        {
            if (!SiteMaster.VerifyRequest(HttpContext.Current.Session))
            {
                return ApiResponse.JSONError(ResponseType.ErrorAuth);
            }
            var user = HttpContext.Current.Session[SKeys.User] as User;
            var poList = JsonConvert.DeserializeObject<string>(data).Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.SQLEscape());
            var poDetails = PdsManager.VerifyPoList(user, poList);
            return ApiResponse.JSONSuccess(poDetails);
        }

        [WebMethod]
        public static string SubmitPoList(string version, string data)
        {
            if (!SiteMaster.VerifyRequest(HttpContext.Current.Session))
            {
                return ApiResponse.JSONError(ResponseType.ErrorAuth);
            }
            var user = HttpContext.Current.Session[SKeys.User] as User;
            var poList = JsonConvert.DeserializeObject<List<PoSummary>>(data);
            var keyList = PdsManager.CreateInvAsn(user, poList);
            return ApiResponse.JSONSuccess(keyList);
        }
    }
}