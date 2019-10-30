using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Collections.Generic;

using System.Threading.Tasks; //Parallel.Invoke
using ThrottledParallelism.Helpers; //AsyncHelper

namespace ThrottledParallelism.Strategies
{
    public class HighLevel_Parallel_Invoke : IGovernedParallelDownloader
    {
        static readonly HttpClient client = new HttpClient();
        public Task DownloadThemAllAsync(IEnumerable<Uri> uris, ProcessResult processResult, byte maxThreads)
        {
            //Single Producer
            var uriChunks = Producer(uris, maxThreads);

            //Multiple consumers < Fork phase
            IEnumerable<Action> consumers = uriChunks.Select(
                (IGrouping<int, Uri> uriChunk) => new Action(() => 
                    AsyncHelper.RunSync(
                        async () => await ConsumerAsync(uriChunk, processResult))));

            //foreach (var uriChunk in uriChunks)
            //{
            //    consumers.Add(() => AsyncHelper.RunSync(
            //       async () => await ConsumerAsync(uriChunk, processResult)));
            //}

            //Waiting for all consumers to finish < Join phase
            Parallel.Invoke(consumers.ToArray()); //!SPOT: Can't use here ToImmutableArray(), cuz params Action[]
            //Parallel.Invoke( /* new ParallelOptions { MaxDegreeOfParallelism = maxThreads }, */ consumers.ToArray());

            return Task.CompletedTask;
        }


        IEnumerable<IGrouping<int, Uri>> Producer(IEnumerable<Uri> uris, byte groupSize)
        {
            int i = 0;
            return uris.GroupBy(_ => i == groupSize ? i = 0 : i++); //!SPOT: Round Robin load balancing
        }

        async Task ConsumerAsync(IEnumerable<Uri> uris, ProcessResult processResult)
        {
            foreach (var uri in uris)
                await WorkerAsync(uri, processResult);
        }

        async Task WorkerAsync(Uri uri, ProcessResult processResult)
        {
            var content = await client.GetStringAsync(uri).ConfigureAwait(false);
            processResult(Thread.CurrentThread.ManagedThreadId.ToString(), content);
        }
    }
}
