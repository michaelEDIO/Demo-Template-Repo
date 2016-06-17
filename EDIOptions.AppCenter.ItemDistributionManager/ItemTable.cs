using EDIOptions.AppCenter.Database;
using EDIOptions.AppCenter.Session;
using System;
using System.Collections.Generic;

namespace EDIOptions.AppCenter.ItemDistributionManager
{
    public struct DistroInfo
    {
        public bool IsSunday { get; set; }
        public bool IsMonday { get; set; }
        public bool IsTuesday { get; set; }
        public bool IsWednesday { get; set; }
        public bool IsThursday { get; set; }
        public bool IsFriday { get; set; }
        public bool IsSaturday { get; set; }
        public int MinDollars { get; set; }
        public int DayRange { get; set; }

        public bool IsEveryday
        {
            get
            {
                return IsSunday && IsMonday && IsTuesday && IsWednesday && IsThursday && IsFriday && IsSaturday;
            }
        }

        public bool IsNoDay
        {
            get
            {
                return !IsSunday && !IsMonday && !IsTuesday && !IsWednesday && !IsThursday && !IsFriday && !IsSaturday;
            }
        }

        public override string ToString()
        {
            if (IsEveryday)
            {
                return "Every Day";
            }
            else if (IsNoDay)
            {
                return "No Days Selected";
            }
            else
            {
                string ret = "";
                if (IsSunday)
                {
                    ret += "Sunday";
                }
                if (IsMonday)
                {
                    if (ret.Length > 0)
                    {
                        ret += ", ";
                    }
                    ret += "Monday";
                }
                if (IsTuesday)
                {
                    if (ret.Length > 0)
                    {
                        ret += ", ";
                    }
                    ret += "Tuesday";
                }
                if (IsWednesday)
                {
                    if (ret.Length > 0)
                    {
                        ret += ", ";
                    }
                    ret += "Wednesday";
                }
                if (IsThursday)
                {
                    if (ret.Length > 0)
                    {
                        ret += ", ";
                    }
                    ret += "Thursday";
                }
                if (IsFriday)
                {
                    if (ret.Length > 0)
                    {
                        ret += ", ";
                    }
                    ret += "Friday";
                }
                if (IsSaturday)
                {
                    if (ret.Length > 0)
                    {
                        ret += ", ";
                    }
                    ret += "Saturday";
                }
                return ret;
            }
        }
    }

    public static class ItemTable
    {
        private const string tableDistroDays = "sbt_groupinfo";

        private const string columnReviewSun = "review_su";
        private const string columnReviewMon = "review_mo";
        private const string columnReviewTue = "review_tu";
        private const string columnReviewWed = "review_we";
        private const string columnReviewThu = "review_th";
        private const string columnReviewFri = "review_fr";
        private const string columnReviewSat = "review_sa";
        private const string columnMinDollars = "mindollars";
        private const string columnDayRange = "dayrange";

        private const string qFetchCurrDist = "SELECT * FROM (SELECT vendornum,qtymin,qtymax,qtyreor FROM (SELECT stid,displays FROM sbt_stores WHERE inactive = 0 AND displays = 1 AND Partner='{0}') AS store INNER JOIN cvs_item_dist ON store.stid = cvs_item_dist.stid GROUP BY vendornum) AS itemlist";
        private const string qFetchBaseDist = "SELECT vendornum,upcnum,itemdesc,qtymin,qtymax,qtyreor FROM cvs_items";
        private const string qUpdateItems = "UPDATE cvs_item_dist i JOIN sbt_stores callback ON callback.Partner='{4}' and i.stid=callback.stid SET qtymin={1}*displays,qtymax={2}*displays,qtyreor={3}*displays WHERE i.vendornum='{0}' AND callback.inactive=0 AND callback.displays>0";

