using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using MoreLinq; //GroupAdjacent

namespace ThrottledParallelism.Strategies
{
    public class LowLevel_Job_GroupAdjacent: IGovernedParallelDownloader
    {
        public Task DownloadThemAllAsync(IEnumerable<Uri> uris, ProcessResult processResult, byte maxThreads)
        {
            //Single producer
            var workerJobs = Producer(uris, processResult);

            //Multiple consumers 
            //The first n (groupMaxItem) job will be go to consumerId:1, the second n items will belong to consumerId:2, etc..
            int consumerId = 0, groupCounter = 0, groupMaxItem = (int)Math.Ceiling((double)workerJobs.Count / maxThreads);
            var consumers = workerJobs.GroupAdjacent(job =>
            {
                bool shouldMoveJobToNextWorker = false;
                shouldMoveJobToNextWorker = ++groupCounter > groupMaxItem;
                if (shouldMoveJobToNextWorker) { groupCounter = 0; consumerId++; }
                return consumerId; //!SPOT: Jobs are distributed based on the workerId
            });

            //Waiting for all workers to finish
            return Task.WhenAll(consumers.Select(ConsumerAsync));
        }

        HashSet<Job> Producer(IEnumerable<Uri> uris, ProcessResult processResult)
        {
            return Enumerable.ToHashSet<Job>(uris.Select(uri => new Job(uri, processResult)));
            //return uris.Select(uri => new Job(uri, processResult)).ToHashSet<Job>(); //If we won't use MoreLinq
        }

        async Task ConsumerAsync(IEnumerable<Job> jobs)
        {
            foreach (var job in jobs)
                await job.ProcessUrl();
        }
    }
    public class Job
    {
        private static readonly HttpClient client = new HttpClient(); //!SPOT: Moved from the top-level to the lower lvl
        private readonly Uri url;
        private readonly ProcessResult processContent;
        public Job(Uri uri, ProcessResult processResult)
        {
            url = uri;
            processContent = processResult;
        }

        public async Task ProcessUrl()
        {
            var content = await client.GetStringAsync(url).ConfigureAwait(false);
            processContent(Thread.CurrentThread.ManagedThreadId.ToString(), content);
        }
    }
}
