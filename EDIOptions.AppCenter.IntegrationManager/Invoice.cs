using EDIOptions.AppCenter.Database;
using System.Collections.Generic;
using System.Linq;
using System;

namespace EDIOptions.AppCenter.IntegrationManager
{
    public class Invoice
    {
        public string NewUniqueKey { get; private set; }
        public string InvReportName { get; set; }

        public string OriginalUniqueKey
        {
            get
            {
                return headRecord[_Column.UniqueKey];
            }
        }

        public string PONumber
        {
            get
            {
                return headRecord[_Column.PONumber];
            }
        }

        public string InvoiceNumber
        {
            get
            {
                return headRecord[_Column.InvoiceNo];
            }
            set
            {
                headRecord[_Column.InvoiceNo] = value;
                foreach (var rec in detlRecords.Values)
                {
                    rec[_Column.InvoiceNo] = value;
                }
            }
        }

        public string ReleaseNum
        {
            get
            {
                return headRecord[_Column.ReleaseNum];
            }
        }

        public string BolNumber
        {
            get
            {
                return headRecord[_Column.BOLNumber];
            }
        }

        public string Customer
        {
            get
            {
                return headRecord[_Column.Customer];
            }
        }

        public string Partner
        {
            get
            {
                return headRecord[_Column.Partner];
            }
        }

        public string Byid
        {
            get
            {
                return headRecord[_Column.BYId];
            }
        }

        public string HProcessed
        {
            get
            {
                return headRecord[_Column.HProcessed];
            }
        }

        public string ScacCode
        {
            get
            {
                return headRecord[_Column.SCACCode];
            }
        }

        public string PackType
        {
            get
            {
                return headRecord[_Column.PLType];
            }
            set
            {
                headRecord[_Column.PLType] = value;
            }
        }

        public string ShipDate
        {
            get
            {
                return headRecord[_Column.ShipmentDate];
            }
        }

        public Dictionary<string, string> headRecord = new Dictionary<string, string>();
        public Dictionary<string, Dictionary<string, string>> detlRecords = new Dictionary<string, Dictionary<string, string>>();

        public Dictionary<string, List<Distribution>> DistInfo = new Dictionary<string, List<Distribution>>();

        public Invoice()
        {
            NewUniqueKey = DBConnect.GenerateUniqueKey();
            InvReportName = "";
        }

        public static Invoice CreateMerged(string masterPONumber, List<Invoice> invoiceList)
        {
            Dictionary<string, string> header = new Dictionary<string, string>();
            foreach (var column in invoiceList[0].headRecord)
            {
                header.Add(column.Key, column.Value);
            }

            header[_Column.PONumber] = masterPONumber;

            Dictionary<string, Dictionary<string, string>> itemIdToItemRec = new Dictionary<string, Dictionary<string, string>>();
            foreach (var item in invoiceList.SelectMany(x => x.detlRecords.Values))
            {
                var id = GetItemID(item);
                if (itemIdToItemRec.ContainsKey(id))
                {
                    int oldQuantity = 0;
                    int newQuantity = 0;
                    if (!int.TryParse(itemIdToItemRec[id][_Column.Quantity], out oldQuantity))
                    {
                        oldQuantity = 0;
                    }

                    if (!int.TryParse(item[_Column.Quantity], out newQuantity))
                    {
                        newQuantity = 0;
                    }
                    itemIdToItemRec[id][_Column.Quantity] = (oldQuantity + newQuantity).ToString();
                }
                else
                {
                    itemIdToItemRec.Add(id, item);
                }
            }

            Invoice ret = new Invoice();
            ret.AddHeader(header);
            foreach (var item in itemIdToItemRec.Values)
            {
                ret.AddDetail(item);
            }

            ret.FixQty();

            return ret;
        }

        public void AddHeaderRecord(DBResult queryHead)
        {
            headRecord.Clear();
            foreach (var field in queryHead.FieldNames)
            {
                headRecord.Add(field.ToLower(), queryHead.FieldByName(field, _SqlValue.Null).SQLEscape().Trim());
            }
        }

        public void AddDetailRecord(DBResult queryDetl)
        {
            Dictionary<string, string> detlRecord = new Dictionary<string, string>();
            foreach (var field in queryDetl.FieldNames)
            {
                detlRecord.Add(field.ToLower(), queryDetl.FieldByName(field, _SqlValue.Null).SQLEscape().Trim());
            }
            detlRecords.Add(detlRecord[_Column.UniqueItem], detlRecord);
        }