        public static DistroInfo GetDistributionDays(User user)
        {
            DistroInfo distroDays = new DistroInfo();
            try
            {
                DBConnect connection = ConnectionsMgr.GetSharedConnection(user, _Database.ESIC);
                {
                    using (var queryDistroDays = connection.Select(new[] { columnReviewSun, columnReviewMon, columnReviewTue, columnReviewWed, columnReviewThu, columnReviewFri, columnReviewSat, columnMinDollars, columnDayRange }, tableDistroDays, string.Format("WHERE {0}='{1}'", _Column.Partner, user.ActivePartner.SQLEscape())))
                    {
                        if (queryDistroDays.AffectedRows == 0)
                        {
                            ProgramLog.LogError(user, "ItemTable", "GetDistributionDays", string.Format("No distribution data for partner \"{0}\"", user.ActivePartner));
                            return distroDays;
                        }
                        queryDistroDays.Read();
                        distroDays.IsSunday = queryDistroDays.Field(0, "0").ToString() != "0";
                        distroDays.IsMonday = queryDistroDays.Field(1, "0").ToString() != "0";
                        distroDays.IsTuesday = queryDistroDays.Field(2, "0").ToString() != "0";
                        distroDays.IsWednesday = queryDistroDays.Field(3, "0").ToString() != "0";
                        distroDays.IsThursday = queryDistroDays.Field(4, "0").ToString() != "0";
                        distroDays.IsFriday = queryDistroDays.Field(5, "0").ToString() != "0";
                        distroDays.IsSaturday = queryDistroDays.Field(6, "0").ToString() != "0";
                        distroDays.MinDollars = int.Parse(queryDistroDays.Field(7, "0"));
                        distroDays.DayRange = int.Parse(queryDistroDays.Field(8, "0"));
                    }
                }
                connection.Close();
                return distroDays;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, "ItemTable", "GetDistributionDays", e.Message);
                return distroDays;
            }
        }

        public static List<ItemInfo> GetItemData(User user)
        {
            Dictionary<string, QuantityInfo> currentDict = new Dictionary<string, QuantityInfo>();
            List<ItemInfo> _itemTable = new List<ItemInfo>();
            try
            {
                DBConnect connection = ConnectionsMgr.GetSharedConnection(user, _Database.ESIC);
                {
                    using (var reader = connection.Query(string.Format(qFetchCurrDist, user.ActivePartner.SQLEscape())))
                    {
                        while (reader.Read())
                        {
                            string vendor = reader.Field(0, "").ToString();
                            QuantityInfo cqi = new QuantityInfo();
                            string min = reader.Field(1, "0");
                            string max = reader.Field(2, "0");
                            string reo = reader.Field(3, "0");
                            cqi.Min = (int)double.Parse(min);
                            cqi.Max = (int)double.Parse(max);
                            cqi.Reorder = (int)double.Parse(reo);
                            currentDict.Add(vendor, cqi);
                        }
                    }

                    using (var reader = connection.Query(qFetchBaseDist))
                    {
                        while (reader.Read())
                        {
                            ItemInfo info = new ItemInfo();
                            info.Vendor = reader.Field(0, "").ToString();
                            info.ItemUPC = reader.Field(1, "").ToString();
                            info.Description = reader.Field(2, "").ToString();
                            info.Base = new QuantityInfo()
                            {
                                Min = int.Parse(reader.Field(3, "0")),
                                Max = int.Parse(reader.Field(4, "0")),
                                Reorder = int.Parse(reader.Field(5, "0"))
                            };
                            if (currentDict.ContainsKey(info.Vendor))
                            {
                                info.Current = currentDict[info.Vendor];
                            }
                            _itemTable.Add(info);
                            ItemInfo infoCopy = info.Clone();
                        }
                    }
                }
                connection.Close();
                return _itemTable;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, "ItemTable", "GetItemData", e.Message);
                return new List<ItemInfo>();
            }
        }

        public static void DoChanges(User user, UpdateData updateData)
        {
            Dictionary<string, string> updateDistroVals = new Dictionary<string, string>()
            {
                {columnReviewSun, updateData.days[0] ? "1" : "0"},
                {columnReviewMon, updateData.days[1] ? "1" : "0"},
                {columnReviewTue, updateData.days[2] ? "1" : "0"},
                {columnReviewWed, updateData.days[3] ? "1" : "0"},
                {columnReviewThu, updateData.days[4] ? "1" : "0"},
                {columnReviewFri, updateData.days[5] ? "1" : "0"},
                {columnReviewSat, updateData.days[6] ? "1" : "0"},
                {columnMinDollars, updateData.minDollars.ToString()},
                {columnDayRange, updateData.dayRange.ToString()}
            };
            try
            {
                DBConnect connection = ConnectionsMgr.GetSharedConnection(user, _Database.ESIC);
                {
                    connection.Update(tableDistroDays, updateDistroVals.ToNameValueCollection(), string.Format("WHERE {0}='{1}'", _Column.Partner, user.ActivePartner.SQLEscape()));

                    foreach (var kvp in updateData.itemInfo)
                    {
                        string qtymin = kvp.Value.Min.ToString();
                        string qtymax = kvp.Value.Max.ToString();
                        string qtyreo = kvp.Value.Reorder.ToString();
                        string vennum = kvp.Key;
                        connection.Query(string.Format(qUpdateItems, vennum.SQLEscape(), qtymin.SQLEscape(), qtymax.SQLEscape(), qtyreo.SQLEscape(), user.ActivePartner.SQLEscape()));
                    }
                }
                connection.Close();
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, "ItemTable", "DoChanges", e.Message);
            }
        }
    }
}