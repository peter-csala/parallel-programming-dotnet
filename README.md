# Welcome to the world of Parallel Programming in .NET

This repository's main purpose is to demonstrate the various tools that the .NET ecosystem provides us to write code that can run in parallel. Feel free to contribute. :)

## Required environment
- .NET Core 3.0
- Visual Studio 2019

## Demo Application
In order to demonstrate the different capabilities, we need a demo application.  
In our case this app will be a **throttled parallel downloader**.
The solution contains three projects: 

### I. - Static File server
It is an ASP.NET Core 3.0 web-application which can serve static files for http clients.  
It exposes the files under the *Resources* folder through the */resources* route.  
Related project: **ThrottledParallelism**

### II. - Throttled Downloader library
It is a .NET Core 3.0 library, which is exposing a simple API and several implementations of it.
```csharp
public interface IGovernedParallelDownloader
{
    Task DownloadThemAllAsync(IEnumerable<Uri> uris, ProcessResult processResult, byte maxThreads);
}
```
Related project: **LogFileServer**

### III. - Benchmark tool
It is a .NET Core 3.0 console application, which is used to perform micro-benchmarking.
It measures execution time, GC cycles, etc.  
Related project: **RunEverythingInParallel**

## Instructions for running
### Benchmark
1) Make sure that Program.cs of the RunEverythingInParallel project look like this:
```csharp
using System;
using System.Threading;
using BenchmarkDotNet.Running; //BenchmarkRunner

namespace RunEverythingInParallel
{
    class Program
    {
        //Use Release 
        static void Main(string[] args)
        {
            Thread.Sleep(1000); //Wait for the WebApp to start
            BenchmarkRunner.Run<ThrottledDownloader>(); //Add as many benchmarks as you want to run
            Console.ReadLine();
        }
    }
}
```
2) Build the solution in Release mode (Set the **Optimize** node in the csproj to true if it is needed)  
3) Hit Ctrl+F5 in Visual Studio
4) If you want to run it without VS (by using the dotnet cli) then run the LogFileServer project prior the RunEverythingInParallel
### Debug
1) Make sure that Program.cs of the RunEverythingInParallel project look like this:
```csharp
using System;
using System.Threading;
using BenchmarkDotNet.Running; //BenchmarkRunner

namespace RunEverythingInParallel
{
    class Program
    {
        //Use Debug
        static void Main(string[] args)
        {
            Thread.Sleep(1000); //Wait for the WebApp to start
            var downloader = new ThrottledDownloader();
            downloader.Setup();
            downloader.RunCSharp3(); //Choose that method which exposes the implementation which you want to debug
        }
    }
}
```
2) Build the solution in Debug mode (Set the **Optimize** node in the csproj to false if it is needed)  
3) Hit F5 in Visual Studio
4) Analyze the choosen code by using the [Parallel Watch, Parallel Stack and Tasks windows](https://docs.microsoft.com/en-us/visualstudio/debugger/walkthrough-debugging-a-parallel-application?view=vs-2019#using-the-parallel-stacks-window-threads-view)

## Demonstrated tools
### Baselines
No. |   Channel | Synchronizer | Workers via | Throttled by | File
--- | --- | --- | --- | --- | ---
1 | IEnumerable | - | main thread | - | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/0%20-%20BaseLine/BaseLine_SyncVersion.cs)
2 | IEnumerable | Task.WhenAll |Task | - | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/0%20-%20BaseLine/BaseLine_WithoutThrottling.cs)

