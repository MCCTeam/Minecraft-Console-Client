using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// This bot automatically re-join the server if kick message contains predefined string (Server is restarting ...)
    /// </summary>
    public class AutoRelog : ChatBot
    {
        private string[] dictionary = new string[0];
        private int attempts;
        private int delay;

        /// <summary>
        /// This bot automatically re-join the server if kick message contains predefined string
        /// </summary>
        /// <param name="DelayBeforeRelog">Delay before re-joining the server (in seconds)</param>
        /// <param name="retries">Number of retries if connection fails (-1 = infinite)</param>
        public AutoRelog(int DelayBeforeRelog, int retries)
        {
            attempts = retries;
            if (attempts == -1) { attempts = int.MaxValue; }
            McClient.ReconnectionAttemptsLeft = attempts;
            delay = DelayBeforeRelog;
            if (delay < 1) { delay = 1; }
            LogDebugToConsoleTranslated("bot.autoRelog.launch", attempts);
        }

        public override void Initialize()
        {
            McClient.ReconnectionAttemptsLeft = attempts;
            if (Settings.AutoRelog_IgnoreKickMessage)
            {
                LogDebugToConsoleTranslated("bot.autoRelog.no_kick_msg");
            }
            else
            {
                if (System.IO.File.Exists(Settings.AutoRelog_KickMessagesFile))
                {
                    LogDebugToConsoleTranslated("bot.autoRelog.loading", System.IO.Path.GetFullPath(Settings.AutoRelog_KickMessagesFile));

                    dictionary = System.IO.File.ReadAllLines(Settings.AutoRelog_KickMessagesFile, Encoding.UTF8);

                    for (int i = 0; i < dictionary.Length; i++)
                    {
                        LogDebugToConsoleTranslated("bot.autoRelog.loaded", dictionary[i]);
                        dictionary[i] = dictionary[i].ToLower();
                    }
                }
                else
                {
                    LogToConsoleTranslated("bot.autoRelog.not_found", System.IO.Path.GetFullPath(Settings.AutoRelog_KickMessagesFile));

                    LogDebugToConsoleTranslated("bot.autoRelog.curr_dir", System.IO.Directory.GetCurrentDirectory());
                }
            }
        }

        public override bool OnDisconnect(DisconnectReason reason, string message)
        {
            if (reason == DisconnectReason.UserLogout)
            {
                LogDebugToConsoleTranslated("bot.autoRelog.ignore");
            }
            else
            {
                message = GetVerbatim(message);
                string comp = message.ToLower();

                LogDebugToConsoleTranslated("bot.autoRelog.disconnect_msg", message);

                if (Settings.AutoRelog_IgnoreKickMessage)
                {
                    LogDebugToConsoleTranslated("bot.autoRelog.reconnect_always");
                    LogToConsoleTranslated("bot.autoRelog.wait", delay);
                    System.Threading.Thread.Sleep(delay * 1000);
                    ReconnectToTheServer();
                    return true;
                }

                foreach (string msg in dictionary)
                {
                    if (comp.Contains(msg))
                    {
                        LogDebugToConsoleTranslated("bot.autoRelog.reconnect", msg);
                        LogToConsoleTranslated("bot.autoRelog.wait", delay);
                        System.Threading.Thread.Sleep(delay * 1000);
                        McClient.ReconnectionAttemptsLeft = attempts;
                        ReconnectToTheServer();
                        return true;
                    }
                }

                LogDebugToConsoleTranslated("bot.autoRelog.reconnect_ignore");
            }

            return false;
        }

        public static bool OnDisconnectStatic(DisconnectReason reason, string message)
        {
            if (Settings.AutoRelog_Enabled)
            {
                AutoRelog bot = new AutoRelog(Settings.AutoRelog_Delay, Settings.AutoRelog_Retries);
                bot.Initialize();
                return bot.OnDisconnect(reason, message);
            }
            return false;
        }
    }
}
