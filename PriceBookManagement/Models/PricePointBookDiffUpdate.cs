using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PriceBookManagement.Models
{
    /// <summary>
    /// PricePointBookDiffUpdate represents a price book diff update for a single symbol as transfarred via websocket
    /// </summary>
    public class PricePointBookDiffUpdate
    {
        public string ID { get; set; }
        public string Symbol { get; set; }
        public string[][] Bids { get; set; }
        public string[][] Asks { get; set; }
    }
}
