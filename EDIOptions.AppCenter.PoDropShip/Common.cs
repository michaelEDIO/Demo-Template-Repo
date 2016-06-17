using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EDIOptions.AppCenter.PoDropShip
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
    }
}
