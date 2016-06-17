using System.Collections.Generic;

namespace EDIOptions.AppCenter.IntegrationManager
{
    public static class Common
    {
        public static List<string> colReportReq = new List<string>()
        {
            _Column.UniqueKey,
            _Column.TrxType,
            _Column.RequestDate,
            _Column.Customer,
            _Column.Partner,
            _Column.ConnectID,
            _Column.PrintKey,
            _Column.OutputName,
            _Column.OutputPath,
            _Column.ReportFormat,
            _Column.Processed,
            _Column.PONumber,
            _Column.UserName,
            _Column.UserEmail,
            _Column.FileDesc
        };

        public static List<string> colIntH810 = new List<string>()
        {
            _Column.UniqueKey,
            _Column.InvoiceNo,
            _Column.TrxDate,
            _Column.ShipmentDate,
            _Column.PONumber,
            _Column.BOLNumber,
            _Column.ReleaseNum,
            _Column.SCACCode,
            _Column.Routing,
            _Column.STId,
            _Column.BYId,
            _Column.Msg,
            _Column.HProcessed,
            _Column.XferType
        };

        public static List<string> colTrxH810 = new List<string>()
        {
            "acctnum",
            _Column.BOLNumber,
            _Column.BTAddr,
            _Column.BTAddr1,
            _Column.BTAddr2,
            _Column.BTCity,
            _Column.BTCountry,
            _Column.BTId,
            _Column.BTName,
            _Column.BTName1,
            _Column.BTName2,
            "btstate",
            "btzip",
            "byaddr",
            "byaddr1",
            "byaddr2",
            "bycity",
            "bycountry",
            "byid",
            "byname",
            "byname1",
            "byname2",
            "bystate",
            "byzip",
            "curcode",
            "customer",
            "daysdue",
            "discamt",
            "discdate",
            "discdays",
            "discountpct",
            "duedate",
            "groupkey",
            "hprocessed",
            "invdate",
            _Column.InvoiceNo,
            "invtype",
            "markid",
            "merchtype",
            "partner",
            "paymethod",
            "podate",
            "ponumber",
            "readdr",
            "readdr1",
            "readdr2",
            "recity",
            "recountry",
            "reid",
            "rename",
            "rename1",
            "rename2",
            "restate",
            "rezip",
            "rptfile",
            "scaccode",
            "senddate",
            "shipdate",
            "sizechrg",
            "staddr",
            "staddr1",
            "staddr2",
            "stcity",
            "stcountry",
            "stid",
            "stname",
            "stname1",
            "stname2",
            "ststate",
            "stzip",
            "termsbasis",
            "termsdesc",
            "termsdisc",
            "termstype",
            "timestamp",
            "totamount",
            "totfreight",
            "totitems",
            "trxdate",
            _Column.UniqueKey,
            "vendid",
            "vendorname"
        };

        public static List<string> colTrxD810 = new List<string>()
        {
            "buyernum",
            "customer",
            "invline",
            _Column.InvoiceNo,
            "itemdesc",
            "origprice",
            "partner",
            "poline",
            "poqty",
            "processed",
            "qtyds",
            "quantity",
            "trxdate",
            _Column.UniqueItem,
            _Column.UniqueKey,
            "unitmeas",
            "unitprice",
            _Column.UPCNum,
            "vendornum",
        };

        public static List<string> colTrxH856 = new List<string>()
        {
            _Column.UniqueKey,
            _Column.BOLNumber,
            "btaddr",
            "btaddr1",
            "btaddr2",
            "btcity",
            "btcountry",
            "btid",
            "btname",
            "btname1",
            "btname2",
            "btstate",
            "btzip",
            "byaddr",
            "byaddr1",
            "byaddr2",
            "bycity",
            "bycountry",
            "byid",
            "byname",
            "byname1",
            "byname2",
            "bystate",
            "byzip",
            "cartons",
            "combinekey",
            "ctntype",
            "customer",
            "delivdate",
            "groupkey",
            "hprocessed",
            "invoceno",
            "lbldownload",
            "lblfile",
            "markid",
            "partner",
            "paymethod",
            "routing",
            "rptfile",
            "scaccode",
            "senddate",
            "shipdate",
            "shipnum",
            "shipweight",
            "shipwgtum",
            "staddr",
            "staddr1",
            "staddr2",
            "stcity",
            "stid",
            "stname",
            "stname1",
            "stname2",
            "ststate",
            "stzip",
            "timestamp",
            "transport",
            "trxdate",
            "trxtype"
        };

        public static List<string> colTrxD856 = new List<string>()
        {
            // From header
            _Column.UniqueKey,

            // generated
            _Column.UniqueItem,

            // store info - don't fill for distributed
            _Column.BYAddr,
            _Column.BYAddr1,
            _Column.BYAddr2,
            _Column.BYCity,
            _Column.BYCountry,
            _Column.BYId,
            _Column.BYName,
            _Column.BYName1,
            _Column.BYName2,
            _Column.BYState,
            _Column.BYZip,

            // Fill after trxc
            _Column.BoxCount, // boxes per item
            _Column.OrderUnitMeasure,// sum per d item
            _Column.ShipmentUnitMeasure,// sum per d item

            // item identifying info + shared info
            _Column.UPCNum,
            _Column.VendID,
            _Column.VendorNum,
            _Column.BuyerNum,
            _Column.InPackSize,
            _Column.PackSize,
            _Column.POLine,
            _Column.OrderUnits,
            _Column.ShipmentUnits,

            // fill in from user
            _Column.Customer,
            _Column.Partner,

            // per single item in d
            _Column.PODate,

            // only on C
            _Column.InvoiceNo,

            // header
            _Column.PONumber,
            _Column.MerchType,
            _Column.CustOrder,
            _Column.Department,

            // "S"
            _Column.Processed,

            // 810
            _Column.UniqueInv
        };

        public static List<string> colTrxC856 = new List<string>()
        {
            _Column.UniqueKey,
            _Column.UniqueItem,
            _Column.UniqueCtn,
            _Column.CartonQuantity,
            _Column.BoxNum,
            _Column.BYId,
            _Column.UniqueInv,
            _Column.InvoiceNo,
            _Column.Customer,
            _Column.Partner,
        };
    }
}