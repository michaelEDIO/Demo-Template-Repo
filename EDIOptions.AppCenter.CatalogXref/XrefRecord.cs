using EDIOptions.AppCenter.Database;
using Newtonsoft.Json;

namespace EDIOptions.AppCenter.CatalogXref
{
    [JsonObject(MemberSerialization.OptOut)]
    public class XrefRecord
    {
        [JsonProperty(PropertyName = "key")]
        public string Key { get; set; } = "";

        [JsonProperty(PropertyName = "vendorname")]
        public string CompanyName { get; set; } = "";

        [JsonProperty(PropertyName = "vendorid")]
        public string GXSAccount { get; set; } = "";

        [JsonProperty(PropertyName = "vendorseq")]
        public string SelectionCode { get; set; } = "";

        [JsonProperty(PropertyName = "brandname")]
        public string BrandName { get; set; } = "";

        public XrefRecord()
        {
        }

        public XrefRecord(DBResult res)
        {
            Key = res.FieldByName(_Column.UniqueKey);
            CompanyName = res.FieldByName(_Column.VendorName, "");
            GXSAccount = res.FieldByName(_Column.VendorId, "");
            SelectionCode = res.FieldByName(_Column.VendorSeq, "");
            BrandName = res.FieldByName(_Column.BrandName, "");
        }
    }
}