using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using PriceBookManagement.Domain.UpdatesDispatchers;
using PriceBookManagement.Entities;
using PriceBookManagement.Models;

namespace Benchmark
{
    [MemoryDiagnoser]
    public class PricePointStoreBookLoadSnapshotBenchmarks
    {
        private static int MaxPriceBooks = 2000;
        private static int SideDeptLimit = 5000;
        private static string defaultQuantity = "100";

        string[][] bids = new string[SideDeptLimit][];
        string[][] asks = new string[SideDeptLimit][];

        [GlobalSetup]
        public void Setup()
        {
            var sampleBid = new string[] { "", defaultQuantity };
            var sampleAsk = new string[] { "", defaultQuantity };

            for (int i = 0; i < SideDeptLimit; i++)
            {
                sampleBid[0] = $"1.{i}001";
                bids[i] = sampleBid;
                sampleAsk[0] = $"2.{i}001";
                asks[i] = sampleAsk;
            }
        }

        [Benchmark]
        public void LoadSnapshots()
        {
            runBooksLoadingTest(parallel: false);
        }

        [Benchmark]
        public void LoadSnapshotsInParallel()
        {
            runBooksLoadingTest(parallel: true);
        }

        private void runBooksLoadingTest(bool parallel)
        {
            var dispatcher = new AsyncUpdatesDispatcher();
            var pricePointStore = new PricePointBookStore(dispatcher);

            var snapshot = new PricePointBookSnapshot()
            {
                Bids = bids,
                Asks = asks,
            };

            for (int i = 0; i < MaxPriceBooks; i++)
            {
                if (parallel)
                {
                    pricePointStore.LoadSnapshotParallel($"SYMBOL_{i}", snapshot);
                }
                else
                {
                    pricePointStore.LoadSnapshot($"SYMBOL_{i}", snapshot);
                }
            }
        }
    }
}
