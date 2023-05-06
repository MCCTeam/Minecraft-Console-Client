using System;
using System.Linq;
using MinecraftClient.Scripting;
using Tomlet.Attributes;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// This bot make the console beep on some specified words. Useful to detect when someone is talking to you, for example.
    /// </summary>
    public class Alerts : ChatBot
    {
        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "Alerts";

            public bool Enabled = false;

            [TomlInlineComment("$ChatBot.Alerts.Beep_Enabled$")]
            public bool Beep_Enabled = true;

            [TomlInlineComment("$ChatBot.Alerts.Trigger_By_Words$")]
            public bool Trigger_By_Words = false;

            [TomlInlineComment("$ChatBot.Alerts.Trigger_By_Rain$")]
            public bool Trigger_By_Rain = false;

            [TomlInlineComment("$ChatBot.Alerts.Trigger_By_Thunderstorm$")]
            public bool Trigger_By_Thunderstorm = false;

            [TomlInlineComment("$ChatBot.Alerts.Log_To_File$")]
            public bool Log_To_File = false;

            [TomlInlineComment("$ChatBot.Alerts.Log_File$")]
            public string Log_File = @"alerts-log.txt";

            [TomlPrecedingComment("$ChatBot.Alerts.Matches$")]
            public string[] Matches = new string[] { "Yourname", " whispers ", "-> me", "admin", ".com" };

            [TomlPrecedingComment("$ChatBot.Alerts.Excludes$")]
            public string[] Excludes = new string[] { "myserver.com", "Yourname>:", "Player Yourname", "Yourname joined", "Yourname left", "[Lockette] (Admin)", " Yourname:", "Yourname is" };

            public void OnSettingUpdate()
            {
                Log_File ??= string.Empty;

                if (!Enabled) return;

                bool checkSuccessed = true;

                if (Trigger_By_Words)
                {
                    if (Log_To_File)
                    {
                        try
                        {
                            System.IO.File.AppendAllText(Log_File, string.Empty);
                        }
                        catch
                        {
                            checkSuccessed = false;
                            LogToConsole(BotName, "Can't write logs to " + System.IO.Path.GetFullPath(Log_File));
                        }
                    }
                }

                if (!checkSuccessed)
                {
                    LogToConsole(BotName, Translations.general_bot_unload);
                    Enabled = false;
                }
            }
        }

        float curRainLevel = 0;
        float curThunderLevel = 0;
        const float threshold = 0.2f;

        /// <summary>
        /// Process text received from the server to display alerts
        /// </summary>
        /// <param name="text">Received text</param>
        public override void GetText(string text)
        {
            if (Config.Trigger_By_Words)
            {
                //Remove color codes and convert to lowercase
                text = GetVerbatim(text).ToLower();

                //Proceed only if no exclusions are found in text
                if (!Config.Excludes.Any(exclusion => text.Contains(exclusion.ToLower())))
                {
                    //Show an alert for each alert item found in text, if any
                    foreach (string alert in Config.Matches.Where(alert => text.Contains(alert.ToLower())))
                    {
                        if (Config.Beep_Enabled)
                            Console.Beep(); //Text found !

                        ConsoleIO.WriteLine(text.Replace(alert, "§c" + alert + "§r"));

                        if (Config.Log_To_File && Config.Log_File.Length > 0)
                        {
                            DateTime now = DateTime.Now;
                            string TimeStamp = "[" + now.Year + '/' + now.Month + '/' + now.Day + ' ' + now.Hour + ':' + now.Minute + ']';
                            System.IO.File.AppendAllText(Config.Log_File, TimeStamp + " " + GetVerbatim(text) + "\n");
                        }
                    }
                }
            }
        }

        public override void OnRainLevelChange(float level)
        {
            if (curRainLevel < threshold && level >= threshold)
            {
                if (Config.Trigger_By_Rain)
                {
                    if (Config.Beep_Enabled)
                    {
                        Console.Beep();
                        Console.Beep();
                    }
                    LogToConsole("§c" + Translations.bot_alerts_start_rain);
                }
            }
            else if (curRainLevel >= threshold && level < threshold)
            {
                if (Config.Trigger_By_Rain)
                {
                    if (Config.Beep_Enabled)
                    {
                        Console.Beep();
                    }
                    LogToConsole("§c" + Translations.bot_alerts_end_rain);
                }
            }
            curRainLevel = level;
        }

        public override void OnThunderLevelChange(float level)
        {
            if (curThunderLevel < threshold && level >= threshold)
            {
                if (Config.Trigger_By_Thunderstorm)
                {
                    if (Config.Beep_Enabled)
                    {
                        Console.Beep();
                        Console.Beep();
                    }
                    LogToConsole("§c" + Translations.bot_alerts_start_thunderstorm);
                }
            }
            else if (curThunderLevel >= threshold && level < threshold)
            {
                if (Config.Trigger_By_Thunderstorm)
                {
                    if (Config.Beep_Enabled)
                    {
                        Console.Beep();
                    }
                    LogToConsole("§c" + Translations.bot_alerts_end_thunderstorm);
                }
            }
            curThunderLevel = level;
        }
    }
}
