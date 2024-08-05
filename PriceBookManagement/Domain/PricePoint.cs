using System;
using System.Windows.Markup;
using csFastFloat;

namespace PriceBookManagement.Entities
{
    /// <summary>
    /// PricePoint represents a pair of price along with offered quantity into a price book
    /// </summary>
    public struct PricePoint
    {
        public float Price { get; set; }
        public float Quantity { get; set; }

        /// <summary>
        /// Tries to parse a raw price point to the internal model
        /// </summary>
        /// <param name="rawPricePoint">A price point represented as two elements string array</param>
        /// <param name="result">The parsed price point as a struct value</param>
        /// <returns>True if the parsing is successful, false - otherwise</returns>
        public static bool TryParse(string[] rawPricePoint, out PricePoint result)
        {
            result = new PricePoint();
            if (rawPricePoint == null || rawPricePoint.Length != 2)
            {
                return false;
            }

            float price, quantity;

            if (FastFloatParser.TryParseFloat(rawPricePoint[0], out price) && FastFloatParser.TryParseFloat(rawPricePoint[1], out quantity))
            {
                result.Price = price;
                result.Quantity = quantity;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Parse a raw price point represented by a string array with two elements using the built in float.TryParse
        /// The function is exposed for benchmarking purposes only
        /// </summary>
        public static bool BuiltInTryParse(string[] rawPricePoint, out PricePoint result)
        {
            result = new PricePoint();
            if (rawPricePoint == null || rawPricePoint.Length != 2)
            {
                return false;
            }

            float price, quantity;

            if (float.TryParse(rawPricePoint[0], out price) && float.TryParse(rawPricePoint[1], out quantity)) {
                result.Price = price;
                result.Quantity = quantity;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Parse a raw price point represented by a string array with two elements using the built in float.Parse
        /// The function is exposed for benchmarking purposes only
        /// </summary>
        public static bool BuildInParse(string[] rawPricePoint, out PricePoint result)
        {
            result = new PricePoint();
            if (rawPricePoint == null || rawPricePoint.Length != 2)
            {
                return false;
            }

            try
            {
                result.Price = float.Parse(rawPricePoint[0]);
                result.Quantity = float.Parse(rawPricePoint[1]);

                return true;
            }
            catch
            {
            }
            
            return false;
        }

        /// <summary>
        /// Converts a price point (price with a quantity to a string array with two elements for optimal RPC transmission 
        /// </summary>
        public static string[] ToRawRepresentation(PricePoint pricePoint) {
            return [
                pricePoint.Price.ToString("0.0000"), 
                pricePoint.Quantity.ToString("0.00000000")
            ];
        }

        /// <summary>
        /// Convert an array of raw price points represented by a slice of strings with 2 elements
        /// 
        /// Example price point: ["0.0001", "100"] 
        /// </summary>
        public static PricePoint[] ToPricePoints(string[][] events)
        {
            if (events == null) {
                return null;
            }
            // 
            // allocate space for the array based on the events size to avoid resizing 
            // 
            var result = new PricePoint[events.Length];

            for (int i=0; i <events.Length; i++) { 
                
                PricePoint p = new PricePoint();

                if (PricePoint.TryParse(events[i], out p))
                {
                    result[i] = p;
                }
                else
                {
                    //
                    // emit metrics for invalid price points to raise an alert in case of regression in integration
                    // 
                }
            }

            return result;
        }
    }
}
