namespace EDIOptions.AppCenter.IntegrationManager
{
    public class InvoiceCounter
    {
        private readonly string baseNum;

        private int counter = 1;

        private string padding = "D4";

        public InvoiceCounter(string baseInvoiceNum)
        {
            if (string.IsNullOrEmpty(baseInvoiceNum))
            {
                baseNum = "BASE_INV";
            }
            else
            {
                baseNum = baseInvoiceNum;
            }
        }

        public void SetPadding(int pad)
        {
            padding = "D" + pad;
        }

        public string GetNextInvoiceNumber()
        {
            string ret = baseNum + counter.ToString(padding);
            counter++;
            return ret.SQLEscape();
        }
    }
}