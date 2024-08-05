using PriceBookManagement.Models;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace PriceBookManagement.Entities
{
    /// <summary>
    /// PricePointBook represents a symbol price point book with two sides - Asks and Bids
    /// </summary>
    public class PricePointBook{

        private string symbol; 
        
        private PricePointBookSide bids;
        private PricePointBookSide asks;
        
        
        public PricePointBook(string symbol){
            this.symbol = symbol;

            bids = new PricePointBookSide(sortedDesc: true);
            asks = new PricePointBookSide(sortedDesc: false);
        }

        public string Symbol
        {
            get
            {
                return symbol;
            }
        }

        /// <summary>
        /// Returns the best bid price in the price book
        /// </summary>
        public PricePoint GetBestBid(){
            return bids.GetOptimalPrice();
        }

        /// <summary>
        /// Returns the best ask price in the price point book
        /// </summary>
        public PricePoint GetBestAsk(){
            return asks.GetOptimalPrice();
        }

        /// <summary>
        /// ApplyUpdatesInParallel performs updates on bids and asks in parallel as they don't share memory
        /// This method was added for benchmarking purposes
        /// </summary>
        public void ApplyUpdatesInParallel(PricePoint[] bidsUpdates, PricePoint[] asksUpdates)
        {
            Task bidsUpdate = Task.Run(() => {
                foreach (var u in bidsUpdates)
                {
                    bids.UpsertPricePoint(u);
                }
            });

            Task asksUpdate = Task.Run(() => {
                foreach (var u in asksUpdates)
                {
                    asks.UpsertPricePoint(u);
                }
            });

            Task.WhenAll(new Task[] { bidsUpdate, asksUpdate }).Wait();
        }

        /// <summary>
        /// Applies external updates to the bids/asks in the order book
        /// </summary>
        public void ApplyUpdates(PricePoint[] bidsUpdates, PricePoint[] asksUpdates)
        {
            if (bidsUpdates != null)
            {
                foreach (var u in bidsUpdates)
                {
                    bids.UpsertPricePoint(u);
                }
            }

            if (asksUpdates != null)
            {
                foreach (var u in asksUpdates)
                {
                    asks.UpsertPricePoint(u);
                }
            }
        }

        /// <summary>
        /// ToSnapshot converts the price book to a raw snapshot object
        /// </summary>>
        public PricePointBookSnapshot ToSnapshot()
        {
            var result = new PricePointBookSnapshot();

            var bidsPricePoints = bids.ToPricePoints();

            result.Bids = new string[bidsPricePoints.Length][];

            for(int i= 0; i < bidsPricePoints.Length; i++)
            {
                result.Bids[i] = PricePoint.ToRawRepresentation(bidsPricePoints[i]);
            }

            var asksPricePoints = asks.ToPricePoints();
            result.Asks = new string[asksPricePoints.Length][];

            for (int i = 0; i < asksPricePoints.Length; i++)
            {
                result.Asks[i] = PricePoint.ToRawRepresentation(asksPricePoints[i]);
            }

            return result;
        }
    }
}