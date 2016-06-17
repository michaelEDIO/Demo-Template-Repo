using EDIOptions.AppCenter.Database;

namespace EDIOptions.AppCenter.LocationReport
{
    public class ItemInfo
    {
        public readonly string Category;
        public readonly string BrandName;
        public readonly string SubCategory;
        public readonly string Class;
        public readonly string SubClass;
        public readonly string Description;
        public readonly string ColorCode;
        public readonly string ColorDesc;

        public ItemInfo(DBResult res)
        {
            Category = res.FieldByName("prodcat", "");
            BrandName = res.FieldByName("deptname", "");
            SubCategory = res.FieldByName("prodsubcat", "");
            Class = res.FieldByName("classdesc", "");
            SubClass = res.FieldByName("subcdesc", "");
            Description = res.FieldByName("itemdesc", "");
            ColorCode = res.FieldByName("colorcode", "");
            ColorDesc = res.FieldByName("itemcolor", "");
        }
    }
}