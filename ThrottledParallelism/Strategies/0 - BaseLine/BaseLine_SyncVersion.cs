using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ThrottledParallelism.Strategies
{
    public class BaseLine_SyncVersion : IGovernedParallelDownloader
    {
        public Task DownloadThemAllAsync(IEnumerable<Uri> uris, ProcessResult processResult, byte maxThreads)
        {
            var client = new WebClient();
            foreach (var uri in uris) //Single Producer
            {
                var content = client.DownloadString(uri); //Single Consumer
                processResult(Thread.CurrentThread.ManagedThreadId.ToString(), content); //Signaling consuming is over
            }  //Signaling producing is over

            return Task.CompletedTask;
        }
    }
}
