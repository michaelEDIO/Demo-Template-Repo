using EDIOptions.AppCenter.Database;
using System.Collections.Generic;

namespace EDIOptions.AppCenter.IntegrationManager
{
    public class ASNCarton
    {
        public string UniqueKey = _SqlValue.Null;
        public string UniqueItem = _SqlValue.Null;
        public string UniqueCtn = _SqlValue.Null;
        public string InvoiceNo = _SqlValue.Null;
        public string UniqueInv = _SqlValue.Null;

        public int CtnQty = 0;
        public int Boxnum = 0;
        public string BYId = _SqlValue.Null;
        public string Customer = _SqlValue.Null;
        public string Partner = _SqlValue.Null;

        public ASNCarton(string uniqueKey, string uniqueItem, Invoice invoice, int ctnqty, int boxnum, bool isInvoicePresent)
        {
            UniqueKey = uniqueKey;
            UniqueItem = uniqueItem;
            UniqueCtn = DBConnect.GenerateUniqueKey();
            if (isInvoicePresent)
            {
                UniqueInv = invoice.NewUniqueKey;
            }

            InvoiceNo = invoice.InvoiceNumber;
            CtnQty = ctnqty;
            Boxnum = boxnum;
            BYId = invoice.Byid;
            Customer = invoice.Customer;
            Partner = invoice.Partner;
        }

        public List<string> ToInsertRecord()
        {
            return new List<string>()
            {
                UniqueKey,
                UniqueItem,
                UniqueCtn,
                CtnQty.ToString(),
                Boxnum.ToString(),
                BYId.SQLEscape(),
                UniqueInv.SQLEscape(),
                InvoiceNo.SQLEscape(),
                Customer.SQLEscape(),
                Partner.SQLEscape(),
            };
        }
    }
}