using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Collections.Immutable; //ToImmutableList
using System.Threading.Channels; //Channel, ChannelReader, ChannelWriter

namespace ThrottledParallelism.Strategies
{
    public class MidLevel_Channels : IGovernedParallelDownloader
    {
        static readonly HttpClient client = new HttpClient();
        public Task DownloadThemAllAsync(IEnumerable<Uri> uris, ProcessResult processResult, byte maxThreads)
        {
            //This is is the channel between the pipeline's command and handlers
            var sharedUris = Channel.CreateBounded<Uri>(
                new BoundedChannelOptions(2 * maxThreads)
                    { SingleWriter = true, SingleReader = false }); //!SPOT: Channels can optimize

            var producer = sharedUris.Writer; //command
            var consumer = sharedUris.Reader; //handler
            
            //Multiple Consumers < Fork phase
            var consumers = Enumerable.Range(0, maxThreads -1) //!SPOT: functional syntax to write a for loop
                .Select( _ => ConsumerAsync(consumer, processResult))
                .ToImmutableList(); //!SPOT: Materialize LINQ expression to start Tasks execute

            //Single Producer
            Task.Run(async () => { //!SPOT: Producer is asynchronous > Consumers & Producer run in parallel
                await ProducerAsync(producer, uris);
                
                //Signaling producing is over
                producer.Complete();
            });

            //Waiting for all workers to finish < Join phase
            return Task.WhenAll(consumers);  

            //return consumer.Completion; //Be aware with this!!!
        }

        async ValueTask ProducerAsync(ChannelWriter<Uri> uriChannel, IEnumerable<Uri> uris) //!SPOT: Replaced task to valueTask
        {
            //Mixing Async + Sync 
            //In case of a single producer it is not a problem, but can't scale well
            foreach (var uri in uris)
            {
                await uriChannel.WaitToWriteAsync(); //Waits until there is a free space
                if (!uriChannel.TryWrite(uri)) //!SPOT: posting data is synchronous
                    return; //It returns false if the channel is closed between WaitToWriteAsync and TryWrite
            }

            //Pure async
            //foreach (var uri in uris)
            //{
            //    await uriChannel.WriteAsync(uri); //Waits until there is a free place & posts the data
            //}
        }

        async Task ConsumerAsync(ChannelReader<Uri> uriChannel, ProcessResult processResult) //!SPOT: This remained TASK
        {
            while (await uriChannel.WaitToReadAsync()) //Waits until an available item appear
            {
                var uri = await uriChannel.ReadAsync(); //Waits until there is an available item
                await WorkerAsync(uri, processResult);
            }
        }

        async Task WorkerAsync(Uri uri, ProcessResult processResult)
        {
            var content = await client.GetStringAsync(uri).ConfigureAwait(false);
            processResult(Thread.CurrentThread.ManagedThreadId.ToString(), content);
        }
    }
}
