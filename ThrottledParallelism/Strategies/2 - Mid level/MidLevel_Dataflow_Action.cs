using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Collections.Generic;

using System.Threading.Tasks; //CancellationTokenSource, TaskCompletionSource
using System.Threading.Tasks.Dataflow; //ActionBlock

namespace ThrottledParallelism.Strategies
{
    public class MidLevel_Dataflow_Action: IGovernedParallelDownloader
    {
        static readonly HttpClient client = new HttpClient();
        public Task DownloadThemAllAsync(IEnumerable<Uri> uris, ProcessResult processResult, byte maxThreads)
        {
            var jobSynchronizer = new CancellationTokenSource(); //!SPOT: Job synchronizer not Consumer synchronizer
            int remainingJobs = uris.Count(); //!SPOT: This can hurt
            
            //Multiple workers < Fork phase
            var workers = new ActionBlock<Uri>(
                async uri => 
                {
                    await WorkerAsync(uri, processResult);
                    Interlocked.Decrement(ref remainingJobs);

                    //Signaling all consumers have finished
                    if (remainingJobs == 0) jobSynchronizer.Cancel(); 
                }, 
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = maxThreads, //!SPOT: throttling is done the actionblock itself
                    CancellationToken = jobSynchronizer.Token //!SPOT: important to link cts to this block
                });


            //Single Producer
            Producer(workers, uris);

            //Waiting for all workers to finish
            var allDownloadCompletedTask = new TaskCompletionSource<object>(); //There is no non-generic tcs
            return workers.Completion //Cancel is signalled < Join phase
                .ContinueWith(t =>
                {
                    allDownloadCompletedTask.SetResult(null); //!SPOT: Translates cancellation into a success task
                }, TaskContinuationOptions.OnlyOnCanceled);
        }

        void Producer(ActionBlock<Uri> workers, IEnumerable<Uri> uris)
        {
            uris.Select(workers.Post)
                .ToList(); //!SPOT: Explicit materialization is needed in order to start posting
        }

        //!SPOT: There is no consumer method
        
        async Task WorkerAsync(Uri uri, ProcessResult processResult)
        {
            var content = await client.GetStringAsync(uri).ConfigureAwait(false);
            processResult(Thread.CurrentThread.ManagedThreadId.ToString(), content);
        }
    }
}
