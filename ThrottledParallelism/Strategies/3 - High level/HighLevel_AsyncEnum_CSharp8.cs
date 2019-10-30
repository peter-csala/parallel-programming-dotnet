using System;
using System.Net.Http;
using System.Threading.Tasks;

using System.Threading; //SemaphoreSlim
using System.Collections.Generic; //IAsyncEnumerable
using System.Collections.Immutable; //ToImmutableList

namespace ThrottledParallelism.Strategies
{
    public class HighLevel_AsyncEnum_CSharp8 : IGovernedParallelDownloader
    {
        static readonly HttpClient client = new HttpClient();
        public async Task DownloadThemAllAsync(IEnumerable<Uri> uris, ProcessResult processResult, byte maxThreads)
        {
            var throttler = new SemaphoreSlim(maxThreads);
            var workers = new List<Task>();
            Task lastGroupOfWorkers = null; //!SPOT: await only the last chunck

            //Single Producer
            await foreach (var uri in ProducerAsync(uris, throttler))
            {
                //Multiple Workers
                workers.Add(WorkerAsync(uri, processResult)
                                .ContinueWith(_ => throttler.Release())); //Signaling consumption is over

                if (workers.Count == maxThreads)
                {
                    var currentJobs = workers.ToImmutableList();
                    workers.Clear();

                    //!SPOT: No need to await it, because the IAsyncEnumerator's MoveNextAsync task
                    //! will not be completed until one of the workers finishes
                    lastGroupOfWorkers = Task.WhenAll(currentJobs); //Fork phase
                }
            }

            //Waiting for the last group of workers to finish
            await lastGroupOfWorkers
                .ContinueWith(_ => throttler?.Dispose()); //Join phase
        }

        async IAsyncEnumerable<Uri> ProducerAsync(IEnumerable<Uri> uris, SemaphoreSlim throttler) //!SPOT: async IAsyncEnumerable
        {
            foreach (var uri in uris)
            {
                await throttler.WaitAsync(); //This will block until there is a free worker
                yield return uri;
            }
        }

        async Task WorkerAsync(Uri uri, ProcessResult processResult)
        {
            var content = await client.GetStringAsync(uri).ConfigureAwait(false);
            processResult(Thread.CurrentThread.ManagedThreadId.ToString(), content);
        }
    }
}
