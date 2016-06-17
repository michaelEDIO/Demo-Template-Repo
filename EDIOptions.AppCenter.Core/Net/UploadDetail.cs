using Newtonsoft.Json;

namespace EDIOptions.AppCenter
{
    [JsonObject(MemberSerialization.OptOut)]
    public class UploadDetail
    {
        public string Extension { get; set; }

        public string Token { get; set; }
    }
}