using EDIOptions.AppCenter.Database;
using EDIOptions.AppCenter.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using static EDIOptions.AppCenter._Column;
using static EDIOptions.AppCenter._Database;
using static EDIOptions.AppCenter._Table;

namespace EDIOptions.AppCenter.PoDropShip
{
    public static class PdsManager
    {
        private const int resultPageSize = 15;

        private static string[] searchCols =
        {
            PONumber,
            PoType
        };

        public static PdsOptions GetOptions(User user)
        {
            try
            {
                DBConnect connection = ConnectionsMgr.GetOCConnection(user, Home);
                var opt = _GetOptions(user, connection);
                connection.Close();
                return opt;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, nameof(PdsManager), nameof(GetOptions), e.Message);
                return new PdsOptions();
            }
        }

        public static List<PoItem> GetCurrentPoList(User user, FilterInfo filter)
        {
            List<PoItem> ret = new List<PoItem>();
            try
            {
                var connection = ConnectionsMgr.GetOCConnection(user, ECGB);
                {
                    var recCount = _GetRecordCount(user, connection, filter);
                    if (recCount > 0)
                    {
                        filter.ResultCount = recCount;
                        filter.MaxPage = (int)(Math.Ceiling(recCount / (double)resultPageSize));
                    }

                    using (var queryCurrent = connection.Select($"{_Column.UniqueKey},{_Column.PONumber},{_Column.PODate},{_Column.ShipmentDate},{_Column.CancelDate},potype,round(totcmpinv) totcmpinv,round(totcmpasn) totcmpasn,round({_Column.TotalItems}) {_Column.TotalItems}", SrcH850, $"WHERE POStatus IN (0, 1) AND {filter.ToFetchQueryString(user, searchCols, resultPageSize)}"))
                    {
                        while (queryCurrent.Read())
                        {
                            PoItem po = new PoItem(queryCurrent);
                            ret.Add(po);
                        }
                    }
                }
                connection.Close();
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, nameof(PdsManager), nameof(GetCurrentPoList), e.Message);
            }
            return ret;
        }

        public static List<ReportDesc> GetReportList(User user, List<string> keyList)
        {
            List<ReportDesc> repList = new List<ReportDesc>();
            List<string> polist = new List<string>();
            string sCustomer = user.Customer.SQLEscape();
            string sPartner = user.ActivePartner.SQLEscape();
            string prefix = $"trx/{user.OCConnID}/downloads/";
            if (keyList.Count == 0)
            {
                return repList;
            }
            try
            {
                var connection = ConnectionsMgr.GetOCConnection(user);

                using (var queryPoNum = connection.Select(PONumber, $"{ECGB}.{SrcH850}", $"WHERE {UniqueKey} IN {keyList.ToSqlValueList()}"))
                {
                    while (queryPoNum.Read())
                    {
                        polist.Add(queryPoNum.Field(0));
                    }
                }
                if (polist.Count <= 0)
                {
                    return repList;
                }
                string filter = $"WHERE {Customer}='{sCustomer}' AND {Partner}='{sPartner}' AND {PONumber} IN {polist.ToSqlValueList()}";

                using (var queryReport = connection.Select("*", $"{GrpAdmin}.{Reports}", filter))
                {
                    while (queryReport.Read())
                    {
                        repList.Add(new ReportDesc(queryReport, prefix));
                    }
                }
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, nameof(PdsManager), nameof(GetReportList), e.Message);
            }
            return repList;
        }

        public static List<PoSummary> VerifyPoList(User user, IEnumerable<string> poList)
        {
            List<PoSummary> ret = new List<PoSummary>();
            try
            {
                string sCustomer = user.Customer.SQLEscape();
                string sPartner = user.ActivePartner.SQLEscape();
                var connection = ConnectionsMgr.GetOCConnection(user, ECGB);
                {
                    using (var queryCurrent = connection.Select($"{UniqueKey},{PONumber}", SrcH850, $"WHERE {Customer}='{sCustomer}' AND {Partner}='{sPartner}' AND POStatus IN (0, 1) AND {PONumber} IN {poList.ToSqlValueList()}"))
                    {
                        while (queryCurrent.Read())
                        {
                            ret.Add(new PoSummary(queryCurrent.Field(0), queryCurrent.Field(1)));
                        }
                    }
                }
                connection.Close();
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, nameof(PdsManager), nameof(VerifyPoList), e.Message);
            }
            return ret;
        }

        public static List<string> CreateInvAsn(User user, List<PoSummary> poList)
        {
            string cCustomer = user.Customer.SQLEscape();
            string cPartner = user.ActivePartner.SQLEscape();
            List<string> sentKeys = new List<string>();
            try
            {
                DBConnect connection = ConnectionsMgr.GetOCConnection(user, ECGB);
                var options = _GetOptions(user, connection);
                var trxDict = GetTrxTypeDict(user, connection, poList.Select(x => x.Key));
                string cVendId = GetVendId(user, connection);
                string invkey = null;
                int fileIdx = 0;
                Dictionary<string,string> invKeys = new Dictionary<string, string>();
                List<List<string>> invRpts = new List<List<string>>();
                foreach (var po in poList)
                {
                    string cPoNumber = po.PONumber.SQLEscape();
                    string cInvNumber = po.InvoiceNumber.SQLEscape();
                    string cBolNumber = po.BolNumber.SQLEscape();
                    string cTrxType = trxDict.ContainsKey(po.Key) ? trxDict[po.Key] : "M";

                    if (options.IsInvoiceEnabled && options.IsPackingEnabled)
                    {
                        connection.Query($"CALL ediproc.CreT856FromS850multi('{cPoNumber}','{cBolNumber}','{cCustomer}','{cPartner}','{cTrxType}','0')");
                        using (var q = connection.Query("SELECT @NewKey AS uniquekey"))
                        {
                            if (q.Read())
                            {
                                string cSourceKey = q.Field(0);
                                using (DBResult res = connection.Query($"SELECT ediproc.CreT810FromT856S850multi('{cSourceKey}','{cInvNumber}','{cCustomer}','{cPartner}','{cPoNumber}','')"))
                                {
                                    if (res.Read())
                                    {
                                        invkey = res.Field(0);
                                        invKeys.Add(cInvNumber, invkey);
                                        invRpts.Add(_CreateReportReq(user, DateTime.Now, invkey, "810", "trx810p.rpt", $"{ cCustomer}_810_{DateTime.Now.ToString("yyyyMMddHHmmss")}{++fileIdx}-report.pdf", "Invoice Report", cCustomer, cPartner, cPoNumber));
                                        connection.Query($"call ediproc.UpdPOInvoiced('{cPoNumber}','{cCustomer}','{cPartner}','UPCNUM', 0)");
                                    }
                                }
                                sentKeys.Add(cSourceKey);
                            }
                        }
                    }
                    else if (options.IsInvoiceEnabled)
                    {
                        using (DBResult res = connection.Query($"SELECT ediproc.CreT810FromS850('{cPoNumber}','{cInvNumber}','{cVendId}','{cCustomer}','{cPartner}')"))
                        {
                            if (res.Read())
                            {
                                invkey = res.Field(0);
                                invKeys.Add(cInvNumber, invkey);
                                invRpts.Add(_CreateReportReq(user, DateTime.Now, invkey, "810", "trx810p.rpt", $"{ cCustomer}_810_{DateTime.Now.ToString("yyyyMMddHHmmss")}{++fileIdx}-report.pdf", "Invoice Report", cCustomer, cPartner, cPoNumber));
                                connection.Query($"call ediproc.UpdPOInvoiced('{cPoNumber}','{cCustomer}','{cPartner}','UPCNUM', 0)");
                            }
                        }
                    }
                    else if (options.IsPackingEnabled)
                    {
                        connection.Query($"CALL ediproc.CreT856FromS850multi('{cPoNumber}','{cBolNumber}','{cCustomer}','{cPartner}','{cTrxType}','0')");
                        using (var q = connection.Query("SELECT @NewKey AS uniquekey"))
                        {
                            if (q.Read())
                            {
                                sentKeys.Add(q.Field(0));
                            }
                        }
                    }
                }
                if(sentKeys.Count() > 0)
                {
                    //UPDATE SHIPDATE TO CURRENT DATE (STORED PROCEDURE LEAVES IT NULL ON PURPOSE)
                    //ALSO UPDATE SHIP WEIGHT AND DELIVDATE
                    connection.Query($"UPDATE TRXH856 SET SHIPDATE='{DateTime.Now.ToMySQLDateStr()}', DELIVDATE='{DateTime.Now.AddDays(2).ToMySQLDateStr()}', SHIPWEIGHT=1 WHERE UNIQUEKEY IN ('{string.Join("','",sentKeys)}')");
                }
                if(invKeys.Count() > 0)
                {
                    connection.Query($"UPDATE TRXH810 SET SHIPDATE='{DateTime.Now.ToMySQLDateStr()}' WHERE UNIQUEKEY IN ('{string.Join("','", invKeys.Values)}') AND CUSTOMER='{cCustomer}' AND PARTNER='{cPartner}'");
                    DBConnect connectAdmin = ConnectionsMgr.GetAdminConnection();
                    InsertMultiple(user, connectAdmin, _Table.ReportReq, Common.colReportReq, invRpts);
                    connectAdmin.Close();
                }
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, nameof(PdsManager), nameof(CreateInvAsn), e.Message);
            }
            return sentKeys;
        }

        private static List<string> _CreateReportReq(User currentUser, DateTime reqTime, string sHeaderKey, string trxType, string rptName, string outputName, string description, string customer, string partner, string ponum)
        {
            return new List<string>()
            {
                DBConnect.GenerateUniqueKey(),
                trxType,
                reqTime.ToMySQLDateTimeStr(),
                customer,
                partner,
                currentUser.OCConnID.SQLEscape(),
                sHeaderKey,
                outputName,
                @"\ecgb\data\networks\" + currentUser.OCConnID.SQLEscape() + @"\outbox",
                rptName,
                "S",
                ponum,
                currentUser.UserName.SQLEscape(),
                currentUser.Email.SQLEscape(),
                description
            };
        }


        private static void InsertMultiple(User user, DBConnect connection, string table, List<string> columns, List<List<string>> records)
        {
            try
            {
                int blockIndex = 0;
                while (true)
                {
                    var currentBlock = records.TakeBlock(1000, ref blockIndex);
                    if (currentBlock.Count() <= 0)
                    {
                        break;
                    }
                    connection.InsertMulti(table, columns.ToSqlColumnList(), currentBlock.ToList());
                }
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, nameof(PdsManager), nameof(InsertMultiple), e.Message);
            }
        }

        private static Dictionary<string, string> GetTrxTypeDict(User user, DBConnect connection, IEnumerable<string> keyList)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();
            try
            {
                using (var queryRes = connection.Query($"SELECT u,IF(c>1,'D','M')FROM(SELECT uniquekey u,COUNT(*)c FROM srch850 h LEFT JOIN srcb850 b using(uniquekey)WHERE uniquekey IN {keyList.ToSqlValueList()} GROUP BY h.uniquekey)x;"))
                {
                    while (queryRes.Read())
                    {
                        ret.Add(queryRes.Field(0), queryRes.Field(1));
                    }
                }
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, nameof(PdsManager), nameof(GetTrxTypeDict), e.Message);
            }
            return ret;
        }

        private static string GetVendId(User user, DBConnect connection)
        {
            try
            {
                using (var query = connection.Select(VendID, $"{Home}.{NetGroup}", $"WHERE {Customer}='{user.Customer.SQLEscape()}' AND {Partner}='{user.ActivePartner.SQLEscape()}'"))
                {
                    if (query.Read())
                    {
                        return query.Field(0);
                    }
                }
                return "";
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, nameof(PdsManager), nameof(GetVendId), e.Message);
                return "";
            }
        }

        private static int _GetRecordCount(User user, DBConnect connection, FilterInfo filter)
        {
            try
            {
                int recCount = 0;
                using (var queryPage = connection.Select("COUNT(*)", SrcH850, $"WHERE {filter.ToSearchQueryString(user, searchCols)} AND POStatus IN (0, 1)"))
                {
                    if (queryPage.Read())
                    {
                        if (!int.TryParse(queryPage.Field(0), out recCount))
                        {
                            recCount = 0;
                        }
                    }
                }
                return recCount;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, nameof(PdsManager), nameof(_GetRecordCount), e.Message);
                return 0;
            }
        }

        private static PdsOptions _GetOptions(User user, DBConnect connection)
        {
            try
            {
                PdsOptions opt = new PdsOptions();
                using (var queryPL = connection.Select(PdsOptions.Columns, Home + "." + NetGroup, $"WHERE {Customer}='{user.Customer.SQLEscape()}' AND {Partner}='{user.ActivePartner.SQLEscape()}'"))
                {
                    if (queryPL.Read())
                    {
                        opt = new PdsOptions(queryPL);
                    }
                }

                return opt;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, nameof(PdsManager), nameof(_GetOptions), e.Message);
                return new PdsOptions();
            }
        }
    }
}