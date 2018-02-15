using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Colosseum.Tools.SystemExtensions.Threading.Tasks
{
    public static class TaskExtensionMethods
    {
        public static Task CancelOnFaulted(this Task task, CancellationTokenSource cts, ILogger logger = null)
        {
            task.ContinueWith(tsk =>
            {
                logger?.LogWarning($"cancelling Task {tsk} because of fault");
                cts.Cancel();
            }, cts.Token, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
            return task;
        }

        public static Task<T> CancelOnFaulted<T>(this Task<T> task, CancellationTokenSource cts, ILogger logger = null)
        {
            task.ContinueWith(tsk =>
            {
                logger?.LogWarning($"cancelling Task {tsk} because of fault");
                cts.Cancel();
            }, cts.Token, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
            return task;
        }
    }
}