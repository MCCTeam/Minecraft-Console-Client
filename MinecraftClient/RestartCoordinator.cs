using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MinecraftClient
{
    internal readonly record struct RestartRequest(
        long ConnectionAttempt,
        TimeSpan Delay,
        bool KeepAccountAndServerSettings);

    internal sealed class RestartCoordinator : IDisposable
    {
        private readonly Lock stateLock = new();
        private readonly Channel<RestartRequest> requests;
        private readonly CancellationTokenSource shutdown = new();
        private readonly Func<RestartRequest, CancellationToken, Task> restart;
        private readonly Action<Exception> reportFailure;
        private readonly Task worker;
        private readonly HashSet<long> pendingAttempts = [];
        private long highestScheduledAttempt = -1;
        private bool stopped;

        internal RestartCoordinator(
            Func<RestartRequest, CancellationToken, Task> restart,
            Action<Exception> reportFailure)
        {
            ArgumentNullException.ThrowIfNull(restart);
            ArgumentNullException.ThrowIfNull(reportFailure);

            this.restart = restart;
            this.reportFailure = reportFailure;
            requests = Channel.CreateUnbounded<RestartRequest>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false,
            });
            worker = ProcessRequestsAsync();
        }

        internal bool HasScheduledRestart(long connectionAttempt)
        {
            lock (stateLock)
                return !stopped && pendingAttempts.Contains(connectionAttempt);
        }

        internal bool TrySchedule(RestartRequest request)
        {
            lock (stateLock)
            {
                if (stopped || request.ConnectionAttempt <= highestScheduledAttempt)
                    return false;

                highestScheduledAttempt = request.ConnectionAttempt;
                pendingAttempts.Add(request.ConnectionAttempt);
                if (requests.Writer.TryWrite(request))
                    return true;

                pendingAttempts.Remove(request.ConnectionAttempt);
                return false;
            }
        }

        internal void Stop()
        {
            lock (stateLock)
            {
                if (stopped)
                    return;

                stopped = true;
                pendingAttempts.Clear();
                requests.Writer.TryComplete();
                shutdown.Cancel();
            }
        }

        private async Task ProcessRequestsAsync()
        {
            try
            {
                await foreach (RestartRequest request in requests.Reader.ReadAllAsync(shutdown.Token).ConfigureAwait(false))
                {
                    try
                    {
                        await restart(request, shutdown.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception exception)
                    {
                        reportFailure(exception);
                    }
                    finally
                    {
                        lock (stateLock)
                            pendingAttempts.Remove(request.ConnectionAttempt);
                    }
                }
            }
            catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
            {
            }
        }

        public void Dispose()
        {
            Stop();
            worker.GetAwaiter().GetResult();
            shutdown.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
