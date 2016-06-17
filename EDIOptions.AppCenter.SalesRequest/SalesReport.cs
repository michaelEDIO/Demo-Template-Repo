using Newtonsoft.Json;
using System.Collections.Generic;

namespace EDIOptions.AppCenter.SalesRequest
{
    [JsonObject(MemberSerialization.OptOut)]
    public class SalesReport
    {
        public string VendorNum { get; set; }

        public decimal UnitCost { get; set; }

        public decimal UnitPrice { get; set; }

        public int OnHand { get; set; }

        public List<SaleData> Details { get; set; }

        public SalesReport()
        {
            VendorNum = "";
            UnitCost = 0;
            UnitPrice = 0;
            Details = new List<SaleData>();
        }
    }
}