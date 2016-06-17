using EDIOptions.AppCenter.Database;
using EDIOptions.AppCenter.Security;
using EDIOptions.AppCenter.Session;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static EDIOptions.AppCenter._Column;
using System.Runtime.InteropServices;

namespace EDIOptions.AppCenter.LocationReport
{
    public class LocationReportHandler
    {
        private static HashSet<string> excludeBol = new HashSet<string>()
        {
            _Partner.Thalia,
            _Partner.MarinesReplenishment,
            _Partner.NavyReplenishment
        };

        private static HashSet<string> excludeAsn = new HashSet<string>()
        {
            _Partner.Anastasia,
            _Partner.Thalia,
            _Partner.MarinesReplenishment,
            _Partner.NavyReplenishment
        };

        private static HashSet<string> excludeBrand = new HashSet<string>()
        {
            _Partner.Anastasia
        };

        private static HashSet<string> excludeAE = new HashSet<string>()
        {
            _Partner.Anastasia,
            _Partner.Thalia,
            _Partner.MarinesReplenishment,
            _Partner.NavyReplenishment
        };

        public static LOCOptions GetOptions(string partner)
        {
            return new LOCOptions()
            {
                ShowLoc = true,
                ShowAsn = !excludeAsn.Contains(partner),
                ShowBol = !excludeBol.Contains(partner),
                ShowStore = true,
                ShowBrand = !excludeBrand.Contains(partner),
                ShowAE = !excludeAE.Contains(partner)
            };
        }

        #region Retrieving filter lists

        /// <summary>
        /// Gets the current list of account executives.
        /// </summary>
        /// <param name="user">The user making the request.</param>
        /// <returns></returns>
        public static List<string> GetAccountExecutiveList(User user)
        {
            List<string> aeList = new List<string>();
            try
            {
                DBConnect connection = ConnectionsMgr.GetSharedConnection(user, _Database.Home);
                {
                    var query = connection.Query("SELECT aename FROM deptdesc GROUP BY aename ORDER BY aename");
                    while (query.Read())
                    {
                        var ae = query.Field(0, "").Trim();
                        if (ae != "")
                        {
                            aeList.Add(ae);
                        }
                    }
                }
                connection.Close();
                aeList.Sort();
            }
            catch (Exception e)
            {
                Log(user, nameof(GetAccountExecutiveList), e);
            }
            return aeList;
        }

        /// <summary>
        /// Gets the current list of brand names.
        /// </summary>
        /// <param name="user">The user making the request.</param>
        /// <returns></returns>
        public static List<string> GetBrandNameList(User user)
        {
            List<string> bnList = new List<string>();
            try
            {
                string sCustomer = user.Customer.SQLEscape().ToLower();
                DBConnect connection = ConnectionsMgr.GetSalesConnection(user, "sales_" + sCustomer);
                {
                    var query = connection.Query("SELECT brand FROM sl_filters GROUP BY brand ORDER BY brand");
                    while (query.Read())
                    {
                        var bn = query.Field(0, "").Trim();
                        if (bn != "")
                        {
                            bnList.Add(bn);
                        }
                    }
                }
                connection.Close();
            }
            catch (Exception e)
            {
                Log(user, nameof(GetBrandNameList), e);
            }
            return bnList;
        }

        /// <summary>
        /// Gets the current list of stores.
        /// </summary>
        /// <param name="user">The user making the request.</param>
        /// <returns></returns>
        public static List<Store> GetStoreList(User user)
        {
            List<Store> stores = new List<Store>();
            string sPartner = user.ActivePartner.SQLEscape();
            string prtCondition = "";
            string storePartner = GetStorePartner(sPartner);
            if (sPartner == _Partner.Marines)
            {
                prtCondition = "AND (LENGTH(xrefid)=3 OR LENGTH(xrefid)=5)"; //EXCLUDE 4 CHAR STORES THAT DON'T END IN 'E' OR 'W' (AKA HAVE 4 CHAR XREFID)
            }
            try
            {
                DBConnect connection = ConnectionsMgr.GetSharedConnection(user, _Database.Home);
                using (DBResult res = connection.Select(new[] { STName, BYId }, _Table.Stinfo, $"WHERE partner='{storePartner}' and byid!='' {prtCondition} GROUP BY byid ORDER BY byid,upddate DESC"))
                {
                    while (res.Read())
                    {
                        var s = new Store(res);
                        stores.Add(s);
                    }
                }
                connection.Close();
            }
            catch (Exception e)
            {
                Log(user, nameof(GetStoreList), e);
            }
            return stores;
        }

        #endregion Retrieving filter lists

        #region Generating reports

        public static string GenerateLocationReport(User user, LocationReportDetails reportDetails)
        {
            switch (user.ActivePartner)
            {
                case _Partner.Anastasia:
                    return GenLocationReportANAS(user, reportDetails);

                case _Partner.Thalia:
                case _Partner.NavyReplenishment:
                case _Partner.MarinesReplenishment:
                    return GenLocationReportThlaNavrMrnr(user, reportDetails);

                default:
                    return GenLocationReportGeneral(user, reportDetails);
            }
        }

        public static string GenerateASNSummaryReport(User user, LocationReportDetails reportDetails)
        {
            switch (user.ActivePartner)
            {
                case _Partner.Anastasia:
                    return "";

                default:
                    return GenASNReportGeneral(user, reportDetails);
            }
        }

