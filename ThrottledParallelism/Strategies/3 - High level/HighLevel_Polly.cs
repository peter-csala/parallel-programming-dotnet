using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Polly; //Policy.Bulkhead

namespace ThrottledParallelism.Strategies
{
    public class HighLevel_Polly : IGovernedParallelDownloader
    {
        private static readonly HttpClient client = new HttpClient();
        public Task DownloadThemAllAsync(IEnumerable<Uri> uris, ProcessResult processResult, byte maxThreads)
        {
            var throttler = Policy.BulkheadAsync(maxThreads, int.MaxValue); //!SPOT: maxQueuingActions param can cause BulkheadRejectedException

            //Single Producer
            //Waiting for all workers to finish
            return Task.WhenAll(
                uris.Select( //Multiple Workers
                    url => throttler.ExecuteAsync( //MaxDegreeOfParallelism
                        async () => await WorkerAsync(url, processResult)
                )));
        }

        async Task WorkerAsync(Uri uri, ProcessResult processResult)
        {
            var content = await client.GetStringAsync(uri).ConfigureAwait(false);
            processResult(Thread.CurrentThread.ManagedThreadId.ToString(), content);
        }
    }
}
