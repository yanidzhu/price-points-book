using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PriceBookManagement.Models
{
    /// <summary>
    /// PricePointBookSnapshot represents a price book snapshot for a single symbol as transfarred via websocket
    /// </summary>
    public class PricePointBookSnapshot
    {
        public string[][] Bids { get; set; }
        public string[][] Asks { get; set; }
    }
}
