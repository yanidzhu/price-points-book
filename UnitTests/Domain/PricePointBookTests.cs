using PriceBookManagement.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.Domain
{
    public class PricePointBookTests
    {
        [Fact]
        public void GetBestBid_ReturnDefaultValue_WithEmptyBook()
        {
            var book = new PricePointBook("BNBBTC");

            var bestBid = book.GetBestBid();

            Assert.Equal(0, bestBid.Price);
            Assert.Equal(0, bestBid.Quantity);
        }

        [Fact]
        public void GetBestAsk_ReturnDefaultValue_WithEmptyBook()
        {
            var book = new PricePointBook("BNBBTC");

            var bestAsk = book.GetBestAsk();

            Assert.Equal(0, bestAsk.Price);
            Assert.Equal(0, bestAsk.Quantity);
        }

        [Fact]
        public void GetBestBid_ReturnExpectedValue_WithPreloadedBids()
        {
            var book = new PricePointBook("BNBBTC");

            PricePoint[] bids = new PricePoint[] {
                new PricePoint{ Price=0.001f, Quantity=100, },
                new PricePoint{ Price=0.002f, Quantity=200, },
                new PricePoint{ Price=0.01f, Quantity=300, },
            };

            book.ApplyUpdates(bids, asksUpdates:null);

            var bestBid = book.GetBestBid();

            Assert.Equal(0.01f, bestBid.Price);
            Assert.Equal(300, bestBid.Quantity);
        }


        [Fact]
        public void GetBestAsk_ReturnExpectedValue_WithPreloadedBids()
        {
            var book = new PricePointBook("BNBBTC");

            PricePoint[] asks = new PricePoint[] {
                new PricePoint{ Price=0.001f, Quantity=100, },
                new PricePoint{ Price=0.002f, Quantity=200, },
                new PricePoint{ Price=0.01f, Quantity=300, },
            };

            book.ApplyUpdates(bidsUpdates:null, asksUpdates: asks);

            var bestAsk = book.GetBestAsk();

            Assert.Equal(0.001f, bestAsk.Price);
            Assert.Equal(100, bestAsk.Quantity);
        }
    }
}
