using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PriceBookManagement.Entities
{
    // Custom comparer that sorts floats in descending order
    public class DescendingComparer : IComparer<float>
    {
        public int Compare(float x, float y)
        {
            return y.CompareTo(x);
        }
    }
    // PricePointBookSide represents a side in a price point book - either bids (desc order collection) or asks (asc order collection) 
    public class PricePointBookSide
    {
        const int ExpectedMaxCapacity = 5000;

        private object sync = new object();

        private bool sortedDesc;

        // 
        // stores quantity per price to ensure O(1) existing price updates 
        //
        private Dictionary<float, float> volumes;

        //
        // stores prices in an ordered manner with binary search tree
        //
        private SortedSet<float> prices;

        public PricePointBookSide(bool sortedDesc)
        {
            this.sortedDesc = sortedDesc;
            volumes = new Dictionary<float, float>(ExpectedMaxCapacity);

            if (sortedDesc) 
            {
                prices = new SortedSet<float>(new DescendingComparer());
            }
            else
            {
                prices = new SortedSet<float>();
            }
        }

        /// <summary>
        /// Returns the optimal price in the side based on the collection order
        /// Max price for bids and min price for asks
        /// </summary>
        public PricePoint GetOptimalPrice()
        {
            lock (sync)
            {
                if (prices.Count == 0)
                {
                    return new PricePoint { };
                }

                var price = prices.First();

                return new PricePoint()
                {
                    Price = price,
                    Quantity = volumes[price]
                };
            }
        }

        /// <summary>
        /// Upsert a price point - if it's a new price points it's created in the collection, and if it's an existing one its quantity would get updated
        /// </summary>
        public void UpsertPricePoint(PricePoint point)
        {
            lock (sync)
            {
                bool isExistingPrice = volumes.ContainsKey(point.Price);

                //
                // check whether we shall delete a price from the book
                //
                if (point.Quantity == 0)
                {
                    //
                    // it's possible that we haven't received this price at all
                    // and local price book diverged from remote exchange price book
                    //
                    if (isExistingPrice)
                    {
                        volumes.Remove(point.Price);
                        prices.Remove(point.Price);

                        return;
                    }
                }

                volumes[point.Price] = point.Quantity;

                if (!isExistingPrice)
                {
                    prices.Add(point.Price);
                }
            }
        }

        /// <summary>
        /// Converts the price point book side into an ordered array of price points
        /// </summary>
        /// <returns></returns>
        public PricePoint[] ToPricePoints()
        {
            lock (sync)
            {
                var result = new PricePoint[prices.Count];

                int i = 0;

                foreach(var p in prices)
                {
                    result[i] = new PricePoint()
                    {
                        Price = p,
                        Quantity = volumes[p],
                    };
                    i++;
                }

                return result;
            }
        }
    }
}
