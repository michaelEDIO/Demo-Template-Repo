using EDIOptions.AppCenter;
using EDIOptions.AppCenter.IntegrationManager;
using EDIOptions.AppCenter.Session;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;

namespace EDIOptCenterNet
{
    public partial class IntegrationManager : System.Web.UI.Page
    {
        protected delegate void pageLoadDelegate();

        protected pageLoadDelegate onPageLoad;

        protected void Page_Load(object sender, EventArgs e)
        {
            SiteMaster.SetAppID(AppID.IntegrationManager);
        }

        private static TableState CreateState(FilterInfo filter, bool hasConsolidated)
        {
            TableState tState = new TableState();
            tState.Filter = filter;

            if (hasConsolidated)
            {
                tState.Columns.AddRange(new[]{
                    new ColumnInfo("Invoice #", "invoiceno", true, true),
                    new ColumnInfo("Trx Date", "trxdate", true, true),
                    new ColumnInfo("Ship Date", "shipdate", true, true),
                    new ColumnInfo("Master PO #", "releasenum", true, true),
                    new ColumnInfo("PO #", "ponumber", true, true),
                    new ColumnInfo("BOL #", "bolnumber", true, true),
                    new ColumnInfo("SCAC", "scaccode", true, true),
                    new ColumnInfo("Type", "xfertype", false, true),
                    new ColumnInfo("Status", "hprocessed", false, false),
                    new ColumnInfo("Errors", "msg", false, false)
                });
            }
            else
            {
                tState.Columns.AddRange(new[]{
                    new ColumnInfo("Invoice #", "invoiceno", true, true),
                    new ColumnInfo("Trx Date", "trxdate", true, true),
                    new ColumnInfo("Ship Date", "shipdate", true, true),
                    new ColumnInfo("PO #", "ponumber", true, true),
                    new ColumnInfo("BOL #", "bolnumber", true, true),
                    new ColumnInfo("SCAC", "scaccode", true, true),
                    new ColumnInfo("Type", "xfertype", false, true),
                    new ColumnInfo("Status", "hprocessed", false, false),
                    new ColumnInfo("Errors", "msg", false, false)
                });
            }

            return tState;
        }

        private static FilterInfo CreateFilter()
        {
            FilterInfo filter = new FilterInfo();

            filter.CurrentPage = 1;
            filter.MaxPage = 1;

            RefineInfo refineTrxDate = RefineInfo.CreateDateRefine("Transaction Date", "trxdate");

            RefineInfo refineStatus = new RefineInfo();
            refineStatus.Header = "Status";
            refineStatus.Column = "hprocessed";
            refineStatus.SelectOptions.AddRange(new[]{
                "Pending & Errors",
                "Pending",
                "Processed",
                "Error",
                "All"
            });

            RefineInfo refineInvType = new RefineInfo();
            refineInvType.Header = "Invoice Type";
            refineInvType.Column = "xfertype";
            refineInvType.SelectOptions.AddRange(new[]
            {
                "Any",
                "Distributed",
                "Mixed",
                "Prepacked",
                "Consolidated"
            });
            refineInvType.SelectedIndex = 0;

            filter.Refinements.AddRange(new[]{
                refineTrxDate,
                refineStatus,
                refineInvType
            });

            return filter;
        }

        [WebMethod]
        public static string InitPendingList(string version, string data)
        {
            if (SiteMaster.VerifyRequest(HttpContext.Current.Session))
            {
                var user = HttpContext.Current.Session[SKeys.User] as User;
                var opt = IntegrationHandler.GetOptions(user);
                var initialFilter = CreateFilter();
                var tData = IntegrationHandler.GetCurrentInvoices(user, initialFilter);
                var hasConsolidated = tData.Any(x => x.TransferType == "C");

                TableState tState = CreateState(initialFilter, hasConsolidated);

                var cOpt = IntegrationHandler.GetCarrierOptions(user);
                return ApiResponse.JSONSuccess(new
                {
                    TableState = tState,
                    TableData = tData,
                    Options = new
                    {
                        Date = DateTime.Now.ToString("MMM dd yyyy"),
                        Invoice = new
                        {
                            Enabled = opt.IsInvoiceEnabled
                        },
                        Packing = new
                        {
                            Enabled = opt.IsPackingEnabled,
                            IsPP = opt.IsPPEnabled,
                            IsMX = opt.IsMXEnabled,
                            IsDS = opt.IsDSEnabled,
                            Default = opt.BoxOption
                        },
                        Shipping = new
                        {
                            Carriers = cOpt
                        }
                    }
                });
            }
            else
            {
                return ApiResponse.JSONError(ResponseType.ErrorAuth);
            }
        }

        [WebMethod]
        public static string GetPendingList(string version, string data)
        {
            if (SiteMaster.VerifyRequest(HttpContext.Current.Session))
            {
                var user = HttpContext.Current.Session[SKeys.User] as User;
                FilterInfo filter = JsonConvert.DeserializeObject<FilterInfo>(data);
                var list = IntegrationHandler.GetCurrentInvoices(user, filter);
                return ApiResponse.JSONSuccess(new
                {
                    TableState = CreateState(filter, list.Any(x => x.TransferType == "C")),
                    TableData = list
                });
            }
            else
            {
                return ApiResponse.JSONError(ResponseType.ErrorAuth);
            }
        }

