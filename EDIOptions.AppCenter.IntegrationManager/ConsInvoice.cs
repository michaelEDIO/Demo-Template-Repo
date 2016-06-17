using Newtonsoft.Json;
using System.Collections.Generic;

namespace EDIOptions.AppCenter.IntegrationManager
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ConsInvoice
    {
        [JsonProperty(PropertyName = "releasenum")]
        public string MasterPONumber { get; set; }

        [JsonProperty(PropertyName = "invList")]
        public List<string> InvoicesGrouped { get; set; }
    }
}