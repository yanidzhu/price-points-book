## System Requirements

The solution is built with .net 8 and Visual Studio 2022

## Implementation Approach

The solution models the problem by introducing the following abstractions:
* `PricePoint` - represents a combination of price along with its quantity in the price book
* `PricePointBookSide` - a set of PricePoints representing a side in the book (e.g. bids or asks), storing the order of price points and their quantities in optimal data structures for insertion, update, lookup and traversal
* `PricePointBook` represents the actual book - consists of two sides (bids and asks) and expose interface for loading snapshots and process diff updates
* `PricePointBookStore` represents a collection of price books for all system supported symbols - acts as an interface to web socket clients - they are expected not to interfere directly with price books
	* Supports different way of processing diff updates - synchronously for all symbols (implemented by `SyncUpdatesDispatcher`) or asynchronously per symbol `AsyncUpdatesDispatcher` to allow parallel processing of different symbol updates and avoid hot spots of popular symbols 
* `PricePointBookSnapshot` and `PricePointBookDiffUpdate` represents the exchange entities as coming from the web sockets
 
### Data structure selection

The solution uses a combination of two internal data structures in `PricePointBookSide`:
* SortedSet (Binary Search Tree) to store prices in ordered way
* Dictionary (hash table) to store quantity per price

The selection was done based on the below comparison:

| Operation / Data struct | PriorityQueue (Binary Heap)      | SortedSet (Binary Search Tree)    | SortedList   |
|------------------------ |---------------------------------:|----------------------------------:|-------------:|
| Insert Element          | O(logN)                          | O(logN)                           | O(N)         |
| Delete Element          | O(logN)                          | O(logN)                           | O(N)         |
| Get Best Price          | O(1)                             | O(1)                              | O(1)         |
| Contains                | O(logN)                          | O(logN)                           | O(N)         |
| Traverse Book In Order  | O(NlogN)                         | O(N)                              | O(N)         |

The usage of a second data structure (Dictionary) to store quantities aims to allow processing quantity updates with time complexity O(1) instead of O(logN) - this comes with the expense of allocate additional memory. 
However Dictionary O(N) + SortedSet O(N) => O(2N) ~ O(N) - memory complexity remains linear.

### Memory usage considerations

The following memory usage considerations were taken:

* Use structs to represent `PricePoints` during updates to avoid allocations on heap 
* Use float instead of decimal to reduce memory used per price / quantity
	* Unexpected rounding errors can occur in arithmetic calculations when you use double or float
	* As the solution does not require mathematical operations over the amounts (e.g. +, - , / , *) it's safe to use float 
	* That results in 4x less storage for amounts - float - 4 bytes, double - 8 bytes , decimal - 16 bytes
	* The solution can be fairly easy migrated to use decimals if the system evolves in the future

##### Struct vs Class

 Benchmarked with a price points book with 5k prices on each side

Struct: 

| Method              | Mean     | Error    | StdDev   | Gen0     | Gen1    | Gen2    | Allocated |
|-------------------- |---------:|---------:|---------:|---------:|--------:|--------:|----------:|
| WithSerialUpdates   | 14.87 ms | 0.875 ms | 2.497 ms | 281.2500 | 46.8750 | 46.8750 |   1.26 MB |
| WithParallelUpdates | 16.16 ms | 0.321 ms | 0.315 ms | 343.7500 | 62.5000 | 31.2500 |   1.66 MB |


Class: 

| Method              | Mean     | Error    | StdDev   | Gen0      | Gen1     | Gen2    | Allocated |
|-------------------- |---------:|---------:|---------:|----------:|---------:|--------:|----------:|
| WithSerialUpdates   | 13.09 ms | 0.349 ms | 1.024 ms | 1453.1250 | 140.6250 | 62.5000 |   6.11 MB |
| WithParallelUpdates | 16.68 ms | 0.318 ms | 0.366 ms | 1562.5000 |  93.7500 | 31.2500 |   6.51 MB |

Memory usage to allocate 2000 price books with 5000 prices in each side is about ~600mb

| Method                  | Mean    | Error    | StdDev   | Gen0       | Gen1       | Gen2      | Allocated |
|------------------------ |--------:|---------:|---------:|-----------:|-----------:|----------:|----------:|
| LoadSnapshots           | 2.001 s | 0.0389 s | 0.0416 s | 49000.0000 | 23000.0000 | 9000.0000 | 599.95 MB |
| LoadSnapshotsInParallel | 1.861 s | 0.0299 s | 0.0249 s | 49000.0000 | 23000.0000 | 9000.0000 | 600.93 MB |

### CPU optimizations

* One of the time consuming task as part of processing snapshots and updates is parsing raw price and quantity data from string to float
	* Instead of using the built-in parsing functions the solution uses an optimized on - https://github.com/CarlVerret/csFastFloat/tree/master
	* That is 2.5x faster compared to the standard parsing functions
	
