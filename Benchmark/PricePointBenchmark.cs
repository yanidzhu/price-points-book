using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Diagnostics.Tracing.Parsers;
using PriceBookManagement.Entities;

namespace Benchmark
{
    [MemoryDiagnoser]
    public class PricePointBenchmarks
    {
        string[] input = new string[] { "0.0001", "0.0002" };
        
        int runs = 10000;
        

        [Benchmark]
        public void WithFastTryParse()
        {
            for(int i = 0; i < runs; i++)
            {
                PricePoint.TryParse(input, out var item);
            }
        }

        [Benchmark]
        public void WithBuildInTryParse()
        {
            for (int i = 0; i < runs; i++)
            {
                PricePoint.BuiltInTryParse(input, out var item);
            }
        }


        [Benchmark]
        public void WithBuiltInParse()
        {
            for (int i = 0; i < runs; i++)
            {
                PricePoint.BuildInParse(input, out var item);
            }
        }
    }
}
