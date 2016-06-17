using System.Collections.Generic;

namespace EDIOptions.AppCenter.IntegrationManager
{
    /// <summary>
    ///  A storage container for shipment numbers.
    /// </summary>
    public class ShipNumCache
    {
        private List<string> shipnums = new List<string>();
        private int index = 0;

        /// <summary>
        /// Constructs a new instance of the <see cref="ShipNumCache"/> class.
        /// </summary>
        public ShipNumCache()
        {
            shipnums = new List<string>();
            index = 0;
        }

        /// <summary>
        /// Adds a set of shipment numbers.
        /// </summary>
        /// <param name="newShipNums">A list of shipment numbers.</param>
        public void Add(List<string> newShipNums)
        {
            shipnums.AddRange(newShipNums);
        }

        /// <summary>
        /// Clears the cache of all numbers.
        /// </summary>
        public void Clear()
        {
            shipnums.Clear();
            index = 0;
        }

        /// <summary>
        /// Gets the next available shipment number.
        /// </summary>
        /// <returns>A 10-digit shipment number. If there are no available shipment numbers left, '0000000000' is returned.</returns>
        public string GetNextShipNum()
        {
            if (index >= shipnums.Count)
            {
                return "0000000000";
            }
            return shipnums[index++];
        }
    }
}