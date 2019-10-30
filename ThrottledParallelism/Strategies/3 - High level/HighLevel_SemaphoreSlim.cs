using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Threading; //SemaphoreSlim

namespace ThrottledParallelism.Strategies
{
    public class HighLevel_SemaphoreSlim : IGovernedParallelDownloader
    {
        private static readonly HttpClient client = new HttpClient();
        public Task DownloadThemAllAsync(IEnumerable<Uri> uris, ProcessResult processResult, byte maxThreads)
        {
            var throttler = new SemaphoreSlim(maxThreads);

            //Single Producer
            //Waiting for all workers to finish
            return Task.WhenAll(
                uris.Select(async uri => //Multiple Workers
                {
                    await throttler.WaitAsync(); //MaxDegreeOfParallelism
                    try { await WorkerAsync(uri, processResult); }
                    finally { throttler.Release(); }
                }
                ));
        }

        async Task WorkerAsync(Uri uri, ProcessResult processResult)
        {
            var content = await client.GetStringAsync(uri).ConfigureAwait(false);
            processResult(Thread.CurrentThread.ManagedThreadId.ToString(), content);
        }
    }
}
