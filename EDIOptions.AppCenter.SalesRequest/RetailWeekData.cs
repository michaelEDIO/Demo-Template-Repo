using Newtonsoft.Json;

namespace EDIOptions.AppCenter.SalesRequest
{
    [JsonObject(MemberSerialization.OptOut)]
    public class RetailWeekData : System.IComparable<RetailWeekData>
    {
        public int Week { get; set; }

        public int Year { get; set; }

        public string WeekStart { get; set; }

        public string WeekEnd { get; set; }

        public string ToSQL()
        {
            return string.Format("RETAILWEEK='{0}' AND RETAILYEAR='{1}'", Week, Year);
        }

        public int CompareTo(RetailWeekData other)
        {
            int yearComp = Year.CompareTo(other.Year);
            if (yearComp != 0)
            {
                return yearComp;
            }
            return Week.CompareTo(other.Week);
        }
    }
}