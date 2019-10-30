using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Collections.Concurrent; //BlockingCollection
using Microsoft.VisualStudio.Threading; //AsyncCountdownEvent

namespace ThrottledParallelism.Strategies
{
    public class LowLevel_Oldest : IGovernedParallelDownloader
    {
        //!SPOT: WebClient does support file://, http://, https://
        //!SPOT: HttpClient only supports http://, https://
        //static readonly WebClient client = new WebClient(); //!SPOT: System.NotSupportedException >> 'WebClient does not support concurrent I/O operations.'
        public async Task DownloadThemAllAsync(IEnumerable<Uri> uris, ProcessResult processResult, byte maxThreads)
        {
            var consumerSynchronizer = new AsyncCountdownEvent(maxThreads); //!SPOT: using AsyncCountDownEvent to be able to call WaitAsync()
            using (var sharedUris = new BlockingCollection<Uri>())
            {
                //Multiple Consumers
                for (var i = 0; i < maxThreads; i++)
                {
                    //Spans jobs < Fork phase
                    ThreadPool.QueueUserWorkItem(
                        consumerParams => Consumer(((AsyncCountdownEvent, BlockingCollection<Uri>))consumerParams, processResult),
                        (consumerSynchronizer, sharedUris)
                    );
                }

                //Single Producer
                Producer(sharedUris, uris);

                //Waiting for all workers to finish < Join phase
                await consumerSynchronizer.WaitAsync(); //!SPOT: Non-blocking and it is inside the using block to keep the sharedUris alive 
            }

            //If we would wait outside of the using block then the sharedUris would be disposed
            //await workerSynchronizer.WaitAsync(); //System.ObjectDisposedException: The collection has been disposed. Object name: 'BlockingCollection'.

            return;
        }

        void Producer(BlockingCollection<Uri> sharedUris, IEnumerable<Uri> uris)
        {
            foreach (var uri in uris)
                sharedUris.Add(uri);

            //Signaling producing is over
            sharedUris.CompleteAdding();
        }

        void Consumer((AsyncCountdownEvent Signaler, BlockingCollection<Uri> Uris) param, ProcessResult processResult)
        {
            var client = new WebClient(); //!SPOT: Webclient / consumer is fine, cuz consumer is sequential
            try
            {
                foreach (var uri in param.Uris.GetConsumingEnumerable())
                    Worker(client, uri, processResult);
            }
            finally
            {
                //Signaling consuming is over
                param.Signaler.Signal(); 
            }
        }

        void Worker(WebClient client, Uri uri, ProcessResult processResult)
        {
            var content = client.DownloadString(uri);
            processResult(Thread.CurrentThread.ManagedThreadId.ToString(), content);
        }
    }
}
