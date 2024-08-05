using PriceBookManagement.Domain.UpdatesDispatchers;
using PriceBookManagement.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;

namespace PriceBookManagement.Entities
{
    /// <summary>
    /// PricePointBookStore keeps track for all price books for all symbols and expose interface to manage them
    /// </summary>
    public class PricePointBookStore
    {
        private IDiffUpdatesDispatcher diffDispatcher;
        public PricePointBookStore():this(new SyncUpdatesDispatcher())
        {
        }

        /// <summary>
        /// PricePointBookStore creates a new instance of price point book store with a given diff updates dispatcher
        /// </summary>
        public PricePointBookStore(IDiffUpdatesDispatcher dispatcher)
        {
            this.diffDispatcher = dispatcher;
        }

        private ConcurrentDictionary<string, PricePointBook> orderBooks = new ConcurrentDictionary<string, PricePointBook>();

        /// <summary>
        /// Initialize a price point book for a symbol with external snapshot
        /// </summary>
        public void LoadSnapshot(string symbol, PricePointBookSnapshot snapshot)
        {
            var book = new PricePointBook(symbol);
            
            book.ApplyUpdates(
                    PricePoint.ToPricePoints(snapshot.Bids), 
                    PricePoint.ToPricePoints(snapshot.Asks)
                    ); 

            orderBooks[symbol] = book;
        }

        /// <summary>
        /// Initialize a price point book for a symbol with external snapshot in parallel - for benchmark purposes
        /// </summary>
        public void LoadSnapshotParallel(string symbol, PricePointBookSnapshot snapshot)
        {
            var book = new PricePointBook(symbol);

            book.ApplyUpdatesInParallel(
                    PricePoint.ToPricePoints(snapshot.Bids),
                    PricePoint.ToPricePoints(snapshot.Asks)
                    );

            orderBooks[symbol] = book;
        }

        /// <summary>
        /// Apply a diff update of bids and asks to the internally stored price book
        /// </summary>
        public void ApplyDiffUpdate(PricePointBookDiffUpdate update)
        {
            if (!orderBooks.ContainsKey(update.Symbol))
            {
                orderBooks[update.Symbol] = new PricePointBook(update.Symbol);
            }

            diffDispatcher.DispatchDiffUpdates(orderBooks[update.Symbol], update);
        }

        /// <summary>
        /// GetBestBidPrice returns the current best buy price for a given symbol
        /// </summary>
        public PricePoint GetBestBidPrice(string symbol)
        {
            if (orderBooks.ContainsKey(symbol)) {
                return orderBooks[symbol].GetBestBid();
            }

            return default;
        }

        /// <summary>
        /// GetBestAskPrice returns the current best sell price for a given symbol
        /// </summary>
        public PricePoint GetBestAskPrice(string symbol)
        {
            if (orderBooks.ContainsKey(symbol))
            {
                return orderBooks[symbol].GetBestAsk();
            }

            return default;
        }

        /// <summary>
        /// Converts back a price points book into a snapshot
        /// </summary>
        public PricePointBookSnapshot GetSnapshot(string symbol)
        {
            if (orderBooks.ContainsKey(symbol))
            {
                var book = orderBooks[symbol];

                return book.ToSnapshot();
            }

            return default;
        }
    }
}
