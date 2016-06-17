using EDIOptions.AppCenter;
using EDIOptions.AppCenter.LocationReport;
using EDIOptions.AppCenter.Session;
using Newtonsoft.Json;
using System;
using System.Web;
using System.Web.Services;

namespace EDIOptCenterNet
{
    public partial class LocationReport : System.Web.UI.Page
    {
        protected static JsonSerializerSettings serializerSettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };

        protected void Page_Load(object sender, EventArgs e)
        {
            SiteMaster.SetAppID(AppID.LocationReport);
        }

        [WebMethod]
        public static string ShowFilter(string version, string data)
        {
            if (SiteMaster.VerifyRequest(HttpContext.Current.Session))
            {
                var partner = (HttpContext.Current.Session[SKeys.User] as User).ActivePartner;
                var locOptions = LocationReportHandler.GetOptions(partner);
                return ApiResponse.JSONSuccess(new
                {
                    Option = new
                    {
                        Asn = locOptions.ShowAsn,
                        Location = locOptions.ShowLoc,
                    },
                    Query = new
                    {
                        Bol = locOptions.ShowBol
                    },
                    Filter = new
                    {
                        Store = locOptions.ShowStore,
                        Brand = locOptions.ShowBrand,
                        AE = locOptions.ShowAE
                    }
                });
            }
            else
            {
                return ApiResponse.JSONError(ResponseType.ErrorAuth);
            }
        }

        [WebMethod]
        public static string GetFilters(string version, string data)
        {
            if (SiteMaster.VerifyRequest(HttpContext.Current.Session))
            {
                try
                {
                    User user = HttpContext.Current.Session[SKeys.User] as User;
                    var filterList = new
                    {
                        AEList = LocationReportHandler.GetAccountExecutiveList(user),
                        BNList = LocationReportHandler.GetBrandNameList(user),
                        STList = LocationReportHandler.GetStoreList(user)
                    };
                    return JsonConvert.SerializeObject(ApiResponse.Success(filterList), serializerSettings);
                }
                catch
                {
                    return JsonConvert.SerializeObject(ApiResponse.Error(ResponseType.ErrorLOCUnknown), serializerSettings);
                }
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
                try
                {
                    User user = HttpContext.Current.Session[SKeys.User] as User;
                    LocationReportDetails ldr = new LocationReportDetails();
                    try
                    {
                        ldr = JsonConvert.DeserializeObject<LocationReportDetails>(data);
                    }
                    catch
                    {
                        return JsonConvert.SerializeObject(ApiResponse.Error(ResponseType.ErrorLOCDateRangeInvalid), serializerSettings);
                    }
                    string fileToken = "";
                    if (ldr.Type == "ASNSummary")
                    {
                        fileToken = LocationReportHandler.GenerateASNSummaryReport(user, ldr);
                    }
                    else
                    {
                        fileToken = LocationReportHandler.GenerateLocationReport(user, ldr);
                    }
                    if (fileToken != "")
                    {
                        return JsonConvert.SerializeObject(ApiResponse.Success(fileToken), serializerSettings);
                    }
                    else
                    {
                        return JsonConvert.SerializeObject(ApiResponse.Error(ResponseType.ErrorLOCReportGenFailed), serializerSettings);
                    }
                }
                catch
                {
                    return JsonConvert.SerializeObject(ApiResponse.Error(ResponseType.ErrorLOCUnknown), serializerSettings);
                }
            }
            else
            {
                return JsonConvert.SerializeObject(ApiResponse.Error(ResponseType.ErrorAuth), serializerSettings);
            }
        }
    }
}