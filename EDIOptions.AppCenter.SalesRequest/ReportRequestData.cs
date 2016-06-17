using Newtonsoft.Json;
using System.Collections.Generic;

namespace EDIOptions.AppCenter.SalesRequest
{
    [JsonObject(MemberSerialization.OptOut)]
    public class ReportRequestData
    {
        public List<string> Stores { get; set; }

        public List<RetailWeekData> Weeks { get; set; }

        public string Email { get; set; }

        public string RequestType { get; set; }

        public ReportRequestData()
        {
            Stores = new List<string>();
            Weeks = new List<RetailWeekData>();
            Email = "";
            RequestType = "ESS";
        }
    }
}