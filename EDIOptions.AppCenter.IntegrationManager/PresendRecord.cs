using Newtonsoft.Json;
using System.Collections.Generic;

namespace EDIOptions.AppCenter.IntegrationManager
{
    [JsonObject(MemberSerialization.OptIn)]
    public class PresendRecord
    {
        [JsonProperty(PropertyName = "isBad")]
        public bool HasBadRecords { get; set; }

        [JsonProperty(PropertyName = "isGood")]
        public bool HasRecordsToSend { get; set; }

        [JsonProperty(PropertyName = "badInvList")]
        public List<string> BadInvoice { get; set; }

        [JsonProperty(PropertyName = "badBolList")]
        public List<string> BadBOL { get; set; }

        [JsonProperty(PropertyName = "invPreList")]
        public List<IntHeadRecord> InvPreList { get; set; }

        [JsonProperty(PropertyName = "asnPreList")]
        public List<AutoMergeRecord> AsnPreList { get; set; }

        [JsonProperty(PropertyName = "invList")]
        public List<string> InvoiceList { get; set; }

        [JsonProperty(PropertyName = "bolList")]
        public List<string> BolList { get; set; }

        [JsonProperty(PropertyName = "consList")]
        public List<ConsInvoice> ConsInvlist { get; set; }

        [JsonProperty(PropertyName = "splitType")]
        public string SplitType { get; set; }

        [JsonProperty(PropertyName = "baseInv")]
        public string BaseInv { get; set; }

        public PresendRecord()
        {
            HasRecordsToSend = false;
            BadInvoice = new List<string>();
            BadBOL = new List<string>();
            InvoiceList = new List<string>();
            BolList = new List<string>();
            InvPreList = new List<IntHeadRecord>();
            AsnPreList = new List<AutoMergeRecord>();
            SplitType = "X";
            BaseInv = "";
        }
    }
}