        private static string GenLocationReportANAS(User user, LocationReportDetails reportDetails)
        {
            try
            {
                string sCustomer = user.Customer.SQLEscape();
                string sPartner = user.ActivePartner.SQLEscape();
                string reportTemplate = "SELECT shipmonth AS `Ship Month`," +
                       "shipdate AS `Ship Date`," +
                       "ponumber AS `PO #`," +
                       "podate AS `PO Date`," +
                       "canceldate AS `Cancel Date`," +
                       "stid AS `STID`," +
                       "stname AS `Ship To Name`," +
                       "SUM(quantity) AS `Quantity`," +
                       "vendornum AS `Vendor #`," +
                       "upcnum AS `UPC #`," +
                       "itemdesc AS `Item Description`," +
                       "SUM(CAST(unitprice AS DECIMAL(14,2))) AS `Unit Price`," +
                       "SUM(CAST(lineprice AS DECIMAL(14,2))) AS `Extended Price`" +
                "FROM" +
                  "(SELECT DATE_FORMAT(h810.shipdate,'%M %Y') AS shipmonth," +
                          "h810.shipdate AS shipdate," +
                          "h810.ponumber AS ponumber," +
                          "h810.podate AS podate," +
                          "h850.canceldate AS canceldate," +
                          "h810.stid AS stid," +
                          "s.stname AS stname," +
                          "ROUND(d810.quantity) AS quantity," +
                          "TRIM(d810.vendornum) AS vendornum," +
                          "d810.upcnum AS upcnum," +
                          "d850.itemdesc AS itemdesc," +
                          "d810.unitprice AS unitprice," +
                          "d810.unitprice * d810.quantity AS lineprice" +
                   " FROM (SELECT uniquekey,uniquepo,shipdate,ponumber,podate,stid,byid FROM ecgb.trxh810 WHERE customer='{0}' AND partner='ANAS' AND shipdate>='{1}' AND shipdate<='{2}') AS h810" +
                   " JOIN ecgb.trxd810 AS d810 ON h810.uniquekey=d810.uniquekey AND !isnull(d810.invoiceno)" +
                   " LEFT JOIN srch850 AS h850 ON h810.uniquepo=h850.uniquekey" +
                   " LEFT JOIN srcd850 AS d850 ON h850.uniquekey=d850.uniquekey AND d810.uniqpoitem=d850.uniqueitem" +
                   " LEFT JOIN temp.xstore AS s ON h810.byid=s.byid" +
                   " WHERE {3}" +
                   " GROUP BY ponumber,vendornum,upcnum,h810.byid,shipdate)" +
                " AS st GROUP BY ponumber,vendornum,upcnum,shipdate;";

                LocPrtSpecific prtInfo = GetPartnerProperties(sPartner);

                string conditionDetail = GetConditionDetail(prtInfo, reportDetails, sPartner);

                string formatReport = string.Format(reportTemplate, sCustomer, reportDetails.StartDate.ToMySQLDateStr(), reportDetails.EndDate.ToMySQLDateStr(), conditionDetail);

                var header = new[] { "Ship Month", "Ship Date", "PO #", "PO Date", "Cancel Date", "STID", "Ship To Name", "Quantity", "Vendor #", "UPC #", "Item Description", "Unit Price", "Extended Price" };

                int colCount = 0;
                int rowCount = 0;
                object[,] reportData = null;

                List<List<string>> storeNameData = new List<List<string>>();
                {
                    string condStore = "";
                    if (reportDetails.Stores.Count > 0)
                    {
                        condStore = string.Format("AND byid IN {0}", reportDetails.Stores.ToSqlValueList());
                    }

                    DBConnect connectionSHR = ConnectionsMgr.GetSharedConnection(user, _Database.Home);
                    using (var queryStore = connectionSHR.Select("byid,stname", _Table.Stinfo, string.Format("WHERE partner='NAVY' {0} GROUP BY byid", condStore)))
                    {
                        while (queryStore.Read())
                        {
                            storeNameData.Add(new List<string>() { queryStore.Field(0), queryStore.Field(1) });
                        }
                    }
                    connectionSHR.Close();
                }

                DBConnect connection = ConnectionsMgr.GetOCConnection(user, _Database.ECGB);

                if (!CreateTempStoreTableFromData(user, connection, storeNameData))
                {
                    return "";
                }

                using (var resultReport = connection.Query(formatReport))
                {
                    colCount = resultReport.FieldCount;
                    rowCount = resultReport.AffectedRows;
                    reportData = CopyResultDataToArray(resultReport);
                }

                return SaveReportToExcelFile(user, header, reportData, rowCount, colCount, new[] { "L:L", "M:M" });
            }
            catch (Exception e)
            {
                Log(user, nameof(GenLocationReportANAS), e);
                return "";
            }
        }

