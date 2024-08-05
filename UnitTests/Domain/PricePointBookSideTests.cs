using PriceBookManagement.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.Domain
{
    public class PricePointBookSideTests
    {
        [Fact]
        public void GetOptimalPrice_ReturnDefaultValue_WithEmptySide()
        {
            var side = new PricePointBookSide(sortedDesc: true);

            var optimal = side.GetOptimalPrice();

            Assert.Equal(0, optimal.Price);
            Assert.Equal(0, optimal.Quantity);
        }

        [Fact]
        public void GetOptimalPrice_ReturnsMaxValue_WithVaryingUpsertsAndDescSorting()
        {
            var side = new PricePointBookSide(sortedDesc:true);

            side.UpsertPricePoint(new PricePoint() { Price = 0.0001f, Quantity = 100 });
            side.UpsertPricePoint(new PricePoint() { Price = 0.001f, Quantity = 200 });
            side.UpsertPricePoint(new PricePoint() { Price = 0.01f, Quantity = 300 });
            side.UpsertPricePoint(new PricePoint() { Price = 0.0002f, Quantity = 400 });

            var optimal = side.GetOptimalPrice();

            Assert.Equal(0.01f, optimal.Price);
            Assert.Equal(300, optimal.Quantity);
        }

        [Fact]
        public void GetOptimalPrice_ReturnsMaxValue_WithInitialOptimalPriceRemoved()
        {
            var side = new PricePointBookSide(sortedDesc: true);

            side.UpsertPricePoint(new PricePoint() { Price = 0.0001f, Quantity = 100 });
            side.UpsertPricePoint(new PricePoint() { Price = 0.001f, Quantity = 200 });
            side.UpsertPricePoint(new PricePoint() { Price = 0.01f, Quantity = 300 });
            side.UpsertPricePoint(new PricePoint() { Price = 0.0002f, Quantity = 400 });

            var optimal = side.GetOptimalPrice();

            Assert.Equal(0.01f, optimal.Price);
            Assert.Equal(300, optimal.Quantity);

            // the optimal price is removed
            side.UpsertPricePoint(new PricePoint() { Price = 0.01f, Quantity = 0 });

            optimal = side.GetOptimalPrice();

            Assert.Equal(0.001f, optimal.Price);
            Assert.Equal(200, optimal.Quantity);
        }

        [Fact]
        public void GetOptimalPrice_ReturnsCorrectQuantity_WithQuantityUpdate()
        {
            var side = new PricePointBookSide(sortedDesc: true);

            side.UpsertPricePoint(new PricePoint() { Price = 0.0001f, Quantity = 100 });
            side.UpsertPricePoint(new PricePoint() { Price = 0.001f, Quantity = 200 });
            side.UpsertPricePoint(new PricePoint() { Price = 0.01f, Quantity = 300 });
            side.UpsertPricePoint(new PricePoint() { Price = 0.0002f, Quantity = 400 });

            var optimal = side.GetOptimalPrice();

            Assert.Equal(0.01f, optimal.Price);
            Assert.Equal(300, optimal.Quantity);

            // the optimal price is removed
            side.UpsertPricePoint(new PricePoint() { Price = 0.01f, Quantity = 3 });

            optimal = side.GetOptimalPrice();

            Assert.Equal(0.01f, optimal.Price);
            Assert.Equal(3, optimal.Quantity);
        }

        [Fact]
        public void GetOptimalPrice_ReturnsMinValue_WithVaryingUpsertsAndAscSorting()
        {
            var side = new PricePointBookSide(sortedDesc:false);

            side.UpsertPricePoint(new PricePoint() { Price = 0.0001f, Quantity = 100 });
            side.UpsertPricePoint(new PricePoint() { Price = 0.001f, Quantity = 200 });
            side.UpsertPricePoint(new PricePoint() { Price = 0.01f, Quantity = 300 });
            side.UpsertPricePoint(new PricePoint() { Price = 0.0002f, Quantity = 400 });

            var optimal = side.GetOptimalPrice();

            Assert.Equal(0.0001f, optimal.Price);
            Assert.Equal(100, optimal.Quantity);
        }

        [Fact]
        public void GetOptimalPrice_ReturnsMinValue_WithUpsertsWithNegativePricesAndAscSorting()
        {
            var side = new PricePointBookSide(sortedDesc: false);

            side.UpsertPricePoint(new PricePoint() { Price = -0.0001f, Quantity = 100 });
            side.UpsertPricePoint(new PricePoint() { Price = -0.001f, Quantity = 200 });
            side.UpsertPricePoint(new PricePoint() { Price = -0.01f, Quantity = 300 });
            side.UpsertPricePoint(new PricePoint() { Price = -0.0002f, Quantity = 400 });

            var optimal = side.GetOptimalPrice();

            Assert.Equal(-0.01f, optimal.Price);
            Assert.Equal(300, optimal.Quantity);
        }
    }
}