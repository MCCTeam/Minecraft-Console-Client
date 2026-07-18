using System;
using System.Threading;
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
            public int Retries = 3;

            [TomlInlineComment("$ChatBot.AutoRelog.Ignore_Kick_Message$")]
            public bool Ignore_Kick_Message = false;

            [TomlPrecedingComment("$ChatBot.AutoRelog.Kick_Messages$")]
            public string[] Kick_Messages = new string[] { "Connection has been lost", "Server is restarting", "Server is full", "Too Many people" };

            [NonSerialized]
            public static int _BotRecoAttempts = 0;

            public void OnSettingUpdate()
            {
                Delay.min = Math.Max(0.1, Delay.min);
                Delay.max = Math.Max(0.1, Delay.max);

                double maxDelaySeconds = int.MaxValue / (double)Settings.ClientTicksPerSecond;
                Delay.min = Math.Min(maxDelaySeconds, Delay.min);
                Delay.max = Math.Min(maxDelaySeconds, Delay.max);

                if (Delay.min > Delay.max)
                    (Delay.min, Delay.max) = (Delay.max, Delay.min);

                if (Retries == -1)
                    Retries = int.MaxValue;

                if (Enabled)
                    for (int i = 0; i < Kick_Messages.Length; i++)
                        Kick_Messages[i] = Kick_Messages[i].ToLower();
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

        private static readonly Lock s_reconnectStateLock = new();
        private static readonly TimeSpan s_stableJoinBeforeRetryReset = TimeSpan.FromSeconds(60);
        private static DateTime? s_lastJoinUtc;

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
            lock (s_reconnectStateLock)
                s_lastJoinUtc = DateTime.UtcNow;
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
            }
            else if (Program.HasRestartPendingForAnotherThread)
            {
                return true;
            }
            else if (CanReconnect())
            {
                message = GetVerbatim(message);
                string comp = message.ToLowerInvariant();

                LogDebugToConsole(string.Format(Translations.bot_autoRelog_disconnect_msg, message));

                if (Config.Ignore_Kick_Message)
                {
                    return LaunchDelayedReconnection(null);
                }

                foreach (string msg in Config.Kick_Messages)
                {
                    if (comp.Contains(msg))
                    {
                        return LaunchDelayedReconnection(msg);
                    }
                }

                LogDebugToConsole(Translations.bot_autoRelog_reconnect_ignore);
            }

            return false;
        }

        private static bool CanReconnect()
        {
            lock (s_reconnectStateLock)
                return Config.Retries < 0 || Configs._BotRecoAttempts < Config.Retries;
        }

        private static void ResetRetriesAfterStableJoin()
        {
            lock (s_reconnectStateLock)
            {
                if (Configs._BotRecoAttempts <= 0 || s_lastJoinUtc is not DateTime lastJoinUtc)
                    return;

                if (DateTime.UtcNow - lastJoinUtc < s_stableJoinBeforeRetryReset)
                    return;

                Configs._BotRecoAttempts = 0;
                s_lastJoinUtc = null;
                McClient.ReconnectionAttemptsLeft = Config.Retries;
            }
        }

        private static bool TryConsumeReconnectAttempt(out int retriesLeft)
        {
            lock (s_reconnectStateLock)
            {
                bool unlimitedRetries = HasUnlimitedRetries();
                if (!unlimitedRetries && Configs._BotRecoAttempts >= Config.Retries)
                {
                    retriesLeft = 0;
                    return false;
                }

                Configs._BotRecoAttempts++;
                s_lastJoinUtc = null;
                retriesLeft = unlimitedRetries ? int.MaxValue : Config.Retries - Configs._BotRecoAttempts;
                if (retriesLeft < 0)
                    retriesLeft = 0;
                return true;
            }
        }

        private static bool HasUnlimitedRetries()
        {
            return Config.Retries < 0 || Config.Retries == int.MaxValue;
        }

        private static void RollBackReconnectAttempt()
        {
            lock (s_reconnectStateLock)
            {
                if (Configs._BotRecoAttempts > 0)
                    Configs._BotRecoAttempts--;
            }
        }

        private bool LaunchDelayedReconnection(string? msg)
        {
            if (!TryConsumeReconnectAttempt(out int retriesLeft))
                return false;

            double delay = Random.Shared.NextDouble() * (Config.Delay.max - Config.Delay.min) + Config.Delay.min;
            LogDebugToConsole(string.Format(string.IsNullOrEmpty(msg) ? Translations.bot_autoRelog_reconnect_always : Translations.bot_autoRelog_reconnect, msg));

            string retriesDisplay = HasUnlimitedRetries()
                ? Translations.bot_autoRelog_retries_unlimited
                : retriesLeft.ToString();

            McClient.ReconnectionAttemptsLeft = retriesLeft;
            if (Program.TryRestart((int)Math.Floor(delay), true))
            {
                LogToConsole(string.Format(Translations.bot_autoRelog_wait_with_retries, delay, retriesDisplay));
                return true;
            }

            RollBackReconnectAttempt();
            return true;
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
