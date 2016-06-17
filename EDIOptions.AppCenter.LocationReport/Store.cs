using EDIOptions.AppCenter.Database;
using Newtonsoft.Json;

namespace EDIOptions.AppCenter.LocationReport
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Store
    {
        /// <summary>
        /// Name of the store
        /// </summary>
        [JsonProperty(PropertyName = "Name")]
        public readonly string Name;

        /// <summary>
        /// Store number
        /// </summary>
        [JsonProperty(PropertyName = "BYID")]
        public readonly string BYID;

        /// <summary>
        /// Number to display
        /// </summary>
        [JsonProperty(PropertyName = "DisplayID")]
        public readonly string DisplayID;

        public Store(DBResult res)
        {
            Name = res.FieldByName(_Column.STName, "").Trim();
            BYID = res.FieldByName(_Column.BYId, "").Trim();
            DisplayID = BYID;
        }
    }
}