        private static string GenLocationReportGeneral(User user, LocationReportDetails reportDetails)
        {
            try
            {
                string sPartner = user.ActivePartner.SQLEscape();
                string reportTemplate = "SELECT ponumber AS `Mil PO`," +
                        "arrivdate AS `DC Month`," +
                        "vendornum  AS `PID Style`," +
                        "colorcode AS `Color Code`," +
                        "colordesc AS `Color Decription`," +
                        "category AS `Category`," +
                        "branddesc AS `Brand Name`," +
                        "scategory AS `Sub Category`," +
                        "prodclass AS `Class`," +
                        "sprodclass AS `Sub Class`," +
                        "itemdesc AS `Description`," +
                        "SUM(CAST(unitprice AS DECIMAL(14,2))) AS `GLC`," +
                        "SUM(CAST(chgprice AS DECIMAL(14,2))) AS `Mil Cost`," +
                        "SUM(CAST(retailprc AS DECIMAL(14,2))) AS `Mil Retail`," +
                        "byid AS `Store #`," +
                        "stname AS `Store Name`," +
                        "shipdate AS `Ship Date`," +
                        "ROUND(SUM(ordunits)) AS `On Order`," +
                        "ROUND(SUM(shpunits)) AS `Shipped`," +
                        "department AS `MMG Dept.`," +
                        "aename AS `Account Executive`" +
                "FROM" +
                    "(SELECT d856.ponumber AS ponumber," +
                            "UPPER(DATE_FORMAT(gh855.ARRIVDATE,'%b-%Y')) AS arrivdate," +
                            "TRIM(gd855.vendornum) AS vendornum," +
                            "TRIM(c.colorcode) AS colorcode," +
                            "TRIM(c.itemcolor) AS colordesc," +
                            "TRIM(d.category) AS category," +
                            "TRIM(IFNULL(c.branddesc,d.deptname)) AS branddesc," +
                            "TRIM(c.scategory) AS scategory," +
                            "TRIM(c.prodclass) AS prodclass," +
                            "TRIM(c.sprodclass) AS sprodclass," +
                            "TRIM(c.itemdesc) AS itemdesc," +
                            "ROUND(gd855.unitprice * SUM(d856.shpunits),2) AS unitprice," +
                            "ROUND(gd855.chgprice * SUM(d856.shpunits),2) AS chgprice," +
                            "ROUND(gd855.retailprc * SUM(d856.shpunits),2) AS retailprc," +
                            "d856.byid AS byid," +
                            "s.stname AS stname," +
                            "h856.shipdate AS shipdate," +
                            "d856.ordunits AS ordunits," +
                            "ROUND(SUM(d856.shpunits)) AS shpunits," +
                            "gh855.department AS department," +
                            "d.aename AS aename" +
                    //" FROM (SELECT uniquekey,shipdate,bolnumber FROM ecgb.head856 WHERE partner in ({0}) AND shipdate>='{1}' AND shipdate<='{2}') AS h856" +
                    //" JOIN ecgb.detl856 AS d856 ON h856.uniquekey=d856.uniquekey" +
                    " FROM (SELECT uniquekey,shipdate,bolnumber FROM mrsk3pl.head856 WHERE partner IN {0} AND shipdate>='{1}' AND shipdate<='{2}') AS h856" +
                    " JOIN mrsk3pl.detl856 AS d856 ON h856.uniquekey=d856.uniquekey and !isnull(d856.invoiceno)" +
                    GetJoinThla(user.ActivePartner) +
                    " JOIN ecgb.ghead855 AS gh855 ON gh855.custorder=d856.ponumber" +
                    " LEFT JOIN temp.xdept AS d ON d.department=gh855.department" +
                    " LEFT JOIN ecgb.gdetl855 AS gd855 ON gd855.uniquekey=gh855.uniquekey" +
                    " AND gd855.upcnum=d856.upcnum" +
                    //" AND gd855.vendornum=d856.vendornum" + /* UPDATE FROM NEIL: We don't need to match Vendornum here since we're using ponumber + upcnum already */
                    " LEFT JOIN ecgb.catinfo AS c ON c.ponumber = gh855.ponumber" +
                    " AND c.upcnum=gd855.upcnum" +
                    " AND c.vendornum=gd855.vendornum" +
                    " LEFT JOIN temp.xstore AS s ON {3}" +
                    " WHERE " + GetConditionThla(user.ActivePartner) + " AND {4}" +
                    " GROUP BY d856.ponumber,d856.vendornum, d856.upcnum,d856.byid)" +
                " AS st GROUP BY ponumber,vendornum,byid;";
                LocPrtSpecific prtInfo = GetPartnerProperties(sPartner);

                string conditionDetail = GetConditionDetail(prtInfo, reportDetails, user.ActivePartner);

                string formatReport = string.Format(reportTemplate, GetPartnerQueryStr(user).ToSqlValueList(), reportDetails.StartDate.ToMySQLDateStr(), reportDetails.EndDate.ToMySQLDateStr(), prtInfo.StoreJoin, conditionDetail);
                //string formatReport = string.Format(reportTemplate, user.ActivePartner.SQLEscape(), reportDetails.StartDate.ToMySQLDateStr(), reportDetails.EndDate.ToMySQLDateStr(), prtInfo.StoreJoin, conditionDetail);

                var header = new[] { "Mil PO", "DC Month", "PID Style", "Color Code", "Color Description", "Category", "Brand Name", "Sub Category", "Class", "Sub Class", "Description", "GLC", "Mil Cost", "Mil Retail", "Store #", "Store Name", "Ship Date", "On Order", "Shipped", "MMG Dept.", "Account Executive" };

                int colCount = 0;
                int rowCount = 0;
                object[,] reportData = null;

                DBConnect connection = ConnectionsMgr.GetSharedConnection(user, _Database.ECGB);
                if (!CreateTempStoreTable(user, connection, reportDetails))
                {
                    return "";
                }
                if (!CreateTempDeptAETable(user, connection))
                {
                    return "";
                }

                using (var resultReport = connection.Query(formatReport))
                {
                    colCount = resultReport.FieldCount;
                    rowCount = resultReport.AffectedRows;
                    reportData = CopyResultDataToArray(resultReport);
                }

                return SaveReportToExcelFile(user, header, reportData, rowCount, colCount, new[] { "L:L", "M:M", "N:N" });
            }
            catch (Exception e)
            {
                Log(user, nameof(GenLocationReportGeneral), e);
                return "";
            }
        }

