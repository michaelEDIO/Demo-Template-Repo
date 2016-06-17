namespace EDIOptions.AppCenter.IntegrationManager
{
    public class Distribution
    {
        public string ItemKey { get; set; }
        public string Quantity { get; set; }

        public Distribution(string key, string qty)
        {
            ItemKey = key;
            Quantity = qty;
        }
    }
}