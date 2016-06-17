using EDIOptions.AppCenter.Database;
using EDIOptions.AppCenter.Security;
using EDIOptions.AppCenter.Session;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Excel;
using System;
using System.IO;

namespace EDIOptions.AppCenter.GVMCostReport
{
    public static class GVMReport
    {
        public static string GenerateReport(User user, DateTime startDate, DateTime endDate)
        {
            try
            {
                DBConnect connection = ConnectionsMgr.GetSharedConnection(user, _Database.ECGB);
                Application app = new Application();
                app.AutomationSecurity = MsoAutomationSecurity.msoAutomationSecurityForceDisable;
                Workbook xlWB = app.Workbooks.Add();
                Worksheet ws = xlWB.Worksheets.Add();
                app.ActiveWindow.SplitRow = 1;
                app.ActiveWindow.FreezePanes = true;

                //string queryReport = "SELECT sum(d.quantity) AS PidQty,d.unitprice AS GLC,d.chgprice AS MilCost,d.retailprc AS MilRetail,d.vendornum AS PID,trim(h.ponumber) AS contract,trim(h.custorder) AS PO,h.department AS Dept,h.deptname AS Brand,h.arrivdate AS INDCDate" +
                //    " FROM gdetl855 AS d JOIN ghead855 AS h ON d.uniquekey=h.uniquekey WHERE arrivdate BETWEEN '{0}' AND '{1}' GROUP BY d.vendornum,h.ponumber ORDER BY h.ponumber,d.upcnum";
                string queryReport = "SELECT sum(d.quantity) AS PidQty,d.unitprice AS GLC,d.chgprice AS MilCost,d.retailprc AS MilRetail,d.vendornum AS PID,c.COLORCODE, c.ITEMCOLOR,trim(h.ponumber) AS contract,trim(h.custorder) AS PO,h.department AS Dept,h.deptname AS Brand,h.arrivdate AS INDCDate" +
                    " FROM gdetl855 AS d JOIN ghead855 AS h ON d.uniquekey=h.uniquekey "+
                    " LEFT join catinfo c on d.VENDORNUM=c.VENDORNUM and d.UPCNUM = c.UPCNUM and h.PONUMBER = c.PONUMBER "+
                    " WHERE h.arrivdate BETWEEN '{0}' AND '{1}' "+
                    //" GROUP BY d.vendornum,h.ponumber ORDER BY h.ponumber,d.upcnum";
                    " GROUP BY h.ponumber,d.vendornum,c.colorcode ORDER BY h.ponumber,d.vendornum,c.colorcode";
                string formatReport = string.Format(queryReport, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
                DBResult resultReport = connection.Query(formatReport);

                char colLastLetter = (char)('A' + resultReport.FieldCount - 1);
                string rowLastIndex = (2 + resultReport.AffectedRows - 1).ToString();

                object[,] reportData = new object[resultReport.AffectedRows, resultReport.FieldCount];
                for (int rowIndex = 0; rowIndex < resultReport.AffectedRows; rowIndex++)
                {
                    resultReport.Read();
                    for (int columnIndex = 0; columnIndex < resultReport.FieldCount; columnIndex++)
                    {
                        reportData[rowIndex, columnIndex] = resultReport.Field2(columnIndex, "'---");
                    }
                }

                var rangeHead = ws.Range["A1", colLastLetter + "1"];
                var rangeBody = ws.Range["A2", colLastLetter + rowLastIndex];
                rangeHead.Value = new[] { "PidQty", "GLC", "MilCost", "MilRetail", "PID", "Color Code", "Color Description", "Contract", "PO #", "Dept.", "Brand", "INDCDate" };
                rangeBody.Value = reportData;
                ws.Columns.AutoFit();
                string diFileName = SiteFileSystem.GetTempFileName();
                xlWB.SaveCopyAs(diFileName);
                xlWB.Close(false);
                string eoToken = Crypt.EncryptFileToFile(user, diFileName);
                File.Delete(diFileName);
                return eoToken;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, "GVMReport", "GenerateReport", e.Message);
                return "";
            }
        }
    }
}