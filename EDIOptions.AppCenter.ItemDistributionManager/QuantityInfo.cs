using Newtonsoft.Json;

namespace EDIOptions.AppCenter.ItemDistributionManager
{
    [JsonObject(MemberSerialization.OptOut)]
    public class QuantityInfo
    {
        public int Min { get; set; }
        public int Max { get; set; }
        public int Reorder { get; set; }

        public QuantityInfo()
        {
            Min = Max = Reorder = 0;
        }

        public QuantityInfo Clone()
        {
            QuantityInfo qi = new QuantityInfo();
            qi.Min = this.Min;
            qi.Max = this.Max;
            qi.Reorder = this.Reorder;
            return qi;
        }
    }
}