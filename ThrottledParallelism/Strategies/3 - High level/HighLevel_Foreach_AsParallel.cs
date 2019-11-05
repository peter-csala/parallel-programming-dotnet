using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Collections.Immutable; //ImmutableList
using ThrottledParallelism.Helpers; //GetAwaiter for ImmutableList<Task>

namespace ThrottledParallelism.Strategies
{
    public class HighLevel_Foreach_AsParallel : IGovernedParallelDownloader
    {
        static HttpClient client = new HttpClient();
        public async Task DownloadThemAllAsync(IEnumerable<Uri> uris, ProcessResult processResult, byte maxThreads)
        {
            //https://github.com/dotnet/corefx/blob/master/src/System.Collections.Immutable/src/System/Collections/Immutable/ImmutableList.cs
            //var workers = ImmutableList.Create<Task>(); //Be aware with this, it creates an empty list
            
            var workers = ImmutableList.CreateBuilder<Task>();
            Func<Uri, Task> worker = uri => WorkerAsync(uri, processResult); 
            foreach (var uri in uris.AsParallel().WithDegreeOfParallelism(maxThreads)) //!SPOT: Similarity with PLINQ
            {
                //If you have choosed to create an empty list then you call its Add it will return a new ImmutableList
                //var newlist = workers.Add(WorkerAsync(uri, processResult)); 

                workers.Add(worker(uri)); 
            }

            await workers.ToImmutableList();
        }

        async Task WorkerAsync(Uri uri, ProcessResult processResult)
        {
            var content = await client.GetStringAsync(uri).ConfigureAwait(false);
            processResult(Thread.CurrentThread.ManagedThreadId.ToString(), content);
        }
    }
}
