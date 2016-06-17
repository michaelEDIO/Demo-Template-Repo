using Newtonsoft.Json;
using System.Collections.Generic;

namespace EDIOptions.AppCenter.IntegrationManager
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SubmitPreRequest
    {
        [JsonProperty(PropertyName = "isAll")]
        public bool? IsAll { get; set; }

        [JsonProperty(PropertyName = "subType")]
        public string SubmitType { get; set; }

        [JsonProperty(PropertyName = "keyList")]
        public List<string> KeyList { get; set; }

        [JsonProperty(PropertyName = "filter")]
        public FilterInfo CurrentFilter { get; set; }

        [JsonProperty(PropertyName = "splitType")]
        public string SplitType { get; set; }

        [JsonProperty(PropertyName = "baseInv")]
        public string BaseInv { get; set; }
    }
}