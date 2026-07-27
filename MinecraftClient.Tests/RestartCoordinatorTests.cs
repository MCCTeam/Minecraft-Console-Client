namespace MinecraftClient.Tests;

public sealed class RestartCoordinatorTests
{
    [Fact]
    public async Task PreparationCompletesBeforeRequestCanExecute()
    {
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bool prepared = false;

        RestartCoordinator coordinator = null!;
        coordinator = new RestartCoordinator(
            (request, cancellationToken) =>
            {
                Assert.True(Volatile.Read(ref prepared));
                Assert.True(coordinator.TryBeginCommit(request, out _));
                completed.SetResult();
                return Task.CompletedTask;
            },
            exception => throw new Xunit.Sdk.XunitException(exception.ToString()));
        using var cleanup = coordinator;

        Assert.True(coordinator.TrySchedule(
            new RestartRequest(1, TimeSpan.Zero, true),
            () =>
            {
                Volatile.Write(ref prepared, true);
                return true;
            }));
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RejectedPreparationDoesNotPublishOrAdvanceAttempt()
    {
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int executions = 0;

        RestartCoordinator coordinator = null!;
        coordinator = new RestartCoordinator(
            (request, cancellationToken) =>
            {
                Interlocked.Increment(ref executions);
                Assert.True(coordinator.TryBeginCommit(request, out _));
                completed.SetResult();
                return Task.CompletedTask;
            },
            exception => throw new Xunit.Sdk.XunitException(exception.ToString()));
        using var cleanup = coordinator;

        Assert.False(coordinator.TrySchedule(
            new RestartRequest(2, TimeSpan.Zero, true),
            () => false));
        Assert.False(coordinator.HasScheduledRestart(2));

        Assert.True(coordinator.TrySchedule(new RestartRequest(2, TimeSpan.Zero, true)));
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(1, executions);
    }

    [Fact]
    public async Task FaultedSourceCleanupPreventsRestartExecution()
    {
        var failureReported = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        int executions = 0;

        using var coordinator = new RestartCoordinator(
            (_, _) =>
            {
                Interlocked.Increment(ref executions);
                return Task.CompletedTask;
            },
            exception => failureReported.SetResult(exception));

        var cleanupFailure = new InvalidOperationException("cleanup failed");
        Assert.True(coordinator.TrySchedule(new RestartRequest(
            3,
            TimeSpan.Zero,
            true,
            SourceCleanupCompletion: Task.FromException(cleanupFailure))));

        Exception reportedException = await failureReported.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Same(cleanupFailure, reportedException);
        Assert.Equal(0, executions);
    }

    [Fact]
    public async Task AutomaticSameAttemptIsCoalescedWhileQueued()
    {
        var blockerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseBlocker = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var queuedCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int queuedExecutions = 0;

        RestartCoordinator coordinator = null!;
        coordinator = new RestartCoordinator(
            async (request, cancellationToken) =>
            {
                if (request.ConnectionAttempt == 9)
                {
                    blockerStarted.SetResult();
                    await releaseBlocker.Task.WaitAsync(cancellationToken);
                    return;
                }

                Interlocked.Increment(ref queuedExecutions);
                Assert.True(coordinator.TryBeginCommit(request, out _));
                queuedCompleted.SetResult();
            },
            exception => throw new Xunit.Sdk.XunitException(exception.ToString()));
        using var cleanup = coordinator;

        Assert.True(coordinator.TrySchedule(new RestartRequest(9, TimeSpan.Zero, true)));
        await blockerStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(coordinator.TrySchedule(new RestartRequest(10, TimeSpan.Zero, true)));
        Assert.False(coordinator.TrySchedule(new RestartRequest(10, TimeSpan.Zero, true)));
        Assert.True(coordinator.HasScheduledRestart(10));

        releaseBlocker.SetResult();
        await queuedCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(1, queuedExecutions);
    }

    [Fact]
    public async Task AutomaticSameAttemptIsCoalescedDuringCallback()
    {
        var callbackStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbackCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int executions = 0;

        RestartCoordinator coordinator = null!;
        coordinator = new RestartCoordinator(
            async (request, cancellationToken) =>
            {
                Interlocked.Increment(ref executions);
                callbackStarted.SetResult();
                await releaseCallback.Task.WaitAsync(cancellationToken);
                Assert.True(coordinator.TryBeginCommit(request, out _));
                callbackCompleted.SetResult();
            },
            exception => throw new Xunit.Sdk.XunitException(exception.ToString()));
        using var cleanup = coordinator;

        Assert.True(coordinator.TrySchedule(new RestartRequest(42, TimeSpan.Zero, true)));
        await callbackStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(coordinator.TrySchedule(new RestartRequest(42, TimeSpan.Zero, true)));

        releaseCallback.SetResult();
        await callbackCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(1, executions);
    }

    [Fact]
    public async Task ExplicitReplacementDuringDelayUsesLatestSnapshotWithoutAnotherExecution()
    {
        var callbackStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowCommit = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbackCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        RestartRequest committedRequest = default;
        int executions = 0;

        RestartCoordinator coordinator = null!;
        coordinator = new RestartCoordinator(
            async (request, cancellationToken) =>
            {
                Interlocked.Increment(ref executions);
                callbackStarted.SetResult();
                await allowCommit.Task.WaitAsync(cancellationToken);
                Assert.True(coordinator.TryBeginCommit(request, out committedRequest));
                callbackCompleted.SetResult();
            },
            exception => throw new Xunit.Sdk.XunitException(exception.ToString()));
        using var cleanup = coordinator;

        Assert.True(coordinator.TrySchedule(new RestartRequest(
            50,
            TimeSpan.FromSeconds(10),
            true,
            CreateSettingsSnapshot("first"))));
        await callbackStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(coordinator.TrySchedule(new RestartRequest(
            50,
            TimeSpan.Zero,
            true,
            CreateSettingsSnapshot("replacement"),
            ReplaceUntilCommit: true)));

        allowCommit.SetResult();
        await callbackCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(1, executions);
        Assert.Equal("replacement", committedRequest.SettingsSnapshot?.Account.Login);
    }

    [Fact]
    public async Task RejectsSameAttemptReplacementAfterCommit()
    {
        var commitStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCommit = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        RestartRequest committedRequest = default;

        RestartCoordinator coordinator = null!;
        coordinator = new RestartCoordinator(
            async (request, cancellationToken) =>
            {
                Assert.True(coordinator.TryBeginCommit(request, out committedRequest));
                commitStarted.SetResult();
                await releaseCommit.Task.WaitAsync(cancellationToken);
            },
            exception => throw new Xunit.Sdk.XunitException(exception.ToString()));
        using var cleanup = coordinator;

        Assert.True(coordinator.TrySchedule(new RestartRequest(
            60,
            TimeSpan.Zero,
            true,
            CreateSettingsSnapshot("committed"))));
        await commitStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(coordinator.TrySchedule(new RestartRequest(
            60,
            TimeSpan.Zero,
            true,
            CreateSettingsSnapshot("rejected"),
            ReplaceUntilCommit: true)));
        Assert.Equal("committed", committedRequest.SettingsSnapshot?.Account.Login);

        releaseCommit.SetResult();
    }

    [Fact]
    public void RejectsStaleAttempt()
    {
        RestartCoordinator coordinator = null!;
        coordinator = new RestartCoordinator(
            (_, _) => Task.CompletedTask,
            exception => throw new Xunit.Sdk.XunitException(exception.ToString()));
        using var cleanup = coordinator;

        Assert.True(coordinator.TrySchedule(new RestartRequest(20, TimeSpan.Zero, true)));
        Assert.False(coordinator.TrySchedule(new RestartRequest(19, TimeSpan.Zero, true)));
    }

    [Fact]
    public async Task RejectsCompletedAttempt()
    {
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        RestartCoordinator coordinator = null!;
        coordinator = new RestartCoordinator(
            (request, cancellationToken) =>
            {
                Assert.True(coordinator.TryBeginCommit(request, out _));
                completed.SetResult();
                return Task.CompletedTask;
            },
            exception => throw new Xunit.Sdk.XunitException(exception.ToString()));
        using var cleanup = coordinator;

        Assert.True(coordinator.TrySchedule(new RestartRequest(20, TimeSpan.Zero, true)));
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(SpinWait.SpinUntil(() => !coordinator.HasScheduledRestart(20), TimeSpan.FromSeconds(5)));

        Assert.False(coordinator.TrySchedule(new RestartRequest(20, TimeSpan.Zero, true)));
    }

    [Fact]
    public async Task TerminalStopCancelsInFlightWorkAndRejectsFurtherRestarts()
    {
        var callbackStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var coordinator = new RestartCoordinator(
            async (_, cancellationToken) =>
            {
                callbackStarted.SetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            },
            exception => throw new Xunit.Sdk.XunitException(exception.ToString()));

        Assert.True(coordinator.TrySchedule(new RestartRequest(1, TimeSpan.Zero, true)));
        await callbackStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        coordinator.Stop();

        Assert.False(coordinator.TrySchedule(new RestartRequest(2, TimeSpan.Zero, true)));
        Assert.False(coordinator.HasScheduledRestart(1));
    }

    private static RestartSettingsSnapshot CreateSettingsSnapshot(string account)
    {
        return new RestartSettingsSnapshot(
            new Settings.MainConfigHelper.MainConfig.AccountInfoConfig(account, "-"),
            "localhost",
            25565);
    }
}
