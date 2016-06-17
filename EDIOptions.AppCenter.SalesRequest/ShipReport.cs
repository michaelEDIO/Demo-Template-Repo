using Newtonsoft.Json;
using System.Collections.Generic;

namespace EDIOptions.AppCenter.SalesRequest
{
    [JsonObject(MemberSerialization.OptOut)]
    public class ShipReport
    {
        public string Store { get; set; }

        public List<ShipReportData> Shipments { get; set; }
    }
}