### Low level abstractions
No. |   Channel | Synchronizer | Workers via | Throttled by | File
--- | --- | --- | --- | --- | ---
1 | [BlockingCollection](https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.blockingcollection-1?view=netcore-3.0) | [AsyncCountdownEvent](https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.threading.asynccountdownevent?view=visualstudiosdk-2019) | [ThreadPool.QueueUserWorkItem](https://docs.microsoft.com/en-us/dotnet/api/system.threading.threadpool.queueuserworkitem?view=netcore-3.0) | Manually (for (i = 0; i < maxThreads; i++)) | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/1-%20Low%20%20level/Lowlevel_Oldest.cs)
2 | BlockingCollection | [CountdownEvent](https://docs.microsoft.com/en-us/dotnet/api/system.threading.countdownevent?view=netcore-3.0) | ThreadPool.QueueUserWorkItem | Manually (for (i = 0; i < maxThreads; i++)) | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/1-%20Low%20%20level/LowLevel_AsyncHelper_RunSync.cs)
3 | BlockingCollection | Parent Task | Children Tasks | Manually (for (i = 0; i < maxThreads; i++)) | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/1-%20Low%20%20level/LowLevel_Task_AttachToParent.cs)
4 | BlockingCollection | Task.WhenAll | Task | Manually (for (i = 0; i < maxThreads; i++)) | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/1-%20Low%20%20level/LowLevel_Task.cs)
5 | IEnumerable<KeyValuePair<Uri, Func<Uri, Task>> | Task.WhenAll | Task | Load balancing by MoreLinq's [Segment](http://morelinq.github.io/2.6/ref/api/html/M_MoreLinq_MoreEnumerable_Segment__1.htm)  | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/1-%20Low%20%20level/LowLevel_Job_Segment.cs)
6 | IGrouping<int, Job> | Task.WhenAll | Task | Load balancing by MoreLinq's [GroupAdjacent](https://morelinq.github.io/2.7/ref/api/html/M_MoreLinq_MoreEnumerable_GroupAdjacent__2_1.htm) | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/1-%20Low%20%20level/LowLevel_Job_GroupAdjacent.cs)

