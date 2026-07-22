using System;
using System.Threading;
using MinecraftClient.Scripting;

namespace MinecraftClient.ChatBots
{
    internal sealed class AutoRelogRetryPolicy
    {
        internal static readonly TimeSpan StableConnectionThreshold = TimeSpan.FromSeconds(60);

        private readonly Lock stateLock = new();
        private readonly TimeProvider timeProvider;
        private int attempts;
        private DateTimeOffset? joinedAt;

        internal AutoRelogRetryPolicy(TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(timeProvider);
            this.timeProvider = timeProvider;
        }

        internal int Attempts
        {
            get
            {
                lock (stateLock)
                    return attempts;
            }
        }

        internal void MarkJoined()
        {
            lock (stateLock)
                joinedAt = timeProvider.GetUtcNow();
        }

        internal bool ResetAfterStableConnection()
        {
            lock (stateLock)
            {
                if (attempts == 0 || joinedAt is not DateTimeOffset connectionStart)
                    return false;

                if (timeProvider.GetUtcNow() - connectionStart < StableConnectionThreshold)
                    return false;

                attempts = 0;
                joinedAt = null;
                return true;
            }
        }

        internal bool TryReserveAttempt(int retryLimit, out int retriesLeft)
        {
            lock (stateLock)
            {
                bool unlimited = retryLimit == -1;
                if (!unlimited && attempts >= retryLimit)
                {
                    retriesLeft = 0;
                    return false;
                }

                attempts++;
                joinedAt = null;
                retriesLeft = unlimited ? -1 : Math.Max(0, retryLimit - attempts);
                return true;
            }
        }

        internal void RollBackReservedAttempt()
        {
            lock (stateLock)
            {
                if (attempts > 0)
                    attempts--;
            }
        }

        internal static bool ShouldReconnect(
            ChatBot.DisconnectReason reason,
            string message,
            bool ignoreKickMessage,
            ReadOnlySpan<string> kickMessages,
            out string? matchedMessage)
        {
            matchedMessage = null;

            if (reason == ChatBot.DisconnectReason.UserLogout)
                return false;

            if (reason == ChatBot.DisconnectReason.ConnectionLost || ignoreKickMessage)
                return true;

            foreach (string candidate in kickMessages)
            {
                if (!string.IsNullOrEmpty(candidate)
                    && message.Contains(candidate, StringComparison.OrdinalIgnoreCase))
                {
                    matchedMessage = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}
