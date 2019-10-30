using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Collections.Generic;

using System.Collections.Immutable; //ImmutableArray
using Newtonsoft.Json.Linq; //JObject, SelectToken

namespace RunEverythingInParallel
{
    public class SampleDataGenerator
    {
        static readonly Random _random = new Random();

        public IEnumerable<Uri> Urls { get; }
        public byte DegreeOfParallelism { get; }

        public SampleDataGenerator(byte degreeOfParallelism)
        {
            DegreeOfParallelism = degreeOfParallelism;
            ServicePointManager.DefaultConnectionLimit = degreeOfParallelism;

            Urls = GetFileUris(GetRootUrl(), GetNFileNames(20));
        }
        
        string GetRootUrl()
        {
            using var reader = new StreamReader("launchSettings.json"); //!SPOT: C#8 using
            var settings = JObject.Parse(reader.ReadToEnd());
            return (string)settings.SelectToken("$.iisSettings.iisExpress.applicationUrl");
        }

        IEnumerable<Uri> GetFileUris(string resourcesRootDir, params IEnumerable<string>[] fileNames)
        {
            return fileNames
                .SelectMany(fn => fn) //Flatten
                .Select(fileName => new Uri($@"{resourcesRootDir}/resources/{fileName}"))
                .ToList();
        }

        IEnumerable<string> GetNFileNames(int n)
        {
            var availableFiles = ImmutableArray.Create("1mb.log", "5mb.log", "10mb.log");  //, "20mb.log", "100mb.log", "1024mb.log" };

            return Enumerable.Range(0, n - 1)
                .Select(_ => availableFiles[_random.Next(0, availableFiles.Length)]) //inclusive min, exclusive upper
                .ToList(); 
        }
    }
}