        private static string GenLocationReportThlaNavrMrnr(User user, LocationReportDetails reportDetails)
        {
            try
            {
                string dbSales = "sales_" + user.Customer;
                string sCustomer = user.Customer.SQLEscape();
                string sPartner = user.ActivePartner.SQLEscape();
                string sStartDate = reportDetails.StartDate.ToMySQLDateStr();
                string sEndDate = reportDetails.EndDate.ToMySQLDateStr();
                string storePartner = GetStorePartner(sPartner);

                string condH856 = $"{Customer}='{sCustomer}' AND {Partner}='{sPartner}' AND {ShipmentDate}>='{sStartDate}' AND {ShipmentDate}<='{sEndDate}'";

                var itemList = GetLocReportData(user, sPartner, condH856);

                var storeLookup = new StLookupDict();
                var itemLookup = new Dictionary<string, ItemInfo>();

                if (itemList.Count > 0)
                {
                    storeLookup = GetStoreInfoLookup(user, storePartner, itemList.Select(x => x.StoreLookup));
                    var vendList = sPartner == _Partner.Thalia ?
                         itemList.Select(x => x.PidStyle.Length > 11 ? x.PidStyle.Substring(0, 11) : x.PidStyle) :
                         itemList.Select(x => x.PidStyle);
                    itemLookup = GetItemInfoLookup(user, sPartner, dbSales, vendList, itemList.Select(x => x.Upc));
                }

                foreach (var item in itemList)
                {
                    item.SetStoreName(sPartner, storeLookup);
                    item.SetItemInfo(itemLookup);
                }

                {
                    HashSet<string> snSet = new HashSet<string>(reportDetails.Stores?.Distinct() ?? new List<string>());
                    HashSet<string> bnSet = new HashSet<string>(reportDetails.BrandNames?.Distinct() ?? new List<string>());
                    itemList = itemList.Where(x =>
                    (reportDetails.POQuery != "" ? x.MilPo == reportDetails.POQuery : true) &&
                    (snSet.Count > 0 ? snSet.Contains(x.StoreLookup) : true) &&
                    (bnSet.Count > 0 ? bnSet.Contains(x.BrandName) : true)
                    ).ToList();
                }

                var header = new[] { "Mil PO", "PID Style", "UPC", "Color Code", "Color Description", "Category", "Brand Name", "Sub Category", "Class", "Sub Class", "Description", "Mil Cost", "Total Mil Cost", "Mil Retail", "Total Mil Retail", "Store #", "Store Name", "Ship Date", "On Order", "Shipped" };

                var reportData = CopyItemInfoToArray(itemList);

                int colCount = header.Length;
                int rowCount = itemList.Count;

                return SaveReportToExcelFile(user, header, reportData, rowCount, colCount, new[] { "L:L", "M:M", "N:N", "O:O" });
            }
            catch (Exception e)
            {
                Log(user, nameof(GenLocationReportThlaNavrMrnr), e);
                return "";
            }
        }

        private static List<LocReportLine> GetLocReportData(User user, string sPartner, string condH856)
        {
            List<LocReportLine> retList = new List<LocReportLine>();
            string queryCatalog = "SELECT st.ponumber AS milpo," +
                    "st.vendornum AS pidstyle," +
                    "st.upcnum AS upc," +
                    "st.milcost AS milcost," +
                    "cast(SUM(st.ttlmilcost) AS decimal(14,2)) AS ttlmilcost," +
                    "st.retail AS retail," +
                    "cast(SUM(st.ttlretailprc) AS decimal(14,2)) AS ttlretail," +
                    "st.storenum AS store," +
                    "st.shipdate AS shipdate," +
                    "round(SUM(st.onorder)) AS onorder," +
                    "round(SUM(st.shipped)) AS shipped " +
            "FROM" +
                "(SELECT d856.ponumber AS ponumber," +
                        "d856.vendornum AS vendornum," +
                        "d856.upcnum AS upcnum," +
                        "s850.unitprice AS milcost," +
                        "s850.unitprice*IFNULL(c856.ctnqty,d856.shpunits) AS ttlmilcost," +
                        "s850.retailprc AS retail," +
                        "s850.retailprc*IFNULL(c856.ctnqty,d856.shpunits) AS ttlretailprc," +
                        "IFNULL(c856.byid,d856.byid) AS storenum," +
                        "h856.shipdate AS shipdate," +
                        "IFNULL(b850.ctnqty,d856.ordunits) AS onorder," +
                        "IFNULL(c856.ctnqty,d856.shpunits) AS shipped " +
                "FROM" +
                    "(SELECT uniquekey," +
                            "shipdate " +
                    "FROM trxh856 " +
                    "WHERE " + condH856 +
                    ") AS h856" +
                " JOIN trxd856 d856 USING (uniquekey)" +
                " LEFT JOIN trxc856 c856 USING (uniquekey," +
                                                "uniqueitem)" +
                " LEFT JOIN srcd850 s850 ON d856.uniquepo=s850.uniquekey" +
                " AND d856.uniqpoitem=s850.uniqueitem" +
                " LEFT JOIN srcb850 b850 ON s850.uniquekey=b850.uniquekey" +
                " AND b850.uniqueitem=s850.uniqueitem" +
                " AND b850.byid=c856.byid ) AS st" +
            " GROUP BY ponumber," +
                        "vendornum," +
                        "upcnum," +
                        "store," +
                        "shipdate";
            try
            {
                DBConnect connection = ConnectionsMgr.GetOCConnection(user, _Database.ECGB);
                using (var queryRpt = connection.Query(queryCatalog))
                {
                    while (queryRpt.Read())
                    {
                        retList.Add(new LocReportLine(queryRpt, sPartner));
                    }
                }
                connection.Close();
            }
            catch (Exception e)
            {
                Log(user, nameof(GetLocReportData), e);
            }
            return retList;
        }

