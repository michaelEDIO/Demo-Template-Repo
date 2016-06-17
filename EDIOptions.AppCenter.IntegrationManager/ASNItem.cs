using EDIOptions.AppCenter.Database;
using System;
using System.Collections.Generic;

namespace EDIOptions.AppCenter.IntegrationManager
{
    public class ASNItem
    {
        public string UniqueInv = _SqlValue.Null;

        public string Customer = _SqlValue.Null;
        public string Partner = _SqlValue.Null;

        public string UniqueKey = _SqlValue.Null;
        public string UniqueItem = _SqlValue.Null;
        public string UPCNumber = _SqlValue.Null;
        public string VendorID = _SqlValue.Null;
        public string VendorNumber = _SqlValue.Null;
        public string BuyerNumber = _SqlValue.Null;
        public int PackSize = 0;
        public int InPackSize = 0;

        public string POLine = _SqlValue.Null;

        public string PODate = _SqlValue.Null;
        public string PONumber = _SqlValue.Null;
        public string MerchType = _SqlValue.Null;
        public string CustOrder = _SqlValue.Null;
        public string Department = _SqlValue.Null;
        public string OrderUM = _SqlValue.Null;
        public string ShipmentUM = _SqlValue.Null;

        public int BoxCount = 0;
        public int OrderUnits = 0;
        public int ShipmentUnits = 0;

        public string InvoiceNumber = _SqlValue.Null;
        public string BYId = _SqlValue.Null;
        public string BYName = _SqlValue.Null;
        public string BYName1 = _SqlValue.Null;
        public string BYName2 = _SqlValue.Null;
        public string BYAddr = _SqlValue.Null;
        public string BYAddr1 = _SqlValue.Null;
        public string BYAddr2 = _SqlValue.Null;
        public string BYCity = _SqlValue.Null;
        public string BYState = _SqlValue.Null;
        public string BYZip = _SqlValue.Null;
        public string BYCountry = _SqlValue.Null;

        public bool IsAuto = false;

        public ASNItem(string uniqueKey, string uniqueInv, Dictionary<string, string> header, Dictionary<string, string> detail, bool isMerged)
        {
            UniqueKey = uniqueKey;
            UniqueItem = DBConnect.GenerateUniqueKey();
            UniqueInv = uniqueInv;

            UPCNumber = detail[_Column.UPCNum];
            VendorID = header[_Column.VendID];
            VendorNumber = detail[_Column.VendorNum];
            BuyerNumber = detail[_Column.BuyerNum];
            PackSize = int.Parse(detail[_Column.PackSize]);
            InPackSize = int.Parse(detail[_Column.InPackSize]);

            PODate = header[_Column.PODate];
            PONumber = header[_Column.PONumber];
            POLine = detail[_Column.POLine];
            OrderUM = detail[_Column.UnitMeasure];
            ShipmentUM = detail[_Column.UnitMeasure];

            MerchType = header[_Column.MerchType];
            CustOrder = header[_Column.CustOrder];
            Department = header[_Column.Department];

            Customer = header[_Column.Customer];
            Partner = header[_Column.Partner];

            BYId = header[_Column.BYId];
            BYName = header[_Column.BYName];
            BYName1 = header[_Column.BYName1];
            BYName2 = header[_Column.BYName2];
            BYAddr = header[_Column.BYAddr];
            BYAddr1 = header[_Column.BYAddr1];
            BYAddr2 = header[_Column.BYAddr2];
            BYCity = header[_Column.BYCity];
            BYState = header[_Column.BYState];
            BYZip = header[_Column.BYZip];
            BYCountry = header[_Column.BYCountry];

            if (!isMerged)
            {
                InvoiceNumber = header[_Column.InvoiceNo];
            }
        }

        public List<string> ToInsertRecord()
        {
            if (IsAuto)
            {
                int boxCount = 0;
                if (PackSize != 0)
                {
                    boxCount = (int)Math.Ceiling((double)ShipmentUnits / PackSize);
                }
                BoxCount = boxCount;
            }

            return new List<string>()
            {
                UniqueKey,
                UniqueItem,
                BYAddr,
                BYAddr1,
                BYAddr2,
                BYCity,
                BYCountry,
                BYId,
                BYName,
                BYName1,
                BYName2,
                BYState,
                BYZip,
                BoxCount.ToString(),
                OrderUM.ToString(),
                ShipmentUM.ToString(),
                UPCNumber,
                VendorID,
                VendorNumber,
                BuyerNumber,
                InPackSize.ToString(),
                PackSize.ToString(),
                POLine.ToString(),
                OrderUnits.ToString(),
                ShipmentUnits.ToString(),
                Customer,
                Partner,
                PODate,
                InvoiceNumber,
                PONumber,
                MerchType,
                CustOrder,
                Department,
                _ProgressFlag.Unprocessed,
                UniqueInv
            };
        }
    }
}