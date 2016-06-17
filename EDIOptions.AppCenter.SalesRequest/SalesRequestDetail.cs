using Newtonsoft.Json;

namespace EDIOptions.AppCenter.SalesRequest
{
    [JsonObject(MemberSerialization.OptOut)]
    public class SalesRequestDetail
    {
        public string RequestDate { get; set; }

        public string OutputName { get; set; }

        public string Email { get; set; }

        public string Status { get; set; }

        public SalesRequestDetail()
        {
            RequestDate = "";
            OutputName = "";
            Email = "";
            Status = "";
        }

        public SalesRequestDetail(string requestDate, string outName, string email, string status)
        {
            RequestDate = requestDate;
            OutputName = outName;
            Email = email;
            Status = status;
        }
    }
}