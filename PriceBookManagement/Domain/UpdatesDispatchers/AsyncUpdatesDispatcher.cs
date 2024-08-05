using PriceBookManagement.Entities;
using PriceBookManagement.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace PriceBookManagement.Domain.UpdatesDispatchers
{
    /// <summary>
    /// AsyncUpdatesDispatcher routes diff updates asynchronously to a price book object
    /// Exposing interface to subscribe upon diff updates are completely processed
    /// </summary>
    public class AsyncUpdatesDispatcher : IDiffUpdatesDispatcher
    {
        private object sync = new object();
        public event EventHandler<PricePointBookDiffUpdate> DiffUpdateDispatched;

        private ConcurrentDictionary<string, TaskCompletionSource> taskCompletionSources = new ConcurrentDictionary<string, TaskCompletionSource>();

        private ConcurrentDictionary<string, Channel<PricePointBookDiffUpdate>> channels = new ConcurrentDictionary<string, Channel<PricePointBookDiffUpdate>>();
        public void DispatchDiffUpdates(PricePointBook pricePointBook, PricePointBookDiffUpdate update)
        {
            var channel = GetChannel(pricePointBook);
            channel.Writer.TryWrite(update);
        }

        /// <summary>
        /// GetChannel gets or creates a synchronization channel for producer/subscribe communication
        /// </summary>
        private Channel<PricePointBookDiffUpdate> GetChannel(PricePointBook pricePointBook)
        {
            if (!channels.ContainsKey(pricePointBook.Symbol))
            {
                lock (sync)
                {
                    // 
                    // ensure singleton creation of a channel per symbol
                    //
                    if (!channels.ContainsKey(pricePointBook.Symbol))
                    {
                        var chan = Channel.CreateUnbounded<PricePointBookDiffUpdate>();

                        // 
                        // register consumer to async perform updated on the price book
                        //
                        Task.Factory.StartNew(async () =>
                        {
                            await ConsumeUpdatesAsync(pricePointBook, chan);
                        });

                        channels[pricePointBook.Symbol] = chan;
                    }
                }
            }

            return channels[pricePointBook.Symbol];
        }

        async Task ConsumeUpdatesAsync(PricePointBook pricePointBook, Channel<PricePointBookDiffUpdate> channel)
        {
            while (await channel.Reader.WaitToReadAsync())
            {
                while (channel.Reader.TryRead(out var update))
                {
                    pricePointBook.ApplyUpdates(
                                PricePoint.ToPricePoints(update.Bids),
                                PricePoint.ToPricePoints(update.Asks));

                    if (this.DiffUpdateDispatched != null)
                    {
                        
                        //
                        // signal for a completed diff update
                        //
                        this.DiffUpdateDispatched(this, update);
                    }

                    if (!taskCompletionSources.IsEmpty)
                    {
                        var key = getTaskCompletionSourcesKey(update.Symbol, update.ID);

                        if (taskCompletionSources.ContainsKey(key))
                        {
                            taskCompletionSources.Remove(key, out var taskCompletionSource);

                            if (taskCompletionSource != null)
                            {
                                taskCompletionSource.SetResult();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Constructs a look up key for a combination of symbol and diff update ID
        /// </summary>
        private string getTaskCompletionSourcesKey(string symbol, string targetEventID)
        {
            return $"{symbol}_{targetEventID}";
        }

        /// <summary>
        /// Interface for clients that need to await the processing of a specific async update
        /// </summary>
        public Task AwaitDispatchedEvent(String symbol, string targetEventID)
        {
            //
            // will be used to turn the async operation of awaiting the last dispatched event into 
            // task so it can be awaited from the main function before consuming the result of the updates
            //
            TaskCompletionSource taskCompletionSource;

            var key = getTaskCompletionSourcesKey(symbol, targetEventID);

            if (taskCompletionSources.ContainsKey(key))
            {
                taskCompletionSource= taskCompletionSources[key];
            } 
            else
            {
                taskCompletionSource = new TaskCompletionSource();
                taskCompletionSources[key] = taskCompletionSource;

            }

            return taskCompletionSource.Task;
        }
    }
}
