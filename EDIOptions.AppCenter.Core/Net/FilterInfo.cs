using System.Collections.Generic;
using EDIOptions.AppCenter.Session;
using Newtonsoft.Json;

namespace EDIOptions.AppCenter
{
    /// <summary>
    /// Represents a filter on table data.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class FilterInfo
    {
        /// <summary>
        /// The current page number.
        /// </summary>
        [JsonProperty(PropertyName="CurrentPage")]
        public int CurrentPage { get; set; }

        /// <summary>
        /// The maximum page number.
        /// </summary>
        [JsonProperty(PropertyName = "MaxPage")]
        public int MaxPage { get; set; }

        /// <summary>
        /// The column being sorted by.
        /// </summary>
        [JsonProperty(PropertyName = "SortColumn")]
        public string SortColumn { get; set; }

        /// <summary>
        /// True if the column being sorted by is in descending order.
        /// </summary>
        [JsonProperty(PropertyName = "SortIsDesc")]
        public bool SortIsDesc { get; set; }

        /// <summary>
        /// The column being searched.
        /// </summary>
        [JsonProperty(PropertyName = "SearchColumn")]
        public string SearchColumn { get; set; }

        /// <summary>
        /// The search query being used.
        /// </summary>
        [JsonProperty(PropertyName = "SearchQuery")]
        public string SearchQuery { get; set; }

        /// <summary>
        /// The number of results for the current query.
        /// </summary>
        [JsonProperty(PropertyName = "ResultCount")]
        public int ResultCount { get; set; }

        /// <summary>
        /// The current list of refinements being applied to the data.
        /// </summary>
        [JsonProperty(PropertyName = "Refinements")]
        public List<RefineInfo> Refinements { get; set; }

        /// <summary>
        /// Constructs a new instance of the <see cref="FilterInfo"/> class.
        /// </summary>
        public FilterInfo()
        {
            CurrentPage = 1;
            MaxPage = 1;
            SortColumn = "";
            SortIsDesc = false;
            SearchColumn = "";
            SearchQuery = "";
            Refinements = new List<RefineInfo>();
        }

        /// <summary>
        /// Returns the limit statement used to select a page within the data.
        /// </summary>
        /// <param name="maxPageSize">The maximum amount of rows in a single page.</param>
        /// <returns></returns>
        public string ToPageString(int maxPageSize)
        {
            int sCurPg = CurrentPage;
            if (sCurPg < 1)
            {
                sCurPg = 1;
            }
            return string.Format("LIMIT {0},{1}", (CurrentPage - 1) * maxPageSize, maxPageSize);
        }

        /// <summary>
        /// Converts any present sort criteria to an 'order by' statement.
        /// </summary>
        /// <returns>The equivalent 'order by' statement, or blank otherwise.</returns>
        public string ToSortString()
        {
            string sSortColumn = string.IsNullOrEmpty(SortColumn) ? "" : SortColumn.Trim().SQLEscape();
            if (sSortColumn != "")
            {
                string sOrder = "ORDER BY `" + sSortColumn + "`";
                if (SortIsDesc)
                {
                    sOrder += " DESC";
                }
                return sOrder;
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// Converts any present search criteria to a series of conditional statements.
        /// </summary>
        /// <param name="cols">A list of columns for the 'search all' case.</param>
        /// <returns>A conditional statement to use in a 'where' statement, or 1 otherwise.</returns>
        private string ToSearchQueryString(string[] cols = null)
        {
            string sSearchColumn = string.IsNullOrWhiteSpace(SearchColumn) ? "" : SearchColumn.Trim().SQLEscape();
            string sSearchQuery = string.IsNullOrWhiteSpace(SearchQuery) ? "" : SearchQuery.Trim().SQLEscape();
            if (sSearchQuery == "")
            {
                return "1";
            }
            if (sSearchColumn != "")
            {
                return string.Format("`{0}` LIKE '%{1}%'", sSearchColumn, sSearchQuery);
            }
            else if (cols != null && cols.Length > 0)
            {
                List<string> sSubQueries = new List<string>();
                foreach (var col in cols)
                {
                    string sCol = string.IsNullOrWhiteSpace(col) ? "" : col.Trim().SQLEscape();
                    if (sCol != "")
                    {
                        sSubQueries.Add(string.Format("`{0}` LIKE '%{1}%'", sCol, sSearchQuery));
                    }
                }
                if (sSubQueries.Count > 0)
                {
                    return "(" + string.Join(" OR ", sSubQueries) + ")";
                }
                else
                {
                    return "1";
                }
            }
            else
            {
                return "1";
            }
        }

        /// <summary>
        /// Converts search criteria and refinements into a series of conditional statements.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cols">A list of columns for the 'search all' case.</param>
        /// <returns>A conditional statement to use in a 'where' statement.</returns>
        public string ToSearchQueryString(User user, string[] cols = null)
        {
            List<string> conditions = new List<string>()
            {
                ToSearchQueryString(cols),
                ToRefineString(),
                string.Format("{0}='{1}'", _Column.Customer, user.Customer),
                string.Format("{0}='{1}'", _Column.Partner, user.ActivePartner)
            };
            return string.Join(" AND ", conditions);
        }

        public string ToFetchQueryString(User user, string[] cols, int resultPageSize)
        {
            return string.Format("{0} {1} {2}", ToSearchQueryString(user, cols), ToSortString(), ToPageString(resultPageSize));
        }

        /// <summary>
        /// Converts any refinements into a series of conditional statements.
        /// </summary>
        /// <returns>A conditional statement to use in a 'where' statement, or 1 otherwise.</returns>
        public string ToRefineString()
        {
            if (Refinements == null || Refinements.Count == 0)
            {
                return "1";
            }
            List<string> refines = new List<string>();
            foreach (var refineInfo in Refinements)
            {
                if (refineInfo == null)
                {
                    continue;
                }
                string filter = refineInfo.ToFilter();
                if (filter != "1")
                {
                    refines.Add(filter);
                }
            }
            if (refines.Count > 0)
            {
                return string.Join(" AND ", refines);
            }
            else
            {
                return "1";
            }
        }
    }
}