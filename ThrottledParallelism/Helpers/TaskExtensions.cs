using System.Collections.Generic;
using System.Threading.Tasks;

using System.Runtime.CompilerServices; //TaskAwaiter
using System.Collections.Immutable; //ImmutableList

namespace ThrottledParallelism.Helpers
{
    //https://devblogs.microsoft.com/pfxteam/await-anything/
    public static class TaskExtensions
    {
        public static TaskAwaiter GetAwaiter(this IEnumerable<Task> tasks)
        {
            return Task.WhenAll(tasks).GetAwaiter();
        }
        public static TaskAwaiter GetAwaiter(this ImmutableList<Task> tasks)
        {
            return Task.WhenAll(tasks).GetAwaiter();
        }
    }
}
