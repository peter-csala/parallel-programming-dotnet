using System;
using System.Net.Http;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;

using System.Threading.Tasks; //Task.WhenAll

namespace ThrottledParallelism.Strategies
{
    public class LowLevel_Task : IGovernedParallelDownloader
    {
        static readonly HttpClient client = new HttpClient();

        public Task DownloadThemAllAsync(IEnumerable<Uri> uris, ProcessResult processResult, byte maxThreads)
        {
            //Single Producer
            var sharedUris = Producer(uris); //!SPOT: There is no workerSynchronizer
            //!SPOT: Ordering is important here < GetConsumingEnumerable() is blocking

            //Multiple Consumers < Fork phase
            var consumers = new List<Task>(maxThreads); //!SPOT: Replaced wrapper Task to Task collection
            for (var i = 0; i < maxThreads; i++)
                consumers.Add(ConsumerAsync(sharedUris, processResult)); //!SPOT: HOT task, starts automatically

            //Waiting for all workers to finish < Join phase
            return Task.WhenAll(consumers)
                .ContinueWith(_ => sharedUris?.Dispose());
        }

        BlockingCollection<Uri> Producer(IEnumerable<Uri> uris)
        {
            var sharedUris = new BlockingCollection<Uri>();
            foreach (var uri in uris)
                sharedUris.Add(uri);

            //Signaling producing is over
            sharedUris.CompleteAdding();

            return sharedUris;
        }

        async Task ConsumerAsync(BlockingCollection<Uri> uris, ProcessResult processResult)
        {
            //If we would process in parallel then it would break the contract

            //Process sequential
            foreach (var uri in uris.GetConsumingEnumerable()) //!SPOT: This is blocking!
                await WorkerAsync(uri, processResult);
        }

        async Task WorkerAsync(Uri uri, ProcessResult processResult)
        {
            var content = await client.GetStringAsync(uri).ConfigureAwait(false);
            processResult(Thread.CurrentThread.ManagedThreadId.ToString(), content);
        }
    }
}