| Method              | Mean    | Error    | StdDev   | Allocated |
|-------------------- |--------:|---------:|---------:|----------:|
| WithFastTryParse    | 1.110 s | 0.0845 s | 0.2438 s |      64 B |
| WithBuildInTryParse | 2.699 s | 0.0875 s | 0.2511 s |      64 B |
| WithBuiltInParse    | 2.858 s | 0.0921 s | 0.2686 s |     400 B |

### Parallel processing

Explored alternatives:
1. Parallel updates on bids and asks in a price point book as they don't share memory

* Parallel import of bids/asks as part of snapshot loading - 2k books with 5k records each side

| Method                  | Mean    | Error    | StdDev   | Gen0       | Gen1       | Gen2      | Allocated |
|------------------------ |--------:|---------:|---------:|-----------:|-----------:|----------:|----------:|
| LoadSnapshots           | 2.001 s | 0.0389 s | 0.0416 s | 49000.0000 | 23000.0000 | 9000.0000 | 599.95 MB |
| LoadSnapshotsInParallel | 1.861 s | 0.0299 s | 0.0249 s | 49000.0000 | 23000.0000 | 9000.0000 | 600.93 MB |
 
* Parallel processing of bids/ask as part of diff update processing - a book with 5k records each side - a diff update with 10-100 bids/asks

Benchmark for 1000 updates with 10-100 bids/asks on a book with 5k prices on each side: 

| Method              | Mean     | Error    | StdDev   | Median   | Gen0     | Allocated |
|-------------------- |---------:|---------:|---------:|---------:|---------:|----------:|
| WithSerialUpdates   | 13.29 ms | 1.392 ms | 3.926 ms | 12.07 ms | 125.0000 | 539.77 KB |
| WithParallelUpdates | 15.16 ms | 0.471 ms | 1.313 ms | 14.67 ms | 218.7500 | 946.14 KB |

The synchronization of tasks in parallel processing adds a small overhear due to the low number of events in a diff update. Thus sequential updates of bids and asks is with lower latency.

* Async processing of diff updates using .net core channels
	* It's possible updates for popular symbols to create hot spots with frequent updates blocking processing diff updates for less popular symbols
	* It's important that updates for a single symbol are processed sequentially to ensure updates are not lost

| Method                                  | Mean     | Error   | StdDev   | Gen0       | Gen1      | Allocated |
|---------------------------------------- |---------:|--------:|---------:|-----------:|----------:|----------:|
| LoadSnapshotsInParallelWithSyncUpdates  | 462.8 ms | 8.85 ms |  6.91 ms | 43000.0000 | 9000.0000 | 175.16 MB |
| LoadSnapshotsInParallelWithAsyncUpdates | 305.8 ms | 6.07 ms | 10.46 ms | 44000.0000 | 9000.0000 |    176 MB |

Async updates via channels result in about 30% in latency without memory overhead but with more complex synchronization approach.
The above benchmark is over 10 000 diff updates (with 100 points in each side of the book - bids/asks) over a collection of 2k price books.

## Commands

### Build the solution

```sh
  dotnet build 
  ```

### Run example input and events

```sh
  cd .\PriceBookManagement\
  dotnet run 
  ```

### Run unit tests

Unit tests are created with xUnit framework

```sh
  cd .\UnitTests\
  dotnet test 
  ```

### Run benchmarks

The benchmarks are setup using <a href='https://github.com/dotnet/BenchmarkDotNet'>BenchmarkDotNet</a>

```sh
  cd .\Benchmark\
  dotnet run --configuration Release
  ```

### Possible further improvements and optimizations

* Code style and abstractions - revise the code based on peers code review feedback
* Unit tests suite is not exhaustive - 90%+ code coverage (line and branch) is reasonable to achieve
* Data validation
	* Ensure prices and quantities don't have negative values
	* Ensure further validation on string amounts incoming from trade exchange
	* More exhaustive checks for null input
	* Handle different case-sensitivity of symbols
* Optimizations
	* Remove internal abstractions such as `PricePoint` struct and have components work with raw arrays as coming from stream - this will be a trade off with code reuse and maintainability 
	* Experiment with fixed number of channels in `AsyncUpdatesDispatcher` - currently the implementation spawns channel per symbol, it possible to have smaller number of channels and route Symbols to channels consistently based on hashing function
	* Reasonable memory usage in a process depends on the hosting environment - based the memory allocation to a host we may further reduce memory usage or improve code abstractions by increasing memory usage 
	* Experiment with LRU cache for string to float mapping - prices and quantities arrive as strings via the web socket streams - prices around currently traded volumes would be re-occurring values - instead of parsing them evaluate performance of caching the parsed floats with a cache that expires
	* Analyze usage patterns of price books - write vs reads frequency and consistency needs - possible changes in synchronization mechanisms - for example to allow parallel reads
* Remove code that was used only for benchmark comparison 