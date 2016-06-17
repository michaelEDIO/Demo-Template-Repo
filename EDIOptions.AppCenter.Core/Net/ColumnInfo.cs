using Newtonsoft.Json;

namespace EDIOptions.AppCenter
{
    [JsonObject(MemberSerialization.OptOut)]
    public class ColumnInfo
    {
        public string Header { get; set; }

        public string Column { get; set; }

        public bool IsSearchable { get; set; }

        public bool IsSortable { get; set; }

        public ColumnInfo()
        {
            Header = "";
            Column = "";
            IsSearchable = false;
            IsSortable = false;
        }

        public ColumnInfo(string head, string col, bool isSearch, bool isSort)
        {
            Header = head;
            Column = col;
            IsSearchable = isSearch;
            IsSortable = isSort;
        }
    }
}