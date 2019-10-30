using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Threading.Tasks.Dataflow; //ActionBlock, BatchBlock
//for complex flows: https://github.com/gridsum/dataflowex

namespace ThrottledParallelism.Strategies
{
    public class MidLevel_Dataflow_Batch : IGovernedParallelDownloader
    {
        static readonly HttpClient client = new HttpClient();
        public Task DownloadThemAllAsync(IEnumerable<Uri> uris, ProcessResult processResult, byte maxThreads)
        {
            //Multiple Consumers 
            var consumers = new ActionBlock<Uri[]>( //Handlers
                uriBatch => ConsumerAsync(uriBatch, processResult), //!SPOT: thread-safety should be guaranteed by us
                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = maxThreads });

            var sharedUris = new BatchBlock<Uri>(2 * maxThreads); //Command

            //This is is the link between the pipeline's command and handlers
            //!SPOT: Link together producer to consumers 
            sharedUris.LinkTo(consumers,
                new DataflowLinkOptions { PropagateCompletion = true }); //!SPOT: without this option the consumers will never finish

            //Single Producer
            Task.Run(async () => {
                await ProducerAsync(sharedUris, uris);

                //Signaling producing is over
                sharedUris.Complete();
            });

            //Waiting for all consumers to finish
            return consumers.Completion;
        }

        async Task ProducerAsync(ITargetBlock<Uri> syncBlock, IEnumerable<Uri> uris)
        {
            foreach (var uri in uris)
                await syncBlock.SendAsync(uri); //Waits until there is a free space to post data
        }

        async Task ConsumerAsync(Uri[] uris, ProcessResult processResult)
        {
            foreach(var uri in uris)
                await WorkerAsync(uri, processResult);
        }

        async Task WorkerAsync(Uri uri, ProcessResult processResult)
        {
            var content = await client.GetStringAsync(uri).ConfigureAwait(false);
            processResult(Thread.CurrentThread.ManagedThreadId.ToString(), content);
        }
    }
}
