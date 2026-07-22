using MinecraftClient.ChatBots;
using MinecraftClient.Scripting;

namespace MinecraftClient.Tests;

public sealed class AutoRelogRetryPolicyTests
{
    [Fact]
    public void DefaultConfigurationUsesUnlimitedRetries()
    {
        Assert.Equal(-1, new AutoRelog.Configs().Retries);
    }

    [Fact]
    public void UnlimitedRetriesNeverExhaust()
    {
        var policy = new AutoRelogRetryPolicy(new ManualTimeProvider());

        for (int attempt = 1; attempt <= 100; attempt++)
        {
            Assert.True(policy.TryReserveAttempt(-1, out int retriesLeft));
            Assert.Equal(-1, retriesLeft);
            Assert.Equal(attempt, policy.Attempts);
        }
    }

    [Fact]
    public void ZeroRetriesDisablesReconnect()
    {
        var policy = new AutoRelogRetryPolicy(new ManualTimeProvider());

        Assert.False(policy.TryReserveAttempt(0, out int retriesLeft));
        Assert.Equal(0, retriesLeft);
        Assert.Equal(0, policy.Attempts);
    }

    [Fact]
    public void FiniteRetryLimitIsExact()
    {
        var policy = new AutoRelogRetryPolicy(new ManualTimeProvider());

        Assert.True(policy.TryReserveAttempt(3, out int firstRetriesLeft));
        Assert.True(policy.TryReserveAttempt(3, out int secondRetriesLeft));
        Assert.True(policy.TryReserveAttempt(3, out int thirdRetriesLeft));
        Assert.False(policy.TryReserveAttempt(3, out int exhaustedRetriesLeft));

        Assert.Equal(2, firstRetriesLeft);
        Assert.Equal(1, secondRetriesLeft);
        Assert.Equal(0, thirdRetriesLeft);
        Assert.Equal(0, exhaustedRetriesLeft);
    }

    [Fact]
    public void RejectedRestartDoesNotConsumeRetry()
    {
        var policy = new AutoRelogRetryPolicy(new ManualTimeProvider());

        Assert.True(policy.TryReserveAttempt(1, out _));
        policy.RollBackReservedAttempt();

        Assert.True(policy.TryReserveAttempt(1, out int retriesLeft));
        Assert.Equal(0, retriesLeft);
    }

    [Fact]
    public void StableConnectionResetsRetryBudget()
    {
        var timeProvider = new ManualTimeProvider();
        var policy = new AutoRelogRetryPolicy(timeProvider);
        Assert.True(policy.TryReserveAttempt(1, out _));

        policy.MarkJoined();
        timeProvider.Advance(AutoRelogRetryPolicy.StableConnectionThreshold - TimeSpan.FromMilliseconds(1));
        Assert.False(policy.ResetAfterStableConnection());

        timeProvider.Advance(TimeSpan.FromMilliseconds(1));
        Assert.True(policy.ResetAfterStableConnection());
        Assert.Equal(0, policy.Attempts);
        Assert.True(policy.TryReserveAttempt(1, out _));
    }

    [Fact]
    public void TransportLossAlwaysReconnectsWhenEnabled()
    {
        bool reconnect = AutoRelogRetryPolicy.ShouldReconnect(
            ChatBot.DisconnectReason.ConnectionLost,
            "A transport-specific error without a configured phrase",
            ignoreKickMessage: false,
            ["Server is restarting"],
            out string? matchedMessage);

        Assert.True(reconnect);
        Assert.Null(matchedMessage);
    }

    [Theory]
    [InlineData(ChatBot.DisconnectReason.InGameKick)]
    [InlineData(ChatBot.DisconnectReason.LoginRejected)]
    public void ServerMessageMatchingIsCaseInsensitive(ChatBot.DisconnectReason reason)
    {
        bool reconnect = AutoRelogRetryPolicy.ShouldReconnect(
            reason,
            "THE SERVER IS RESTARTING NOW",
            ignoreKickMessage: false,
            ["server is restarting"],
            out string? matchedMessage);

        Assert.True(reconnect);
        Assert.Equal("server is restarting", matchedMessage);
    }

    [Fact]
    public void UserLogoutNeverReconnects()
    {
        Assert.False(AutoRelogRetryPolicy.ShouldReconnect(
            ChatBot.DisconnectReason.UserLogout,
            "Server is restarting",
            ignoreKickMessage: true,
            ["Server is restarting"],
            out _));
    }

    [Fact]
    public void IgnoreKickMessageAllowsNonmatchingServerKick()
    {
        Assert.True(AutoRelogRetryPolicy.ShouldReconnect(
            ChatBot.DisconnectReason.InGameKick,
            "Administrative removal",
            ignoreKickMessage: true,
            [],
            out _));
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset utcNow = DateTimeOffset.UnixEpoch;

        public override DateTimeOffset GetUtcNow() => utcNow;

        internal void Advance(TimeSpan duration)
        {
            utcNow += duration;
        }
    }
}
