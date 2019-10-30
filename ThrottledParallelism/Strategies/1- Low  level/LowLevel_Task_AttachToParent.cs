using System;
using System.Net.Http;
using System.Threading;
using System.Collections.Generic;

using System.Collections.Concurrent; //BlockingCollection
using System.Threading.Tasks; //Task, Task.Factory.StartNew, TaskCreationOption

namespace ThrottledParallelism.Strategies
{
    public class LowLevel_Task_AttachToParent : IGovernedParallelDownloader
    {
        static readonly HttpClient client = new HttpClient();

        //https://kudchikarsk.com/tasks-in-csharp/task-parallelism-c/#attaching-child-tasks-to-a-parent-task
        public Task DownloadThemAllAsync(IEnumerable<Uri> uris, ProcessResult processResult, byte maxThreads)
        {
            var sharedUris = new BlockingCollection<Uri>(); //!SPOT: can't using block here
            var consumerSynchronizer = new Task(() => //!SPOT: Replaced CountdownEvent to Task
            {
                //Multiple Consumers
                for (var i = 0; i < maxThreads; i++)
                {
                    //Spans child jobs < Fork phase
                    Task.Factory.StartNew<Task>( //!SPOT: Replaced QueueUserWorkItem to StartNew << it is a hot task
                        async state =>
                        {
                            var urls = (BlockingCollection<Uri>)state;
                            await ConsumerAsync(urls, processResult);
                        },
                        sharedUris,
                        TaskCreationOptions.AttachedToParent)
                        .Unwrap(); //!SPOT: Flattens Task<Task> to Task
                }

                //Single Producer
                Producer(sharedUris, uris);

            }); //!SPOT: it's just a declaration << it is a cold task

            consumerSynchronizer.Start();
            
            //Waiting for all workers to finish < Join phase
            return consumerSynchronizer 
                .ContinueWith(_ => sharedUris?.Dispose()); 
        }

        void Producer(BlockingCollection<Uri> sharedUris, IEnumerable<Uri> uris)
        {
            foreach (var uri in uris)
                sharedUris.Add(uri);

            //Signaling producing is over
            sharedUris.CompleteAdding();
        }

        async Task ConsumerAsync(BlockingCollection<Uri> uris, ProcessResult processResult)
        {
            foreach (var uri in uris.GetConsumingEnumerable())
                await WorkerAsync(uri, processResult);
        }

        Task WorkerAsync(Uri uri, ProcessResult processResult)
            => client.GetStringAsync(uri)
                .ContinueWith(downloadTask => processResult(Thread.CurrentThread.ManagedThreadId.ToString(), downloadTask.Result), 
                    TaskContinuationOptions.NotOnFaulted);
    }
}
