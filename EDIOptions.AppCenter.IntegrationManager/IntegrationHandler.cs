using EDIOptions.AppCenter.Database;
using EDIOptions.AppCenter.Session;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EDIOptions.AppCenter.IntegrationManager
{
    public static class IntegrationHandler
    {
        private const string sourceName = "IntegrationHandler";

        private static string[] searchCols = new[]
        {
            _Column.BOLNumber,
            _Column.InvoiceNo,
            _Column.PONumber,
            _Column.SCACCode,
            _Column.ReleaseNum
        };

        private const int resultPageSize = 15;

        public static ITMOptions GetOptions(User user)
        {
            try
            {
                DBConnect connection = _GetMainConnection(user);
                var opt = _GetOptions(user, connection);
                connection.Close();
                return opt;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, sourceName, "GetOptions", e.Message);
                return new ITMOptions();
            }
        }

        public static int GetRecordCount(User user, FilterInfo filter)
        {
            try
            {
                int recCount = 0;
                DBConnect connection = _GetMainConnection(user);
                {
                    using (var queryPage = connection.Select("count(1)", _Table.IntH810, string.Format("WHERE hprocessed != 'Y' AND {0}", filter.ToSearchQueryString(user, searchCols))))
                    {
                        if (queryPage.Read())
                        {
                            if (!int.TryParse(queryPage.Field(0), out recCount))
                            {
                                recCount = 0;
                            }
                        }
                    }
                }
                connection.Close();
                return recCount;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, sourceName, "GetRecordCount", e.Message);
                return 0;
            }
        }

        public static List<ScacEntry> GetCarrierOptions(User user)
        {
            try
            {
                DBConnect connection = ConnectionsMgr.GetAdminConnection();
                var scacList = _GetScacOptions(user, connection);
                connection.Close();
                return scacList;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, sourceName, "GetCarrierOptions", e.Message);
                return new List<ScacEntry>();
            }
        }

        #region Fetch invoice list under filter

        public static List<IntHeadRecord> GetCurrentInvoices(User user, FilterInfo filter)
        {
            try
            {
                List<IntHeadRecord> recList = new List<IntHeadRecord>();
                List<string> invList = new List<string>();
                List<string> bolList = new List<string>();

                // Form conditional filter
                DBConnect connection = _GetMainConnection(user);
                {
                    // Get record count
                    var recCount = _GetRecordCount(user, connection, filter);
                    if (recCount > 0)
                    {
                        filter.ResultCount = recCount;
                        filter.MaxPage = (int)(Math.Ceiling(recCount / (double)resultPageSize));
                    }
                    // Get sent record list
                    using (var queryCurrent = connection.Select(Common.colIntH810.ToSqlColumnList(), _Table.IntH810, string.Format("WHERE {0}", filter.ToFetchQueryString(user, searchCols, resultPageSize))))
                    {
                        while (queryCurrent.Read())
                        {
                            IntHeadRecord ihr = new IntHeadRecord(queryCurrent);
                            invList.Add(ihr.InvoiceNumber);
                            bolList.Add(ihr.BolNumber);
                            recList.Add(ihr);
                        }
                    }

                    var lookup = _GetSentRecordLookup(user, connection, invList, bolList);

                    foreach (var ihr in recList.Where(x => x.HProcessed == _ProgressFlag.Unprocessed))
                    {
                        Validate(ihr, lookup);
                    }
                }
                connection.Close();
                return recList;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, sourceName, "GetCurrentInvoices", e.Message);
                return new List<IntHeadRecord>();
            }
        }

        #endregion Fetch invoice list under filter

        #region Remove selected invoices

        public static void RemoveInvoices(User user, List<string> keyList)
        {
            try
            {
                DBConnect connection = _GetMainConnection(user);
                {
                    connection.Delete(_Table.IntH810, string.Format("WHERE {0} IN {1}", _Column.UniqueKey, keyList.ToSqlValueList()));
                    connection.Delete(_Table.IntD810, string.Format("WHERE {0} IN {1}", _Column.UniqueKey, keyList.ToSqlValueList()));
                }
                connection.Close();
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, sourceName, "RemoveInvoices", e.Message);
            }
        }

        #endregion Remove selected invoices

        #region Reset selected invoices

        public static void ResetInvoices(User user, List<string> keyList)
        {
            try
            {
                string sKeyList = keyList.ToSqlValueList();
                DBConnect connection = _GetMainConnection(user);
                {
                    connection.Update(_Table.IntH810, new Dictionary<string, string>() { { _Column.HProcessed, _ProgressFlag.Unprocessed } }, string.Format("WHERE {0} IN {1}", _Column.UniqueKey, sKeyList));
                    connection.Update(_Table.IntD810, new Dictionary<string, string>() { { _Column.Processed, _ProgressFlag.Unprocessed } }, string.Format("WHERE {0} IN {1}", _Column.UniqueKey, sKeyList));
                }
                connection.Close();
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, sourceName, "ResetInvoices", e.Message);
            }
        }

        #endregion Reset selected invoices

        #region Edit select invoices

        public static void EditInvoices(User user, List<IntHeadRecord> updateRecs)
        {
            try
            {
                DBConnect connection = _GetMainConnection(user);
                {
                    List<string> updateCols = new List<string>()
                    {
                        _Column.UniqueKey,
                        _Column.Customer,
                        _Column.Partner,
                        _Column.BOLNumber,
                        _Column.SCACCode,
                        _Column.Routing,
                        _Column.ShipmentDate
                    };

                    List<string> replaceCols = new List<string>()
                    {
                        string.Format("{0}=VALUES({0})", _Column.BOLNumber),
                        string.Format("{0}=VALUES({0})", _Column.SCACCode),
                        string.Format("{0}=VALUES({0})", _Column.Routing),
                        string.Format("{0}=VALUES({0})", _Column.ShipmentDate),
                    };

                    List<string> updateVals = new List<string>();

                    foreach (var record in updateRecs)
                    {
                        List<string> recVals = new List<string>();
                        recVals.Add(record.Key);
                        recVals.Add(user.Customer);
                        recVals.Add(user.ActivePartner);
                        recVals.Add(record.BolNumber);
                        recVals.Add(record.ScacCode);
                        recVals.Add(record.Routing);
                        DateTime temp;
                        if (DateTime.TryParse(record.ShipDate, out temp))
                        {
                            recVals.Add(temp.ToMySQLDateStr());
                        }
                        else
                        {
                            recVals.Add(_SqlValue.Null);
                        }
                        updateVals.Add(recVals.ToSqlValueList());
                    }

                    connection.Query(string.Format("INSERT INTO {0} ({1}) VALUES {2} ON DUPLICATE KEY UPDATE {3}", _Table.IntH810, updateCols.ToSqlColumnList(), string.Join(",", updateVals), string.Join(",", replaceCols)));
                }
                connection.Close();
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, sourceName, "EditInvoices", e.Message);
            }
        }

        #endregion Edit select invoices

        #region Edit filtered invoices

        public static void EditFilteredInvoices(User user, FilterInfo filter, IntHeadRecord changeRecord)
        {
            try
            {
                Dictionary<string, string> updatePack = new Dictionary<string, string>();

                if (changeRecord.BolNumber != "")
                {
                    updatePack.Add(_Column.BOLNumber, changeRecord.BolNumber.SQLEscape());
                }
                if (changeRecord.Routing != "")
                {
                    updatePack.Add(_Column.Routing, changeRecord.Routing.SQLEscape());
                }
                if (changeRecord.ScacCode != "X" && changeRecord.ScacCode != "")
                {
                    updatePack.Add(_Column.SCACCode, changeRecord.ScacCode.SQLEscape());
                }

                DateTime temp;
                if (DateTime.TryParse(changeRecord.ShipDate, out temp))
                {
                    updatePack.Add(_Column.ShipmentDate, temp.ToMySQLDateStr());
                }

                if (updatePack.Count > 0)
                {
                    DBConnect connection = _GetMainConnection(user);
                    connection.Update(_Table.IntH810, updatePack, string.Format("WHERE hprocessed != 'Y' AND {0}", filter.ToSearchQueryString(user, searchCols)));
                    connection.Close();
                }
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, sourceName, "EditFilteredInvoices", e.Message);
            }
        }

        #endregion Edit filtered invoices

        #region Get prelist for submission

        public static string GetSubmitType(User user, FilterInfo currentFilter)
        {
            try
            {
                List<string> fs = new List<string>();
                DBConnect connection = _GetMainConnection(user);
                using (var queryHead = connection.Select($"DISTINCT({_Column.XferType})", _Table.IntH810, string.Format("WHERE hprocessed='S' AND {0}", currentFilter.ToSearchQueryString(user, searchCols))))
                {
                    while (queryHead.Read())
                    {
                        string type = queryHead.Field(0);
                        switch (type)
                        {
                            case _InvoiceTransferType.Mixed:
                            case _InvoiceTransferType.Distributed:
                            case _InvoiceTransferType.PrePacked:
                            case _InvoiceTransferType.Consolidated:
                                fs.Add(type);
                                break;

                            default:
                                break;
                        }
                    }
                }
                if (fs.Count == 1)
                {
                    return fs[0];
                }
                else
                {
                    return "X";
                }
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, sourceName, nameof(GetSubmitType), e.Message);
                return "X";
            }
        }

        public static string GetSubmitType(User user, List<string> keyList)
        {
            try
            {
                List<string> fs = new List<string>();
                DBConnect connection = _GetMainConnection(user);
                using (var queryHead = connection.Select($"DISTINCT({_Column.XferType})", _Table.IntH810, $"WHERE hprocessed='S' AND {_Column.UniqueKey} IN {keyList.ToSqlValueList()}"))
                {
                    while (queryHead.Read())
                    {
                        string type = queryHead.Field(0);
                        switch (type)
                        {
                            case _InvoiceTransferType.Mixed:
                            case _InvoiceTransferType.Distributed:
                            case _InvoiceTransferType.PrePacked:
                            case _InvoiceTransferType.Consolidated:
                                fs.Add(type);
                                break;

                            default:
                                break;
                        }
                    }
                }
                if (fs.Count == 1)
                {
                    return fs[0];
                }
                else
                {
                    return "X";
                }
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, sourceName, nameof(GetSubmitType), e.Message);
                return "X";
            }
        }

        public static PresendRecord GetSendRecord(User user, SubmitPreRequest request)
        {
            try
            {
                DBConnect connection = _GetMainConnection(user);
                var options = _GetOptions(user, connection);
                PresendRecord psr;
                switch (request.SubmitType)
                {
                    case _ITMSubmitType.InvoiceOnly:
                        psr = _GetInvoicePreList(user, connection, request, options);
                        break;

                    default:
                        psr = _GetAsnPreList(user, connection, request, options);
                        break;
                }
                if (request.SplitType == _InvoiceProcessType.Merge)
                {
                    psr.ConsInvlist = _GetConsInvList(user, connection, request);
                }
                return psr;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, sourceName, "GetSendRecord", e.Message);
                return new PresendRecord();
            }
        }

        #endregion Get prelist for submission

        #region Submit records

        #region Gathering base records

        private static void _GetRecordsByKey(User user, DBConnect connection, ITMState state, List<string> keyList, bool isSplit)
        {
            try
            {
                Dictionary<string, Invoice> transferDict = new Dictionary<string, Invoice>();
                string conditionKey = string.Format("WHERE {0} IN {1}", _Column.UniqueKey, keyList.ToSqlValueList());

                using (var queryHead = connection.Select(_SqlValue.All, _Table.IntH810, conditionKey + " AND hprocessed='S'"))
                {
                    while (queryHead.Read())
                    {
                        Invoice tg = new Invoice();
                        tg.AddHeaderRecord(queryHead);
                        transferDict.Add(tg.OriginalUniqueKey, tg);
                    }
                }

                using (var queryDetl = connection.Select(_SqlValue.All, _Table.IntD810, conditionKey))
                {
                    while (queryDetl.Read())
                    {
                        string uniqueKey = queryDetl.FieldByName(_Column.UniqueKey);
                        transferDict[uniqueKey].AddDetailRecord(queryDetl);
                    }
                }

                if (isSplit)
                {
                    using (var queryCtn = connection.Select(new[] { _Column.UniqueKey, _Column.UniqueItem, _Column.BYId, _Column.CartonQuantity }, _Table.IntC810, conditionKey))
                    {
                        while (queryCtn.Read())
                        {
                            string uniquekey = queryCtn.FieldByName(_Column.UniqueKey);
                            transferDict[uniquekey].AddCartonRecord(queryCtn);
                        }
                    }
                }

                var filteredInvList = isSplit ? transferDict.Values.SelectMany(x => x.Split()) : transferDict.Values;

                List<string> invList = new List<string>();
                List<string> bolList = new List<string>();
                foreach (var record in filteredInvList)
                {
                    invList.Add(record.InvoiceNumber);
                    bolList.Add(record.BolNumber);
                }

                var lookup = _GetSentRecordLookup(user, connection, invList, bolList);

                foreach (var record in filteredInvList)
                {
                    if (Validate(record, lookup))
                    {
                        state.AddRecord(record);
                    }
                }
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, sourceName, "_GetRecordsByKey", e.Message);
            }
        }

        private static void _GetRecordsByFilter(User user, DBConnect connection, ITMState state, FilterInfo filter, bool isSplit)
        {
            try
            {
                Dictionary<string, Invoice> transferDict = new Dictionary<string, Invoice>();

                List<string> uniqueKeyList = new List<string>();

                using (var queryHead = connection.Select(_SqlValue.All, _Table.IntH810, string.Format("WHERE hprocessed='S' AND {0}", filter.ToSearchQueryString(user, searchCols))))
                {
                    while (queryHead.Read())
                    {
                        Invoice tg = new Invoice();
                        tg.AddHeaderRecord(queryHead);
                        transferDict.Add(tg.OriginalUniqueKey, tg);
                        uniqueKeyList.Add(tg.OriginalUniqueKey);
                    }
                }

                string conditionKey = string.Format("WHERE {0} IN {1}", _Column.UniqueKey, uniqueKeyList.ToSqlValueList());

                using (var queryDetl = connection.Select(_SqlValue.All, _Table.IntD810, conditionKey))
                {
                    while (queryDetl.Read())
                    {
                        string uniqueKey = queryDetl.FieldByName(_Column.UniqueKey);
                        transferDict[uniqueKey].AddDetailRecord(queryDetl);
                    }
                }

                if (isSplit)
                {
                    using (var queryCtn = connection.Select(new[] { _Column.UniqueKey, _Column.UniqueItem, _Column.BYId, _Column.CartonQuantity }, _Table.IntC810, conditionKey))
                    {
                        while (queryCtn.Read())
                        {
                            string uniquekey = queryCtn.FieldByName(_Column.UniqueKey);
                            transferDict[uniquekey].AddCartonRecord(queryCtn);
                        }
                    }
                }

                var filteredInvList = isSplit ? transferDict.Values.SelectMany(x => x.Split()) : transferDict.Values;

                List<string> invList = new List<string>();
                List<string> bolList = new List<string>();

                foreach (var record in filteredInvList)
                {
                    invList.Add(record.InvoiceNumber);
                    bolList.Add(record.BolNumber);
                }

                var lookup = _GetSentRecordLookup(user, connection, invList, bolList);

                HashSet<string> bolBlacklist = new HashSet<string>();

                foreach (var ihr in filteredInvList)
                {
                    if (!bolBlacklist.Contains(ihr.BolNumber) && !Validate(ihr, lookup) && !bolBlacklist.Contains(ihr.BolNumber))
                    {
                        bolBlacklist.Add(ihr.BolNumber);
                    }
                }

                foreach (var record in filteredInvList.Where(x => !bolBlacklist.Contains(x.BolNumber)))
                {
                    state.AddRecord(record);
                }
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, sourceName, "_GetRecordsByFilter", e.Message);
            }
        }

        private static void _GetRecordsByBol(User user, DBConnect connection, ITMState state, List<string> bolList, bool isSplit)
        {
            try
            {
                Dictionary<string, Invoice> transferDict = new Dictionary<string, Invoice>();
                string conditionBol = string.Format("WHERE hprocessed='S' AND {0} IN {1} AND {2}='{3}' AND {4}='{5}'", _Column.BOLNumber, bolList.ToSqlValueList(), _Column.Customer, user.Customer.SQLEscape(), _Column.Partner, user.ActivePartner.SQLEscape());

                List<string> uniqueKeyList = new List<string>();

                using (var queryHead = connection.Select(_SqlValue.All, _Table.IntH810, conditionBol))
                {
                    while (queryHead.Read())
                    {
                        Invoice tg = new Invoice();
                        tg.AddHeaderRecord(queryHead);
                        transferDict.Add(tg.OriginalUniqueKey, tg);
                        uniqueKeyList.Add(tg.OriginalUniqueKey);
                    }
                }

                string conditionKey = string.Format("WHERE {0} IN {1}", _Column.UniqueKey, uniqueKeyList.ToSqlValueList());

                using (var queryDetl = connection.Select(_SqlValue.All, _Table.IntD810, conditionKey))
                {
                    while (queryDetl.Read())
                    {
                        string uniqueKey = queryDetl.FieldByName(_Column.UniqueKey);
                        transferDict[uniqueKey].AddDetailRecord(queryDetl);
                    }
                }

                if (isSplit)
                {
                    using (var queryCtn = connection.Select(new[] { _Column.UniqueKey, _Column.UniqueItem, _Column.BYId, _Column.CartonQuantity }, _Table.IntC810, conditionKey))
                    {
                        while (queryCtn.Read())
                        {
                            string uniquekey = queryCtn.FieldByName(_Column.UniqueKey);
                            transferDict[uniquekey].AddCartonRecord(queryCtn);
                        }
                    }
                }

                var filteredInvList = isSplit ? transferDict.Values.SelectMany(x => x.Split()) : transferDict.Values;

                List<string> invList = new List<string>();
                foreach (var record in filteredInvList)
                {
                    invList.Add(record.InvoiceNumber);
                }

                var lookup = _GetSentRecordLookup(user, connection, invList, bolList);

                HashSet<string> bolBlacklist = new HashSet<string>();

                foreach (var ihr in filteredInvList)
                {
                    if (!bolBlacklist.Contains(ihr.BolNumber) && !Validate(ihr, lookup) && !bolBlacklist.Contains(ihr.BolNumber))
                    {
                        bolBlacklist.Add(ihr.BolNumber);
                    }
                }

                foreach (var record in filteredInvList.Where(x => !bolBlacklist.Contains(x.BolNumber)))
                {
                    state.AddRecord(record);
                }
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, sourceName, "_GetRecordsByBol", e.Message);
            }
        }

        #endregion Gathering base records

        #region Sending records

        public static List<string> SendInvoices(User user, SubmitRequest request)
        {
            try
            {
                DateTime reqTime = DateTime.Now;
                DBConnect connection = _GetMainConnection(user);
                ITMState state = new ITMState(request, user);

                if (request.IsAll == true)
                {
                    _GetRecordsByFilter(user, connection, state, request.Filter, request.SplitType == "S");

                    if (!state.HasRecords())
                    {
                        ProgramLog.LogError(user, nameof(IntegrationHandler), nameof(SendInvoices), "No records found to transfer.");
                        return new List<string>();
                    }

                    if (request.SubmitType != _ITMSubmitType.InvoiceOnly)
                    {
                        state.SetAllPackTypes(request.AllPackType);
                        state.AddShipmentNumbers(_GetShipmentNumbers(user, connection, state.AsnCount));
                    }
                }
                else
                {
                    if (request.SubmitType == _ITMSubmitType.InvoiceOnly)
                    {
                        _GetRecordsByKey(user, connection, state, request.KeyList, request.SplitType == _InvoiceProcessType.Split);
                    }
                    else
                    {
                        _GetRecordsByBol(user, connection, state, request.MergeList.Select(x => x.BolNumber).ToList(), request.SplitType == _InvoiceProcessType.Split);
                    }

                    if (!state.HasRecords())
                    {
                        ProgramLog.LogError(user, nameof(IntegrationHandler), nameof(SendInvoices), "No records found to transfer.");
                        return new List<string>();
                    }

                    if (request.SubmitType != _ITMSubmitType.InvoiceOnly)
                    {
                        state.SetSelectedPackTypes();
                        state.AddShipmentNumbers(_GetShipmentNumbers(user, connection, state.AsnCount));
                    }
                }

                state.RemoveSingleRecs();
                state.SetBaseInvoiceNumber(request.BaseInv);

                if (request.SubmitType != _ITMSubmitType.AsnOnly)
                {
                    state.Create810Recs(reqTime, user, request.SplitType == _InvoiceProcessType.Merge);
                }
                if (request.SubmitType != _ITMSubmitType.InvoiceOnly)
                {
                    state.Create856Recs(reqTime, request.SubmitType == _ITMSubmitType.InvoiceAndAsn);
                }

                if (request.SubmitType != _ITMSubmitType.AsnOnly)
                {
                    InsertMultiple(user, connection, _Table.TrxD810, Common.colTrxD810, state.detl810);
                    InsertMultiple(user, connection, _Table.TrxH810, Common.colTrxH810, state.head810);
                }

                if (request.SubmitType != _ITMSubmitType.InvoiceOnly)
                {
                    InsertMultiple(user, connection, _Table.TrxC856, Common.colTrxC856, state.cart856);
                    InsertMultiple(user, connection, _Table.TrxD856, Common.colTrxD856, state.detl856);
                    InsertMultiple(user, connection, _Table.TrxH856, Common.colTrxH856, state.head856);
                }

                if (state.repoReq.Count > 0)
                {
                    DBConnect connectAdmin = ConnectionsMgr.GetAdminConnection();
                    InsertMultiple(user, connectAdmin, _Table.ReportReq, Common.colReportReq, state.repoReq);
                    connectAdmin.Close();
                }

                state.ValidateSentASNs(connection);

                connection.Update(_Table.IntH810, new Dictionary<string, string> { { _Column.HProcessed, _ProgressFlag.Success } }, string.Format("WHERE {0} IN {1}", _Column.UniqueKey, state.srcKeyList.ToSqlValueList()));

                connection.Close();
                if (state.head856.Count > 0)
                {
                    return state.head856.Select(x => x[0]).ToList();
                }
                else
                {
                    return new List<string>();
                }
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, sourceName, "SendInvoices", e.Message + "\n" + e.StackTrace);
                return new List<string>();
            }
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
                ProgramLog.LogError(user, nameof(IntegrationHandler), nameof(InsertMultiple), e.Message);
            }
        }

        #endregion Sending records

        #endregion Submit records

        #region Utility methods

        private static DBConnect _GetMainConnection(User user)
        {
            try
            {
#if DEBUG
                return ConnectionsMgr.GetOCConnection(user, _Database.Temp);
#else
                return ConnectionsMgr.GetOCConnection(user, _Database.ECGB);
#endif
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, sourceName, "_GetMainConnection", e.Message);
                return new DBConnect();
            }
        }

        private static int _GetRecordCount(User user, DBConnect connection, FilterInfo filter)
        {
            try
            {
                int recCount = 0;
                using (var queryPage = connection.Select("count(1)", _Table.IntH810, string.Format("WHERE {0}", filter.ToSearchQueryString(user, searchCols))))
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
                ProgramLog.LogError(user, sourceName, "_GetRecordCount", e.Message);
                return 0;
            }
        }

        private static ITMOptions _GetOptions(User user, DBConnect connection)
        {
            try
            {
                ITMOptions opt = new ITMOptions();
                using (var queryPL = connection.Select(ITMOptions.Columns, _Database.Home + "." + _Table.NetGroup, string.Format("WHERE {0}='{1}' AND {2}='{3}'", _Column.Customer, user.Customer.SQLEscape(), _Column.Partner, user.ActivePartner.SQLEscape())))
                {
                    if (queryPL.Read())
                    {
                        opt = new ITMOptions(queryPL);
                    }
                }

                if (!(opt.IsPPEnabled || opt.IsMXEnabled || opt.IsDSEnabled))
                {
                    // No overall pack if all 3 flags are false
                    opt.IsPackingEnabled = false;
                }
                else if (!opt.IsPackingEnabled)
                {
                    // No pack flags if overall pack is disabled
                    opt.IsPPEnabled = opt.IsMXEnabled = opt.IsDSEnabled = false;
                }
                return opt;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, sourceName, "_GetOptions", e.Message);
                return new ITMOptions();
            }
        }

        private static List<ConsInvoice> _GetConsInvList(User user, DBConnect connection, SubmitPreRequest request)
        {
            try
            {
                string conditionInv = "0";
                if (request.IsAll == true)
                {
                    conditionInv = $"WHERE {request.CurrentFilter.ToSearchQueryString(user, searchCols)}";
                }
                else
                {
                    conditionInv = $"WHERE {_Column.UniqueKey} IN {request.KeyList.ToSqlValueList()}";
                }

                Dictionary<string, ConsInvoice> consInvDict = new Dictionary<string, ConsInvoice>();

                using (var queryInv = connection.Select(new[] { _Column.InvoiceNo, _Column.ReleaseNum }, _Table.IntH810, conditionInv))
                {
                    while (queryInv.Read())
                    {
                        var relNum = queryInv.FieldByName(_Column.ReleaseNum);
                        if (consInvDict.ContainsKey(relNum))
                        {
                            consInvDict[relNum].InvoicesGrouped.Add(queryInv.FieldByName(_Column.InvoiceNo));
                        }
                        else
                        {
                            ConsInvoice ci = new ConsInvoice();
                            ci.MasterPONumber = relNum;
                            ci.InvoicesGrouped = new List<string>() { queryInv.FieldByName(_Column.InvoiceNo) };
                            consInvDict.Add(ci.MasterPONumber, ci);
                        }
                    }
                }

                return consInvDict.Values.ToList();
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, sourceName, nameof(_GetConsInvList), e.Message);
                return new List<ConsInvoice>();
            }
        }

        private static PresendRecord _GetInvoicePreList(User user, DBConnect connection, SubmitPreRequest request, ITMOptions options)
        {
            try
            {
                List<IntHeadRecord> recHolder = new List<IntHeadRecord>();
                List<string> invList = new List<string>();
                List<string> bolList = new List<string>();
                string conditionInv = "";
                if (request.IsAll == true)
                {
                    conditionInv = string.Format("WHERE {0}", request.CurrentFilter.ToSearchQueryString(user, searchCols));
                }
                else
                {
                    conditionInv = string.Format("WHERE {0} IN {1}", _Column.UniqueKey, request.KeyList.ToSqlValueList());
                }

                using (var queryInv = connection.Select(new[] { _Column.UniqueKey, _Column.SCACCode, _Column.ShipmentDate, _Column.InvoiceNo, _Column.BOLNumber, _Column.STId, _Column.HProcessed, _Column.XferType }, _Table.IntH810, conditionInv))
                {
                    while (queryInv.Read())
                    {
                        IntHeadRecord ihr = new IntHeadRecord();
                        ihr.Key = queryInv.FieldByName(_Column.UniqueKey);
                        ihr.InvoiceNumber = queryInv.FieldByName(_Column.InvoiceNo, "");
                        ihr.ShipDate = queryInv.FieldByName(_Column.ShipmentDate, "");
                        ihr.BolNumber = queryInv.FieldByName(_Column.BOLNumber, "");
                        ihr.StId = queryInv.FieldByName(_Column.STId, "");
                        ihr.HProcessed = queryInv.FieldByName(_Column.HProcessed, "");
                        ihr.ScacCode = queryInv.FieldByName(_Column.SCACCode, "");
                        ihr.TransferType = queryInv.FieldByName(_Column.XferType, "M");
                        invList.Add(ihr.InvoiceNumber);
                        bolList.Add(ihr.BolNumber);
                        recHolder.Add(ihr);
                    }
                }

                var lookup = _GetSentRecordLookup(user, connection, invList, bolList);

                bool hasBad = false;

                foreach (var ihr in recHolder)
                {
                    if (!Validate(ihr, lookup))
                    {
                        hasBad = true;
                    }
                }

                var goodRec = recHolder.Where(x => x.HProcessed == "S");
                var badRec = recHolder.Except(goodRec);

                var badInv = badRec.Select(x => x.InvoiceNumber).Distinct().OrderBy(x => x);
                var badBol = badRec.Select(x => x.BolNumber).Distinct().OrderBy(x => x);

                PresendRecord psr = new PresendRecord();
                psr.SplitType = request.SplitType;
                psr.BaseInv = request.BaseInv;
                psr.HasBadRecords = hasBad;
                psr.HasRecordsToSend = goodRec.Count() > 0;
                psr.BadBOL.AddRange(badBol);
                psr.BadInvoice.AddRange(badInv);
                if (request.IsAll == true)
                {
                    psr.InvoiceList.AddRange(goodRec.Select(x => x.InvoiceNumber));
                }
                else
                {
                    psr.InvPreList.AddRange(goodRec);
                }

                return psr;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, sourceName, "_GetInvoicePreList", e.Message);
                return new PresendRecord();
            }
        }

        private static Tuple<IEnumerable<string>, IEnumerable<string>> _GetInvBolFromRequest(User user, DBConnect connection, SubmitPreRequest request, ITMOptions options)
        {
            try
            {
                Tuple<List<string>, List<string>> ret = new Tuple<List<string>, List<string>>(new List<string>(), new List<string>());
                List<string> invList = new List<string>();
                List<string> bolList = new List<string>();
                string condition = "";
                if (request.IsAll == true)
                {
                    condition = string.Format("WHERE {0}", request.CurrentFilter.ToSearchQueryString(user, searchCols));
                }
                else
                {
                    condition = string.Format("WHERE {0} IN {1}", _Column.UniqueKey, request.KeyList.ToSqlValueList());
                }

                using (var queryInvBol = connection.Select(new[] { _Column.InvoiceNo, _Column.BOLNumber }, _Table.IntH810, condition))
                {
                    while (queryInvBol.Read())
                    {
                        invList.Add(queryInvBol.FieldByName(_Column.InvoiceNo));
                        bolList.Add(queryInvBol.FieldByName(_Column.BOLNumber));
                    }
                }

                return new Tuple<IEnumerable<string>, IEnumerable<string>>(invList.Distinct(), bolList.Distinct());
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, sourceName, "_GetInvBolFromRequest", e.Message);
                return new Tuple<IEnumerable<string>, IEnumerable<string>>(new List<string>(), new List<string>());
            }
        }

        private static PresendRecord _GetAsnPreList(User user, DBConnect connection, SubmitPreRequest request, ITMOptions options)
        {
            try
            {
                List<IntHeadRecord> recHolder = new List<IntHeadRecord>();
                List<string> invList = new List<string>();
                List<string> bolList = new List<string>();

                var invBolList = _GetInvBolFromRequest(user, connection, request, options);
                string conditionBol = string.Format("WHERE {0}='{1}' AND {2}='{3}' AND {4} IN {5}", _Column.Customer, user.Customer, _Column.Partner, user.ActivePartner, _Column.BOLNumber, invBolList.Item2.ToSqlValueList());

                // Then, select records from this.
                using (DBResult queryMerge = connection.Select(new[] { _Column.UniqueKey, _Column.InvoiceNo, _Column.SCACCode, _Column.BOLNumber, _Column.ShipmentDate, _Column.STId, _Column.BYId, _Column.XferType, _Column.HProcessed }, _Table.IntH810, conditionBol))
                {
                    while (queryMerge.Read())
                    {
                        IntHeadRecord ihr = new IntHeadRecord();
                        ihr.Key = queryMerge.FieldByName(_Column.UniqueKey);
                        ihr.InvoiceNumber = queryMerge.FieldByName(_Column.InvoiceNo, "");
                        ihr.BolNumber = queryMerge.FieldByName(_Column.BOLNumber, "");
                        ihr.ShipDate = queryMerge.FieldByName(_Column.ShipmentDate, "");
                        ihr.ScacCode = queryMerge.FieldByName(_Column.SCACCode, "");
                        ihr.StId = queryMerge.FieldByName(_Column.STId, "");
                        ihr.ById = queryMerge.FieldByName(_Column.BYId, "");
                        ihr.TransferType = queryMerge.FieldByName(_Column.XferType, "M");
                        ihr.HProcessed = queryMerge.FieldByName(_Column.HProcessed, "X");
                        recHolder.Add(ihr);
                    }
                }

                var lookup = _GetSentRecordLookup(user, connection, invBolList.Item1, invBolList.Item2);

                HashSet<string> bolBlacklist = new HashSet<string>();

                foreach (var ihr in recHolder)
                {
                    if (bolBlacklist.Contains(ihr.BolNumber))
                    {
                        ihr.HProcessed = "X";
                    }
                    else
                    {
                        bool good = Validate(ihr, lookup);
                        if (!good)
                        {
                            if (!bolBlacklist.Contains(ihr.BolNumber))
                            {
                                bolBlacklist.Add(ihr.BolNumber);
                            }
                        }
                    }
                }

                var goodRecs = recHolder.Where(x => !bolBlacklist.Contains(x.BolNumber));
                var badRec = recHolder.Except(goodRecs);

                var badInv = badRec.Select(x => x.InvoiceNumber).Distinct().OrderBy(x => x);
                var badBol = badRec.Select(x => x.BolNumber).Distinct().OrderBy(x => x);

                Dictionary<string, AutoMergeRecord> autoMergeDict = new Dictionary<string, AutoMergeRecord>();

                foreach (var record in goodRecs)
                {
                    string bol = record.BolNumber;
                    string byid = record.ById;
                    if (autoMergeDict.ContainsKey(bol))
                    {
                        autoMergeDict[bol].KeyList.Add(record.Key);
                        autoMergeDict[bol].InvoiceList.Add(record.InvoiceNumber);
                    }
                    else
                    {
                        autoMergeDict.Add(bol, new AutoMergeRecord(record));
                    }
                    if (!autoMergeDict[bol].IsDistributed && (record.TransferType == _InvoiceTransferType.Distributed || autoMergeDict[bol].LastById != byid))
                    {
                        autoMergeDict[bol].IsDistributed = true;
                    }
                }

                PresendRecord psr = new PresendRecord();
                psr.SplitType = request.SplitType;
                psr.BaseInv = request.BaseInv;
                psr.HasBadRecords = bolBlacklist.Count > 0;
                psr.HasRecordsToSend = autoMergeDict.Count > 0;
                psr.BadBOL.AddRange(badBol);
                psr.BadInvoice.AddRange(badInv);
                if (request.IsAll == true)
                {
                    psr.BolList.AddRange(autoMergeDict.Values.Select(x => x.BolNumber));
                }
                else
                {
                    psr.AsnPreList.AddRange(autoMergeDict.Values);
                }

                return psr;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, sourceName, "_GetAsnPreList", e.Message);
                return new PresendRecord();
            }
        }

        private static List<string> _GetShipmentNumbers(User user, DBConnect connection, int numCount)
        {
            try
            {
                List<string> shipNumList = new List<string>();
                int shipNum = 0;
                using (var queryShipNum = connection.Select("NextShipNum", "home.company", string.Format("WHERE {0}='{1}'", _Column.Customer, user.Customer.SQLEscape())))
                {
                    if (queryShipNum.AffectedRows == 0)
                    {
                        ProgramLog.LogError(user, "IntegrationManager", "GetNextShipNum", string.Format("Unable to find customer {0} in company table.", user.Customer.SQLEscape()));
                        return new List<string>();
                    }
                    queryShipNum.Read();
                    shipNum = int.Parse(queryShipNum.Field(0, "0"));
                }
                int max = shipNum + numCount;
                for (; shipNum < max; shipNum++)
                {
                    shipNumList.Add(shipNum.ToString("D10"));
                }
                connection.Update("home.company", new Dictionary<string, string>() { { "NextShipNum", max.ToString() } }, string.Format("WHERE {0}='{1}'", _Column.Customer, user.Customer.SQLEscape()));
                return shipNumList;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, sourceName, "_GetShipmentNumbers", e.Message);
                return new List<string>();
            }
        }

        private static LookupRecList _GetSentRecordLookup(User user, DBConnect connection, IEnumerable<string> invList, IEnumerable<string> bolList)
        {
            try
            {
                LookupRecList sent = new LookupRecList();
                if (invList.Count() > 0)
                {
                    string strInvList = invList.ToSqlValueList();
                    string conditionInv = string.Format("WHERE {0}='{1}' AND {2}='{3}' AND {4} IN {5}", _Column.Customer, user.Customer.SQLEscape(), _Column.Partner, user.ActivePartner.SQLEscape(), _Column.InvoiceNo, strInvList);
                    using (var queryPresentInv = connection.Query("SELECT DISTINCT invoiceno FROM trxh810 " + conditionInv))
                    {
                        while (queryPresentInv.Read())
                        {
                            string inv = queryPresentInv.FieldByName(_Column.InvoiceNo).Trim();
                            if (!sent.InvoiceNumList.Contains(inv))
                            {
                                sent.InvoiceNumList.Add(inv);
                            }
                        }
                    }
                }
                if (bolList.Count() > 0)
                {
                    string strBolList = bolList.ToSqlValueList();
                    string conditionBol = string.Format("WHERE {0}='{1}' AND {2}='{3}' AND {4} IN {5}", _Column.Customer, user.Customer.SQLEscape(), _Column.Partner, user.ActivePartner.SQLEscape(), _Column.BOLNumber, strBolList);
                    using (var queryPresentBol = connection.Query("SELECT DISTINCT bolnumber FROM trxh856 " + conditionBol))
                    {
                        while (queryPresentBol.Read())
                        {
                            string bol = queryPresentBol.FieldByName(_Column.BOLNumber).Trim();
                            if (!sent.BolNumList.Contains(bol))
                            {
                                sent.BolNumList.Add(bol);
                            }
                        }
                    }
                }
                return sent;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, sourceName, "_GetSentRecords", e.Message);
                return new LookupRecList();
            }
        }

        private static List<ScacEntry> _GetScacOptions(User user, DBConnect connection)
        {
            try
            {
                List<ScacEntry> pickList = new List<ScacEntry>();
                var filterID = ConnectionsMgr.GetOCConnInfo(user).Id.SQLEscape();
                var filterPartner = user.ActivePartner.SQLEscape();
                var condition = string.Format("WHERE ({0} IS NULL OR {0}='{1}') AND ({2} IS NULL OR {2}='{3}')", _Column.ConnectID, filterID, _Column.Partner, filterPartner);
                using (var queryPick = connection.Select(new[] { _Column.PickVal, _Column.PickDesc }, _Table.PickVal, condition))
                {
                    while (queryPick.Read())
                    {
                        ScacEntry sc = new ScacEntry(queryPick);
                        pickList.Add(sc);
                    }
                }
                return pickList;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, sourceName, "_GetScacOptions", e.Message);
                return new List<ScacEntry>();
            }
        }

        #endregion Utility methods

        #region Validation

        private static bool Validate(IntHeadRecord ihr, LookupRecList lookup)
        {
            bool allGood = true;
            if (ihr.HProcessed == "X")
            {
                return false;
            }
            if (lookup.InvoiceNumList.Contains(ihr.InvoiceNumber))
            {
                allGood = false;
                ihr.HProcessed = "X";
                if (ihr.Messages.Length > 0)
                {
                    ihr.Messages += "\r\n";
                }
                ihr.Messages += "An invoice with the same number is on the portal.";
            }
            if (lookup.BolNumList.Contains(ihr.BolNumber))
            {
                allGood = false;
                ihr.HProcessed = "X";
                if (ihr.Messages.Length > 0)
                {
                    ihr.Messages += "\r\n";
                }
                ihr.Messages += "An ASN with the same BOL is on the portal.";
            }
            if (string.IsNullOrWhiteSpace(ihr.InvoiceNumber))
            {
                allGood = false;
                ihr.HProcessed = "X";
                if (ihr.Messages.Length > 0)
                {
                    ihr.Messages += "\r\n";
                }
                ihr.Messages += "An invoice number is required.";
            }
            if (string.IsNullOrWhiteSpace(ihr.ScacCode))
            {
                allGood = false;
                ihr.HProcessed = "X";
                if (ihr.Messages.Length > 0)
                {
                    ihr.Messages += "\r\n";
                }
                ihr.Messages += "A SCAC code is required.";
            }
            if (string.IsNullOrWhiteSpace(ihr.BolNumber))
            {
                allGood = false;
                ihr.HProcessed = "X";
                if (ihr.Messages.Length > 0)
                {
                    ihr.Messages += "\r\n";
                }
                ihr.Messages += "A BOL number is required.";
            }
            DateTime temp;
            if (!DateTime.TryParse(ihr.ShipDate, out temp))
            {
                allGood = false;
                ihr.HProcessed = "X";
                if (ihr.Messages.Length > 0)
                {
                    ihr.Messages += "\r\n";
                }
                ihr.Messages += "A valid ship date is required.";
            }
            return allGood;
        }

        private static bool Validate(Invoice ihr, LookupRecList lookup)
        {
            if (ihr.HProcessed == "X" ||
                string.IsNullOrWhiteSpace(ihr.InvoiceNumber) ||
                string.IsNullOrWhiteSpace(ihr.BolNumber) ||
                string.IsNullOrWhiteSpace(ihr.ScacCode) ||
                lookup.InvoiceNumList.Contains(ihr.InvoiceNumber) ||
                lookup.BolNumList.Contains(ihr.BolNumber))
            {
                return false;
            }
            DateTime temp;
            if (!DateTime.TryParse(ihr.ShipDate, out temp))
            {
                return false;
            }
            return true;
        }

        #endregion Validation
    }
}