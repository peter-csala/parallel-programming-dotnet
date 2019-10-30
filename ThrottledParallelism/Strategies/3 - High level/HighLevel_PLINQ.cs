using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Linq; //ParallelQuery
using ThrottledParallelism.Helpers; //AsyncHelper

namespace ThrottledParallelism.Strategies
{
    public class HighLevel_PLINQ : IGovernedParallelDownloader
    {
        static readonly HttpClient client = new HttpClient();
        public Task DownloadThemAllAsync(IEnumerable<Uri> uris, ProcessResult processResult, byte maxThreads)
        {
            /* Blocking version BUT ForAll uses the NotBuffered option, which means whenever an item is processed it yields the element immediately
                https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/merge-options-in-plinq
                https://docs.microsoft.com/en-us/dotnet/api/system.linq.parallelenumerable.forall?view=netframework-4.8#remarks
              */
            //uris
            //    .AsParallel() 
            //    .WithDegreeOfParallelism(maxThreads)
            //    .WithExecutionMode(ParallelExecutionMode.ForceParallelism) 
            //    .ForAll(uri => AsyncHelper.RunSync(
            //        async () => await WorkerAsync(uri, processResult))); 

            //return Task.CompletedTask;

            //Non-blocking version
            ParallelQuery<Task> workers = uris
                .AsParallel() //!SPOT: It will not do what you think it should do, might run in sync
                .WithDegreeOfParallelism(maxThreads) //Fork phase
                .WithExecutionMode(ParallelExecutionMode.ForceParallelism) //!SPOT: Force to run in parallel
                .Select(uri => WorkerAsync(uri, processResult)); //!SPOT: Replaced ForAll to Select

            //Waiting for all workers to finish
            //var _ = workers.ToList(); //Hot tasks
            return Task.WhenAll(workers);  //Cold tasks < Join phase
        }

        async Task WorkerAsync(Uri uri, ProcessResult processResult)
        {
            var content = await client.GetStringAsync(uri).ConfigureAwait(false);
            processResult(Thread.CurrentThread.ManagedThreadId.ToString(), content);
        }
    }
}
