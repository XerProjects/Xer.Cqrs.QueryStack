﻿namespace System.Threading.Tasks
{
    internal static class TaskUtility
    {
        internal static readonly Task CompletedTask = Task.FromResult(true);

        internal static TResult AwaitResult<TResult>(this Task<TResult> task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            return task.GetAwaiter().GetResult();
        }

        internal static Task<TResult> FromException<TResult>(Exception ex)
        {
            TaskCompletionSource<TResult> completionSource = new TaskCompletionSource<TResult>();
            completionSource.TrySetException(ex);
            return completionSource.Task;
        }
    }
}
