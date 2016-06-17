using Newtonsoft.Json;
using System.Collections.Generic;

namespace EDIOptions.AppCenter.ChangePO
{
    [JsonObject(MemberSerialization.OptOut)]
    public class CPOSummaryHead
    {
        public string UniqueKey { get; set; }
        public string PONumber { get; set; }
        public string POChangeDate { get; set; }
        public string Purpose { get; set; }
        public string Affected { get; set; }
        public string Status { get; set; }
        public List<CPOSummaryDetail> Details { get; set; }
    }
}