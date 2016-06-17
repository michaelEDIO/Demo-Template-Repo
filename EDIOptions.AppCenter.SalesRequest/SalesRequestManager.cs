using EDIOptions.AppCenter.Database;
using EDIOptions.AppCenter.Session;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EDIOptions.AppCenter.SalesRequest
{
    public static class SalesRequestManager
    {
        private const string tableStores = "smaster";

        private const string tableSales = "vmisales";

        private const string columnRetailWeek = "retailweek";
        private const string columnRetailWeekStart = "retailweekstart";
        private const string columnRetailWeekEnd = "retailweekend";
        private const string columnRetailYear = "retailyear";

        public static List<SalesRequestDetail> GetPastOrders(User user)
        {
            List<SalesRequestDetail> pastRequests = new List<SalesRequestDetail>();
            try
            {
                var connection = ConnectionsMgr.GetAdminConnection();
                {
                    var request = connection.Select(new[] { _Column.RequestDate, _Column.OutputName, _Column.ToEmail, _Column.Processed }, _Table.SalesRequest, string.Format("WHERE {0}='{1}' AND {2}='{3}' ORDER BY reqdate DESC LIMIT 10", _Column.Customer, user.Customer.SQLEscape(), _Column.ConnectID, user.OCConnID.SQLEscape()));

                    while (request.Read())
                    {
                        pastRequests.Add(new SalesRequestDetail(
                            ((DateTime)request.Field2(0)).ToString("MMM dd, yyyy"),
                            request.Field(1, ""),
                            request.Field(2, ""),
                            request.Field(3, "").ToUpper() == "Y" ? "Processed" : "Unprocessed"
                        ));
                    }
                }
                connection.Close();
                return pastRequests;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, "SalesRequestManager", "GetPastOrders", e.Message);
                return new List<SalesRequestDetail>();
            }
        }

        public static List<string> GetStoreList(User user)
        {
            List<string> storeList = new List<string>();
            try
            {
                var connection = ConnectionsMgr.GetSharedConnection(user, _Database.ESIC);
                {
                    var queryStores = connection.Select(_Column.STId, tableStores, string.Format("WHERE {0}='{1}'", _Column.Partner, user.ActivePartner.SQLEscape()));

                    while (queryStores.Read())
                    {
                        storeList.Add(queryStores.Field(0).TrimEnd());
                    }
                }
                connection.Close();
                return storeList;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, "SalesRequestManager", "GetStoreList", e.Message);
                return new List<string>();
            }
        }

        public static List<RetailWeekData> GetRetailWeeks(User user)
        {
            string dateLatest = "";
            List<RetailWeekData> retailWeekList = new List<RetailWeekData>();
            try
            {
                var connectionSalesData = ConnectionsMgr.GetSharedConnection(user, _Database.ESIC);
                {
                    var querySalesDataLatest = connectionSalesData.Select("startdate", tableSales, string.Format("WHERE {0}='{1}' ORDER BY {2} DESC LIMIT 1", _Column.Partner, user.ActivePartner.SQLEscape(), "startdate"));

                    if (querySalesDataLatest.Read())
                    {
                        DateTime temp;
                        if (!DateTime.TryParse(querySalesDataLatest.Field(0), out temp))
                        {
                            dateLatest = DateTime.Now.ToMySQLDateStr();
                        }
                        else
                        {
                            dateLatest = querySalesDataLatest.Field(0);
                        }
                    }
                    else
                    {
                        dateLatest = DateTime.Now.ToMySQLDateStr();
                    }
                }
                connectionSalesData.Close();

                var connectionAdminData = ConnectionsMgr.GetAdminConnection();
                {
                    var resultQueryWeek = connectionAdminData.Select(new[] { columnRetailWeek, columnRetailYear, columnRetailWeekStart, columnRetailWeekEnd }, _Table.RetailCalendar, string.Format("WHERE {0}<'{1}'", columnRetailWeekStart, dateLatest));

                    while (resultQueryWeek.Read())
                    {
                        var date = resultQueryWeek.Field2(2) as DateTime?;
                        if (date != null && date < DateTime.Now)
                        {
                            RetailWeekData retailWeek = new RetailWeekData();
                            retailWeek.Week = int.Parse(resultQueryWeek.Field(0));
                            retailWeek.Year = int.Parse(resultQueryWeek.Field(1));
                            retailWeek.WeekStart = resultQueryWeek.Field(2);
                            retailWeek.WeekEnd = resultQueryWeek.Field(3);
                            retailWeekList.Add(retailWeek);
                        }
                    }
                }
                connectionAdminData.Close();
                retailWeekList.Sort((weekA, weekB) => weekB.CompareTo(weekA));
                return retailWeekList;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, "SalesRequestManager", "GetRetailWeeks", e.Message);
                return new List<RetailWeekData>();
            }
        }

        public static void SendRequest(User user, ReportRequestData ord)
        {
            string storesConcat = "";
            if (ord.Stores.Count > 0)
            {
                storesConcat = string.Format("STORE IN ({1})", _Column.STId, string.Join(",", from store in ord.Stores select "'" + store.SQLEscape() + "'"));
            }
            string retailConcat = string.Join("; ", from week in ord.Weeks select week.ToSQL());
            var genDate = DateTime.Now;
            try
            {
                var connection = ConnectionsMgr.GetAdminConnection();
                {
                    var vals = new Dictionary<string, string>()
                    {
                        {_Column.UniqueKey, connection.GetNewKey()},
                        {_Column.RequestDate, genDate.ToMySQLDateTimeStr()},
                        {_Column.Customer, user.Customer.SQLEscape()},
                        {_Column.ConnectID, user.OCConnID.SQLEscape()},
                        {_Column.OutputName, string.Format("{0}_{1}_{2}.xlsx", user.Customer.SQLEscape(), 852, genDate.ToString("yyyyMMddHHmmssF"))},
                        {_Column.OutputPath, string.Format(@"\ecgb\data\networks\{0}\outbox", user.OCConnID.SQLEscape())},
                        {_Column.Filter, string.Format("{0}|{1}|{2}|{3}", ord.RequestType, storesConcat, retailConcat, user.ActivePartner.SQLEscape())},
                        {_Column.Processed, _ProgressFlag.Unprocessed},
                        {_Column.ToEmail, ord.Email.SQLEscape()},
                        {_Column.OptCenterUpload, "1"}
                    };
                    connection.Insert(_Table.SalesRequest, vals.ToNameValueCollection());
                }
                connection.Close();
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, "SalesRequestManager", "GetStoreList", e.Message);
            }
        }

        #region Sales Report

        public static List<List<SalesReport>> GenerateSalesReportList(User user, ReportRequestData ord)
        {
            try
            {
                if (ord.Stores.Count == 0)
                {
                    return new List<List<SalesReport>>() { new List<SalesReport>() };
                }
                else
                {
                    List<List<SalesReport>> ret = new List<List<SalesReport>>();
                    for (int i = 0; i < ord.Stores.Count; i++)
                    {
                        ret.Add(_GenSalesReport(user, ord.Stores[i], ord.Weeks));
                    }
                    return ret;
                }
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, "SalesRequestManager", "GenerateSalesReportList", e.Message);
                return new List<List<SalesReport>>();
            }
        }

        private static List<SalesReport> _GenSalesReport(User user, string storeId, List<RetailWeekData> listQueryWeeks)
        {
            string querySelectSalesData = "SELECT ihead.vendornum AS vendornum,ibody.unitcost AS unitcost,ibody.unitprice AS unitprice,SUM(IFNULL(onhand.qavailable,0)) as onhand";
            string querySelectSalesDataTemplate = ",SUM(IFNULL(sales{0}.quantity,0))AS sold{0},SUM(IFNULL(orders{0}.quantity,0))AS order{0}";

            string querySalesDataFormat = " FROM(SELECT uniqitem,unitcost,unitprice FROM vmiitem WHERE partner='{0}')AS ibody" +
                                          " LEFT JOIN(SELECT uniquekey,vendornum FROM imaster)AS ihead ON ihead.uniquekey=ibody.uniqitem";
            string queryOnHandFormat = " LEFT JOIN(SELECT * FROM(SELECT uniqueitem, qavailable, uniquest, updatedt FROM vmioh WHERE partner='{0}' AND uniquest='{1}' ORDER BY enddate DESC) AS onhand0 GROUP BY onhand0.uniqueitem)AS onhand ON ihead.uniquekey=onhand.uniqueitem";
            string queryFromSalesDataTemplate = " LEFT JOIN(SELECT uniqitem,SUM(quantity)AS quantity FROM vmisales WHERE partner='{0}' AND enddate>='{1}' AND enddate<='{2}' {3} GROUP BY uniqitem)AS sales{5} ON sales{5}.uniqitem=ihead.uniquekey" +
                                                " LEFT JOIN(SELECT pd.vendornum AS vendornum,ROUND(SUM(pd.quantity),0)AS quantity FROM(SELECT uniquekey FROM phead850 WHERE partner='{0}' AND podate>='{1}' AND podate<='{2}' {4})AS ph LEFT JOIN pdetl850 AS pd ON ph.uniquekey=pd.uniquekey GROUP BY vendornum)AS orders{5} ON orders{5}.vendornum=ihead.vendornum";
            string queryFromSalesDataEnd = " GROUP BY ihead.vendornum";
            string stidComp = _Column.STId + "='" + storeId.SQLEscape() + "'";
            List<SalesReport> salesReport = new List<SalesReport>();
            try
            {
                var connection = ConnectionsMgr.GetSharedConnection(user, _Database.ESIC);
                {
                    var queryStoreKeys = connection.Select(_Column.UniqueKey, tableStores, "WHERE " + stidComp);
                    string storeUniqueKey = "";
                    if (queryStoreKeys.Read())
                    {
                        storeUniqueKey = queryStoreKeys.Field(0);
                    }
                    string conditionStoreID = "AND " + stidComp;
                    string conditionStoreKey = string.Format("AND {0}='{1}'", _Column.UniqueSt, storeUniqueKey);

                    string queryFromSalesData = string.Format(querySalesDataFormat, user.ActivePartner.SQLEscape());
                    queryFromSalesData += string.Format(queryOnHandFormat, user.ActivePartner.SQLEscape(), storeUniqueKey);

                    for (int i = 0; i < listQueryWeeks.Count; i++)
                    {
                        querySelectSalesData += string.Format(querySelectSalesDataTemplate, i);
                        queryFromSalesData += string.Format(queryFromSalesDataTemplate, user.ActivePartner.SQLEscape(), listQueryWeeks[i].WeekStart, listQueryWeeks[i].WeekEnd, conditionStoreKey, conditionStoreID, i);
                    }

                    string queryCombined = querySelectSalesData + queryFromSalesData + queryFromSalesDataEnd;
                    var result = connection.Query(queryCombined);

                    while (result.Read())
                    {
                        SalesReport sr = new SalesReport();
                        sr.VendorNum = result.Field(0);
                        sr.UnitCost = decimal.Parse(result.Field(1, "0"));
                        sr.UnitPrice = decimal.Parse(result.Field(2, "0"));
                        sr.OnHand = int.Parse(result.Field(3, "0"));
                        sr.Details = new List<SaleData>();
                        for (int i = 0; i < listQueryWeeks.Count; i++)
                        {
                            SaleData sd = new SaleData();
                            sd.RetailWeek = listQueryWeeks[i];
                            sd.Sold = int.Parse(result.Field(4 + (i * 2), "0"));
                            sd.Order = int.Parse(result.Field(5 + (i * 2), "0"));
                            sr.Details.Add(sd);
                        }
                        salesReport.Add(sr);
                    }
                }
                connection.Close();
                return salesReport;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, "SalesRequestManager", "_GenSalesReport", e.Message);
                return new List<SalesReport>();
            }
        }

        #endregion Sales Report

        #region Shipment Report

        public static List<ShipReport> GenerateShipmentReportList(User user, ReportRequestData ord)
        {
            try
            {
                if (ord.Stores.Count == 0)
                {
                    return new List<ShipReport>();
                }
                else
                {
                    List<ShipReport> ret = new List<ShipReport>();
                    foreach (var store in ord.Stores)
                    {
                        ret.Add(_GenShipReport(user, store, ord.Weeks));
                    }
                    return ret;
                }
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, "SalesRequestManager", "GenerateShipmentReportList", e.Message);
                return new List<ShipReport>();
            }
        }

        private static ShipReport _GenShipReport(User user, string store, List<RetailWeekData> retailWeeks)
        {
            ShipReport shipReport = new ShipReport();
            shipReport.Store = store.SQLEscape();
            shipReport.Shipments = new List<ShipReportData>();

            var queryShipInfo = string.Format("SELECT {0},{1},IFNULL({2},'')FROM {3}", _Column.UniqueKey, _Column.ShipmentDate, _Column.BOLNumber, _Table.PHead850) +
                                 string.Format(" WHERE {0}='{1}' AND {2}='{3}' AND({4})", _Column.Partner, user.ActivePartner.SQLEscape(), _Column.STId, shipReport.Store, string.Join("OR", from week in retailWeeks select string.Format("({0}>='{1}' AND {0}<='{2}')", _Column.ShipmentDate, week.WeekStart.SQLEscape(), week.WeekEnd.SQLEscape())));
            var queryQtyInfo = string.Format("SELECT {0},{1},IFNULL({2},0) FROM {3}", _Column.VendorNum, _Column.UPCNum, _Column.Quantity, _Table.PDetl850);
            try
            {
                var connectionSalesData = ConnectionsMgr.GetSharedConnection(user, _Database.ESIC);
                var result = connectionSalesData.Query(queryShipInfo);

                while (result.Read())
                {
                    ShipReportData srd = new ShipReportData();
                    string keyShip = result.Field(0, "");
                    srd.ShipDate = ((DateTime)result.Field2(1)).ToString("MMM dd, yyyy");
                    srd.TrackingNumber = result.Field(2, "").Trim();
                    srd.Quantity = 0;
                    srd.Items = new List<ShipItemInfo>();
                    if (keyShip != "")
                    {
                        var resultQuantity = connectionSalesData.Query(queryQtyInfo + string.Format(" WHERE {0}='{1}'", _Column.UniqueKey, keyShip));
                        while (resultQuantity.Read())
                        {
                            ShipItemInfo itemInfo = new ShipItemInfo();
                            itemInfo.VendorNum = resultQuantity.Field(0);
                            itemInfo.UPCNum = resultQuantity.Field(1);
                            itemInfo.Quantity = (int)decimal.Parse(resultQuantity.Field(2));
                            srd.Items.Add(itemInfo);
                        }
                        if (srd.Items.Count > 0)
                        {
                            srd.Quantity = srd.Items.Sum(item => item.Quantity);
                        }
                    }
                    shipReport.Shipments.Add(srd);
                }
                connectionSalesData.Close();
                return shipReport;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, "SalesRequestManager", "_GenShipReport", e.Message);
                return shipReport;
            }
        }

        #endregion Shipment Report
    }
}