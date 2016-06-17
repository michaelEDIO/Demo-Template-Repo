using Newtonsoft.Json;

namespace EDIOptions.AppCenter.Support
{
    [JsonObject(MemberSerialization.OptOut)]
    public class ReportDetail
    {
        public string Name { get; set; }

        public string Company { get; set; }

        public string Email { get; set; }

        public string Message { get; set; }
    }
}