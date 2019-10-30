using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Collections.Immutable; //ImmutableList, ToImmutable
using System.Threading.Tasks.Dataflow; //BufferBlock

namespace ThrottledParallelism.Strategies
{
    public class MidLevel_Dataflow_Buffer: IGovernedParallelDownloader
    {
        static readonly HttpClient client = new HttpClient();
        public Task DownloadThemAllAsync(IEnumerable<Uri> uris, ProcessResult processResult, byte maxThreads)
        {
            var sharedUris = new BufferBlock<Uri>(
                new DataflowBlockOptions { BoundedCapacity = 2 * maxThreads });

            //Multiple Consumers < Fork phase
            var consumers = ImmutableList.CreateBuilder<Task>(); //!SPOT: Using ImmutableList with Builder
            for (int i = 0; i < maxThreads; i++)
                consumers.Add(ConsumerAsync(sharedUris, processResult));

            //Single Producer
            Task.Run(async () => {
                await ProducerAsync(sharedUris, uris);

                //Signaling producing is over
                sharedUris.Complete();
            });

            //Waiting for all consumers to finish < Join phase
            return Task.WhenAll(consumers.ToImmutable()); //!SPOT: Using immutable task collection
        }

        async Task ProducerAsync(ITargetBlock<Uri> syncBlock, IEnumerable<Uri> uris)
        {
            foreach (var uri in uris)
                await syncBlock.SendAsync(uri); //Waits until there is a free space to post data
        }

        //Semi Batch processor
        async Task ConsumerAsync(ISourceBlock<Uri> syncBlock, ProcessResult processResult)
        {
            //!SPOT: blocking GetConsumingEnumerable() vs non-blocking OutputAvailableAsync
            while (await syncBlock.OutputAvailableAsync()) //Waits until an available item appear
            {
                var uri = await syncBlock.ReceiveAsync(); //Waits until there is an available item
                await WorkerAsync(uri, processResult);
            }
        }

        async Task WorkerAsync(Uri uri, ProcessResult processResult)
        {
            var content = await client.GetStringAsync(uri).ConfigureAwait(false);
            processResult(Thread.CurrentThread.ManagedThreadId.ToString(), content);
        }
    }
}
