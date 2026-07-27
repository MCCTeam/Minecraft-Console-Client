using MinecraftClient.Scripting;

namespace MinecraftClient.Tests;

public sealed class McClientConnectionFailureTests
{
    [Fact]
    public void LoginRejectedClaimPreventsSyntheticConnectionLostFallback()
    {
        var lifecycle = new ConnectionAttemptLifecycle();

        Assert.True(lifecycle.TryBeginDisconnect());
        Assert.True(lifecycle.IsFailureClaimed);
        Assert.False(lifecycle.TryBeginDisconnect());

        lifecycle.CompleteDisconnect();

        Assert.True(lifecycle.IsFailureClaimed);
        Assert.False(lifecycle.TryBeginDisconnect());
    }

    [Fact]
    public void UnclaimedGenericFailureCanBeClaimedExactlyOnce()
    {
        var lifecycle = new ConnectionAttemptLifecycle();

        Assert.False(lifecycle.IsFailureClaimed);
        Assert.True(lifecycle.TryBeginDisconnect());
        Assert.False(lifecycle.TryBeginDisconnect());
    }

    [Fact]
    public void HeldBotsAreRestoredBeforeFailureAndReceiveOriginalMessageOnce()
    {
        const string rejectionMessage = "You are not white-listed on this server!";
        var bot = new RecordingBot();
        List<ChatBot> heldBots = [bot];
        var loadedBots = new List<ChatBot>();

        ConnectionAttemptLifecycle.RestoreHeldBots(heldBots, loadedBots.Add);
        foreach (ChatBot loadedBot in loadedBots)
            loadedBot.OnDisconnect(ChatBot.DisconnectReason.LoginRejected, rejectionMessage);

        Assert.Empty(heldBots);
        Assert.Single(loadedBots);
        Assert.Equal(1, bot.DisconnectCount);
        Assert.Equal(ChatBot.DisconnectReason.LoginRejected, bot.LastReason);
        Assert.Equal(rejectionMessage, bot.LastMessage);
    }

    [Fact]
    public void OfflineRouteStaysOwnedAcrossReplacementAndSuccessfulHandoff()
    {
        var route = new AttemptOwnedRoute();
        int activations = 0;
        int deactivations = 0;

        Assert.True(route.TryActivate(7, () => activations++));
        Assert.False(route.TryActivate(7, () => activations++));
        Assert.True(route.TryTransfer(7, 8));
        Assert.False(route.TryDeactivate(7, () => deactivations++));
        Assert.Equal(8, route.OwnerAttempt);
        Assert.True(route.TryDeactivate(8, () => deactivations++));

        Assert.Equal(1, activations);
        Assert.Equal(1, deactivations);
        Assert.Equal(-1, route.OwnerAttempt);
    }

    [Fact]
    public void InitialConnectionAttemptCanOwnOfflineRoute()
    {
        var route = new AttemptOwnedRoute();
        int activations = 0;

        Assert.True(route.TryActivate(0, () => activations++));

        Assert.Equal(1, activations);
        Assert.Equal(0, route.OwnerAttempt);
    }

    [Fact]
    public void StaleCleanupCannotClearNewerOfflineRoute()
    {
        var route = new AttemptOwnedRoute();
        int deactivations = 0;

        Assert.True(route.TryActivate(10, () => { }));
        Assert.True(route.TryActivate(11, () => { }));
        Assert.False(route.TryDeactivate(10, () => deactivations++));

        Assert.Equal(11, route.OwnerAttempt);
        Assert.Equal(0, deactivations);
    }

    private sealed class RecordingBot : ChatBot
    {
        internal int DisconnectCount { get; private set; }
        internal DisconnectReason? LastReason { get; private set; }
        internal string? LastMessage { get; private set; }

        public override bool OnDisconnect(DisconnectReason reason, string message)
        {
            DisconnectCount++;
            LastReason = reason;
            LastMessage = message;
            return false;
        }
    }
}
