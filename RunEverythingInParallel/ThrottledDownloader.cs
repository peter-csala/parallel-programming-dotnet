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
    [SimpleJob(BenchmarkDotNet.Engines.RunStrategy.ColdStart, targetCount: 3)]
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

        [Benchmark(Baseline =true)]
        public void RunBaseLine_Sequentially() => new BaseLine_SyncVersion().ExectureExperiment(settings);

        [Benchmark()] //https://en.wikipedia.org/wiki/Embarrassingly_parallel
        public void RunBaseLine_EmbarrassinglyParallel() => new BaseLine_WithoutThrottling().ExectureExperiment(settings); 

        [Benchmark]
        public void RunCSharp3() => new LowLevel_Oldest().ExectureExperiment(settings);

        [Benchmark]
        public void RunCSharp5() => new MidLevel_Dataflow_Batch().ExectureExperiment(settings);

        [Benchmark]
        public void RunCSharp6() => new HighLevel_PLINQ().ExectureExperiment(settings);

        [Benchmark]
        public void RunCSharp8() => new HighLevel_AsyncEnum_CSharp8().ExectureExperiment(settings);

        [Benchmark]
        public void RunBonus() => new HighLevel_Polly().ExectureExperiment(settings);
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
