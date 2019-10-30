using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using MoreLinq; //Segment

namespace ThrottledParallelism.Strategies
{
    public class LowLevel_Job_Segment : IGovernedParallelDownloader
    {
        private static readonly HttpClient client = new HttpClient();
        public Task DownloadThemAllAsync(IEnumerable<Uri> uris, ProcessResult processResult, byte maxThreads)
        {
            //Single producer
            var workerJobs = Producer(uris, processResult);

            //Multiple consumers
            //first n jobs will belong to consumerId:1, {newSegment} second chunk will belong to consumerId:2, {newSegment}, etc..
            int segmentCounter = 0, segmentThreshold = (int)Math.Ceiling((double)workerJobs.Count / maxThreads);
            var consumers = workerJobs.Segment(job => //!SPOT: There is an explicit load balancing
            {
                    bool isNewSegmentNeeded = false;
                    isNewSegmentNeeded = ++segmentCounter > segmentThreshold;
                    if (isNewSegmentNeeded) segmentCounter = 0;
                    return isNewSegmentNeeded;
                });

            //Waiting for all workers to finish
            return Task.WhenAll(consumers.Select(ConsumerAsync)); 
        }

        //!SPOT: There is a design change: action is next to the data
        List<KeyValuePair<Uri, Func<Uri, Task>>> Producer(IEnumerable<Uri> uris, ProcessResult processResult)
        {
            return uris
                .Select(uri => new KeyValuePair<Uri, Func<Uri, Task>>(uri, url => WorkerAsync(url, processResult)))
                .ToList();
        }

        async Task ConsumerAsync(IEnumerable<KeyValuePair<Uri, Func<Uri, Task>>> jobs)
        {
            //Process in parallel (!It breaks the contract)
            //return Task.WhenAll(jobs.Select(async job => await job.Value(job.Key)));

            //Process sequential
            foreach (var job in jobs)
                await job.Value(job.Key);
        }

        async Task WorkerAsync(Uri uri, ProcessResult processResult)
        {
            var content = await client.GetStringAsync(uri).ConfigureAwait(false);
            processResult(Thread.CurrentThread.ManagedThreadId.ToString(), content);
        }
    }
}
