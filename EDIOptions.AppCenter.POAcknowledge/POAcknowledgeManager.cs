using EDIOptions.AppCenter.Session;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Excel;
using System;

namespace EDIOptions.AppCenter.POAcknowledge
{
    public static class POAcknowledgeManager
    {
        public static ResponseType VerifyFile(User user, string xlFileName)
        {
            Application app = null;
            Workbook wb = null;
            Worksheet xlWS = null;
            try
            {
                app = new Application();
                app.AutomationSecurity = MsoAutomationSecurity.msoAutomationSecurityForceDisable;
                wb = app.Workbooks.Open(Filename: xlFileName, ReadOnly: true);
                if (wb.Worksheets.Count == 0)
                {
                    wb.Close();
                    return ResponseType.ErrorAPOUnknown;
                }
                xlWS = wb.Worksheets[1];
                ResponseType result = CheckSheet(user, xlWS);
                return result;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, "POAcknowledgeManager", "VerifyFile", e.Message);
                return ResponseType.ErrorAPOUnknown;
            }
            finally
            {
                if (app != null && wb != null)
                {
                    wb.Close();
                }
            }
        }

        private static ResponseType CheckSheet(User user, Worksheet xlWS)
        {
            try
            {
                switch (user.ActivePartner)
                {
                    case _Partner.Zales:
                        {
                            // A2 required.
                            // G2 required.
                            string PONum = Convert.ToString(xlWS.Range["A2"].Value2);
                            string StatusCode = Convert.ToString(xlWS.Range["G2"].Value2);
                            if (string.IsNullOrWhiteSpace(PONum))
                            {
                                return ResponseType.ErrorAPOInvalidPO;
                            }
                            if (!(StatusCode == "AK" || StatusCode == "RJ"))
                            {
                                return ResponseType.ErrorAPOInvalidStatusCode;
                            }
                            else
                            {
                                return ResponseType.SuccessAPO;
                            }
                        }
                    case _Partner.BIWorldwide:
                        {
                            // A2 required.
                            // For each line item, must be one of IA, AC, IB, IC, IR
                            // For IA, AC, IB, expected ship date is required.
                            // For IB, New Qty is required.
                            string PONum = Convert.ToString(xlWS.Range["A2"].Value2);
                            string lineCountStr = Convert.ToString(xlWS.Range["F2"].Value2);
                            int colStatusCode = 11;
                            int colExpectedShipDate = 12;
                            int colNewQty = 13;
                            int rowLineItemStart = 5;
                            if (string.IsNullOrWhiteSpace(PONum))
                            {
                                return ResponseType.ErrorAPOInvalidPO;
                            }
                            int lineCount;
                            if (!int.TryParse(lineCountStr, out lineCount) || lineCount <= 0)
                            {
                                return ResponseType.ErrorAPOInvalidLineCount;
                            }
                            else
                            {
                                for (int i = 0; i < lineCount; i++)
                                {
                                    string statusCode = Convert.ToString(xlWS.Cells[rowLineItemStart + i, colStatusCode].Value2);
                                    string exShipDate = Convert.ToString(xlWS.Cells[rowLineItemStart + i, colExpectedShipDate].Value);
                                    string newQty = Convert.ToString(xlWS.Cells[rowLineItemStart + i, colNewQty].Value);
                                    switch (statusCode.ToUpper())
                                    {
                                        case "IA":
                                        case "AC":
                                            {
                                                if (string.IsNullOrWhiteSpace(exShipDate))
                                                {
                                                    return ResponseType.ErrorAPOInvalidShipDate;
                                                }
                                            }
                                            break;

                                        case "IB":
                                            {
                                                if (string.IsNullOrWhiteSpace(exShipDate))
                                                {
                                                    return ResponseType.ErrorAPOInvalidShipDate;
                                                }
                                                else if (string.IsNullOrWhiteSpace(newQty))
                                                {
                                                    return ResponseType.ErrorAPOInvalidNewQty;
                                                }
                                            }
                                            break;

                                        case "IC":
                                        case "IR":
                                            break;

                                        default:
                                            return ResponseType.ErrorAPOInvalidStatusCode;
                                    }
                                }
                                return ResponseType.SuccessAPO;
                            }
                        }
                    case _Partner.IndigoBooks:
                        {
                            // A2 required.
                            // For each line item, must be one of IA, DR, IB, IR, IQ
                            // For DR, new delivery date is required.
                            // For IQ and IB, New Qty is required.
                            string PONum = Convert.ToString(xlWS.Range["A2"].Value2);
                            string lineCountStr = Convert.ToString(xlWS.Range["F2"].Value2);
                            int colStatusCode = 11;
                            int colNewDelivDate = 12;
                            int colNewQty = 13;
                            int rowLineItemStart = 5;
                            if (string.IsNullOrWhiteSpace(PONum))
                            {
                                return ResponseType.ErrorAPOInvalidPO;
                            }
                            int lineCount;
                            if (!int.TryParse(lineCountStr, out lineCount) || lineCount <= 0)
                            {
                                return ResponseType.ErrorAPOInvalidLineCount;
                            }
                            else
                            {
                                for (int i = 0; i < lineCount; i++)
                                {
                                    string statusCode = Convert.ToString(xlWS.Cells[rowLineItemStart + i, colStatusCode].Value2);
                                    string newDeliveryDate = Convert.ToString(xlWS.Cells[rowLineItemStart + i, colNewDelivDate].Value);
                                    string newQty = Convert.ToString(xlWS.Cells[rowLineItemStart + i, colNewQty].Value);
                                    switch (statusCode.ToUpper())
                                    {
                                        case "DR":
                                            {
                                                if (string.IsNullOrWhiteSpace(newDeliveryDate))
                                                {
                                                    return ResponseType.ErrorAPOInvalidDelivDate;
                                                }
                                            }
                                            break;

                                        case "IQ":
                                        case "IB":
                                            {
                                                if (string.IsNullOrWhiteSpace(newQty))
                                                {
                                                    return ResponseType.ErrorAPOInvalidNewQty;
                                                }
                                            }
                                            break;

                                        case "IA":
                                        case "IR":
                                            break;

                                        default:
                                            return ResponseType.ErrorAPOInvalidStatusCode;
                                    }
                                }
                                return ResponseType.SuccessAPO;
                            }
                        }
                    case _Partner.Walmart:
                        {
                            // A2 required.
                            // For each line item, must be one of AR, IA, IR
                            // For IR, description must be one of "bad_sku", "merchant_request", "out_of_stock", "discontinued"
                            string PONum = Convert.ToString(xlWS.Range["A2"].Value2);
                            string lineCountStr = Convert.ToString(xlWS.Range["F2"].Value2);
                            int colStatusCode = 9;
                            int colStatusDesc = 10;
                            int rowLineItemStart = 5;
                            if (string.IsNullOrWhiteSpace(PONum))
                            {
                                return ResponseType.ErrorAPOInvalidPO;
                            }
                            int lineCount;
                            if (!int.TryParse(lineCountStr, out lineCount) || lineCount <= 0)
                            {
                                return ResponseType.ErrorAPOInvalidLineCount;
                            }
                            else
                            {
                                for (int i = 0; i < lineCount; i++)
                                {
                                    string statusCode = Convert.ToString(xlWS.Cells[rowLineItemStart + i, colStatusCode].Value2);
                                    string statusDesc = Convert.ToString(xlWS.Cells[rowLineItemStart + i, colStatusDesc].Value2);
                                    switch (statusCode.ToUpper())
                                    {
                                        case "AR":
                                        case "IA":
                                            break;

                                        case "IR":
                                            {
                                                if (string.IsNullOrWhiteSpace(statusDesc))
                                                {
                                                    return ResponseType.ErrorAPOInvalidDelivDate;
                                                }
                                                else
                                                {
                                                    switch (statusDesc)
                                                    {
                                                        case "bad_sku":
                                                        case "merchant_request":
                                                        case "out_of_stock":
                                                        case "discontinued":
                                                            break;

                                                        default:
                                                            {
                                                                return ResponseType.ErrorAPOInvalidStatusDesc;
                                                            }
                                                    }
                                                }
                                            }
                                            break;

                                        default:
                                            return ResponseType.ErrorAPOInvalidStatusCode;
                                    }
                                }
                                return ResponseType.SuccessAPO;
                            }
                        }
                    default:
                        {
                            return ResponseType.WarningAPOUnverifiedAccept;
                        }
                }
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, "POAcknowledgeManager", "CheckSheet", e.Message);
                return ResponseType.ErrorAPOUnknown;
            }
        }
    }
}