        [WebMethod]
        public static string GetInvoiceCount(string version, string data)
        {
            if (SiteMaster.VerifyRequest(HttpContext.Current.Session))
            {
                var user = HttpContext.Current.Session[SKeys.User] as User;
                FilterInfo filter = JsonConvert.DeserializeObject<FilterInfo>(data);
                var recCt = IntegrationHandler.GetRecordCount(user, filter);
                return ApiResponse.JSONSuccess(recCt);
            }
            else
            {
                return ApiResponse.JSONError(ResponseType.ErrorAuth);
            }
        }

        [WebMethod]
        public static string RemoveInvoices(string version, string data)
        {
            if (SiteMaster.VerifyRequest(HttpContext.Current.Session))
            {
                var user = HttpContext.Current.Session[SKeys.User] as User;
                List<string> updateRecords = JsonConvert.DeserializeObject<List<string>>(data);

                IntegrationHandler.RemoveInvoices(user, updateRecords);
                return ApiResponse.JSONSuccess();
            }
            else
            {
                return ApiResponse.JSONError(ResponseType.ErrorAuth);
            }
        }

        [WebMethod]
        public static string ResetInvoices(string version, string data)
        {
            if (SiteMaster.VerifyRequest(HttpContext.Current.Session))
            {
                var user = HttpContext.Current.Session[SKeys.User] as User;
                List<string> updateRecords = JsonConvert.DeserializeObject<List<string>>(data);

                IntegrationHandler.ResetInvoices(user, updateRecords);
                return ApiResponse.JSONSuccess();
            }
            else
            {
                return ApiResponse.JSONError(ResponseType.ErrorAuth);
            }
        }

        [WebMethod]
        public static string EditFilteredInvoices(string version, string data)
        {
            if (SiteMaster.VerifyRequest(HttpContext.Current.Session))
            {
                var user = HttpContext.Current.Session[SKeys.User] as User;
                var pack = JObject.Parse(data);
                if (pack["filter"] != null && pack["data"] != null)
                {
                    FilterInfo filter = JsonConvert.DeserializeObject<FilterInfo>(pack["filter"].ToString());
                    IntHeadRecord ihr = JsonConvert.DeserializeObject<IntHeadRecord>(pack["data"].ToString());
                    IntegrationHandler.EditFilteredInvoices(user, filter, ihr);
                    return ApiResponse.JSONSuccess();
                }
                else
                {
                    return ApiResponse.JSONError(ResponseType.ErrorITMInvoiceSendUnknown);
                }
            }
            else
            {
                return ApiResponse.JSONError(ResponseType.ErrorAuth);
            }
        }

        [WebMethod]
        public static string EditInvoices(string version, string data)
        {
            if (SiteMaster.VerifyRequest(HttpContext.Current.Session))
            {
                var user = HttpContext.Current.Session[SKeys.User] as User;
                List<IntHeadRecord> updateRecords = JsonConvert.DeserializeObject<List<IntHeadRecord>>(data);
                IntegrationHandler.EditInvoices(user, updateRecords);
                return ApiResponse.JSONSuccess();
            }
            else
            {
                return ApiResponse.JSONError(ResponseType.ErrorAuth);
            }
        }

        [WebMethod]
        public static string GetSubmitStatusFlt(string version, string data)
        {
            if (!SiteMaster.IsValid()) return ApiResponse.JSONError(ResponseType.ErrorAuth);

            var user = HttpContext.Current.Session[SKeys.User] as User;
            var filter = JsonConvert.DeserializeObject<FilterInfo>(data);
            var type = IntegrationHandler.GetSubmitType(user, filter);
            return ApiResponse.JSONSuccess(type);
        }

        [WebMethod]
        public static string GetSubmitStatusInv(string version, string data)
        {
            if (!SiteMaster.IsValid()) return ApiResponse.JSONError(ResponseType.ErrorAuth);

            var user = HttpContext.Current.Session[SKeys.User] as User;
            var keyList = JsonConvert.DeserializeObject<List<string>>(data);
            var type = IntegrationHandler.GetSubmitType(user, keyList);
            return ApiResponse.JSONSuccess(type);
        }

        [WebMethod]
        public static string GetSendList(string version, string data)
        {
            if (SiteMaster.VerifyRequest(HttpContext.Current.Session))
            {
                var user = HttpContext.Current.Session[SKeys.User] as User;
                SubmitPreRequest sr = JsonConvert.DeserializeObject<SubmitPreRequest>(data);
                var pr = IntegrationHandler.GetSendRecord(user, sr);
                return ApiResponse.JSONSuccess(pr);
            }
            else
            {
                return ApiResponse.JSONError(ResponseType.ErrorAuth);
            }
        }

        [WebMethod]
        public static string SendRecords(string version, string data)
        {
            if (SiteMaster.VerifyRequest(HttpContext.Current.Session))
            {
                var user = HttpContext.Current.Session[SKeys.User] as User;
                var mergeRequest = JsonConvert.DeserializeObject<SubmitRequest>(data);
                try
                {
                    var uniqueKeys = IntegrationHandler.SendInvoices(user, mergeRequest);
                    return ApiResponse.JSONSuccess(uniqueKeys);
                }
                catch
                {
                    return ApiResponse.JSONError(ResponseType.ErrorITMInvoiceSendUnknown);
                }
            }
            else
            {
                return ApiResponse.JSONError(ResponseType.ErrorAuth);
            }
        }
    }
}