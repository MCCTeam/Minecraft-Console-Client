using System;
using System.Linq;
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

            [TomlInlineComment("$config.ChatBot.Alerts.Beep_Enabled$")]
            public bool Beep_Enabled = true;

            [TomlInlineComment("$config.ChatBot.Alerts.Trigger_By_Words$")]
            public bool Trigger_By_Words = false;

            [TomlInlineComment("$config.ChatBot.Alerts.Trigger_By_Rain$")]
            public bool Trigger_By_Rain = false;

            [TomlInlineComment("$config.ChatBot.Alerts.Trigger_By_Thunderstorm$")]
            public bool Trigger_By_Thunderstorm = false;

            [TomlInlineComment("$config.ChatBot.Alerts.Matches_File$")]
            public string Matches_File = @"alerts.txt";

            [TomlInlineComment("$config.ChatBot.Alerts.Excludes_File$")]
            public string Excludes_File = @"alerts-exclude.txt";

            [TomlInlineComment("$config.ChatBot.Alerts.Log_To_File$")]
            public bool Log_To_File = false;

            [TomlInlineComment("$config.ChatBot.Alerts.Log_File$")]
            public string Log_File = @"alerts-log.txt";

            public void OnSettingUpdate()
            {
                Matches_File ??= string.Empty;
                Excludes_File ??= string.Empty;
                Log_File ??= string.Empty;

                if (!Enabled) return;

                bool checkSuccessed = true;

                if (Trigger_By_Words)
                {
                    if (!System.IO.File.Exists(Matches_File))
                    {
                        checkSuccessed = false;
                        LogToConsole(BotName, "File not found: " + System.IO.Path.GetFullPath(Matches_File));
                    }

                    if (!System.IO.File.Exists(Excludes_File))
                    {
                        checkSuccessed = false;
                        LogToConsole(BotName, "File not found: " + System.IO.Path.GetFullPath(Excludes_File));
                    }

                    if (Log_To_File)
                    {
                        try
                        {
                            System.IO.File.AppendAllText(Log_File, string.Empty);
                        }
                        catch
                        {
                            checkSuccessed = false;
                            LogToConsole(BotName, "Can't write logs to " + System.IO.Path.GetFullPath(Excludes_File));
                        }
                    }
                }

                if (!checkSuccessed)
                {
                    LogToConsole(BotName, Translations.TryGet("general.bot_unload"));
                    Enabled = false;
                }
            }
        }

        private string[] dictionary = Array.Empty<string>();
        private string[] excludelist = Array.Empty<string>();

        float curRainLevel = 0;
        float curThunderLevel = 0;
        const float threshold = 0.2f;

        /// <summary>
        /// Intitialize the Alerts bot
        /// </summary>
        public override void Initialize()
        {
            if (Config.Trigger_By_Words)
            {
                dictionary = LoadDistinctEntriesFromFile(Config.Matches_File);
                excludelist = LoadDistinctEntriesFromFile(Config.Excludes_File);
            }
        }

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
                if (!excludelist.Any(exclusion => text.Contains(exclusion)))
                {
                    //Show an alert for each alert item found in text, if any
                    foreach (string alert in dictionary.Where(alert => text.Contains(alert)))
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
                    LogToConsole(Translations.TryGet("bot.alerts.start_rain"));
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
                    LogToConsole(Translations.TryGet("bot.alerts.end_rain"));
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
                    LogToConsole(Translations.TryGet("bot.alerts.start_thunderstorm"));
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
                    LogToConsole(Translations.TryGet("bot.alerts.end_thunderstorm"));
                }
            }
            curThunderLevel = level;
        }
    }
}
