using System;
using System.Net.Http;
using System.Threading;
using System.Collections.Generic;

using System.Threading.Tasks; //Parallel.ForEach
using System.Collections.Concurrent; //Partioner
using ThrottledParallelism.Helpers; //AsyncHelper

namespace ThrottledParallelism.Strategies
{
    public class HighLevel_Parallel_Foreach : IGovernedParallelDownloader
    {
        static readonly HttpClient client = new HttpClient();
        public Task DownloadThemAllAsync(IEnumerable<Uri> uris, ProcessResult processResult, byte maxThreads)
        {
            //Single Producer
            var uriPartitions = Producer(uris);

            //Waiting for all workers to finish
            Parallel.ForEach //!SPOT: Blocks until all threads finishes 
                ( //Fork phase
                    uriPartitions,
                    new ParallelOptions { MaxDegreeOfParallelism = maxThreads },
                    uri => AsyncHelper.RunSync(
                        async () => await WorkerAsync(uri, processResult)) //Multiple Workers
                ); //Join phase

            return Task.CompletedTask;
        }

        Partitioner<Uri> Producer(IEnumerable<Uri> uris)
        {
            return Partitioner.Create(uris, EnumerablePartitionerOptions.NoBuffering);
        }

        //!SPOT: There is no Consumer layer

        async Task WorkerAsync(Uri uri, ProcessResult processResult)
        {
            var content = await client.GetStringAsync(uri).ConfigureAwait(false);
            processResult(Thread.CurrentThread.ManagedThreadId.ToString(), content);
        }
    }
}
