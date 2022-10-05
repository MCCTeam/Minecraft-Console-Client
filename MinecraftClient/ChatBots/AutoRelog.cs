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

            [TomlInlineComment("$config.ChatBot.AutoRelog.Kick_Messages_File$")]
            public string Kick_Messages_File = @"kickmessages.txt";

            public void OnSettingUpdate()
            {
                if (Delay.min > Delay.max)
                    (Delay.min, Delay.max) = (Delay.max, Delay.min);

                if (Retries == -1)
                    Retries = int.MaxValue;
            }

            public struct Range
            {
                public int min, max;

                public Range(int value)
                {
                    value = Math.Max(value, 1);
                    min = max = value;
                }

                public Range(int min, int max)
                {
                    min = Math.Max(min, 1);
                    max = Math.Max(max, 1);
                    this.min = min;
                    this.max = max;
                }
            }
        }

        private static readonly Random random = new();
        private string[] dictionary = Array.Empty<string>();

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
            else
            {
                if (System.IO.File.Exists(Config.Kick_Messages_File))
                {
                    LogDebugToConsoleTranslated("bot.autoRelog.loading", System.IO.Path.GetFullPath(Config.Kick_Messages_File));

                    dictionary = System.IO.File.ReadAllLines(Config.Kick_Messages_File, Encoding.UTF8);

                    for (int i = 0; i < dictionary.Length; i++)
                    {
                        LogDebugToConsoleTranslated("bot.autoRelog.loaded", dictionary[i]);
                        dictionary[i] = dictionary[i].ToLower();
                    }
                }
                else
                {
                    LogToConsoleTranslated("bot.autoRelog.not_found", System.IO.Path.GetFullPath(Config.Kick_Messages_File));

                    LogDebugToConsoleTranslated("bot.autoRelog.curr_dir", System.IO.Directory.GetCurrentDirectory());
                }
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

                foreach (string msg in dictionary)
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
