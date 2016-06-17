using EDIOptions.AppCenter;
using EDIOptions.AppCenter.CatalogXref;
using EDIOptions.AppCenter.Session;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Services;

namespace EDIOptCenterNet
{
    public partial class CatalogXref : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            SiteMaster.SetAppID(AppID.CatalogXref);
        }

        private static FilterInfo CreateFilter()
        {
            FilterInfo filter = new FilterInfo();

            return filter;
        }

        private static TableState CreateState(FilterInfo filter)
        {
            TableState tState = new TableState();
            tState.Filter = filter;

            tState.Columns.AddRange(new[]{
                new ColumnInfo("Company Name", "vendorname", true, true),
                new ColumnInfo("GXS Account", "vendorid", true, true),
                new ColumnInfo("Selection Code", "vendorseq", true, true),
                new ColumnInfo("Brand Name", "brandname", true, true),
            });

            return tState;
        }

        [WebMethod]
        public static string InitXrefList(string version, string data)
        {
            if (!SiteMaster.VerifyRequest(HttpContext.Current.Session))
            {
                return ApiResponse.JSONError(ResponseType.ErrorAuth);
            }

            var user = HttpContext.Current.Session[SKeys.User] as User;
            var filter = CreateFilter();

            return ApiResponse.JSONSuccess(new
            {
                TableState = CreateState(filter),
                TableData = CatalogXrefManager.GetXrefList(user, filter)
            });
        }

        [WebMethod]
        public static string GetXrefList(string version, string data)
        {
            if (!SiteMaster.VerifyRequest(HttpContext.Current.Session))
            {
                return ApiResponse.JSONError(ResponseType.ErrorAuth);
            }

            var user = HttpContext.Current.Session[SKeys.User] as User;
            var filter = JsonConvert.DeserializeObject<FilterInfo>(data);

            return ApiResponse.JSONSuccess(new
            {
                TableState = CreateState(filter),
                TableData = CatalogXrefManager.GetXrefList(user, filter)
            });
        }

        [WebMethod]
        public static string CreateXref(string version, string data)
        {
            if (!SiteMaster.VerifyRequest(HttpContext.Current.Session))
            {
                return ApiResponse.JSONError(ResponseType.ErrorAuth);
            }

            var user = HttpContext.Current.Session[SKeys.User] as User;

            var xref = JsonConvert.DeserializeObject<XrefRecord>(data);

            CatalogXrefManager.CreateXref(user, xref);

            return ApiResponse.JSONSuccess();
        }

        [WebMethod]
        public static string EditXref(string version, string data)
        {
            if (!SiteMaster.VerifyRequest(HttpContext.Current.Session))
            {
                return ApiResponse.JSONError(ResponseType.ErrorAuth);
            }

            var user = HttpContext.Current.Session[SKeys.User] as User;

            var xref = JsonConvert.DeserializeObject<List<XrefRecord>>(data);

            CatalogXrefManager.EditXref(user, xref);

            return ApiResponse.JSONSuccess();
        }

        [WebMethod]
        public static string RemoveXref(string version, string data)
        {
            if (!SiteMaster.VerifyRequest(HttpContext.Current.Session))
            {
                return ApiResponse.JSONError(ResponseType.ErrorAuth);
            }
            var user = HttpContext.Current.Session[SKeys.User] as User;
            List<string> recordKeys = JsonConvert.DeserializeObject<List<string>>(data);
            CatalogXrefManager.RemoveXref(user, recordKeys);
            return ApiResponse.JSONSuccess();
        }
    }
}