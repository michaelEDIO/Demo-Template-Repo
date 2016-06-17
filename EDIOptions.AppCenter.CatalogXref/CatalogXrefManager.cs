using EDIOptions.AppCenter.Database;
using EDIOptions.AppCenter.Session;
using System;
using System.Collections.Generic;
using static EDIOptions.AppCenter._Column;
using static EDIOptions.AppCenter._Database;
using static EDIOptions.AppCenter._Table;

namespace EDIOptions.AppCenter.CatalogXref
{
    public static class CatalogXrefManager
    {
        private const int resultPageSize = 15;

        private static string[] searchCols =
        {
            VendorName,
            VendorId,
            VendorSeq,
            BrandName
        };

        public static List<XrefRecord> GetXrefList(User user, FilterInfo filter)
        {
            List<XrefRecord> ret = new List<XrefRecord>();
            try
            {
                var connection = ConnectionsMgr.GetOCConnection(user, Home);
                {
                    var recCount = _GetRecordCount(user, connection, filter);
                    if (recCount > 0)
                    {
                        filter.ResultCount = recCount;
                        filter.MaxPage = (int)(Math.Ceiling(recCount / (double)resultPageSize));
                    }

                    using (var queryCurrent = connection.Select("*", CatXref, $"WHERE {filter.ToFetchQueryString(user, searchCols, resultPageSize)}"))
                    {
                        while (queryCurrent.Read())
                        {
                            XrefRecord xr = new XrefRecord(queryCurrent);
                            ret.Add(xr);
                        }
                    }
                }
                connection.Close();
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, nameof(CatalogXrefManager), nameof(GetXrefList), e.Message);
            }
            return ret;
        }

        public static bool CreateXref(User user, XrefRecord rec)
        {
            string sVendName = rec.CompanyName.Truncate(60).SQLEscape();
            string sVendId = rec.GXSAccount.Truncate(15).SQLEscape();
            string sVendSeq = rec.SelectionCode.Truncate(3).SQLEscape();
            string sBrandName = rec.BrandName.Truncate(80).SQLEscape();
            string sCustomer = user.Customer.SQLEscape();
            string sPartner = user.ActivePartner.SQLEscape();
            string skey = DBConnect.GenerateUniqueKey();
            Dictionary<string, string> insertDict = new Dictionary<string, string>()
            {
                { UniqueKey, skey },
                { VendorName, sVendName },
                { VendorId, sVendId },
                { VendorSeq, sVendSeq },
                { BrandName, sBrandName },
                { Customer, sCustomer },
                { Partner, sPartner }
            };
            try
            {
                DBConnect connection = ConnectionsMgr.GetOCConnection(user, Home);
                {
                    connection.Insert(CatXref, insertDict);
                }
                connection.Close();
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, nameof(CatalogXrefManager), nameof(CreateXref), e.Message);

                return false;
            }
            return true;
        }

        public static void EditXref(User user, List<XrefRecord> updateRecs)
        {
            try
            {
                DBConnect connection = ConnectionsMgr.GetOCConnection(user, Home);
                {
                    List<string> updateCols = new List<string>()
                    {
                        UniqueKey,
                        VendorName,
                        VendorId,
                        VendorSeq,
                        BrandName,
                        Customer,
                        Partner
                    };

                    List<string> replaceCols = new List<string>()
                    {
                        string.Format("{0}=VALUES({0})", VendorName),
                        string.Format("{0}=VALUES({0})", VendorId),
                        string.Format("{0}=VALUES({0})", VendorSeq),
                        string.Format("{0}=VALUES({0})", BrandName),
                    };

                    List<string> updateVals = new List<string>();

                    foreach (var record in updateRecs)
                    {
                        List<string> recVals = new List<string>();
                        recVals.Add(record.Key);
                        recVals.Add(record.CompanyName.Truncate(60).SQLEscape());
                        recVals.Add(record.GXSAccount.Truncate(15).SQLEscape());
                        recVals.Add(record.SelectionCode.Truncate(3).SQLEscape());
                        recVals.Add(record.BrandName.Truncate(80).SQLEscape());
                        recVals.Add(user.Customer.SQLEscape());
                        recVals.Add(user.ActivePartner.SQLEscape());
                        updateVals.Add(recVals.ToSqlValueList());
                    }

                    connection.Query(string.Format("INSERT INTO {0} ({1}) VALUES {2} ON DUPLICATE KEY UPDATE {3}", CatXref, updateCols.ToSqlColumnList(), string.Join(",", updateVals), string.Join(",", replaceCols)));
                }
                connection.Close();
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, nameof(CatalogXrefManager), nameof(EditXref), e.Message);
            }
        }

        public static void RemoveXref(User user, List<string> keyList)
        {
            try
            {
                DBConnect connection = ConnectionsMgr.GetOCConnection(user, Home);
                {
                    connection.Delete(CatXref, $"WHERE {UniqueKey} IN {keyList.ToSqlValueList()}");
                }
                connection.Close();
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, nameof(CatalogXrefManager), nameof(RemoveXref), e.Message);
            }
        }

        private static int _GetRecordCount(User user, DBConnect connection, FilterInfo filter)
        {
            int recCount = 0;
            try
            {
                using (var queryPage = connection.Select("COUNT(*)", CatXref, $"WHERE {filter.ToSearchQueryString(user, searchCols)}"))
                {
                    if (queryPage.Read())
                    {
                        if (!int.TryParse(queryPage.Field(0), out recCount))
                        {
                            recCount = 0;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, nameof(CatalogXrefManager), nameof(_GetRecordCount), e.Message);
            }
            return recCount;
        }
    }
}