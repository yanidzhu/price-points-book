using PriceBookManagement.Domain.UpdatesDispatchers;
using PriceBookManagement.Entities;
using PriceBookManagement.Models;
using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace OrderBook
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Running example input and events: ");

            var symbol = "bnbbtc";

            var snapshot = new PricePointBookSnapshot
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


            PricePointBookDiffUpdate[] updates = {
                    new PricePointBookDiffUpdate{
                        ID = "1",
                        Symbol = symbol,
                        Bids = [["0.0024", "10"]],
                        Asks = [["0.0026", "100"]],
                    },
                    new PricePointBookDiffUpdate{
                        ID = "2",
                        Symbol = symbol,
                        Bids = [["0.0024", "8"]],
                        Asks = [["0.0028", "0"]],
                    },
                    new PricePointBookDiffUpdate
                    {
                        ID = "3",
                        Symbol = symbol,
                        Bids = [["0.0024", "0"]],
                        Asks = [["0.0026", "15"], ["0.0027", "5"]],
                    },
                    new PricePointBookDiffUpdate
                    {
                        ID = "4",
                        Symbol = symbol,
                        Bids = [["0.0025", "100"]],
                        Asks = [["0.0026", "0"], ["0.0027", "5"]],
                    },
                    new PricePointBookDiffUpdate
                    {
                        ID = "5",
                        Symbol = symbol,
                        Bids = [["0.0025", "0"]],
                        Asks = [["0.0026", "15"], ["0.0024", "0"]],
                    }
            };

            var dispatcher = new AsyncUpdatesDispatcher();
            var pricePointStore = new PricePointBookStore(dispatcher);
            var awaitTask = AwaitDispatchedEvents(dispatcher, pricePointStore, symbol, updates.Last().ID);
            
            pricePointStore.LoadSnapshot(symbol, snapshot);

            Console.WriteLine("Snapshot loaded");

            PrintCurrentBestPrices(symbol, pricePointStore);

            foreach(var u in updates)
            {
                ApplyDiffUpdate(pricePointStore, u);
            }

            awaitTask.Wait();

            Console.WriteLine();
            Console.WriteLine($"{symbol} Snapshot at finish: ");

            PrintSnapshot(pricePointStore, symbol);
        }

        private static Task AwaitDispatchedEvents(IDiffUpdatesDispatcher dispatcher, 
            PricePointBookStore pricePointStore, String symbol, string lastEventID)
        {
            //
            // will be used to turn the async operation of awaiting the last dispatched event into 
            // task so it can be awaited from the main function before consuming the result of the updates
            //
            var taskCompletionSource = new TaskCompletionSource();

            dispatcher.DiffUpdateDispatched += (object sender, PricePointBookDiffUpdate e) =>
            {
                Console.WriteLine("Dispatched update :" + e.ID);

                Console.WriteLine();
                Console.WriteLine($"Performed diff update: {e.ID}");
                PrintCurrentBestPrices(e.Symbol, pricePointStore);

                if (e.Symbol == symbol && e.ID == lastEventID)
                {
                    //
                    // signal that we have processed the last diff update
                    //
                    taskCompletionSource.SetResult();
                }
            };

            return taskCompletionSource.Task;
        }

        private static void PrintSnapshot(PricePointBookStore pricePointStore, String symbol)
        {
            var snapshot = pricePointStore.GetSnapshot(symbol);
            Console.WriteLine("BIDS:");
            Array.ForEach(snapshot.Bids, x => Console.WriteLine("[{0}]", string.Join(", ", x)));
            Console.WriteLine("ASKS:");
            Array.ForEach(snapshot.Asks, x => Console.WriteLine("[{0}]", string.Join(", ", x)));
        }

        private static void ApplyDiffUpdate(PricePointBookStore pricePointStore, PricePointBookDiffUpdate update)
        {
            pricePointStore.ApplyDiffUpdate(update);
        }

        private static void PrintCurrentBestPrices(string symbol, PricePointBookStore pricePointStore)
        {
            Console.WriteLine("Symbol: {0} -  Best BUY price: {1}/{2} , best ASK price: {3}/{4}",
                symbol,
                pricePointStore.GetBestBidPrice(symbol).Price,
                pricePointStore.GetBestBidPrice(symbol).Quantity,
                pricePointStore.GetBestAskPrice(symbol).Price,
                pricePointStore.GetBestAskPrice(symbol).Quantity);
        }
    }
}