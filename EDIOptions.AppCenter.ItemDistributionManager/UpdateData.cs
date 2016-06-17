using System.Collections.Generic;
using Newtonsoft.Json;

namespace EDIOptions.AppCenter.ItemDistributionManager
{
    [JsonObject(MemberSerialization.OptOut)]
    public struct UpdateData
    {
        public bool[] days { get; set; }

        public int minDollars { get; set; }

        public int dayRange { get; set; }

        public Dictionary<string, QuantityInfo> itemInfo { get; set; }
    }
}