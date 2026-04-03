using System;
using System.Threading;
using System.Threading.Tasks;

namespace MinecraftClient
{
    internal interface IMainThreadTask
    {
        void ExecuteSynchronously();
        void Cancel();
    }

    /// <summary>
    /// Holds an asynchronous task with return value
    /// </summary>
    /// <typeparam name="T">Type of the return value</typeparam>
    public sealed class TaskWithResult<T> : IMainThreadTask
    {
        private readonly Func<T> task;
        private readonly TaskCompletionSource<T> completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int taskState;

        /// <summary>
        /// Create a new asynchronous task with return value
        /// </summary>
        /// <param name="task">Delegate with return value</param>
        public TaskWithResult(Func<T> task)
        {
            this.task = task;
        }

        /// <summary>
        /// Check whether the task has finished running
        /// </summary>
        public bool HasRun => completionSource.Task.IsCompleted;

        /// <summary>
        /// Get the task result (return value of the inner delegate)
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Thrown if the task is not finished yet</exception>
        public T Result
        {
            get
            {
                if (!completionSource.Task.IsCompleted)
                    throw new InvalidOperationException("Attempting to retrieve the result of an unfinished task");

                return completionSource.Task.GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Get the exception thrown by the inner delegate, if any
        /// </summary>
        public Exception? Exception
        {
            get
            {
                return completionSource.Task.Exception?.InnerException;
            }
        }

        public Task<T> AsTask()
        {
            return completionSource.Task;
        }

        /// <summary>
        /// Execute the task in the current thread and set the <see cref="Result"/> property or <see cref=""/>to the returned value
        /// </summary>
        public void ExecuteSynchronously()
        {
            if (Interlocked.CompareExchange(ref taskState, 1, 0) != 0)
                throw new InvalidOperationException("Attempting to run a task twice");

            try
            {
                completionSource.TrySetResult(task());
            }
            catch (Exception e)
            {
                completionSource.TrySetException(e);
            }
        }

        public void Cancel()
        {
            if (Interlocked.CompareExchange(ref taskState, 1, 0) != 0)
                return;

            completionSource.TrySetException(new OperationCanceledException("Main-thread task was canceled before execution."));
        }

        /// <summary>
        /// Wait until the task has run from another thread and get the returned value or exception thrown by the task
        /// </summary>
        /// <returns>Task result once available</returns>
        /// <exception cref="System.Exception">Any exception thrown by the task</exception>
        public T WaitGetResult()
        {
            return completionSource.Task.GetAwaiter().GetResult();
        }
    }
}
