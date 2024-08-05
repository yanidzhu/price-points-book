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
    /// IDiffUpdatesDispatcher defines an interface for dispatching diff updates to a price point book
    /// </summary>
    public interface IDiffUpdatesDispatcher
    {
        event EventHandler<PricePointBookDiffUpdate> DiffUpdateDispatched;
        void DispatchDiffUpdates(PricePointBook pricePointBook, PricePointBookDiffUpdate update);
    }
}
