using System;
using System.Net.Http;
using System.Threading;

using System.Threading.Tasks; //Task.WhenAny
using System.Collections.Generic; //HashSet

namespace ThrottledParallelism.Strategies
{
    public class HighLevel_AsyncEnumerator_Feeder : IGovernedParallelDownloader
    {
        static readonly HttpClient client = new HttpClient();
        public Task DownloadThemAllAsync(IEnumerable<Uri> uris, ProcessResult processResult, byte maxThreads)
            => uris.AsyncForEach(async uri => await WorkerAsync(uri, processResult), maxThreads); //!SPOT: SINGLE LINE

        async Task WorkerAsync(Uri uri, ProcessResult processResult)
        {
            var content = await client.GetStringAsync(uri).ConfigureAwait(false);
            processResult(Thread.CurrentThread.ManagedThreadId.ToString(), content);
        }
    }

    public static partial class ParallelForEach
    {
        //https://codereview.stackexchange.com/a/203487
        public static async Task AsyncForEach<T>(this IEnumerable<T> source, Func<T, Task> body, int degreeOfParallelism)
        {
            var toBeProcessedJobs = new HashSet<Task>(); //!SPOT: there is no concurrency issue, cuz it is either read or write
            var remainingJobsEnumerator = source.GetEnumerator();

            void AddNewJob() //!SPOT: local function
            {
                if (remainingJobsEnumerator.MoveNext()) //If there is more to process
                {
                    var readyToProcessJob = body(remainingJobsEnumerator.Current);
                    toBeProcessedJobs.Add(readyToProcessJob);
                }
            }

            //Initialize
            while (toBeProcessedJobs.Count < degreeOfParallelism) 
            {
                AddNewJob();
            }

            while (toBeProcessedJobs.Count > 0)
            {
                Task processed = await Task.WhenAny(toBeProcessedJobs).ConfigureAwait(false); //!SPOT: WhenAny
                toBeProcessedJobs.Remove(processed); //!SPOT: Eliminates the finished one from the remaining list
                AddNewJob(); //!SPOT: Refreshes remaining list with a new job
            }

            return;
        }
    }
}
