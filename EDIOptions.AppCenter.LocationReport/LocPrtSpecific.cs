namespace EDIOptions.AppCenter.LocationReport
{
    internal class LocPrtSpecific
    {
        public LocPrtSpecific()
        {
        }

        private string _StoreJoin;

        /// <summary>
        /// Statement used to join stinfo to detl856
        /// </summary>
        public string StoreJoin
        {
            get { return _StoreJoin; }
            set { _StoreJoin = value; }
        }

        private string _StoreWhere;

        /// <summary>
        /// Statement used in where statement for store
        /// </summary>
        public string StoreWhere
        {
            get { return _StoreWhere; }
            set { _StoreWhere = value; }
        }

        private string _BuyerName;

        public string BuyerName
        {
            get { return _BuyerName; }
            set { _BuyerName = value; }
        }
    }
}