### Mid level abstractions
No. |   Channel | Synchronizer | Workers via | Throttled by | File
--- | --- | --- | --- | --- | ---
1 | [ActionBlock](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.dataflow.actionblock-1?view=netcore-3.0) | [CancellationTokenSource](https://docs.microsoft.com/en-us/dotnet/api/system.threading.cancellationtokensource?view=netcore-3.0) + [Interlocked](https://docs.microsoft.com/en-us/dotnet/api/system.threading.interlocked?view=netcore-3.0) | Task | [ExecutionDataflowBlockOptions](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.dataflow.executiondataflowblockoptions?view=netcore-3.0) | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/2%20-%20Mid%20level/MidLevel_Dataflow_Action.cs)
2 | ActionBlock + [BatchBlock](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.dataflow.batchblock-1?view=netcore-3.0) | [PropagateCompletion](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.dataflow.dataflowlinkoptions.propagatecompletion?view=netcore-3.0#System_Threading_Tasks_Dataflow_DataflowLinkOptions_PropagateCompletion) + [Completion](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.dataflow.actionblock-1.completion?view=netcore-3.0) | Task | ExecutionDataflowBlockOptions | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/2%20-%20Mid%20level/MidLevel_Dataflow_Batch.cs)
3 | [BufferBlock](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.dataflow.bufferblock-1?view=netcore-3.0) | Task.WhenAll + [ImmutableList](https://docs.microsoft.com/en-us/dotnet/api/system.collections.immutable.immutablelist-1.builder.toimmutable?view=netcore-3.0#System_Collections_Immutable_ImmutableList_1_Builder_ToImmutable) | Task | Manually (for (i = 0; i < maxThreads; i++)) | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/2%20-%20Mid%20level/MidLevel_Dataflow_Buffer.cs)
4 | [Channel](https://docs.microsoft.com/en-us/dotnet/api/system.threading.channels.channel.createbounded?view=netcore-3.0) | Task.WhenAll | Task | Manually (Enumerable.Range(0, maxThreads -1)) | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/2%20-%20Mid%20level/MidLevel_Channels.cs)

### High level abstractions
No. |   Channel | Synchronizer | Workers via | Throttled by | File
--- | --- | --- | --- | --- | ---
1 | [Partitioner](https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.partitioner.create?view=netcore-3.0) | [Parallel.Foreach](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.parallel.foreach?view=netcore-3.0) | Task + AsyncHelper.RunSync!!! | [ParallelOptions](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.paralleloptions?view=netcore-3.0) | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/3%20-%20High%20level/HighLevel_Parallel_Foreach.cs)
2 | IGrouping<int, Uri> | [Parallel.Invoke](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.parallel.invoke?view=netcore-3.0) | Task + AsyncHelper.RunSync!!! | GroupBy (round robin) | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/3%20-%20High%20level/HighLevel_Parallel_Invoke.cs) 
3 | IGrouping<int, Uri> | [Parallel.For +TLS](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.parallel.for?view=netcore-3.0#System_Threading_Tasks_Parallel_For__1_System_Int32_System_Int32_System_Func___0__System_Func_System_Int32_System_Threading_Tasks_ParallelLoopState___0___0__System_Action___0__) | Task + AsyncHelper.RunSync!!! | GroupBy + Parallel.For | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/3%20-%20High%20level/HighLevel_Parallel_For_TLS.cs)
4 | [ConcurrentQueue](https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentqueue-1?view=netcore-3.0) | Task.WhenAll | Task | Manually (Enumerable.Range(0, maxThreads -1)) | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/3%20-%20High%20level/HighLevel_ConcurrentQueue.cs)
5 | IEnumerable<Uri> | [ParallelForEachAsync](https://github.com/Dasync/AsyncEnumerable#example-3-async-parallel-for-each) | Task | ParalellelForEachAsync | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/3%20-%20High%20level/HighLevel_AsyncEnumerator.cs)
6 | HashSet<Task> | Task.**WhenAny** | Task | Manually only during initialization | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/3%20-%20High%20level/HighLevel_AsyncEnumerator_Feeder.cs)
7 | [IAsyncEnumerable<Uri>](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1?view=netcore-3.0) | await last Task | Task |[SemaphoreSlim](https://docs.microsoft.com/en-us/dotnet/api/system.threading.semaphoreslim?view=netcore-3.0) | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/3%20-%20High%20level/HighLevel_AsyncEnum_CSharp8.cs)
8 | [ParallelQuery](https://docs.microsoft.com/en-us/dotnet/api/system.linq.parallelquery?view=netcore-3.0) | Task.WhenAll | Task | [WithDegreeOfParallelism](https://docs.microsoft.com/en-us/dotnet/api/system.linq.parallelenumerable.withdegreeofparallelism?view=netcore-3.0#System_Linq_ParallelEnumerable_WithDegreeOfParallelism__1_System_Linq_ParallelQuery___0__System_Int32_) | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/3%20-%20High%20level/HighLevel_PLINQ.cs)
9 | IEnumerable | Task.WhenAll | Task | SemaphoreSlim | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/3%20-%20High%20level/HighLevel_SemaphoreSlim.cs)
10 | IEnumerable | Task.WhenAll | Task | [BulkheadAsync](https://github.com/App-vNext/Polly/wiki/Bulkhead) | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/3%20-%20High%20level/HighLevel_Polly.cs)

## Sample benchmark result
BenchmarkDotNet=v0.11.5   
OS=Windows 10.0.17134.1069 (1803/April2018Update/Redstone4)  
Intel Core i7-8650U CPU 1.90GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores Frequency=2062501 Hz, Resolution=484.8482 ns
.NET Core SDK=3.0.100  
  Host     : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), 64bit RyuJIT  
IterationCount=3  RunStrategy=ColdStart    

Method | 	Mean	Error |	StdDev |	Ratio |	RatioSD |	Gen 0 |	Gen 1 |	Gen 2 |	Allocated
--- | --- | --- | --- | --- | --- | --- | --- | ---
BaseLine_Sequentially |	3.151 s |	1.3267 s |	0.0727 s |	1.00	0.00 |	377000.0000 |	47000.0000 |	38000.0000 |	1600419.34 KB
BaseLine_EmbarrassinglyParallel |	1.055 s |	1.8874 s |	0.1035 s |	0.33 |	0.03 |	270000.0000 |	1000.0000 |	1000.0000 |	58.29 KB
CSharp3 |	1.426 s |	0.7124 s |	0.0390 s |	0.45 |	0.02 |	320000.0000 |	8000.0000 |	7000.0000 |	1.98 KB
CSharp5 |	1.943 s |	1.6989 s |	0.0931 s |	0.62 |	0.02 |	383000.0000 |	4000.0000 |	4000.0000 |	4.32 KB
CSharp6 |	1.318 s |	0.6582 s |	0.0361 s |	0.42 |	0.00 |	367000.0000 |	1000.0000 |	1000.0000 |	22.23 KB
CSharp8 |	1.340 s |	1.8518 s |	0.1015 s |	0.42 |	0.02 |	300000.0000 |	4000.0000 |	4000.0000 |	13.41 KB
Bonus |	1.999 s |	2.2747 s |	0.1247 s |	0.63 |	0.04 |	405000.0000 |	3000.0000 |	3000.0000 |	30.4 KB

## Known missing sample codes
- foreach + AsParallel
- Reactive eXtensions
- ideas are more than welcome
