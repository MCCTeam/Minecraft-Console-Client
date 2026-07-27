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
        bool ReplaceUntilCommit = false,
        long RequestId = 0);

    internal readonly record struct RestartSettingsSnapshot(
        Settings.MainConfigHelper.MainConfig.AccountInfoConfig Account,
        string ServerIP,
        ushort ServerPort);

    internal enum RestartRequestState
    {
        Replaceable,
        Committing,
    }

    internal readonly record struct PendingRestart(
        long RequestId,
        RestartRequest Request,
        RestartRequestState State);

    internal sealed class RestartCoordinator : IDisposable
    {
        private readonly Lock stateLock = new();
        private readonly Channel<RestartRequest> requests;
        private readonly CancellationTokenSource shutdown = new();
        private readonly Func<RestartRequest, CancellationToken, Task> restart;
        private readonly Action<Exception> reportFailure;
        private readonly Task worker;
        private readonly Dictionary<long, PendingRestart> pendingAttempts = [];
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
                if (stopped)
                    return false;

                if (pendingAttempts.TryGetValue(request.ConnectionAttempt, out PendingRestart pendingRequest))
                {
                    if (pendingRequest.State != RestartRequestState.Replaceable || !request.ReplaceUntilCommit)
                        return false;

                    request = request with { RequestId = pendingRequest.RequestId };
                    pendingAttempts[request.ConnectionAttempt] = pendingRequest with { Request = request };
                    return true;
                }

                if (request.ConnectionAttempt <= highestScheduledAttempt)
                    return false;

                highestScheduledAttempt = Math.Max(highestScheduledAttempt, request.ConnectionAttempt);
                request = request with { RequestId = ++nextRequestId };
                pendingAttempts[request.ConnectionAttempt] = new PendingRestart(
                    request.RequestId,
                    request,
                    RestartRequestState.Replaceable);
                if (requests.Writer.TryWrite(request))
                    return true;

                pendingAttempts.Remove(request.ConnectionAttempt);
                return false;
            }
        }

        internal bool TryBeginCommit(RestartRequest scheduledRequest, out RestartRequest latestRequest)
        {
            lock (stateLock)
            {
                if (stopped
                    || !pendingAttempts.TryGetValue(scheduledRequest.ConnectionAttempt, out PendingRestart pendingRequest)
                    || pendingRequest.RequestId != scheduledRequest.RequestId
                    || pendingRequest.State != RestartRequestState.Replaceable)
                {
                    latestRequest = default;
                    return false;
                }

                latestRequest = pendingRequest.Request;
                pendingAttempts[scheduledRequest.ConnectionAttempt] = pendingRequest with
                {
                    State = RestartRequestState.Committing,
                };
                return true;
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
                        if (!pendingAttempts.TryGetValue(request.ConnectionAttempt, out PendingRestart pendingRequest)
                            || pendingRequest.RequestId != request.RequestId)
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
                            if (pendingAttempts.TryGetValue(request.ConnectionAttempt, out PendingRestart pendingRequest)
                                && pendingRequest.RequestId == request.RequestId)
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
