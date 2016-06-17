using Newtonsoft.Json;

namespace EDIOptions.AppCenter
{
    [JsonObject(MemberSerialization.OptOut)]
    public class ApiResponse
    {
        public const string StatusSuccess = "good";
        public const string StatusWarning = "caution";
        public const string StatusError = "warning";

        public bool success { get; set; }

        public string type { get; set; }

        public object data { get; set; }

        [JsonIgnore]
        public static JsonSerializerSettings DefaultSerializerSettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };

        public static ApiResponse Success(object respdata = null)
        {
            return new ApiResponse()
            {
                success = true,
                type = StatusSuccess,
                data = respdata
            };
        }

        public static ApiResponse Warning(object respdata = null)
        {
            return new ApiResponse()
            {
                success = true,
                type = StatusWarning,
                data = respdata
            };
        }

        public static ApiResponse Error(ResponseType errorType)
        {
            return new ApiResponse()
            {
                success = false,
                type = StatusError,
                data = new
                {
                    type = errorType,
                    msg = ResponseDescription.Get(errorType)
                }
            };
        }

        public static string JSONSuccess(object respdata = null)
        {
            return JsonConvert.SerializeObject(Success(respdata), DefaultSerializerSettings);
        }

        public static string JSONWarning(object respdata = null)
        {
            return JsonConvert.SerializeObject(Warning(respdata), DefaultSerializerSettings);
        }

        public static string JSONError(ResponseType errorType)
        {
            return JsonConvert.SerializeObject(Error(errorType), DefaultSerializerSettings);
        }
    }
}