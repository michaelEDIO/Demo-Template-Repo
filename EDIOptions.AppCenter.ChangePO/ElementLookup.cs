using EDIOptions.AppCenter.Database;
using EDIOptions.AppCenter.Session;
using System;
using System.Collections.Generic;

namespace EDIOptions.AppCenter
{
    /// <summary>
    /// A class for looking up element codes.
    /// </summary>
    public static class ElementLookup
    {
        private static Dictionary<string, Dictionary<string, string>> _cache = new Dictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// Clears the lookup cache.
        /// </summary>
        public static void ClearCache()
        {
            _cache.Clear();
        }

        /// <summary>
        /// Gets the description of a given code.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="usEleNum">The element number of the code.</param>
        /// <param name="usCode">The code.</param>
        /// <returns>The description of the given code.</returns>
        public static string GetDesc(User user, string usEleNum, string usCode)
        {
            string sEleNum = usEleNum.SQLEscape();
            string sLookup = usCode.SQLEscape();
            string desc = "";
            if (_cache.ContainsKey(sEleNum))
            {
                if (_cache[sEleNum].ContainsKey(sLookup))
                {
                    return _cache[sEleNum][sLookup];
                }
                else
                {
                    desc = _GetElemDesc(user, sEleNum, sLookup);
                    if (desc != "")
                    {
                        _cache[sEleNum].Add(sLookup, desc);
                    }
                    else
                    {
                        desc = sLookup;
                    }
                }
            }
            else
            {
                desc = _GetElemDesc(user, sEleNum, sLookup);
                if (desc != "")
                {
                    _cache.Add(sEleNum, new Dictionary<string, string>() { { sLookup, desc } });
                }
                else
                {
                    desc = sLookup;
                }
            }
            return desc;
        }

        private static string _GetElemDesc(User user, string usEleNum, string usCode)
        {
            DBConnect connect = new DBConnect();
            try
            {
                connect.Connect(ConnectionsMgr.GetAdminConnInfo());
                var queryCodes = connect.Select(_Column.CodeDesc, _Table.DisaCode, string.Format("WHERE {0}='{1}' AND {2}='{3}'", _Column.EleNum, usEleNum.SQLEscape(), _Column.Code, usCode.SQLEscape()));
                if (queryCodes.Read())
                {
                    return queryCodes.Field(0);
                }
                else
                {
                    return "";
                }
            }
            catch (Exception e)
            {
                ProgramLog.LogError(user, "ElementLookup", "_GetElemDesc", e.Message);
                return "";
            }
        }
    }
}