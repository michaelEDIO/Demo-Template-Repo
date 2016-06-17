using Newtonsoft.Json;

namespace EDIOptions.AppCenter.ChangePO
{
    [JsonObject(MemberSerialization.OptOut)]
    public class CPOSummaryDetail
    {
        public string ChangeType { get; set; }
        public string Quantity { get; set; }
        public string ChangeQuantity { get; set; }
        public string UnitPrice { get; set; }
        public string RetailPrc { get; set; }
        public string UPC { get; set; }
        public string VendorNum { get; set; }
        public string ItemDesc { get; set; }
        public string PackSize { get; set; }
        public string Dropship { get; set; }
        public string Status { get; set; }
    }
}