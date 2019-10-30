# Welcome to the world of Parallel Programming in .NET

This repository's main purpose is to demonstrate the various tools that the .NET ecosystem provides us to write code that can run in parallel. Fill free to contribute. :)

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

## Demonstrated tools
### Low level abstrantions
No. |   Channel | Synchronizer | Workers by | File
--- | --- | --- | --- | ---
1 | BlockingCollection | AsyncCountdownEvent | ThreadPool.QueueUserWorkItem | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/1-%20Low%20%20level/Lowlevel_Oldest.cs)
2 | BlockingCollection | CountdownEvent | ThreadPool.QueueUserWorkItem | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/1-%20Low%20%20level/LowLevel_AsyncHelper_RunSync.cs)
3 | BlockingCollection | Parent Task | Children Tasks | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/1-%20Low%20%20level/LowLevel_Task_AttachToParent.cs)
4 | BlockingCollection | Task.WhenAll | Task | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/1-%20Low%20%20level/LowLevel_Task.cs)
5 | IEnumerable<KeyValuePair<Uri, Func<Uri, Task>> | MoreLinq's [Segment](http://morelinq.github.io/2.6/ref/api/html/M_MoreLinq_MoreEnumerable_Segment__1.htm) + Task.WhenAll | Task | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/1-%20Low%20%20level/LowLevel_Job_Segment.cs)
6 | IGrouping<int, Job> | MoreLinq's [GroupAdjacent](https://morelinq.github.io/2.7/ref/api/html/M_MoreLinq_MoreEnumerable_GroupAdjacent__2_1.htm) + Task.WhenAll | Task | [Link](https://github.com/peter-csala/parallel-programming-dotnet/blob/master/ThrottledParallelism/Strategies/1-%20Low%20%20level/LowLevel_Job_GroupAdjacent.cs)

### Mid level abstrantions
No. |   Channel | Synchronizer | Workers by | File
--- | --- | --- | --- | ---
TBD | TBD | TBD | TBD | TBD

### High level abstrantions
No. |   Channel | Synchronizer | Workers by | File
--- | --- | --- | --- | ---
TBD | TBD | TBD | TBD | TBD
