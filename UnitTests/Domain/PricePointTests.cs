using PriceBookManagement.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.Domain
{
    public class PricePointTests
    {
        [Fact]
        public void TryParse_ReturnsFalse_WithEmptyArray()
        {
            var parsed = PricePoint.TryParse(null, out var result);

            Assert.False(parsed);
        }

        [Theory]
        [InlineData("0.00915100", "0.14800000", "0.14800000")] // 3 strings
        [InlineData("A", "0.14800000")] // one of the strings is not valid amount
        [InlineData("", "")]
        [InlineData(null, null)]
        [InlineData("0.14800000", null)]
        public void TryParse_ReturnsFalse_WithInvaidInput(params string[] input)
        {
            var parsed = PricePoint.TryParse(input, out var result);

            Assert.False(parsed);
        }

        [Theory]
        [InlineData(0.00915100f, 0.14800000f, "0.00915100", "0.14800000")] // small value price and volume
        [InlineData(0.00915101f, 0.14800001f, "0.00915101", "0.14800001")] // hiher precision price and volume
        [InlineData(1234567891234567890.00915101f, 0.14800001f, "1234567891234567890.00915101", "0.14800001")]
        [InlineData(-0.00915101f, -0.14800001f, "-0.00915101", "-0.14800001")] // negavite numbers
        [InlineData(5f, 1000f, "5", "1000")] // no floating point
        public void TryParse_ReturnsTrue_WithValidInput(float price, float volume, params string[] input)
        {
            var parsed = PricePoint.TryParse(input, out var result);

            Assert.True(parsed);
            Assert.Equal(price, result.Price);
            Assert.Equal(volume, result.Quantity);
        }
       
    }
}
