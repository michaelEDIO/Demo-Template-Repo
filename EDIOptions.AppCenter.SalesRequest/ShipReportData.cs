using Newtonsoft.Json;
using System.Collections.Generic;

namespace EDIOptions.AppCenter.SalesRequest
{
    [JsonObject(MemberSerialization.OptOut)]
    public class ShipReportData
    {
        public string ShipDate { get; set; }

        public string TrackingNumber { get; set; }

        public int Quantity { get; set; }

        public List<ShipItemInfo> Items { get; set; }
    }
}