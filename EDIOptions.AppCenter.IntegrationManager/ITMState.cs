using EDIOptions.AppCenter.Database;
using EDIOptions.AppCenter.Session;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EDIOptions.AppCenter.IntegrationManager
{
    public class ITMState
    {
        private SubmitRequest request;
        private ShipNumCache shipNumCache = new ShipNumCache();
        private User currentUser;

        public List<List<string>> head810 = new List<List<string>>();
        public List<List<string>> detl810 = new List<List<string>>();
        public List<List<string>> head856 = new List<List<string>>();
        public List<List<string>> detl856 = new List<List<string>>();
        public List<List<string>> cart856 = new List<List<string>>();
        public List<List<string>> repoReq = new List<List<string>>();

        private List<Invoice> srcRecords = new List<Invoice>();

        public List<string> srcKeyList = new List<string>();

        public Dictionary<string, List<Invoice>> bolToSrcRecordDict = new Dictionary<string, List<Invoice>>();

        private Dictionary<string, string> invNumtoInvUniqueKey = new Dictionary<string, string>();

        private Dictionary<string, List<Invoice>> masterPoToSrcRecordDict = new Dictionary<string, List<Invoice>>();

        private InvoiceCounter invoiceCounter;

        public ITMState(SubmitRequest srq, User user)
        {
            request = srq;
            currentUser = user;
        }

        public bool HasRecords()
        {
            return srcRecords.Count > 0;
        }

        public void SetBaseInvoiceNumber(string baseInv)
        {
            invoiceCounter = new InvoiceCounter(baseInv);
        }

        public int AsnCount
        {
            get
            {
                return bolToSrcRecordDict.Count;
            }
        }

        public void AddRecord(Invoice record)
        {
            srcRecords.Add(record);
            srcKeyList.Add(record.OriginalUniqueKey);
            if (bolToSrcRecordDict.ContainsKey(record.BolNumber))
            {
                bolToSrcRecordDict[record.BolNumber].Add(record);
            }
            else
            {
                bolToSrcRecordDict.Add(record.BolNumber, new List<Invoice>() { record });
            }
            if (!record.ReleaseNum.IsSqlNullOrEmpty())
            {
                if (masterPoToSrcRecordDict.ContainsKey(record.ReleaseNum))
                {
                    masterPoToSrcRecordDict[record.ReleaseNum].Add(record);
                }
                else
                {
                    masterPoToSrcRecordDict.Add(record.ReleaseNum, new List<Invoice>() { record });
                }
            }
            if (!invNumtoInvUniqueKey.ContainsKey(record.InvoiceNumber))
            {
                invNumtoInvUniqueKey.Add(record.InvoiceNumber, record.NewUniqueKey);
            }
        }

        public void AddShipmentNumbers(List<string> shipNums)
        {
            shipNumCache.Add(shipNums);
        }

        public void SetAllPackTypes(string plType)
        {
            foreach (var invoice in srcRecords)
            {
                invoice.PackType = plType;
            }
        }

        public void RemoveSingleRecs()
        {
            foreach (var rec in masterPoToSrcRecordDict.Where(x => x.Value.Count <= 1).Select(x => x.Key).ToList())
            {
                masterPoToSrcRecordDict.Remove(rec);
            }
        }

        public void SetSelectedPackTypes()
        {
            foreach (var mergeRecord in request.MergeList)
            {
                foreach (var invoice in bolToSrcRecordDict[mergeRecord.BolNumber])
                {
                    invoice.PackType = mergeRecord.PackType;
                }
            }
        }

        public void Create810Recs(DateTime reqTime, User user, bool isMerge)
        {
            head810.Clear();
            detl810.Clear();
            int fileIdx = 0;
            string rptName = "trx810p.rpt";
            string rptDesc = "Invoice Report";
            if (isMerge)
            {
                invoiceCounter.SetPadding(masterPoToSrcRecordDict.Values.Count.ToString().Length);
                // Merge each thing in the thing, then move on...
                foreach (var mergeRecList in masterPoToSrcRecordDict)
                {
                    string newPONumber = mergeRecList.Key;
                    Invoice merged = Invoice.CreateMerged(newPONumber, mergeRecList.Value);
                    merged.InvoiceNumber = invoiceCounter.GetNextInvoiceNumber();
                    merged.InvReportName = $"{user.Customer}_810_{DateTime.Now.ToString("yyyyMMddHHmmss")}{++fileIdx}-report.pdf";
                    head810.AddRange(merged.ToHead810Record(user.Customer, ++fileIdx));
                    detl810.AddRange(merged.ToDetl810Record());
                    _CreateReportReq(reqTime, merged.NewUniqueKey, merged, "810", rptName, merged.InvReportName, rptDesc);
                }
                foreach (var rg in srcRecords)
                {
                    if (!masterPoToSrcRecordDict.ContainsKey(rg.ReleaseNum))
                    {
                        head810.AddRange(rg.ToHead810Record(user.Customer, ++fileIdx));
                        detl810.AddRange(rg.ToDetl810Record());
                        _CreateReportReq(reqTime, rg.NewUniqueKey, rg, "810", rptName, rg.InvReportName, rptDesc);
                    }
                }
            }
            else
            {
                foreach (var rg in srcRecords)
                {
                    head810.AddRange(rg.ToHead810Record(user.Customer, ++fileIdx));
                    detl810.AddRange(rg.ToDetl810Record());
                    _CreateReportReq(reqTime, rg.NewUniqueKey, rg, "810", rptName, rg.InvReportName, rptDesc);
                }
            }
        }

        public void Create856Recs(DateTime reqTime, bool isInvoicePresent)
        {
            if (request.IsAll == true)
            {
                foreach (var transferGroup in bolToSrcRecordDict.Values)
                {
                    _Create856(reqTime, transferGroup, request.AllPackType, request.AllCartType, isInvoicePresent);
                }
            }
            else
            {
                foreach (var req in request.MergeList)
                {
                    _Create856(reqTime, bolToSrcRecordDict[req.BolNumber], req.PackType, req.CartonDistType, isInvoicePresent);
                }
            }
        }

        public void ValidateSentASNs(DBConnect connection)
        {
            foreach (var rec in head856)
            {
                connection.Query(string.Format("SELECT ediproc.AssignCartonIds('{0}')", rec[0]));
            }
        }

        private void _Create856(DateTime reqTime, List<Invoice> srcRecs, string plType, string cartType, bool isInvoicePresent)
        {
            string head856UniqueKey = DBConnect.GenerateUniqueKey();

            var itemDict = _GetAsnItems(head856UniqueKey, srcRecs, cartType == _CartonDistType.Automatic);

            var cartonItems = _GetAsnCartons(cartType, head856UniqueKey, srcRecs, itemDict, isInvoicePresent);

            if (plType != _PackingListType.PrePacked)
            {
                foreach (var cart in cartonItems)
                {
                    cart856.Add(cart.ToInsertRecord());
                }
            }

            foreach (var rec in itemDict.Values)
            {
                detl856.Add(rec.ToInsertRecord());
            }

            _Create856Header(head856UniqueKey, srcRecs[0]);
            //_CreateReportReq(reqTime, head856UniqueKey, srcRecs[0]); /* NOT NEEDED ANYMORE NOW THAT PORTAL DOES IT */
        }

        private void _Create856Header(string sHeaderKey, Invoice srcRecord)
        {
            var headRec = new List<string>();
            foreach (var col in Common.colTrxH856)
            {
                switch (col)
                {
                    case _Column.UniqueKey:
                    case _Column.GroupKey:
                        headRec.Add(sHeaderKey);
                        break;

                    case "invoceno":
                        headRec.Add(srcRecord.InvoiceNumber);
                        break;

                    case _Column.TrxType:
                        headRec.Add(srcRecord.PackType);
                        break;

                    case _Column.ShipmentNumber:
                        headRec.Add(shipNumCache.GetNextShipNum());
                        break;
                    case _Column.HProcessed:
                        {
                            headRec.Add(""); //push in saved state
                            break;
                        }
                    default:
                        if (srcRecord.headRecord.ContainsKey(col))
                        {
                            headRec.Add(srcRecord.headRecord[col]);
                        }
                        else
                        {
                            headRec.Add(_SqlValue.Null);
                        }
                        break;
                }
            }
            head856.Add(headRec);
        }

        private void _CreateReportReq(DateTime reqTime, string sHeaderKey, Invoice srcRecord, string trxType, string rptName, string outputName, string description)
        {
            repoReq.Add(new List<string>()
            {
                DBConnect.GenerateUniqueKey(),
                trxType,
                reqTime.ToMySQLDateTimeStr(),
                srcRecord.Customer,
                srcRecord.Partner,
                currentUser.OCConnID.SQLEscape(),
                sHeaderKey,
                outputName,
                @"\ecgb\data\networks\" + currentUser.OCConnID.SQLEscape() + @"\outbox",
                rptName,
                "S",
                srcRecord.PONumber,
                currentUser.UserName.SQLEscape(),
                currentUser.Email.SQLEscape(),
                description
            });
        }

        private static Dictionary<string, ASNItem> _GetAsnItems(string headerUniqueKey, List<Invoice> invList, bool isAutomatic)
        {
            Dictionary<string, ASNItem> itemDict = new Dictionary<string, ASNItem>();
            bool isMerged = invList.Count > 1;
            foreach (var record in invList)
            {
                string poNum = record.PONumber;
                foreach (var item in record.detlRecords.Values)
                {
                    string itemID = poNum + "_" + GetItemID(item);
                    if (!itemDict.ContainsKey(itemID))
                    {
                        ASNItem newItem = new ASNItem(headerUniqueKey, record.NewUniqueKey, record.headRecord, item, isMerged);
                        itemDict.Add(itemID, newItem);
                    }
                    else
                    {
                        itemDict[itemID].UniqueInv = record.NewUniqueKey;
                    }
                    if (isAutomatic)
                    {
                        itemDict[itemID].IsAuto = true;
                    }
                }
            }
            return itemDict;
        }

        private static List<ASNCarton> _GetAsnCartons(string cartType, string headerUniqueKey, List<Invoice> invList, Dictionary<string, ASNItem> itemDict, bool isInvoicePresent)
        {
            List<ASNCarton> ret = new List<ASNCarton>();
            int currentCartonNumber = 1;
            int maxCartonNumber = 0;
            Dictionary<string, int> idToCartNum = new Dictionary<string, int>();
            foreach (var invoice in invList)
            {
                string poNum = invoice.PONumber;
                if (cartType == _CartonDistType.PerPO)
                {
                    if (idToCartNum.ContainsKey(poNum))
                    {
                        currentCartonNumber = idToCartNum[poNum];
                    }
                    else
                    {
                        currentCartonNumber = ++maxCartonNumber;
                        idToCartNum.Add(poNum, currentCartonNumber);
                    }
                }
                foreach (var item in invoice.detlRecords.Values)
                {
                    string itemID = poNum + "_" + GetItemID(item);
                    string storeID = poNum + "_" + invoice.Byid;
                    switch (cartType)
                    {
                        case _CartonDistType.Manual:
                            if (idToCartNum.ContainsKey(itemID))
                            {
                                currentCartonNumber = idToCartNum[itemID];
                            }
                            else
                            {
                                currentCartonNumber = maxCartonNumber + 1;
                                maxCartonNumber = currentCartonNumber;
                                idToCartNum.Add(itemID, currentCartonNumber);
                            }
                            break;

                        case _CartonDistType.PerPOStore:
                            if (idToCartNum.ContainsKey(storeID))
                            {
                                currentCartonNumber = idToCartNum[storeID];
                            }
                            else
                            {
                                currentCartonNumber = maxCartonNumber + 1;
                                maxCartonNumber = currentCartonNumber;
                                idToCartNum.Add(storeID, currentCartonNumber);
                            }
                            break;
                    }
                    int qty = 0;
                    if (!int.TryParse(item[_Column.Quantity], out qty))
                    {
                        qty = 0;
                    }
                    ASNCarton newCarton = new ASNCarton(headerUniqueKey, itemDict[itemID].UniqueItem, invoice, qty, currentCartonNumber, isInvoicePresent);
                    ret.Add(newCarton);
                    itemDict[itemID].ShipmentUnits += qty;
                    itemDict[itemID].OrderUnits += qty;
                    itemDict[itemID].BoxCount++;
                }
            }

            return ret;
        }

        private static string GetItemID(Dictionary<string, string> item)
        {
            var f = item[_Column.UPCNum];
            if (f.IsSqlNullOrEmpty())
            {
                f = item[_Column.EANNum];
            }
            if (f.IsSqlNullOrEmpty())
            {
                f = item[_Column.GTIN];
            }
            if (f.IsSqlNullOrEmpty())
            {
                f = item[_Column.BuyerNum];
            }
            if (f.IsSqlNullOrEmpty())
            {
                f = item[_Column.VendorNum];
            }
            return f;
        }
    }
}