using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Diagnostics.Tracing.Parsers;
using PriceBookManagement.Entities;

namespace Benchmark
{
    [MemoryDiagnoser]
    public class PricePointBookBenchmarks
    {
        //  
        // https://binance-docs.github.io/apidocs/spot/en/#diff-depth-stream
        // base on " for most use cases the depth limit of 5000 is enough to understand the market and trade effectively"
        //
        private static int sideDepthLimit = 5000;

        private static int updatesCount = 1000;
        private int updateBatchSize = 100;

        private static string symbol = "BNBBTC";

        private PricePointBook? book;

        [GlobalSetup]
        public void Setup()
        {
            book = CreatePricePointBook(symbol, false);
        }

        protected static PricePointBook CreatePricePointBook(string symbol, bool parallel = false)
        {
            PricePointBook book = new PricePointBook(symbol);

            var defaultQuantity = "100";

            string[][] bids = new string[sideDepthLimit][];
            string[][] asks = new string[sideDepthLimit][];

            for (int i = 0; i < sideDepthLimit; i++) {
                
                bids[i] = [ $"1.{i}001", defaultQuantity ];
                asks[i] = [$"2.{i}001", defaultQuantity];
            }

            if (parallel) {
                book.ApplyUpdatesInParallel(PricePoint.ToPricePoints(bids), PricePoint.ToPricePoints(asks));
            }
            else
            {
                book.ApplyUpdates(PricePoint.ToPricePoints(bids), PricePoint.ToPricePoints(asks));
            }

            return book;
        }

        [Benchmark]
        public void WithSerialUpdates()
        {
            RunBookUpdatesTest(false);
        }

        private void RunBookUpdatesTest(bool parallel)
        {
            // ensure book is initialised in the Setup function
            if (book != null)
            {
                var bids = new PricePoint[updateBatchSize];
                var asks = new PricePoint[updateBatchSize];

                for (int i = 0; i < updatesCount; i++)
                {
                    for (int j = 0; j < updateBatchSize; j++)
                    {
                        bids[j] = new PricePoint();
                        asks[j] = new PricePoint();


                        if (j == updateBatchSize - 1)
                        {
                            //
                            // simulate deletes due to trades
                            //
                            bids[j].Quantity = book.GetBestBid().Price;
                            bids[j].Quantity = 0;

                            asks[j].Quantity = book.GetBestAsk().Price;
                            asks[j].Quantity = 0;
                        }
                        else
                        {
                            bids[j].Price = i + j;
                            bids[j].Quantity = j;

                            asks[j].Price = i + j;
                            asks[j].Quantity = j;
                        }
                    }

                    if (parallel)
                    {
                        book.ApplyUpdatesInParallel(bids, asks);
                    }
                    else
                    {
                        book.ApplyUpdates(bids, asks);
                    }
                }
            }
        }

        [Benchmark]
        public void WithParallelUpdates()
        {
            RunBookUpdatesTest(true);
        }
    }
}
