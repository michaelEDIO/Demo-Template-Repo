using Newtonsoft.Json;

namespace EDIOptions.AppCenter.SalesRequest
{
    [JsonObject(MemberSerialization.OptOut)]
    public class ShipItemInfo
    {
        public string VendorNum { get; set; }

        public string UPCNum { get; set; }

        public int Quantity { get; set; }
    }
}