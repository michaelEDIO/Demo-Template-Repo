using Newtonsoft.Json;
using System;
using System.Text;

namespace EDIOptions.AppCenter.PoDropShip
{
    [JsonObject(MemberSerialization.OptIn)]
    public class PoSummary
    {
        private static int counter = 0;

        [JsonProperty(PropertyName = "key")]
        public string Key { get; set; }

        [JsonProperty(PropertyName = "ponumber")]
        public string PONumber { get; set; }

        [JsonProperty(PropertyName = "invoiceno")]
        public string InvoiceNumber { get; set; }

        [JsonProperty(PropertyName = "bolnumber")]
        public string BolNumber { get; set; }

        public PoSummary(string key, string ponum)
        {
            Key = key;
            PONumber = ponum;
            StringBuilder sb = new StringBuilder();
            foreach (var c in PONumber)
            {
                if (char.IsDigit(c))
                {
                    sb.Append(c);
                }
            }
            InvoiceNumber = sb.ToString();
            BolNumber = DateTime.Now.ToString("yyyyMMddHHmmss") + counter.ToString("D3");
            counter++;
        }

        public PoSummary()
        {

        }
    }
}