        private static StLookupDict GetStoreInfoLookup(User user, string storePartner, IEnumerable<string> storeList)
        {
            StLookupDict stLookup = new StLookupDict();
            string compList = storeList.ToSqlValueList();
            try
            {
                DBConnect connection = ConnectionsMgr.GetSharedConnection(user);
                using (var querySt = connection.Select(new[] { BYId, XrefId, STName }, _Table.Stinfo, $"WHERE {Partner}='{storePartner}' AND ({BYId} IN {compList} OR TRIM({XrefId}) IN {compList})"))
                {
                    while (querySt.Read())
                    {
                        string xref = querySt.FieldByName(XrefId);
                        string byid = querySt.FieldByName(BYId);
                        string stname = querySt.FieldByName(STName);
                        if (!stLookup.byidDict.ContainsKey(byid))
                        {
                            stLookup.byidDict.Add(byid, stname);
                        }
                        if (!stLookup.xrefDict.ContainsKey(xref))
                        {
                            stLookup.xrefDict.Add(xref, stname);
                        }
                    }
                }
                connection.Close();
            }
            catch (Exception e)
            {
                Log(user, nameof(GetStoreInfoLookup), e);
            }
            return stLookup;
        }

        private static Dictionary<string, ItemInfo> GetItemInfoLookup(User user, string sPartner, string dbSales, IEnumerable<string> vendList, IEnumerable<string> upcList)
        {
            Dictionary<string, ItemInfo> itemDict = new Dictionary<string, ItemInfo>();
            try
            {
                DBConnect connection = ConnectionsMgr.GetSalesConnection(user, dbSales);
                string colVendNum = sPartner == _Partner.Thalia ?
                    $"SUBSTR(TRIM({VendorNum}),1,11)" :
                    $"TRIM({VendorNum})";
                using (var queryItemInfo = connection.Select("vendornum,upcnum,prodcat,deptname,prodsubcat,classdesc,subcdesc,itemdesc,colorcode,itemcolor", _Table.Sl_MasterCat, $"WHERE {colVendNum} IN {vendList.ToSqlValueList()} AND TRIM({UPCNum}) IN {upcList.ToSqlValueList()} group by vendornum,upcnum"))
                {
                    while (queryItemInfo.Read())
                    {
                        string key = queryItemInfo.FieldByName(VendorNum).Trim() + queryItemInfo.FieldByName(UPCNum).Trim();
                        if (!itemDict.ContainsKey(key))
                        {
                            itemDict.Add(key, new ItemInfo(queryItemInfo));
                        }
                    }
                }
                connection.Close();
            }
            catch (Exception e)
            {
                Log(user, nameof(GetItemInfoLookup), e);
            }
            return itemDict;
        }

        private static object[,] CopyItemInfoToArray(List<LocReportLine> itemList)
        {
            int colCount = 20;
            int rowCount = itemList.Count;
            object[,] reportData = new object[rowCount, colCount];

            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                reportData[rowIndex, 0] = itemList[rowIndex].MilPo;
                reportData[rowIndex, 1] = itemList[rowIndex].PidStyle;
                reportData[rowIndex, 2] = itemList[rowIndex].Upc;
                reportData[rowIndex, 3] = itemList[rowIndex].ColorCode;
                reportData[rowIndex, 4] = itemList[rowIndex].ColorDesc;
                reportData[rowIndex, 5] = itemList[rowIndex].Category;
                reportData[rowIndex, 6] = itemList[rowIndex].BrandName;
                reportData[rowIndex, 7] = itemList[rowIndex].SubCategory;
                reportData[rowIndex, 8] = itemList[rowIndex].Class;
                reportData[rowIndex, 9] = itemList[rowIndex].SubClass;
                reportData[rowIndex, 10] = itemList[rowIndex].Description;
                reportData[rowIndex, 11] = itemList[rowIndex].MilCost;
                reportData[rowIndex, 12] = itemList[rowIndex].TtlMilCost;
                reportData[rowIndex, 13] = itemList[rowIndex].Retail;
                reportData[rowIndex, 14] = itemList[rowIndex].TtlRetail;
                reportData[rowIndex, 15] = itemList[rowIndex].Store;
                reportData[rowIndex, 16] = itemList[rowIndex].StoreName;
                reportData[rowIndex, 17] = itemList[rowIndex].ShipDate;
                reportData[rowIndex, 18] = itemList[rowIndex].OnOrder;
                reportData[rowIndex, 19] = itemList[rowIndex].Shipped;
            }

            return reportData;
        }

