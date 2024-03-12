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

                Delay.min = Math.Min(int.MaxValue / 10, Delay.min);
                Delay.max = Math.Min(int.MaxValue / 10, Delay.max);

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

        private static readonly Random random = new();

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
            else if (Config.Retries < 0 || Configs._BotRecoAttempts < Config.Retries)
            {
                message = GetVerbatim(message);
                string comp = message.ToLower();

                LogDebugToConsole(string.Format(Translations.bot_autoRelog_disconnect_msg, message));

                if (Config.Ignore_Kick_Message)
                {
                    Configs._BotRecoAttempts++;
                    LaunchDelayedReconnection(null);
                    return true;
                }

                foreach (string msg in Config.Kick_Messages)
                {
                    if (comp.Contains(msg))
                    {
                        Configs._BotRecoAttempts++;
                        LaunchDelayedReconnection(msg);
                        return true;
                    }
                }

                LogDebugToConsole(Translations.bot_autoRelog_reconnect_ignore);
            }

            return false;
        }

        private void LaunchDelayedReconnection(string? msg)
        {
            double delay = random.NextDouble() * (Config.Delay.max - Config.Delay.min) + Config.Delay.min;
            LogDebugToConsole(string.Format(string.IsNullOrEmpty(msg) ? Translations.bot_autoRelog_reconnect_always : Translations.bot_autoRelog_reconnect, msg));
            
            // TODO: Change this translation string to add the retries left text
            LogToConsole(string.Format(Translations.bot_autoRelog_wait, delay) + $" ({Config.Retries - Configs._BotRecoAttempts} retries left)");
            ReconnectToTheServer(Config.Retries - Configs._BotRecoAttempts, (int)Math.Floor(delay), true);
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
