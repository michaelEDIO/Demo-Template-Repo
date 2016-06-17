using System.Collections.Generic;
using Newtonsoft.Json;

namespace EDIOptions.AppCenter
{
    [JsonObject(MemberSerialization.OptOut)]
    public class TableState
    {
        public List<ColumnInfo> Columns { get; set; }

        public FilterInfo Filter { get; set; }

        public TableState()
        {
            Columns = new List<ColumnInfo>();
            Filter = new FilterInfo();
        }
    }
}