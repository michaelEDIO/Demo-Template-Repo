using Newtonsoft.Json;

namespace EDIOptions.AppCenter.ItemDistributionManager
{
    [JsonObject(MemberSerialization = MemberSerialization.OptOut)]
    public class ItemInfo
    {
        public string Vendor { get; set; }
        public string ItemUPC { get; set; }
        public string Description { get; set; }
        public string Price { get; set; }
        public QuantityInfo Base { get; set; }
        public QuantityInfo Current { get; set; }

        [JsonIgnore]
        public int BaseMin { get { return Base.Min; } set { Base.Min = value; } }

        [JsonIgnore]
        public int BaseMax { get { return Base.Max; } set { Base.Min = value; } }

        [JsonIgnore]
        public int BaseReorder { get { return Base.Reorder; } set { Base.Reorder = value; } }

        [JsonIgnore]
        public int CurMin { get { return Current.Min; } set { Current.Min = value; } }

        [JsonIgnore]
        public int CurMax { get { return Current.Max; } set { Current.Max = value; } }

        [JsonIgnore]
        public int CurReorder { get { return Current.Reorder; } set { Current.Reorder = value; } }

        [JsonIgnore]
        public bool Update { get; set; }

        public ItemInfo()
        {
            Vendor = "TEST";
            Description = "Test product.";
            Base = new QuantityInfo();
            Current = new QuantityInfo();
            Update = false;
        }

        public ItemInfo Clone()
        {
            return new ItemInfo()
            {
                Vendor = this.Vendor,
                ItemUPC = this.ItemUPC,
                Description = this.Description,
                Price = this.Price,
                Base = this.Base.Clone(),
                Current = this.Current.Clone()
            };
        }
    }
}