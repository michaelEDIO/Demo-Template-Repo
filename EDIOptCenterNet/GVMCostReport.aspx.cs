using EDIOptions.AppCenter;
using EDIOptions.AppCenter.GVMCostReport;
using EDIOptions.AppCenter.Session;
using Newtonsoft.Json;
using System;
using System.Web;
using System.Web.Services;

namespace EDIOptCenterNet
{
    public partial class GVMCostReport : System.Web.UI.Page
    {
        protected static JsonSerializerSettings serializerSettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };

        protected void Page_Load(object sender, EventArgs e)
        {
            SiteMaster.SetAppID(AppID.GVMCostReport);
        }

        [WebMethod]
        public static string GenerateReport(string version, string data)
        {
            if (SiteMaster.VerifyRequest(HttpContext.Current.Session))
            {
                try
                {
                    User user = HttpContext.Current.Session[SKeys.User] as User;
                    ReportDateRange rdr = new ReportDateRange();
                    try
                    {
                        rdr = JsonConvert.DeserializeObject<ReportDateRange>(data);
                    }
                    catch
                    {
                        return JsonConvert.SerializeObject(ApiResponse.Error(ResponseType.ErrorGVMDateRangeInvalid), serializerSettings);
                    }
                    string fileToken = GVMReport.GenerateReport(user, rdr.StartDate, rdr.EndDate);
                    if (fileToken != "")
                    {
                        return JsonConvert.SerializeObject(ApiResponse.Success(fileToken), serializerSettings);
                    }
                    else
                    {
                        return JsonConvert.SerializeObject(ApiResponse.Error(ResponseType.ErrorGVMReportGenFailed), serializerSettings);
                    }
                }
                catch
                {
                    return JsonConvert.SerializeObject(ApiResponse.Error(ResponseType.ErrorGVMUnknown), serializerSettings);
                }
            }
            else
            {
                return JsonConvert.SerializeObject(ApiResponse.Error(ResponseType.ErrorAuth), serializerSettings);
            }
        }
    }
}