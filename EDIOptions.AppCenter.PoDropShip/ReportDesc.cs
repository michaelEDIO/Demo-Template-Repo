using EDIOptions.AppCenter.Database;
using Newtonsoft.Json;

namespace EDIOptions.AppCenter.PoDropShip
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ReportDesc
    {
        [JsonProperty(PropertyName = "key")]
        public readonly string Key;

        [JsonProperty(PropertyName = "ponumber")]
        public readonly string PoNumber;

        [JsonProperty(PropertyName = "trxtype")]
        public readonly string TrxType;

        [JsonProperty(PropertyName = "description")]
        public readonly string Description;

        [JsonProperty(PropertyName = "filename")]
        public readonly string FileName;

        [JsonProperty(PropertyName = "filepath")]
        public readonly string FilePath;

        public ReportDesc(DBResult res, string prefix)
        {
            Key = res.FieldByName(_Column.UniqueKey);
            PoNumber = res.FieldByName(_Column.PONumber);
            TrxType = res.FieldByName(_Column.TrxType);
            FileName = res.FieldByName("filename1");
            FilePath = $"{prefix}{FileName}";
            Description = res.FieldByName("description");
        }
    }
}