using PriceBookManagement.Entities;
using PriceBookManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PriceBookManagement.Domain.UpdatesDispatchers
{
    /// <summary>
    /// SyncUpdatesDispatcher routes diff updates synchronously to a price book object
    /// </summary>
    public class SyncUpdatesDispatcher: IDiffUpdatesDispatcher
    {
        public event EventHandler<PricePointBookDiffUpdate> DiffUpdateDispatched;

        public void DispatchDiffUpdates(PricePointBook pricePointBook, PricePointBookDiffUpdate update)
        {
            pricePointBook.ApplyUpdates(
                                PricePoint.ToPricePoints(update.Bids),
                                PricePoint.ToPricePoints(update.Asks)
                                );

            if (this.DiffUpdateDispatched != null)
            {
                this.DiffUpdateDispatched(this, update);
            }
        }
    }
}
