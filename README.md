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

## Instructions
TBD

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
6 | | | | | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/3%20-%20High%20level/HighLevel_AsyncEnumerator_Feeder.cs)
7 | | | | | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/3%20-%20High%20level/HighLevel_AsyncEnum_CSharp8.cs)
8 | | | | | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/3%20-%20High%20level/HighLevel_PLINQ.cs)
9 | | | | | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/3%20-%20High%20level/HighLevel_SemaphoreSlim.cs)
10 | | | | | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/3%20-%20High%20level/HighLevel_Polly.cs)

## Known missing sample codes
- foreach + AsParallel
- Reactive eXtensions
- ideas are more than welcome
