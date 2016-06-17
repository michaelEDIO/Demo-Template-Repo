using EDIOptions.AppCenter.Database;
using System.Collections.Generic;

namespace EDIOptions.AppCenter.LocationReport
{
    public class LocReportLine
    {
        public readonly string MilPo;
        public readonly string PidStyle;
        public readonly string Upc;
        public string ColorCode { get; set; }
        public string ColorDesc { get; set; }
        public string Category { get; set; }
        public string BrandName { get; set; }
        public string SubCategory { get; set; }
        public string Class { get; set; }
        public string SubClass { get; set; }
        public string Description { get; set; }
        public readonly string MilCost;
        public readonly string TtlMilCost;
        public readonly string Retail;
        public readonly string TtlRetail;
        public readonly string Store;
        public readonly string StoreLookup;
        public string StoreName { get; set; }
        public readonly string ShipDate;
        public readonly string OnOrder;
        public readonly string Shipped;

        public LocReportLine(DBResult res, string partner)
        {
            MilPo = res.FieldByName("milpo").Trim();
            PidStyle = res.FieldByName("pidstyle", "").Trim();
            Upc = res.FieldByName("upc", "").Trim();
            MilCost = res.FieldByName("milcost", "0");
            TtlMilCost = res.FieldByName("ttlmilcost", "0");
            Retail = res.FieldByName("retail", "0");
            TtlRetail = res.FieldByName("ttlretail", "0");
            Store = res.FieldByName("store").Trim();
            switch (partner)
            {
                case _Partner.NavyReplenishment:
                    // p.BuyerName = "d.nexbuyer";
                    // p.StoreJoin = "substr(lpad(replace(replace(d856.byid,'E',''),'W',''),4,'0'),2,3)=" + p.StoreWhere;
                    StoreLookup = Store.Replace("E", "").Replace("W", "").PadLeft(4, '0').Substring(1, 3);
                    break;

                case _Partner.MarinesReplenishment:
                    // p.BuyerName = "d.mcxbuyer";
                    // p.StoreJoin = "if(LENGTH(trim(d856.byid))>4,s.byid=d856.byid, trim(s.xrefid)=substr(trim(LEADING '0' from d856.byid),1,3))"; //WE ARE EXLUDING 4 CHAR STORES THAT DON'T END IN E/W
                    if (Store.Length > 4)
                    {
                        StoreLookup = Store;
                    }
                    else
                    {
                        StoreLookup = Store.TrimStart('0').Substring(0, 3);
                    }
                    break;

                default:
                    StoreLookup = Store;
                    break;
            }
            ShipDate = res.FieldByName("shipdate");
            OnOrder = res.FieldByName("onorder");
            Shipped = res.FieldByName("shipped");
        }

        public void SetStoreName(string sPartner, StLookupDict storeList)
        {
            if (sPartner == _Partner.MarinesReplenishment)
            {
                if (Store.Length > 4)
                {
                    if (storeList.byidDict.ContainsKey(StoreLookup))
                    {
                        StoreName = storeList.byidDict[StoreLookup];
                    }
                }
                else
                {
                    if (storeList.xrefDict.ContainsKey(StoreLookup))
                    {
                        StoreName = storeList.xrefDict[StoreLookup];
                    }
                }
            }
            else
            {
                if (storeList.byidDict.ContainsKey(StoreLookup))
                {
                    StoreName = storeList.byidDict[StoreLookup];
                }
            }
        }

        public void SetItemInfo(Dictionary<string, ItemInfo> lookup)
        {
            string key = PidStyle + Upc;
            if (lookup.ContainsKey(key))
            {
                var val = lookup[key];
                ColorCode = val.ColorCode;
                ColorDesc = val.ColorDesc;
                Category = val.Category;
                BrandName = val.BrandName;
                SubCategory = val.SubCategory;
                Class = val.Class;
                SubClass = val.SubClass;
                Description = val.Description;
            }
        }
    }
}