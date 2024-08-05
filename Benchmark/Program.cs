// See https://aka.ms/new-console-template for more information
using Benchmark;
using BenchmarkDotNet.Running;

//
// Running all benchmarks at once will take some time 
// Uncomment specific benchmarks to execute them 


//BenchmarkRunner.Run<PricePointBenchmarks>();
//BenchmarkRunner.Run<PricePointBookBenchmarks>();
//BenchmarkRunner.Run<PricePointStoreBookLoadSnapshotBenchmarks>();
BenchmarkRunner.Run<PricePointBooksStoreDiffUpdatesBenchmarks>();
