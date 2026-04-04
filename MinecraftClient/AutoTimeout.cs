using System;
using System.Threading;
using System.Threading.Tasks;

namespace MinecraftClient
{
    /// <summary>
    /// Allow easy timeout on pieces of code
    /// </summary>
    /// <remarks>
    /// By ORelio - (c) 2014 - CDDL 1.0
    /// </remarks>
    public class AutoTimeout
    {
        /// <summary>
        /// Perform the specified action with specified timeout
        /// </summary>
        /// <param name="action">Action to run</param>
        /// <param name="timeout">Maximum timeout in milliseconds</param>
        /// <returns>True if the action finished whithout timing out</returns>
        public static bool Perform(Action action, int timeout)
        {
            return Perform(action, TimeSpan.FromMilliseconds(timeout));
        }

        public static Task<bool> PerformAsync(Action action, int timeout, CancellationToken cancellationToken = default)
        {
            return PerformAsync(action, TimeSpan.FromMilliseconds(timeout), cancellationToken);
        }

        /// <summary>
        /// Perform the specified action with specified timeout
        /// </summary>
        /// <param name="action">Action to run</param>
        /// <param name="timeout">Maximum timeout</param>
        /// <returns>True if the action finished whithout timing out</returns>
        public static bool Perform(Action action, TimeSpan timeout)
        {
            return PerformAsync(action, timeout).GetAwaiter().GetResult();
        }

        public static async Task<bool> PerformAsync(Action action, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(action);

            try
            {
                await Task.Run(action, cancellationToken).WaitAsync(timeout, cancellationToken);
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return false;
            }
            catch (TimeoutException)
            {
                return false;
            }
        }
    }
}
