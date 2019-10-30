using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Threading; //CountdownEvent, ThreadPool
using System.Collections.Concurrent; //BlockingCollection
using ThrottledParallelism.Helpers; //AsyncHelper

namespace ThrottledParallelism.Strategies
{
    public class LowLevel_AsyncHelper_RunSync: IGovernedParallelDownloader
    {
        static readonly HttpClient client = new HttpClient(); //!SPOT: Replaced WebClient to HttpClient (from sync to async API)

        public Task DownloadThemAllAsync(IEnumerable<Uri> uris, ProcessResult processResult, byte maxThreads)
        {
            using (var consumerSynchronizer = new CountdownEvent(maxThreads)) 
            using (var sharedUris = new BlockingCollection<Uri>())
            {
                //Multiple consumers
                for (var i = 0; i < maxThreads; i++)
                {
                    //Spans jobs < Fork phase
                    ThreadPool.QueueUserWorkItem(_ => 
                        AsyncHelper.RunSync(async () => 
                        {
                            try
                            {
                                await ConsumerAsync(sharedUris, processResult); //!SPOT: Taking advantange of closure
                            }
                            finally
                            {
                                //Signaling consuming is over
                                consumerSynchronizer.Signal(); //!SPOT: Taking advantange of closure
                            }
                        }));
                }

                //Single Producer
                Producer(sharedUris, uris);

                //Waiting for all workers to finish < Join phase
                consumerSynchronizer.Wait(); //!SPOT: blocking

                return Task.CompletedTask;
            }
        }

        void Producer(BlockingCollection<Uri> sharedUris, IEnumerable<Uri> uris)
        {
            foreach (var uri in uris)
                sharedUris.Add(uri);

            //Signaling producing is over
            sharedUris.CompleteAdding();
        }

        //!SPOT: Moved the synchronization logic to the caller, where we are relying on closure
        async Task ConsumerAsync(BlockingCollection<Uri> uris, ProcessResult processResult)
        {
            foreach (var uri in uris.GetConsumingEnumerable())
                await WorkerAsync(uri, processResult);
        }

        async Task WorkerAsync(Uri uri, ProcessResult processResult)
        {
            var content = await client.GetStringAsync(uri).ConfigureAwait(false);
            processResult(Thread.CurrentThread.ManagedThreadId.ToString(), content);
        }
    }
}
