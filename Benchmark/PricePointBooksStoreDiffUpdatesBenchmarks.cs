using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.MicrosoftWindowsWPF;
using PriceBookManagement.Domain.UpdatesDispatchers;
using PriceBookManagement.Entities;
using PriceBookManagement.Models;

namespace Benchmark
{
    [MemoryDiagnoser]
    public class PricePointBooksStoreDiffUpdatesBenchmarks
    {
        private static int maxPriceBooks = 2000;
        private static int sideDepthLimit = 5000;
        private static string defaultQuantity = "100";
        private static string zeroQuantity = "0";
        private static int updatesCount = 10000;
        private int updateBatchSize = 100;

        string[][] bids = new string[sideDepthLimit][];
        string[][] asks = new string[sideDepthLimit][];

        PricePointBookStore bookStoreAsyncTest, bookStoreSyncTest;

        AsyncUpdatesDispatcher asyncDispatcher = new AsyncUpdatesDispatcher();
        SyncUpdatesDispatcher syncDispatcher = new SyncUpdatesDispatcher();




        [GlobalSetup]
        public void Setup()
        {
            var sampleBid = new string[] { "", defaultQuantity };
            var sampleAsk = new string[] { "", defaultQuantity };

            for (int i = 0; i < sideDepthLimit; i++)
            {
                sampleBid[0] = $"1.{i}001";
                bids[i] = sampleBid;
                sampleAsk[0] = $"2.{i}001";
                asks[i] = sampleAsk;

            }

            bookStoreSyncTest = getPricePointBookWithLoadedSnapshots(syncDispatcher);
            bookStoreAsyncTest = getPricePointBookWithLoadedSnapshots(asyncDispatcher);

        }

        [Benchmark]
        public void LoadSnapshotsInParallelWithSyncUpdates()
        {
            runUpdates(bookStoreSyncTest);
        }

        [Benchmark]
        public void LoadSnapshotsInParallelWithAsyncUpdates()
        {
            var lastUpdateSymbol = $"SYMBOL_{(updatesCount -1)% maxPriceBooks}";
            var lastUpdateID = $"{updatesCount - 1}";

            var awaitTask = asyncDispatcher.AwaitDispatchedEvent(lastUpdateSymbol, lastUpdateID);

            var lastUpdate = runUpdates(bookStoreAsyncTest);

            if(lastUpdate.Symbol != lastUpdateSymbol || lastUpdate.ID  != lastUpdateID)
            {
                throw new Exception($"Wrong setup await task - expected {lastUpdateSymbol}/{lastUpdateID} , actual {lastUpdate.Symbol}/{lastUpdate.ID}");
            }

            // Console.WriteLine($"Awaiting on thread: {Thread.CurrentThread.ManagedThreadId} for {lastUpdateSymbol}/{lastUpdateID}");

            awaitTask.Wait();
        }

        private PricePointBookDiffUpdate runUpdates(PricePointBookStore pricePointBookStore)
        {
            string[][] bids = new string[updateBatchSize][];
            string[][] asks = new string[updateBatchSize][];

            var lastDiff = new PricePointBookDiffUpdate();

            for (int i = 0; i < updatesCount; i++)
            {
                var symbolID = i % maxPriceBooks;
                lastDiff.Symbol = $"SYMBOL_{symbolID}";
                lastDiff.ID = i.ToString();

                for (int j = 0; j < updateBatchSize; j++)
                {
                    bids[j]= [$"1.{j}001", defaultQuantity];
                    asks[j] = [$"1.{j}001", defaultQuantity];
                }

                // 
                // simulate delete of optimal price point
                //
                var bestBid = pricePointBookStore.GetBestBidPrice(lastDiff.Symbol);
                var bestAsk = pricePointBookStore.GetBestAskPrice(lastDiff.Symbol);

                // set the last elements with optimal price and 0 quantity
                bids[updateBatchSize-1] = [bestBid.Price.ToString(), zeroQuantity];
                asks[updateBatchSize - 1] = [bestAsk.Price.ToString(), zeroQuantity];

                lastDiff.Bids = bids;
                lastDiff.Asks = asks;

                pricePointBookStore.ApplyDiffUpdate(lastDiff);
            }

            return lastDiff;
        }

        private PricePointBookStore getPricePointBookWithLoadedSnapshots(IDiffUpdatesDispatcher dispatcher)
        {
            var pricePointStore = new PricePointBookStore(dispatcher);

            var snapshot = new PricePointBookSnapshot()
            {
                Bids = bids,
                Asks = asks,
            };

            for (int i = 0; i < maxPriceBooks; i++)
            {
                pricePointStore.LoadSnapshotParallel($"SYMBOL_{i}", snapshot);
            }

            return pricePointStore;
        }
    }
}
