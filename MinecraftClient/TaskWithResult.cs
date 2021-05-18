using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MinecraftClient
{
    /// <summary>
    /// Holds an asynchronous task with return value
    /// </summary>
    /// <typeparam name="T">Type of the return value</typeparam>
    public class TaskWithResult<T>
    {
        private AutoResetEvent resultEvent = new AutoResetEvent(false);
        private Func<T> task;
        private T result = default(T);
        private Exception exception = null;
        private bool taskRun = false;
        private object taskRunLock = new object();

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
        public bool HasRun
        {
            get
            {
                return taskRun;
            }
        }

        /// <summary>
        /// Get the task result (return value of the inner delegate)
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Thrown if the task is not finished yet</exception>
        public T Result
        {
            get
            {
                if (taskRun)
                {
                    return result;
                }
                else throw new InvalidOperationException("Attempting to retrieve the result of an unfinished task");
            }
        }

        /// <summary>
        /// Get the exception thrown by the inner delegate, if any
        /// </summary>
        public Exception Exception
        {
            get
            {
                return exception;
            }
        }

        /// <summary>
        /// Execute the task in the current thread and set the <see cref="Result"/> property or <see cref=""/>to the returned value
        /// </summary>
        public void ExecuteSynchronously()
        {
            // Make sur the task will not run twice
            lock (taskRunLock)
            {
                if (taskRun)
                {
                    throw new InvalidOperationException("Attempting to run a task twice");
                }
            }

            // Run the task
            try
            {
                result = task();
            }
            catch (Exception e)
            {
                exception = e;
            }

            // Mark task as complete and release wait event
            lock (taskRunLock)
            {
                taskRun = true;
            }
            resultEvent.Set();
        }

        /// <summary>
        /// Wait until the task has run from another thread and get the returned value or exception thrown by the task
        /// </summary>
        /// <returns>Task result once available</returns>
        /// <exception cref="System.Exception">Any exception thrown by the task</exception>
        public T WaitGetResult()
        {
            // Wait only if the result is not available yet
            bool mustWait = false;
            lock (taskRunLock)
            {
                mustWait = !taskRun;
            }
            if (mustWait)
            {
                resultEvent.WaitOne();
            }

            // Receive exception from task
            if (exception != null)
                throw exception;

            return result;
        }
    }
}
