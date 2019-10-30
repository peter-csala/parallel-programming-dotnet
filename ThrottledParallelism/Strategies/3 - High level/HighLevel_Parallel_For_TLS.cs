using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using ThrottledParallelism.Helpers; //AsyncHelper
using System.Collections.Immutable; //ImmutableStack, ImmutableInterlock

namespace ThrottledParallelism.Strategies
{
    public class HighLevel_Parallel_For_TLS: IGovernedParallelDownloader
    {
        static readonly HttpClient client = new HttpClient();
        public Task DownloadThemAllAsync(IEnumerable<Uri> uris, ProcessResult processResult, byte maxThreads)
        {
            //Single Producer
            var uriChunksIterator = Producer(uris, maxThreads);

            //!SPOT:An accumulator for the successfully processed urls
            var processedUrls = ImmutableStack<IEnumerable<Uri>>.Empty; 
            
            //Waiting for all consumers to finish
            Parallel.For<IEnumerable<Uri>>
                (
                fromInclusive: 0,
                toExclusive: maxThreads, 
                localInit: () => //Fork phase
                    {
                        uriChunksIterator.MoveNext();
                        return uriChunksIterator.Current; //!SPOT: To be processed uris are stored in Thread Local Storage (TLS)
                    },
                body: (_, __, urls) =>
                    {
                        AsyncHelper.RunSync(async () => await ConsumerAsync(urls, processResult));
                    
                        //TODO Smart logic can be injected here to remove the unsuccessful ones
                        return urls;
                    },
                localFinally: //Join phase
                    partitionUrl => ImmutableInterlocked.Push(ref processedUrls, partitionUrl) //!SPOT: Merge the uri lists (from TLS) into one
                );


            return Task.CompletedTask;
        }

        IEnumerator<IGrouping<int, Uri>> Producer(IEnumerable<Uri> uris, byte groupSize) //!SPOT: IEnumerable > IEnumerator
        {
            int i = 0;
            var uriChunks = uris.GroupBy(_ => i++ % groupSize); //!SPOT: A smarter way to hash
            return uriChunks.GetEnumerator(); //A stream of uri streams
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
