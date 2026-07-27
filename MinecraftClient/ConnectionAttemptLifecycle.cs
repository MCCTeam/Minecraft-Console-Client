using System;
using System.Collections.Generic;
using System.Threading;
using MinecraftClient.Scripting;

namespace MinecraftClient
{
    internal sealed class ConnectionAttemptLifecycle
    {
        private int disconnectState;

        internal bool IsFailureClaimed => Volatile.Read(ref disconnectState) != 0;

        internal bool TryBeginDisconnect()
        {
            return Interlocked.CompareExchange(ref disconnectState, 1, 0) == 0;
        }

        internal void CompleteDisconnect()
        {
            Volatile.Write(ref disconnectState, 2);
        }

        internal static void RestoreHeldBots(ICollection<ChatBot> heldBots, Action<ChatBot> loadBot)
        {
            ArgumentNullException.ThrowIfNull(heldBots);
            ArgumentNullException.ThrowIfNull(loadBot);

            foreach (ChatBot bot in heldBots)
                loadBot(bot);
            heldBots.Clear();
        }
    }

    internal sealed class AttemptOwnedRoute
    {
        private const long NoOwner = -1;
        private readonly Lock stateLock = new();
        private long ownerAttempt = NoOwner;

        internal long OwnerAttempt
        {
            get
            {
                lock (stateLock)
                    return ownerAttempt;
            }
        }

        internal bool TryActivate(long connectionAttempt, long currentConnectionAttempt, Action activate)
        {
            ArgumentNullException.ThrowIfNull(activate);

            lock (stateLock)
            {
                if (connectionAttempt < currentConnectionAttempt || ownerAttempt >= connectionAttempt)
                    return false;

                ownerAttempt = connectionAttempt;
                activate();
                return true;
            }
        }

        internal bool TryDeactivate(long connectionAttempt, Action deactivate)
        {
            ArgumentNullException.ThrowIfNull(deactivate);

            lock (stateLock)
            {
                if (ownerAttempt != connectionAttempt)
                    return false;

                ownerAttempt = NoOwner;
                deactivate();
                return true;
            }
        }

        internal bool TryTransfer(long sourceConnectionAttempt, long targetConnectionAttempt)
        {
            lock (stateLock)
            {
                if (ownerAttempt != sourceConnectionAttempt)
                    return false;

                ownerAttempt = targetConnectionAttempt;
                return true;
            }
        }

        internal bool TryDeactivate(Action deactivate)
        {
            ArgumentNullException.ThrowIfNull(deactivate);

            lock (stateLock)
            {
                if (ownerAttempt == NoOwner)
                    return false;

                ownerAttempt = NoOwner;
                deactivate();
                return true;
            }
        }
    }
}
