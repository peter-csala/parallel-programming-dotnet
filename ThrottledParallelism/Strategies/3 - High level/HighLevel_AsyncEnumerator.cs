using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Collections.Concurrent; //Partitioner
using System.Collections.Async; //ParallelForEachAsync (nuget: AsyncEnumerable)

namespace ThrottledParallelism.Strategies
{
    public class HighLevel_AsyncEnumerator: IGovernedParallelDownloader
    {
        static readonly HttpClient client = new HttpClient();
        public Task DownloadThemAllAsync(IEnumerable<Uri> uris, ProcessResult processResult, byte maxThreads)
            => uris.ParallelForEachAsync(async uri => await WorkerAsync(uri, processResult), maxThreads); //!SPOT: SINGLE LINE
            //=> uris.ForEachAsync(async uri => await WorkerAsync(uri, processResult), maxThreads); //With Partioner

        async Task WorkerAsync(Uri uri, ProcessResult processResult)
        {
            var content = await client.GetStringAsync(uri).ConfigureAwait(false);
            processResult(Thread.CurrentThread.ManagedThreadId.ToString(), content);
        }
    }

    public static partial class ParallelForEach
    {
        //https://blogs.msdn.microsoft.com/pfxteam/2012/03/05/implementing-a-simple-foreachasync-part-2/
        public static Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> body, int degreeOfParallelism)
        {
            return Task.WhenAll(
                Partitioner.Create(source)
                    .GetPartitions(degreeOfParallelism) //!SPOT: Partitions the source into degreeOfParallelism number of partitions
                    .Select(partition => 
                        Task.Run(async () => { //!SPOT: For each partion we span a dedicated thread << a.k.a consumer
                            using (partition) 
                                while (partition.MoveNext()) //!SPOT: Iterates through the partition and applies the given function
                                    await body(partition.Current);
                        }))
            );
        }
    }
}
