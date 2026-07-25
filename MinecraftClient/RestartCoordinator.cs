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
        bool KeepAccountAndServerSettings,
        RestartSettingsSnapshot? SettingsSnapshot = null,
        long RequestId = 0);

    internal readonly record struct RestartSettingsSnapshot(
        Settings.MainConfigHelper.MainConfig.AccountInfoConfig Account,
        string ServerIP,
        ushort ServerPort);

    internal sealed class RestartCoordinator : IDisposable
    {
        private readonly Lock stateLock = new();
        private readonly Channel<RestartRequest> requests;
        private readonly CancellationTokenSource shutdown = new();
        private readonly Func<RestartRequest, CancellationToken, Task> restart;
        private readonly Action<Exception> reportFailure;
        private readonly Task worker;
        private readonly Dictionary<long, long> pendingAttempts = [];
        private long highestScheduledAttempt = -1;
        private long nextRequestId;
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
                return !stopped && pendingAttempts.ContainsKey(connectionAttempt);
        }

        internal bool TrySchedule(RestartRequest request)
        {
            lock (stateLock)
            {
                bool hasPendingRequest = pendingAttempts.TryGetValue(request.ConnectionAttempt, out long previousRequestId);
                if (stopped || request.ConnectionAttempt < highestScheduledAttempt
                    || (request.ConnectionAttempt == highestScheduledAttempt && !hasPendingRequest))
                    return false;

                highestScheduledAttempt = Math.Max(highestScheduledAttempt, request.ConnectionAttempt);
                request = request with { RequestId = ++nextRequestId };
                pendingAttempts[request.ConnectionAttempt] = request.RequestId;
                if (requests.Writer.TryWrite(request))
                    return true;

                if (hasPendingRequest)
                    pendingAttempts[request.ConnectionAttempt] = previousRequestId;
                else
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
                    lock (stateLock)
                    {
                        if (!pendingAttempts.TryGetValue(request.ConnectionAttempt, out long requestId)
                            || requestId != request.RequestId)
                            continue;
                    }

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
                        {
                            if (pendingAttempts.TryGetValue(request.ConnectionAttempt, out long requestId)
                                && requestId == request.RequestId)
                                pendingAttempts.Remove(request.ConnectionAttempt);
                        }
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
