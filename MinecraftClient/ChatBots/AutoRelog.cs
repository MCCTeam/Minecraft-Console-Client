using System;
using System.Text;
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

            [TomlInlineComment("$config.ChatBot.AutoRelog.Delay$")]
            public Range Delay = new(10);

            [TomlInlineComment("$config.ChatBot.AutoRelog.Retries$")]
            public int Retries = 3;

            [TomlInlineComment("$config.ChatBot.AutoRelog.Ignore_Kick_Message$")]
            public bool Ignore_Kick_Message = false;

            [TomlPrecedingComment("$config.ChatBot.AutoRelog.Kick_Messages$")]
            public string[] Kick_Messages = new string[] { "Connection has been lost", "Server is restarting", "Server is full", "Too Many people" };

            public void OnSettingUpdate()
            {
                Delay.min = Math.Max(1, Delay.min);
                Delay.max = Math.Max(1, Delay.max);
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
                public int min, max;

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
            LogDebugToConsoleTranslated("bot.autoRelog.launch", Config.Retries);
        }

        public override void Initialize()
        {
            McClient.ReconnectionAttemptsLeft = Config.Retries;
            if (Config.Ignore_Kick_Message)
            {
                LogDebugToConsoleTranslated("bot.autoRelog.no_kick_msg");
            }
        }

        public override bool OnDisconnect(DisconnectReason reason, string message)
        {
            if (reason == DisconnectReason.UserLogout)
            {
                LogDebugToConsoleTranslated("bot.autoRelog.ignore_user_logout");
            }
            else
            {
                message = GetVerbatim(message);
                string comp = message.ToLower();

                LogDebugToConsoleTranslated("bot.autoRelog.disconnect_msg", message);

                if (Config.Ignore_Kick_Message)
                {
                    LaunchDelayedReconnection(null);
                    return true;
                }

                foreach (string msg in Config.Kick_Messages)
                {
                    if (comp.Contains(msg))
                    {
                        LaunchDelayedReconnection(msg);
                        return true;
                    }
                }

                LogDebugToConsoleTranslated("bot.autoRelog.reconnect_ignore");
            }

            return false;
        }

        private void LaunchDelayedReconnection(string? msg)
        {
            int delay = random.Next(Config.Delay.min, Config.Delay.max);
            LogDebugToConsoleTranslated(String.IsNullOrEmpty(msg) ? "bot.autoRelog.reconnect_always" : "bot.autoRelog.reconnect", msg);
            LogToConsoleTranslated("bot.autoRelog.wait", delay);
            System.Threading.Thread.Sleep(delay * 1000);
            ReconnectToTheServer();
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
