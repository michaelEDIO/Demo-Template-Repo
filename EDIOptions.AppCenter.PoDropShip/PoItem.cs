using EDIOptions.AppCenter.Database;
using Newtonsoft.Json;
using System;

namespace EDIOptions.AppCenter.PoDropShip
{
    [JsonObject(MemberSerialization.OptIn)]
    public class PoItem
    {
        [JsonProperty(PropertyName = "key")]
        public readonly string Key;

        [JsonProperty(PropertyName = "ponumber")]
        public readonly string PoNumber;

        [JsonProperty(PropertyName = "podate")]
        public readonly string PoDate;

        [JsonProperty(PropertyName = "shipdate")]
        public readonly string ShipDate;

        [JsonProperty(PropertyName = "canceldate")]
        public readonly string CancelDate;

        [JsonProperty(PropertyName = "potype")]
        public readonly string PoType;

        [JsonProperty(PropertyName = "invtotal")]
        public readonly string InvTotal;

        [JsonProperty(PropertyName = "asntotal")]
        public readonly string AsnTotal;

        [JsonProperty(PropertyName = "invstatus")]
        public readonly string InvStatus;

        [JsonProperty(PropertyName = "asnstatus")]
        public readonly string AsnStatus;

        public PoItem(DBResult res)
        {
            Key = res.FieldByName(_Column.UniqueKey);
            PoNumber = res.FieldByName(_Column.PONumber, "").Trim();
            var pod = (DateTime)res.FieldByName2(_Column.PODate, DateTime.MinValue);
            var ship = (DateTime)res.FieldByName2(_Column.ShipmentDate, DateTime.MinValue);
            var canc = (DateTime)res.FieldByName2(_Column.CancelDate, DateTime.MinValue);
            PoDate = pod == DateTime.MinValue ? "--" : pod.ToString("MMM dd, yyyy");
            ShipDate = ship == DateTime.MinValue ? "--" : ship.ToString("MMM dd, yyyy");
            CancelDate = canc == DateTime.MinValue ? "--" : canc.ToString("MMM dd, yyyy");
            PoType = res.FieldByName("potype", "").Trim();
            int tci = 0;
            int tca = 0;
            int tct = 0;
            int.TryParse(res.FieldByName("totcmpinv", "0"), out tci);
            int.TryParse(res.FieldByName("totcmpasn", "0"), out tca);
            int.TryParse(res.FieldByName(_Column.TotalItems, "0"), out tct);
            InvTotal = $"{tci}/{tct}";
            AsnTotal = $"{tca}/{tct}";
            InvStatus = (tci == tct) ? "good" : "caution";
            AsnStatus = (tca == tct) ? "good" : "caution";
        }
    }
}