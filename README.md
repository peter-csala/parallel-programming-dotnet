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
TBD
