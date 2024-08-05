using PriceBookManagement.Domain.UpdatesDispatchers;
using PriceBookManagement.Entities;
using PriceBookManagement.Models;

namespace UnitTests.Domain
{
    public class PricePointBookStoreTests
    {
        PricePointBookSnapshot snapshot = new PricePointBookSnapshot
        {
            Bids = [
                      ["0.0024", "14.70000000"],
                      ["0.0022", "6.40000000"],
                      [ "0.0020", "9.70000000"]
                     ],
            Asks = [
                        ["0.0024","14.90000000"],
                        ["0.0026","3.60000000"],
                        ["0.0028","1.00000000"]
                      ]
        };

        [Fact]
        public void GetBestBidAndAsk_ReturnCorrectValue_WithSnapshotloaded()
        {
            var store = createStoreWithSnapshot("BNBBTC");

            var bestBid = store.GetBestBidPrice("BNBBTC");
            var bestAsk = store.GetBestAskPrice("BNBBTC");

            Assert.Equal(0.0024f, bestBid.Price);
            Assert.Equal(14.70f, bestBid.Quantity);

            Assert.Equal(0.0024f, bestAsk.Price);
            Assert.Equal(14.90f, bestAsk.Quantity);
        }

        [Fact]
        public void GetBestBidAndAsk_ReturnDefaultValue_WithEmptyStore()
        {
            var store = new PricePointBookStore();

            var bestBid = store.GetBestBidPrice("BNBBTC");
            var bestAsk = store.GetBestAskPrice("BNBBTC");

            Assert.Equal(0f, bestBid.Price);
            Assert.Equal(0f, bestBid.Quantity);

            Assert.Equal(0f, bestAsk.Price);
            Assert.Equal(0f, bestAsk.Quantity);
        }

        [Fact]
        public void GetBestBidAndAsk_ReturnCorrectValue_WithMultipleSnapshotsloaded()
        {
            var store = createStoreWithSnapshot("BNBBTC");

            store.LoadSnapshot("BNBBTC2", snapshot);

            store.ApplyDiffUpdate(new PricePointBookDiffUpdate()
            {
                Symbol= "BNBBTC2",
                Bids= [["0.0024", "0"]],
            });

            store.ApplyDiffUpdate(new PricePointBookDiffUpdate()
            {
                Symbol = "BNBBTC",
                Bids = [["0.0024", "10"]],
            });

            var bestBid = store.GetBestBidPrice("BNBBTC");

            Assert.Equal(0.0024f, bestBid.Price);
            Assert.Equal(10.0f, bestBid.Quantity);

            var bestBid2 = store.GetBestBidPrice("BNBBTC2");
            Assert.Equal(0.0022f, bestBid2.Price);
            Assert.Equal(6.40f, bestBid2.Quantity);
        }

        [Fact]
        public async void GetBestBidAndAsk_ReturnCorrectValue_WithAsyncEventsDispatching()
        {
            var dispatcher = new AsyncUpdatesDispatcher();
            var store = new PricePointBookStore(dispatcher);

            store.LoadSnapshot("BNBBTC", snapshot);
            store.LoadSnapshot("BNBBTC2", snapshot);

            var task1 = dispatcher.AwaitDispatchedEvent("BNBBTC", "2");
            var task2 = dispatcher.AwaitDispatchedEvent("BNBBTC2", "2");

            // delete best buy
            store.ApplyDiffUpdate(new PricePointBookDiffUpdate()
            {
                ID="1",
                Symbol = "BNBBTC",
                Bids = [["0.0024", "0"]],
            });

            // delete best buy
            store.ApplyDiffUpdate(new PricePointBookDiffUpdate()
            {
                ID="1",
                Symbol = "BNBBTC2",
                Bids = [["0.0024", "0"]],
            });

            // update best buy quantity
            store.ApplyDiffUpdate(new PricePointBookDiffUpdate()
            {
                ID = "2",
                Symbol = "BNBBTC",
                Bids = [["0.0022", "1"]],
            });

            // restore the deleted best buy
            store.ApplyDiffUpdate(new PricePointBookDiffUpdate()
            {
                ID = "2",
                Symbol = "BNBBTC2",
                Bids = [["0.0024", "1"]],
            });

            await task1;
            await task2;

            var bestBid = store.GetBestBidPrice("BNBBTC");

            Assert.Equal(0.0022f, bestBid.Price);
            Assert.Equal(1f, bestBid.Quantity);

            var bestBid2 = store.GetBestBidPrice("BNBBTC2");
            Assert.Equal(0.0024f, bestBid2.Price);
            Assert.Equal(1f, bestBid2.Quantity);
        }


        [Fact]
        public void GetSnapshot_ReturnExpectedValue_WithSamesnapshotLoaded()
        {
            var store = createStoreWithSnapshot("bnbbtc");

            var restoredSnapshot = store.GetSnapshot("bnbbtc");

            Assert.Equal(snapshot.Bids, restoredSnapshot.Bids);
            Assert.Equal(snapshot.Asks, restoredSnapshot.Asks);
        }

        private PricePointBookStore createStoreWithSnapshot(string symbol = "bnbbtc")
        {
            var dispatcher = new SyncUpdatesDispatcher();
            var pricePointStore = new PricePointBookStore(dispatcher);

            pricePointStore.LoadSnapshot(symbol, snapshot);

            return pricePointStore;
        }
    }
}