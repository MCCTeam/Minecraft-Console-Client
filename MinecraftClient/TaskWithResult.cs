using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MinecraftClient
{
    public class TaskWithResult
    {
        private Delegate Task;
        private AutoResetEvent ResultEvent = new AutoResetEvent(false);

        public object Result;

        public TaskWithResult(Delegate task)
        {
            Task = task;
        }

        /// <summary>
        /// Execute the delegate and set the <see cref="Result"/> property to the returned value
        /// </summary>
        /// <returns>Value returned from delegate</returns>
        public object Execute()
        {
            Result = Task.DynamicInvoke();
            return Result;
        }

        /// <summary>
        /// Block the program execution
        /// </summary>
        public void Block()
        {
            ResultEvent.WaitOne();
        }

        /// <summary>
        /// Resume the program execution
        /// </summary>
        public void Release()
        {
            ResultEvent.Set();
        }
    }
}
