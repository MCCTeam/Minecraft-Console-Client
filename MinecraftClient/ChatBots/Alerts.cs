using System;
using System.Linq;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// This bot make the console beep on some specified words. Useful to detect when someone is talking to you, for example.
    /// </summary>
    public class Alerts : ChatBot
    {
        private string[] dictionary = Array.Empty<string>();
        private string[] excludelist = Array.Empty<string>();
        private bool logToFile = false;
        float curRainLevel = 0;
        float curThunderLevel = 0;
        const float threshold = 0.2f;

        /// <summary>
        /// Intitialize the Alerts bot
        /// </summary>
        public override void Initialize()
        {
            if (Settings.Alerts_Trigger_By_Words)
            {
                dictionary = LoadDistinctEntriesFromFile(Settings.Alerts_MatchesFile);
                excludelist = LoadDistinctEntriesFromFile(Settings.Alerts_ExcludesFile);
                logToFile = Settings.Alerts_File_Logging;
            }
        }

        /// <summary>
        /// Process text received from the server to display alerts
        /// </summary>
        /// <param name="text">Received text</param>
        public override void GetText(string text)
        {
            if (Settings.Alerts_Trigger_By_Words)
            {
                //Remove color codes and convert to lowercase
                text = GetVerbatim(text).ToLower();

                //Proceed only if no exclusions are found in text
                if (!excludelist.Any(exclusion => text.Contains(exclusion)))
                {
                    //Show an alert for each alert item found in text, if any
                    foreach (string alert in dictionary.Where(alert => text.Contains(alert)))
                    {
                        if (Settings.Alerts_Beep_Enabled)
                            Console.Beep(); //Text found !

                        ConsoleIO.WriteLine(text.Replace(alert, "§c" + alert + "§r"));

                        if (logToFile && Settings.Alerts_LogFile.Length > 0)
                        {
                            DateTime now = DateTime.Now;
                            string TimeStamp = "[" + now.Year + '/' + now.Month + '/' + now.Day + ' ' + now.Hour + ':' + now.Minute + ']';
                            System.IO.File.AppendAllText(Settings.Alerts_LogFile, TimeStamp + " " + GetVerbatim(text) + "\n");
                        }
                    }
                }
            }
        }

        public override void OnRainLevelChange(float level)
        {
            if (curRainLevel < threshold && level >= threshold)
            {
                if (Settings.Alerts_Trigger_By_Rain)
                {
                    if (Settings.Alerts_Beep_Enabled)
                    {
                        Console.Beep();
                        Console.Beep();
                    }
                    LogToConsole(Translations.TryGet("bot.alerts.start_rain"));
                }
            }
            else if (curRainLevel >= threshold && level < threshold)
            {
                if (Settings.Alerts_Trigger_By_Rain)
                {
                    if (Settings.Alerts_Beep_Enabled)
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
                if (Settings.Alerts_Trigger_By_Thunderstorm)
                {
                    if (Settings.Alerts_Beep_Enabled)
                    {
                        Console.Beep();
                        Console.Beep();
                    }
                    LogToConsole(Translations.TryGet("bot.alerts.start_thunderstorm"));
                }
            }
            else if (curThunderLevel >= threshold && level < threshold)
            {
                if (Settings.Alerts_Trigger_By_Thunderstorm)
                {
                    if (Settings.Alerts_Beep_Enabled)
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
