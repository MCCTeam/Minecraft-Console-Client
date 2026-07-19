using System;
using MinecraftClient.Scripting;
using Tomlet.Attributes;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// This bot automatically re-join the server if kick message contains predefined string (Server is restarting ...)
    /// </summary>
    public class AutoRelog : ChatBot
    {
        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "AutoRelog";

            public bool Enabled = false;

            [TomlInlineComment("$ChatBot.AutoRelog.Delay$")]
            public Range Delay = new(3);

            [TomlInlineComment("$ChatBot.AutoRelog.Retries$")]
            public int Retries = -1;

            [TomlInlineComment("$ChatBot.AutoRelog.Ignore_Kick_Message$")]
            public bool Ignore_Kick_Message = false;

            [TomlPrecedingComment("$ChatBot.AutoRelog.Kick_Messages$")]
            public string[] Kick_Messages = new string[] { "Connection has been lost", "Server is restarting", "Server is full", "Too Many people" };

            public void OnSettingUpdate()
            {
                Kick_Messages ??= Array.Empty<string>();

                if (!double.IsFinite(Delay.min))
                    Delay.min = 0.1;
                if (!double.IsFinite(Delay.max))
                    Delay.max = 0.1;

                Delay.min = Math.Max(0.1, Delay.min);
                Delay.max = Math.Max(0.1, Delay.max);

                double maxDelaySeconds = (uint.MaxValue - 1) / 1000D;
                Delay.min = Math.Min(maxDelaySeconds, Delay.min);
                Delay.max = Math.Min(maxDelaySeconds, Delay.max);

                if (Delay.min > Delay.max)
                    (Delay.min, Delay.max) = (Delay.max, Delay.min);

                if (Retries < -1)
                    Retries = -1;
            }

            public struct Range
            {
                public double min, max;

                public Range()
                {
                    min = 0;
                    max = 0;
                }

                public Range(int value)
                {
                    min = max = value;
                }

                public Range(int min, int max)
                {
                    this.min = min;
                    this.max = max;
                }
            }
        }

        private static readonly AutoRelogRetryPolicy s_retryPolicy = new(TimeProvider.System);

        /// <summary>
        /// This bot automatically re-join the server if kick message contains predefined string
        /// </summary>
        /// <param name="DelayBeforeRelogMin">Minimum delay before re-joining the server (in seconds)</param>
        /// <param name="DelayBeforeRelogMax">Maximum delay before re-joining the server (in seconds)</param>
        /// <param name="retries">Number of retries if connection fails (-1 = infinite)</param>
        public AutoRelog()
        {
            LogDebugToConsole(string.Format(Translations.bot_autoRelog_launch, Config.Retries));
        }

        public override void Initialize()
        {
            _Initialize();
        }

        public override void AfterGameJoined()
        {
            s_retryPolicy.MarkJoined();
        }

        public override void Update()
        {
            ResetRetriesAfterStableJoin();
        }

        private void _Initialize()
        {
            McClient.ReconnectionAttemptsLeft = Config.Retries;
            if (Config.Ignore_Kick_Message)
            {
                LogDebugToConsole(Translations.bot_autoRelog_no_kick_msg);
            }
        }

        public override bool OnDisconnect(DisconnectReason reason, string message)
        {
            if (reason == DisconnectReason.UserLogout)
            {
                LogDebugToConsole(Translations.bot_autoRelog_ignore_user_logout);
                return false;
            }

            message = GetVerbatim(message);
            LogDebugToConsole(string.Format(Translations.bot_autoRelog_disconnect_msg, message));

            if (!AutoRelogRetryPolicy.ShouldReconnect(
                    reason,
                    message,
                    Config.Ignore_Kick_Message,
                    Config.Kick_Messages,
                    out string? matchedMessage))
            {
                LogDebugToConsole(Translations.bot_autoRelog_reconnect_ignore);
                return false;
            }

            return LaunchDelayedReconnection(matchedMessage);
        }

        private static void ResetRetriesAfterStableJoin()
        {
            if (s_retryPolicy.ResetAfterStableConnection())
                McClient.ReconnectionAttemptsLeft = Config.Retries;
        }

        private static bool HasUnlimitedRetries()
        {
            return Config.Retries == -1;
        }

        private bool LaunchDelayedReconnection(string? msg)
        {
            if (!s_retryPolicy.TryReserveAttempt(Config.Retries, out int retriesLeft))
                return false;

            double delay = Random.Shared.NextDouble() * (Config.Delay.max - Config.Delay.min) + Config.Delay.min;
            LogDebugToConsole(string.Format(string.IsNullOrEmpty(msg) ? Translations.bot_autoRelog_reconnect_always : Translations.bot_autoRelog_reconnect, msg));

            string retriesDisplay = HasUnlimitedRetries()
                ? Translations.bot_autoRelog_retries_unlimited
                : retriesLeft.ToString();

            McClient.ReconnectionAttemptsLeft = retriesLeft;
            if (Program.TryRestart(TimeSpan.FromSeconds(delay), true))
            {
                LogToConsole(string.Format(Translations.bot_autoRelog_wait_with_retries, delay, retriesDisplay));
                return true;
            }

            s_retryPolicy.RollBackReservedAttempt();
            return Program.HasRestartPending;
        }

        public static bool OnDisconnectStatic(DisconnectReason reason, string message)
        {
            if (Config.Enabled)
            {
                AutoRelog bot = new();
                bot.Initialize();
                return bot.OnDisconnect(reason, message);
            }
            return false;
        }
    }
}
