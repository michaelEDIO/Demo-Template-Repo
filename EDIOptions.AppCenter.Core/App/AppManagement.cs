using EDIOptions.AppCenter.Database;
using EDIOptions.AppCenter.Session;
using System;
using System.Collections.Generic;

namespace EDIOptions.AppCenter
{
    public static class AppManagement
    {
        private const string columnFlagIDM = "sh_asp_idm";
        private const string columnFlagSRQ = "sh_asp_srq";
        private const string columnFlagCPO = "sh_asp_cpo";
        private const string columnFlagGVM = "sh_asp_gvm";
        private const string columnFlagAPO = "sh_asp_apo";
        private const string columnFlagITM = "sh_asp_itm";
        private const string columnFlagDEM = "sh_asp_dem";
        private const string columnFlagLOC = "sh_asp_loc";
        private const string columnFlagEVC = "sh_asp_evm";
        private const string columnFlagPDS = "sh_asp_pds";
        private const string columnFlagXCT = "sh_asp_xct";

        private static Dictionary<string, AppDetail> columnToDetail = new Dictionary<string, AppDetail>()
        {
            { columnFlagIDM, new AppDetail(_App.ItemDistributionManager, "IDM.aspx") },
            { columnFlagSRQ, new AppDetail(_App.SalesRequest, "SalesRequest.aspx") },
            { columnFlagCPO, new AppDetail(_App.ChangePO, "ChangePO.aspx") },
            { columnFlagGVM, new AppDetail(_App.GVMCostReport, "GVMCostReport.aspx") },
            { columnFlagAPO, new AppDetail(_App.POAcknowledge, "POAcknowledge.aspx") },
            { columnFlagITM, new AppDetail(_App.IntegrationManager, "IntegrationManager.aspx") },
            { columnFlagLOC, new AppDetail(_App.LocationReport, "LocationReport.aspx") },
            { columnFlagEVC, new AppDetail(_App.VMIOrder, "VMIOrder.aspx") },
            { columnFlagDEM, new AppDetail(_App.DemoTemplate, "DemoTemplate.aspx") },
            { columnFlagPDS, new AppDetail(_App.PoDropShip, "PoDropShip.aspx") },
            { columnFlagXCT, new AppDetail(_App.CatalogXref, "CatalogXref.aspx") }
        };

        private static Dictionary<AppID, string> idToColumn = new Dictionary<AppID, string>()
        {
            {AppID.SalesRequest, columnFlagSRQ},
            {AppID.ItemDistributionManager, columnFlagIDM},
            {AppID.ChangePO, columnFlagCPO},
            {AppID.GVMCostReport, columnFlagGVM},
            {AppID.POAcknowledge, columnFlagAPO},
            {AppID.IntegrationManager, columnFlagITM},
            {AppID.LocationReport, columnFlagLOC},
            {AppID.VMIOrder, columnFlagEVC },
            {AppID.PoDropShip, columnFlagPDS},
            {AppID.DemoTemplate, columnFlagITM},
            {AppID.CatalogXref, columnFlagXCT}
        };

        private static string[] appFlags =
        {
            columnFlagIDM,
            columnFlagSRQ,
            columnFlagCPO,
            columnFlagGVM,
            columnFlagAPO,
            columnFlagITM,
            columnFlagLOC,
            columnFlagEVC,
            columnFlagPDS,
            //columnFlagDEM,
            columnFlagXCT
        };

        public static Dictionary<string, string> GetAllowedApps(User user)
        {
            DBConnect connection = new DBConnect();
            try
            {
                connection.Connect(ConnectionsMgr.GetOCConnInfo(user, _Database.Home));
                Dictionary<string, string> ret = new Dictionary<string, string>();

                using (var queryUserLevelAP = connection.Select(appFlags, _Table.UserInfo, string.Format("WHERE {0}='{1}' AND {2}='{3}' AND {4}='{5}'", _Column.Customer, user.Customer.SQLEscape(), _Column.Partner, user.ActivePartner.SQLEscape(), _Column.UserName, user.UserName.SQLEscape())))
                {
                    while (queryUserLevelAP.Read())
                    {
                        for (int i = 0; i < appFlags.Length; i++)
                        {
                            string f = queryUserLevelAP.Field(i);
                            if (f == "1")
                            {
                                string name = columnToDetail[appFlags[i]].appName;
                                string url = columnToDetail[appFlags[i]].appURL;
                                if (!ret.ContainsKey(name))
                                {
                                    ret.Add(name, url);
                                }
                            }
                        }
                    }
                    if (ret.Count > 0)
                    {
                        return ret;
                    }
                }

                using (var queryCustPartAP = connection.Select(appFlags, _Table.NetGroup, string.Format("WHERE {0}='{1}' AND {2}='{3}'", _Column.Customer, user.Customer.SQLEscape(), _Column.Partner, user.ActivePartner.SQLEscape())))
                {
                    while (queryCustPartAP.Read())
                    {
                        for (int i = 0; i < appFlags.Length; i++)
                        {
                            string f = queryCustPartAP.Field(i);
                            if (f == "1")
                            {
                                string name = columnToDetail[appFlags[i]].appName;
                                string url = columnToDetail[appFlags[i]].appURL;
                                if (!ret.ContainsKey(name))
                                {
                                    ret.Add(name, url);
                                }
                            }
                        }
                    }
                    return ret;
                }
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, nameof(AppManagement), nameof(GetAllowedApps), e.Message);
                return new Dictionary<string, string>();
            }
        }

        public static bool IsAllowed(User user, AppID app)
        {
            try
            {
                if (user == null || app == AppID.None)
                {
                    return false;
                }
                var connection = ConnectionsMgr.GetOCConnection(user);

                using (var queryUserLevelAP = connection.Select(idToColumn[app], _Table.UserInfo, string.Format("WHERE {0}='{1}' AND {2}='{3}' AND {4}='{5}'", _Column.Customer, user.Customer.SQLEscape(), _Column.Partner, user.ActivePartner.SQLEscape(), _Column.UserName, user.UserName.SQLEscape())))
                {
                    if (queryUserLevelAP.AffectedRows != 0)
                    {
                        queryUserLevelAP.Read();
                        return queryUserLevelAP.Field(0) != "0";
                    }
                }

                using (var queryCustPartAP = connection.Select(idToColumn[app], _Table.NetGroup, string.Format("WHERE {0}='{1}' AND {2}='{3}'", _Column.Customer, user.Customer.SQLEscape(), _Column.Partner, user.ActivePartner.SQLEscape())))
                {
                    if (queryCustPartAP.AffectedRows == 0)
                    {
                        ProgramLog.LogError(user, nameof(AppManagement), nameof(IsAllowed), string.Format("Error: No information found for customer {0} and partner {1}.", user.Customer, user.ActivePartner));
                        return false;
                    }
                    queryCustPartAP.Read();
                    return queryCustPartAP.Field(0) != "0";
                }
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, nameof(AppManagement), nameof(IsAllowed), e.Message);
                return false;
            }
        }
    }
}