        private static string GenASNReportGeneral(User user, LocationReportDetails reportDetails)
        {
            try
            {
                string sPartner = user.ActivePartner.SQLEscape();
                string reportTemplate = "SELECT d856.ponumber AS `Mil PO`," +
                       "gh855.custodate AS `Mil PO Date`," +
                       "UPPER(DATE_FORMAT(gh855.ARRIVDATE,'%b-%Y')) AS `DC Month`," +
                       "d856.shipnum AS `ASN #`," +
                       "TRIM(gd855.vendornum) AS `PID Style`," +
                       "TRIM(d856.upcnum) AS `UPC #`," +
                       "TRIM(c.colorcode) AS `Color Code`," +
                       "TRIM(c.itemcolor) AS `Color Decription`," +
                       "d856.cartid AS `Carton ID`," +
                       "TRIM(d.category) AS `Category`," +
                       "TRIM(IFNULL(c.branddesc,d.deptname)) AS `Brand Name`," +
                       "TRIM(c.scategory) AS `Sub Category`," +
                       "TRIM(c.prodclass) AS `Class`," +
                       "TRIM(c.sprodclass) AS `Sub Class`," +
                       "TRIM(c.itemdesc) AS `Description`," +
                       "ROUND(gd855.unitprice * SUM(d856.shpunits),2) AS `GLC`," +
                       "ROUND(gd855.chgprice * SUM(d856.shpunits),2) AS `Mil Cost`," +
                       "ROUND(gd855.retailprc * SUM(d856.shpunits),2) AS `Mil Retail`," +
                       "gh855.fobtype AS `East/West`," +
                       "d856.byid AS `Store #`," +
                       "s.stname AS `Store Name`," +
                       "h856.shipdate AS `Ship Date`," +
                       "ROUND(SUM(d856.ordunits)) AS `On Order`," +
                       "ROUND(SUM(d856.shpunits)) AS `Shipped`," +
                       "gh855.department AS `MMG Dept.`," +
                       "{0} AS `Buyer Name`," +
                       "d.aename AS `Account Executive`" +
                //" FROM(SELECT uniquekey,shipdate,bolnumber FROM ecgb.head856 WHERE partner='{1}' AND shipdate>='{2}' AND shipdate<='{3}') AS h856" +
                //" JOIN ecgb.detl856 AS d856 ON h856.uniquekey=d856.uniquekey" +
                " FROM(SELECT uniquekey,shipdate,bolnumber FROM mrsk3pl.head856 WHERE partner IN {1} AND shipdate>='{2}' AND shipdate<='{3}') AS h856" +
                " JOIN mrsk3pl.detl856 AS d856 ON h856.uniquekey=d856.uniquekey and !isnull(d856.invoiceno)" +
                GetJoinThla(user.ActivePartner) +
                " JOIN ecgb.ghead855 AS gh855 ON gh855.custorder=d856.ponumber" +
                " LEFT JOIN temp.xdept AS d ON d.department=gh855.department" +
                " LEFT JOIN ecgb.gdetl855 AS gd855 ON gd855.uniquekey=gh855.uniquekey" +
                " AND gd855.upcnum=d856.upcnum" +
                //" AND gd855.vendornum=d856.vendornum" + /* UPDATE FROM NEIL: We don't need to match Vendornum here since we're using ponumber + upcnum already */
                " LEFT JOIN ecgb.catinfo AS c ON c.ponumber = gh855.ponumber" +
                " AND c.upcnum=gd855.upcnum" +
                " AND c.vendornum=gd855.vendornum" +
                " LEFT JOIN temp.xstore AS s ON {4}" +
                " WHERE " + GetConditionThla(user.ActivePartner) + " AND {5}" +
                " GROUP BY h856.shipdate,d856.shipnum,d856.upcnum,d856.cartid,d856.ponumber,d856.podate,gd855.vendornum,gd855.itemdesc,d856.byid,d.aename,{0},gh855.department;";
                LocPrtSpecific prtInfo = GetPartnerProperties(sPartner);

                string conditionDetail = GetConditionDetail(prtInfo, reportDetails, user.ActivePartner);

                string formatReport = string.Format(reportTemplate, prtInfo.BuyerName, GetPartnerQueryStr(user).ToSqlValueList(), reportDetails.StartDate.ToString("yyyy-MM-dd"), reportDetails.EndDate.ToString("yyyy-MM-dd"), prtInfo.StoreJoin, conditionDetail);
                //string formatReport = string.Format(reportTemplate, prtInfo.BuyerName, user.ActivePartner.SQLEscape(), reportDetails.StartDate.ToString("yyyy-MM-dd"), reportDetails.EndDate.ToString("yyyy-MM-dd"), prtInfo.StoreJoin, conditionDetail);

                var header = new[] { "Mil PO", "Mil PO Date", "DC Month", "ASN #", "PID Style", "UPC #", "Color Code", "Color Description", "Carton ID", "Category", "Brand Name", "Sub Category", "Class", "Sub Class", "Description", "GLC", "Mil Cost", "Mil Retail", "East/West", "Store #", "Store Name", "Ship Date", "On Order", "Shipped", "MMG Dept.", "Buyer Name", "Account Executive" };

                int colCount = 0;
                int rowCount = 0;
                object[,] reportData = null;

                DBConnect connection = ConnectionsMgr.GetSharedConnection(user, _Database.ECGB);
                if (!CreateTempStoreTable(user, connection, reportDetails))
                {
                    return "";
                }
                if (!CreateTempDeptTable(user, connection))
                {
                    return "";
                }

                using (var resultReport = connection.Query(formatReport))
                {
                    colCount = resultReport.FieldCount;
                    rowCount = resultReport.AffectedRows;
                    reportData = CopyResultDataToArray(resultReport);
                }

                return SaveReportToExcelFile(user, header, reportData, rowCount, colCount, new[] { "P:P", "Q:Q", "R:R" });
            }
            catch (Exception e)
            {
                Log(user, nameof(GenASNReportGeneral), e);
                return "";
            }
        }

