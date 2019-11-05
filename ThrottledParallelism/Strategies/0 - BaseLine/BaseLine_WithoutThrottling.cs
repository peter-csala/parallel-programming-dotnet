using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using ThrottledParallelism.Helpers; //GetAwaiter for IEnumerable<Task>

namespace ThrottledParallelism.Strategies
{
    public class BaseLine_WithoutThrottling : IGovernedParallelDownloader
    {
        static readonly HttpClient client = new HttpClient();
        public async Task DownloadThemAllAsync(IEnumerable<Uri> uris, ProcessResult processResult, byte maxThreads)
        {
            Task workerTask(Uri uri) => WorkerAsync(uri, processResult); //!SPOT: Partial function
            await uris.Select(workerTask); //!SPOT: Customer awaiter
        }

        //public Task DownloadThemAllAsync(IEnumerable<Uri> uris, ProcessResult processResult, byte maxThreads)
        //{
        //    Task workerTask(Uri uri) => WorkerAsync(uri, processResult); //!SPOT: Partial function
        //    return Task.WhenAll(uris.Select(workerTask));
        //}

        async Task WorkerAsync(Uri uri, ProcessResult processResult)
        {
            var content = await client.GetStringAsync(uri).ConfigureAwait(false);
            processResult(Thread.CurrentThread.ManagedThreadId.ToString(), content);
        }
    }
}
