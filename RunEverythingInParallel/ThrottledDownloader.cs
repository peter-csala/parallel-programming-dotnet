using System;
using System.Collections.Generic;

using ThrottledParallelism;
using ThrottledParallelism.Strategies;

using BenchmarkDotNet.Attributes; //HtmlExporter, MemoryDiagnoser, SimpleJob, GlobalSetup, BenckMark
using BenchmarkDotNet.Diagnosers; //MemoryDiagnoser

namespace RunEverythingInParallel
{
    [HtmlExporter]
    [MemoryDiagnoser]
    [SimpleJob(BenchmarkDotNet.Engines.RunStrategy.ColdStart, targetCount: 2)]
    public class ThrottledDownloader
    {
        DownloaderSettings settings;

        [GlobalSetup]
        public void Setup()
        {
            var generator = new SampleDataGenerator(4);

            settings = new DownloaderSettings
            {
                Processor = (workerId, content) => Console.WriteLine($"{string.Format("{0:2}", workerId)}: ~{content.Length / (1024 * 1024)}MB"),
                MaxDegreeOfParallelism = generator.DegreeOfParallelism,
                Urls = generator.Urls,
            };
        }

        [Benchmark(Baseline = true)]
        public void RunBaseLine_Sequentially() => RunExperiment<BaseLine_SyncVersion>();

        [Benchmark()] //https://en.wikipedia.org/wiki/Embarrassingly_parallel
        public void RunBaseLine_EmbarrassinglyParallel() => RunExperiment<BaseLine_WithoutThrottling>();

        [Benchmark]
        public void RunCSharp3() => RunExperiment<LowLevel_Oldest>();

        [Benchmark]
        public void RunCSharp5() => RunExperiment<MidLevel_Dataflow_Batch>();

        [Benchmark]
        public void RunCSharp6() => RunExperiment<HighLevel_PLINQ>();

        [Benchmark]
        public void RunCSharp8() => RunExperiment<HighLevel_AsyncEnum_CSharp8>();

        [Benchmark]
        public void RunBonus() => RunExperiment<HighLevel_Polly>();

        internal void RunExperiment<T>() 
        where T: IGovernedParallelDownloader, new() 
        {
            new T().ExectureExperiment(settings);
        }
    }

    public class DownloaderSettings
    {
        public byte MaxDegreeOfParallelism { get; set; }
        public IEnumerable<Uri> Urls { get; set; }
        public ProcessResult Processor { get; set; }
    }

    public static class IGovernedParallelDownloaderExt
    {
        public static void ExectureExperiment(this IGovernedParallelDownloader downloader, DownloaderSettings settings)
        {
            downloader.DownloadThemAllAsync(settings.Urls, settings.Processor, settings.MaxDegreeOfParallelism).Wait();
        }
    }
}