        private static List<string> GetPartnerQueryStr(User user)
        {
            string activePrt = user.ActivePartner;
            List<string> prts = new List<string>();
            switch (activePrt)
            {
                case _Partner.Navy: { prts.Add("MRSN"); break; }
                case _Partner.AAFES: { prts.Add("MRSA"); break; }
                case _Partner.Marines: { prts.Add("MRSM"); prts.Add("MRSL"); prts.Add("MRSS"); break; }
                default: { prts.Add(activePrt); break; }
            }

            return prts;
        }

        private static LocPrtSpecific GetPartnerProperties(string sPartner)
        {
            LocPrtSpecific p = new LocPrtSpecific();
            p.StoreWhere = "s.byid";
            switch (sPartner)
            {
                case _Partner.AAFES:
                    p.BuyerName = "d.aafesbuyer";
                    p.StoreJoin = "d856.byid=" + p.StoreWhere;
                    break;

                case _Partner.Navy:
                    p.BuyerName = "d.nexbuyer";
                    p.StoreJoin = "substr(lpad(replace(replace(d856.byid,'E',''),'W',''),4,'0'),2,3)=" + p.StoreWhere;
                    break;

                case _Partner.Marines:
                    p.BuyerName = "d.mcxbuyer";
                    p.StoreJoin = "if(LENGTH(trim(d856.byid))>4,s.byid=d856.byid, trim(s.xrefid)=substr(trim(LEADING '0' from d856.byid),1,3))"; //WE ARE EXLUDING 4 CHAR STORES THAT DON'T END IN E/W
                    break;

                default:
                    p.BuyerName = "d.mcxbuyer";
                    p.StoreJoin = "d856.byid=" + p.StoreWhere;
                    break;
            }
            return p;
        }

        private static string SaveReportToExcelFile(User user, string[] columnHeaders, object[,] reportData, int rowCount, int colCount, string[] currencyCols)
        {
            try
            {
                Application app = new Application();
                app.AutomationSecurity = MsoAutomationSecurity.msoAutomationSecurityForceDisable;
                Workbook xlWB = app.Workbooks.Add();
                Worksheet ws = xlWB.ActiveSheet;
                app.ActiveWindow.SplitRow = 1;
                app.ActiveWindow.FreezePanes = true;

                char colLastLetter = (char)('A' + colCount - 1);
                string rowLastIndex = (2 + rowCount - 1).ToString();

                var rangeHead = ws.Range["A1", colLastLetter + "1"];
                var rangeBody = ws.Range["A2", colLastLetter + rowLastIndex];
                rangeBody.NumberFormat = "@";

                rangeHead.Value = columnHeaders;
                rangeBody.Value = reportData;

                foreach (var col in currencyCols)
                {
                    ws.Range[col].NumberFormat = "$#,##0.00";
                }
                ws.Columns.AutoFit();

                string diFileName = SiteFileSystem.GetTempFileName();
                xlWB.SaveCopyAs(diFileName);
                xlWB.Close(false);
                app.Quit();
                Marshal.ReleaseComObject(ws);
                Marshal.ReleaseComObject(xlWB);
                Marshal.ReleaseComObject(app);
                string eoToken = Crypt.EncryptFileToFile(user, diFileName);
                File.Delete(diFileName);
                return eoToken;
            }
            catch (Exception e)
            {
                Log(user, nameof(SaveReportToExcelFile), e);
                return "";
            }
        }

        private static string GetConditionDetail(LocPrtSpecific prtInfo, LocationReportDetails reportDetails, string partner)
        {
            string conditionDetail = "1";

            if (!string.IsNullOrWhiteSpace(reportDetails.BOLQuery))
            {
                if (partner == _Partner.Anastasia)
                {
                    conditionDetail += string.Format(" AND h810.bolnumber='{0}'", reportDetails.BOLQuery.Trim().SQLEscape());
                }
                else
                {
                    conditionDetail += string.Format(" AND h856.bolnumber='{0}'", reportDetails.BOLQuery.Trim().SQLEscape());
                }
            }

            if (!string.IsNullOrWhiteSpace(reportDetails.POQuery))
            {
                if (partner == _Partner.Anastasia)
                {
                    conditionDetail += string.Format(" AND h810.ponumber='{0}'", reportDetails.POQuery.Trim().SQLEscape());
                }
                else
                {
                    conditionDetail += string.Format(" AND d856.ponumber='{0}'", reportDetails.POQuery.Trim().SQLEscape());
                }
            }

            if (reportDetails.Stores.Count > 0)
            {
                conditionDetail += string.Format(" AND {0} IN {1}", prtInfo.StoreWhere, reportDetails.Stores.ToSqlValueList());
            }

            if (partner != _Partner.Anastasia && reportDetails.BrandNames.Count > 0)
            {
                conditionDetail += string.Format(" AND IFNULL(c.branddesc,d.deptname) IN {0}", reportDetails.BrandNames.ToSqlValueList());
            }

            if (partner != _Partner.Anastasia && reportDetails.AENames.Count > 0)
            {
                conditionDetail += string.Format(" AND d.aename IN {0}", reportDetails.AENames.ToSqlValueList());
            }

            return conditionDetail;
        }

        private static string GetJoinThla(string activePartner)
        {
            return activePartner == _Partner.Thalia ? "JOIN mrsk3pl.head850 AS h850 ON h850.ponumber=d856.ponumber " : "";
        }

        private static string GetConditionThla(string activePartner)
        {
            return activePartner == _Partner.Thalia ? "h850.vendid IN ('53750510','53750509')" : "1";
        }

