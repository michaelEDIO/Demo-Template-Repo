using System;
using System.Collections.Generic;
using EDIOptions.AppCenter.Database;
using EDIOptions.AppCenter.Session;

namespace EDIOptions.AppCenter
{
    public class ProcessQueue
    {
        public static bool CreateUploadRecord(User user, DateTime reqDate, string reqType, string fileName)
        {
            // uniquekey, preqdate, customer, partner, preqtype, preqaction, pfilename, pcustomfile, presultfile, presultdate, processed
            DBConnect connection = new DBConnect();
            bool success = false;
            try
            {
                connection.Connect(ConnectionsMgr.GetOCConnInfo(user, _Database.Home));
                var insertVals = new Dictionary<string, string>()
                {
                    {_Column.UniqueKey, connection.GetNewKey()},
                    {_Column.PReqDate, reqDate.ToString("yyyy-MM-dd HH:mm:ss")},
                    {_Column.Customer, user.Customer.SQLEscape()},
                    {_Column.PReqType, reqType.SQLEscape()},
                    {_Column.PFileName, fileName.SQLEscape()},
                    {_Column.PCustomFile, "1"},
                    {_Column.Partner, user.ActivePartner.SQLEscape()}
                };
                using (var res = connection.Insert(_Table.ProcessQ, insertVals.ToNameValueCollection()))
                {
                    success = res.AffectedRows != 0;
                }
                connection.Close();
                return success;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, "ProcessQueue", "CreateUploadRecord", e.Message);
                return false;
            }
        }

        public static List<ProcessRecord> GetPreviousRecords(User user)
        {
            List<ProcessRecord> listRecord = new List<ProcessRecord>();
            DBConnect connection = new DBConnect();
            try
            {
                var filter = string.Format("WHERE {0}='{1}' AND {2}='{3}' AND {4}>'{5}'", _Column.Customer, user.Customer.SQLEscape(), _Column.Partner, user.ActivePartner.SQLEscape(), _Column.PReqDate, DateTime.Now.Subtract(new TimeSpan(30, 0, 0, 0)).ToString("yyyy-MM-dd HH:mm:ss"));
                connection.Connect(ConnectionsMgr.GetOCConnInfo(user, _Database.Home));
                using (var query = connection.Select(new[] { _Column.PReqDate, _Column.PReqType, _Column.Processed }, _Table.ProcessQ, filter))
                {
                    while (query.Read())
                    {
                        ProcessRecord pr = new ProcessRecord();
                        pr.Date = query.Field(0, "");
                        pr.Type = query.Field(1, "");
                        pr.Status = query.Field(2, "");
                        listRecord.Add(pr);
                    }
                }
                connection.Close();
                return listRecord;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, "ProcessQueue", "GetPreviousRecords", e.Message);
                connection.Close();
                return new List<ProcessRecord>();
            }
        }

        public static Dictionary<string, string> GetReqTypeDict(User user)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();
            DBConnect connection = new DBConnect();
            try
            {
                connection.Connect(ConnectionsMgr.GetAdminConnInfo());
                using (var query = connection.Select(new[] { _Column.TrxType, _Column.TrxDesc }, _Table.TrxInfo))
                {
                    while (query.Read())
                    {
                        ret.Add(query.Field(0), query.Field(1));
                    }
                }
                connection.Close();
                ret.Add("832_A", "Item Attributes");
                ret.Add("850_X", "PO Store X-Ref");
                ret.Add("856_X", "ASN Release X-Ref");
                ret.Add("832_P", "Al Tayer Item Attributes");
                return ret;
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, "ProcessQueue", "GetReqTypeDict", e.Message);
                connection.Close();
                return new Dictionary<string, string>();
            }
        }
    }
}