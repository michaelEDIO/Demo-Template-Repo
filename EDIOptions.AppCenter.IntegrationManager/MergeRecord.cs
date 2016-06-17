using Newtonsoft.Json;

namespace EDIOptions.AppCenter.IntegrationManager
{
    [JsonObject(MemberSerialization.OptIn)]
    public class MergeRecord
    {
        [JsonProperty(PropertyName = "bolnumber")]
        public string BolNumber { get; set; }

        [JsonProperty(PropertyName = "cartType")]
        public string CartonDistType { get; set; }

        [JsonProperty(PropertyName = "packType")]
        public string PackType { get; set; }
    }
}