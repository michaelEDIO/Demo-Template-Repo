using Newtonsoft.Json;
using System.Collections.Generic;

namespace EDIOptions.AppCenter.IntegrationManager
{
    [JsonObject(MemberSerialization.OptIn)]
    public class AutoMergeRecord
    {
        [JsonProperty(PropertyName = "keyList")]
        public List<string> KeyList { get; set; }

        [JsonProperty(PropertyName = "invList")]
        public List<string> InvoiceList { get; set; }

        [JsonProperty(PropertyName = "bolnumber")]
        public string BolNumber { get; set; }

        [JsonProperty(PropertyName = "stid")]
        public string StId { get; set; }

        [JsonProperty(PropertyName = "isdistributed")]
        public bool IsDistributed { get; set; }

        [JsonProperty(PropertyName = "xfertype")]
        public string TransferType { get; set; }

        public string LastById { get; set; }

        public AutoMergeRecord(IntHeadRecord res)
        {
            KeyList = new List<string>() { res.Key };
            InvoiceList = new List<string>() { res.InvoiceNumber };
            BolNumber = res.BolNumber;
            StId = res.ById;
            LastById = res.ById;
            IsDistributed = res.TransferType == _InvoiceTransferType.Distributed;
            TransferType = res.TransferType;
        }
    }
}