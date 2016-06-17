using Newtonsoft.Json;

namespace EDIOptions.AppCenter
{
    [JsonObject(MemberSerialization.OptOut)]
    public class ProcessRecord
    {
        public string Date { get; set; }

        public string Type { get; set; }

        public string Status { get; set; }
    }
}