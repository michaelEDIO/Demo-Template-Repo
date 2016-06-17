using Newtonsoft.Json;

namespace EDIOptions.AppCenter.SalesRequest
{
    [JsonObject(MemberSerialization.OptOut)]
    public class SaleData
    {
        public RetailWeekData RetailWeek { get; set; }

        public int Sold { get; set; }

        public int Order { get; set; }

        public int OnHand { get; set; }
    }
}