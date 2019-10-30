using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ThrottledParallelism
{
    public delegate void ProcessResult(string workerId, string content);
    public interface IGovernedParallelDownloader
    {
        Task DownloadThemAllAsync(IEnumerable<Uri> uris, ProcessResult processResult, byte maxThreads);
    }

    public static class DownloaderExt
    {
        public static Task DownloadThemAllAsync(this IGovernedParallelDownloader downloader, IEnumerable<Uri> uris, ProcessResult processResult, byte? maxThreads)
        {
            return downloader.DownloadThemAllAsync(uris, processResult, maxThreads ?? (byte)Environment.ProcessorCount);
        }
    }
}
