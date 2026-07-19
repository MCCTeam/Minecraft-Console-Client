namespace MinecraftClient.Tests;

public sealed class RestartCoordinatorTests
{
    [Fact]
    public async Task CoalescesSameAttemptAndQueuesNewerAttempt()
    {
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        using var coordinator = new RestartCoordinator(
            async (request, cancellationToken) =>
            {
                if (request.ConnectionAttempt == 10)
                {
                    firstStarted.SetResult();
                    await releaseFirst.Task.WaitAsync(cancellationToken);
                }
                else if (request.ConnectionAttempt == 11)
                {
                    secondCompleted.SetResult();
                }
            },
            exception => throw new Xunit.Sdk.XunitException(exception.ToString()));

        Assert.True(coordinator.TrySchedule(new RestartRequest(10, TimeSpan.Zero, true)));
        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(coordinator.TrySchedule(new RestartRequest(10, TimeSpan.Zero, true)));
        Assert.True(coordinator.TrySchedule(new RestartRequest(11, TimeSpan.Zero, true)));
        Assert.True(coordinator.HasScheduledRestart(11));

        releaseFirst.SetResult();
        await secondCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void RejectsStaleAttempt()
    {
        using var coordinator = new RestartCoordinator(
            (_, _) => Task.CompletedTask,
            exception => throw new Xunit.Sdk.XunitException(exception.ToString()));

        Assert.True(coordinator.TrySchedule(new RestartRequest(20, TimeSpan.Zero, true)));
        Assert.False(coordinator.TrySchedule(new RestartRequest(19, TimeSpan.Zero, true)));
    }

    [Fact]
    public void TerminalStopRejectsFurtherRestarts()
    {
        using var coordinator = new RestartCoordinator(
            (_, _) => Task.CompletedTask,
            exception => throw new Xunit.Sdk.XunitException(exception.ToString()));

        coordinator.Stop();

        Assert.False(coordinator.TrySchedule(new RestartRequest(1, TimeSpan.Zero, true)));
        Assert.False(coordinator.HasScheduledRestart(1));
    }
}
