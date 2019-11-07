using System;
using System.Threading;

using BenchmarkDotNet.Running; //BenchmarkRunner
using ThrottledParallelism.Strategies;

namespace RunEverythingInParallel
{
    class Program
    {
        //Use Release 
        static void Main(string[] args)
        {
            Thread.Sleep(1000); //Wait for the WebApp to start
            BenchmarkRunner.Run<ThrottledDownloader>();
            Console.ReadLine();
        }

        //Use Debug < analyzie debug > windows > parallel stack
        //static void Main(string[] args)
        //{
        //    Thread.Sleep(1000); //Wait for the WebApp to start
        //    var downloader = new ThrottledDownloader();
        //    downloader.Setup();
        //    downloader.RunExperiment<HighLevel_Foreach_AsParallel>();
        //}
    }
}
