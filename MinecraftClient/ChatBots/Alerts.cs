using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// This bot make the console beep on some specified words. Useful to detect when someone is talking to you, for example.
    /// </summary>
    public class Alerts : ChatBot
    {
        private string[] dictionary = new string[0];
        private string[] excludelist = new string[0];

        /// <summary>
        /// Intitialize the Alerts bot
        /// </summary>
        public override void Initialize()
        {
            dictionary = LoadDistinctEntriesFromFile(Settings.Alerts_MatchesFile);
            excludelist = LoadDistinctEntriesFromFile(Settings.Alerts_ExcludesFile);
        }

        /// <summary>
        /// Update settings when reloaded
        /// </summary>
        public override void OnSettingsReloaded()
        {
            if (!Settings.Alerts_Enabled)
            {
                UnloadBot();
                return;
            }

            dictionary = LoadDistinctEntriesFromFile(Settings.Alerts_MatchesFile);
            excludelist = LoadDistinctEntriesFromFile(Settings.Alerts_ExcludesFile);
        }


        /// <summary>
        /// Process text received from the server to display alerts
        /// </summary>
        /// <param name="text">Received text</param>
        public override void GetText(string text)
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
                }
            }
        }
    }
}
