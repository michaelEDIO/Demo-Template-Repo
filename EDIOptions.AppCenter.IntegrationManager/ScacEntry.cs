using EDIOptions.AppCenter.Database;
using Newtonsoft.Json;

namespace EDIOptions.AppCenter.IntegrationManager
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ScacEntry
    {
        [JsonProperty(PropertyName = "scaccode")]
        public string ScacCode { get; set; }

        [JsonProperty(PropertyName = "scacdesc")]
        public string Description { get; set; }

        public ScacEntry()
        {
            ScacCode = "";
            Description = "";
        }

        public ScacEntry(DBResult res)
        {
            ScacCode = res.FieldByName(_Column.PickVal);
            Description = res.FieldByName(_Column.PickDesc);
        }
    }
}