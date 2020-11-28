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
        private static Random random = new Random();
        private string[] dictionary = new string[0];
        private int attempts;
        private int delayMin;
        private int delayMax;

        /// <summary>
        /// This bot automatically re-join the server if kick message contains predefined string
        /// </summary>
        /// <param name="DelayBeforeRelogMin">Minimum delay before re-joining the server (in seconds)</param>
        /// <param name="DelayBeforeRelogMax">Maximum delay before re-joining the server (in seconds)</param>
        /// <param name="retries">Number of retries if connection fails (-1 = infinite)</param>
        public AutoRelog(int DelayBeforeRelogMin, int DelayBeforeRelogMax, int retries)
        {
            attempts = retries;
            if (attempts == -1) { attempts = int.MaxValue; }
            McClient.ReconnectionAttemptsLeft = attempts;
            delayMin = DelayBeforeRelogMin;
            delayMax = DelayBeforeRelogMax;
            if (delayMin < 1)
                delayMin = 1;
            if (delayMax < delayMin)
                delayMax = delayMin;
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
                LogDebugToConsoleTranslated("bot.autoRelog.ignore_user_logout");
            }
            else
            {
                message = GetVerbatim(message);
                string comp = message.ToLower();

                LogDebugToConsoleTranslated("bot.autoRelog.disconnect_msg", message);

                if (Settings.AutoRelog_IgnoreKickMessage)
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

        private void LaunchDelayedReconnection(string msg)
        {
            int delay = random.Next(delayMin, delayMax);
            LogDebugToConsoleTranslated(String.IsNullOrEmpty(msg) ? "bot.autoRelog.reconnect_always" : "bot.autoRelog.reconnect", msg);
            LogToConsoleTranslated("bot.autoRelog.wait", delay);
            System.Threading.Thread.Sleep(delay * 1000);
            ReconnectToTheServer();
        }

        public static bool OnDisconnectStatic(DisconnectReason reason, string message)
        {
            if (Settings.AutoRelog_Enabled)
            {
                AutoRelog bot = new AutoRelog(Settings.AutoRelog_Delay_Min, Settings.AutoRelog_Delay_Max, Settings.AutoRelog_Retries);
                bot.Initialize();
                return bot.OnDisconnect(reason, message);
            }
            return false;
        }
    }
}