        public void AddCartonRecord(DBResult queryCart)
        {
            string byid = queryCart.FieldByName(_Column.BYId);
            if (DistInfo.ContainsKey(byid))
            {
                DistInfo[byid].Add(new Distribution(queryCart.FieldByName(_Column.UniqueItem), queryCart.FieldByName(_Column.CartonQuantity)));
            }
            else
            {
                DistInfo.Add(byid, new List<Distribution>() { new Distribution(queryCart.FieldByName(_Column.UniqueItem), queryCart.FieldByName(_Column.CartonQuantity)) });
            }
        }

        public List<Invoice> Split()
        {
            List<Invoice> ret = new List<Invoice>();
            if (DistInfo.Count <= 0)
            {
                ret.Add(this);
            }
            else
            {
                // For each item in the dist, create a new record
                foreach (var kvp in DistInfo)
                {
                    Invoice tempIV = new Invoice();

                    // First, copy the header
                    Dictionary<string, string> header = new Dictionary<string, string>();
                    foreach (var column in headRecord)
                    {
                        header.Add(column.Key, column.Value);
                    }
                    // Then, generate new key, and change byid and invnum

                    header[_Column.UniqueKey] = DBConnect.GenerateUniqueKey();
                    header[_Column.BYId] = kvp.Key;
                    header[_Column.InvoiceNo] = header[_Column.InvoiceNo] + "-" + kvp.Key;
                    tempIV.AddHeader(header);

                    // Then, for each item in the dist...
                    foreach (var item in kvp.Value)
                    {
                        // Lookup the item, and copy it.
                        Dictionary<string, string> itemDetl = new Dictionary<string, string>();
                        foreach (var column in detlRecords[item.ItemKey])
                        {
                            itemDetl.Add(column.Key, column.Value);
                        }
                        // Change the uniquekey,uniqueitem,quantity

                        itemDetl[_Column.UniqueKey] = tempIV.OriginalUniqueKey;
                        itemDetl[_Column.UniqueItem] = DBConnect.GenerateUniqueKey();
                        itemDetl[_Column.Quantity] = item.Quantity;
                        tempIV.AddDetail(itemDetl);
                    }

                    // After, adjust totamount and qty
                    tempIV.FixQty();

                    ret.Add(tempIV);
                }
            }
            return ret;
        }

        private void AddHeader(Dictionary<string, string> head)
        {
            headRecord = head;
        }

        private void AddDetail(Dictionary<string, string> detl)
        {
            detlRecords.Add(detl[_Column.UniqueItem], detl);
        }

        private void FixQty()
        {
            int totItems = detlRecords.Count;
            decimal totAmount = 0;
            foreach (var f in detlRecords.Values)
            {
                int quantity = 0;
                decimal unitPrice = 0;
                if (!decimal.TryParse(f[_Column.UnitPrice], out unitPrice))
                {
                    unitPrice = 0;
                }
                if (!int.TryParse(f[_Column.Quantity], out quantity))
                {
                    quantity = 0;
                }
                totAmount += unitPrice * quantity;
            }
            headRecord[_Column.TotalItems] = totItems.ToString();
            headRecord[_Column.TotAmount] = totAmount.ToString("F2");
        }

        public List<List<string>> ToHead810Record(string customer, int fileIdx)
        {
            InvReportName = $"{customer}_810_{DateTime.Now.ToString("yyyyMMddHHmmss")}{++fileIdx}-report.pdf";
            List<List<string>> retList = new List<List<string>>();
            List<string> ret = new List<string>();
            foreach (var col in Common.colTrxH810)
            {
                switch (col)
                {
                    case _Column.UniqueKey:
                    case _Column.GroupKey:
                        ret.Add(NewUniqueKey);
                        break;
                    case _Column.RptFile:
                        ret.Add(InvReportName);
                        break;
                    default:
                        if (headRecord.ContainsKey(col))
                        {
                            ret.Add(headRecord[col]);
                        }
                        else
                        {
                            ret.Add(_SqlValue.Null);
                        }
                        break;
                }
            }
            retList.Add(ret);
            return retList;
        }

        public List<List<string>> ToDetl810Record()
        {
            List<List<string>> ret = new List<List<string>>();
            foreach (var record in detlRecords.Values)
            {
                List<string> rec = new List<string>();
                foreach (var column in Common.colTrxD810)
                {
                    switch (column)
                    {
                        case _Column.UniqueKey:
                            rec.Add(NewUniqueKey);
                            break;

                        case _Column.OriginalPrice:
                            rec.Add(record[_Column.UnitPrice]);
                            break;

                        case _Column.POQuantity:
                            rec.Add(record[_Column.Quantity]);
                            break;

                        default:
                            if (record.ContainsKey(column))
                            {
                                rec.Add(record[column]);
                            }
                            else
                            {
                                rec.Add(_SqlValue.Null);
                            }
                            break;
                    }
                }
                ret.Add(rec);
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