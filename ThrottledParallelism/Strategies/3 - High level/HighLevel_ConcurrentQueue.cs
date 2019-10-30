using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Collections.Concurrent; //ConcurrentQueue

namespace ThrottledParallelism.Strategies
{
    public class HighLevel_ConcurrentQueue : IGovernedParallelDownloader
    {
        static readonly HttpClient client = new HttpClient();
        public Task DownloadThemAllAsync(IEnumerable<Uri> uris, ProcessResult processResult, byte maxThreads)
        {
            //Single Producer
            var sharedUris = Producer(uris);

            //Multiple Consumers < Fork phase
            //!SPOT: Deferred / Lazy execution
            var consumers = Enumerable.Range(1, maxThreads)
                .Select(_ => ConsumerAsync(sharedUris, processResult)); //Cold Task

            //Eager execution version
            //var workers = Enumerable.Range(1, maxThreads)
            //  .Select(_ => Consumer(producer, processResult)).ToArray(); //Hot Task

            //Waiting for all workers to finish < Join phase
            return Task.WhenAll(consumers);
        }

        IProducerConsumerCollection<Uri> Producer(IEnumerable<Uri> urls)
        {
            return new ConcurrentQueue<Uri>(urls);
        }

        async Task ConsumerAsync(IProducerConsumerCollection<Uri> uris, ProcessResult processResult)
        {
            while (uris.TryTake(out var uri))
                await WorkerAsync(uri, processResult);
        }

        async Task WorkerAsync(Uri uri, ProcessResult processResult)
        {
            var content = await client.GetStringAsync(uri).ConfigureAwait(false);
            processResult(Thread.CurrentThread.ManagedThreadId.ToString(), content);
        }
    }
}