        private static object[,] CopyResultDataToArray(DBResult resultReport)
        {
            int colCount = resultReport.FieldCount;
            int rowCount = resultReport.AffectedRows;
            object[,] reportData = new object[rowCount, colCount];

            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                resultReport.Read();
                for (int columnIndex = 0; columnIndex < colCount; columnIndex++)
                {
                    reportData[rowIndex, columnIndex] = resultReport.Field2(columnIndex, "'---");
                }
            }

            return reportData;
        }

        private static string GetStorePartner(string sPartner)
        {
            switch (sPartner)
            {
                case _Partner.Thalia:
                    return _Partner.AAFES;

                case _Partner.Anastasia:
                case _Partner.NavyReplenishment:
                    return _Partner.Navy;

                case _Partner.MarinesReplenishment:
                    return _Partner.Marines;

                default:
                    return sPartner;
            }
        }

        private static bool CreateTempDeptAETable(User user, DBConnect connection)
        {
            try
            {
                connection.DropTable("temp.xdept", true);
                List<DBField> fieldList = new List<DBField>()
                {
                    new DBField("department", DBField.FieldTypes.VARCHAR, "20"),
                    new DBField("deptname", DBField.FieldTypes.VARCHAR, "60", true),
                    new DBField("category", DBField.FieldTypes.VARCHAR, "30", true),
                    new DBField("aename", DBField.FieldTypes.VARCHAR, "20", true)
                };
                connection.CreateTemp("temp.xdept", fieldList, "department");
                connection.Query("INSERT INTO temp.xdept SELECT department,deptname,category,aename FROM home.deptdesc");
                return true;
            }
            catch (Exception e)
            {
                Log(user, nameof(CreateTempDeptAETable), e);
                return false;
            }
        }

        private static bool CreateTempDeptTable(User user, DBConnect connection)
        {
            try
            {
                connection.DropTable("temp.xdept", true);
                List<DBField> fieldList = new List<DBField>()
                {
                    new DBField("department", DBField.FieldTypes.VARCHAR, "20"),
                    new DBField("deptname", DBField.FieldTypes.VARCHAR, "60", true),
                    new DBField("category", DBField.FieldTypes.VARCHAR, "30", true),
                    new DBField("aename", DBField.FieldTypes.VARCHAR, "20", true)
                };
                string colBuyer = "";
                switch (user.ActivePartner.SQLEscape())
                {
                    default:
                    case _Partner.Marines:
                        colBuyer = "mcxbuyer";
                        break;

                    case _Partner.AAFES:
                        colBuyer = "aafesbuyer";
                        break;

                    case _Partner.Navy:
                        colBuyer = "nexbuyer";
                        break;
                }
                fieldList.Add(new DBField(colBuyer, DBField.FieldTypes.VARCHAR, "20"));
                string queryInsert = string.Format("INSERT INTO temp.xdept SELECT department,deptname,category,aename,{0} FROM home.deptdesc", colBuyer);
                connection.CreateTemp("temp.xdept", fieldList, "department");
                connection.Query(queryInsert);
                return true;
            }
            catch (Exception e)
            {
                Log(user, nameof(CreateTempDeptTable), e);
                return false;
            }
        }

        private static bool CreateTempStoreTable(User user, DBConnect connection, LocationReportDetails reportDetails)
        {
            try
            {
                connection.DropTable("temp.xstore", true);
                List<DBField> fieldList = new List<DBField>()
                {
                    new DBField("byid", DBField.FieldTypes.VARCHAR, "20", false),
                    new DBField("stname", DBField.FieldTypes.VARCHAR, "35", false)
                };
                string extra = "";
                if (user.ActivePartner.SQLEscape() == _Partner.Marines)
                {
                    fieldList.Add(new DBField("xrefid", DBField.FieldTypes.VARCHAR, "20", false));
                    extra += ",xrefid";
                }

                string condStore = "";
                if (reportDetails.Stores.Count > 0)
                {
                    condStore = string.Format("AND byid IN {0}", reportDetails.Stores.ToSqlValueList());
                }

                string insertQuery = string.Format("INSERT INTO temp.xstore SELECT byid,stname{0} FROM home.stinfo WHERE partner='{1}' {2} GROUP BY byid", extra, GetStorePartner(user.ActivePartner.SQLEscape()), condStore);
                connection.CreateTemp("temp.xstore", fieldList, "byid");
                connection.Query(insertQuery);
                return true;
            }
            catch (Exception e)
            {
                Log(user, nameof(CreateTempStoreTable), e);
                return false;
            }
        }

        private static bool CreateTempStoreTableFromData(User user, DBConnect connection, List<List<string>> storeNameData)
        {
            try
            {
                connection.DropTable("temp.xstore", true);
                List<DBField> fieldList = new List<DBField>()
                {
                    new DBField("byid", DBField.FieldTypes.VARCHAR, "20", false),
                    new DBField("stname", DBField.FieldTypes.VARCHAR, "35", false)
                };

                connection.CreateTemp("temp.xstore", fieldList, "byid");
                connection.InsertMulti("temp.xstore", "byid,stname", storeNameData);
                return true;
            }
            catch (Exception e)
            {
                Log(user, nameof(CreateTempStoreTableFromData), e);
                return false;
            }
        }

        #endregion Generating reports

        private static void Log(User user, string method, Exception e) => ProgramLog.LogError(user, nameof(LocationReportHandler), method, e.Message);
    }
}