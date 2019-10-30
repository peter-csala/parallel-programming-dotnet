using System;
using System.IO;

namespace RunEverythingInParallel
{
    public class SampleFileGenerator
    {
        static readonly Random _random = new Random();
        public void GenerateSampleFile(int sizeInMb)
        {
            byte[] buffer = new byte[sizeInMb * 1024 * 1024];
            _random.NextBytes(buffer);
            File.WriteAllBytes($"{sizeInMb}mb.log", buffer);
        }
    }
}
