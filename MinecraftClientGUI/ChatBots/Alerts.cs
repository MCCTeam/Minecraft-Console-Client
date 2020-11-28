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

                    if (ConsoleIO.BasicIO) //Using a GUI? Pass text as is.
                        ConsoleIO.WriteLine(text.Replace(alert, "§c" + alert + "§r"));

                    else //Using Console Prompt : Print text with alert highlighted
                    {
                        string[] splitted = text.Split(new string[] { alert }, StringSplitOptions.None);

                        ConsoleColor fore = Console.ForegroundColor;
                        ConsoleColor back = Console.BackgroundColor;

                        if (splitted.Length > 0)
                        {
                            Console.BackgroundColor = ConsoleColor.DarkGray;
                            Console.ForegroundColor = ConsoleColor.White;
                            ConsoleIO.Write(splitted[0]);

                            for (int i = 1; i < splitted.Length; i++)
                            {
                                Console.BackgroundColor = ConsoleColor.Yellow;
                                Console.ForegroundColor = ConsoleColor.Red;
                                ConsoleIO.Write(alert);

                                Console.BackgroundColor = ConsoleColor.DarkGray;
                                Console.ForegroundColor = ConsoleColor.White;
                                ConsoleIO.Write(splitted[i]);
                            }
                        }

                        Console.BackgroundColor = back;
                        Console.ForegroundColor = fore;
                        ConsoleIO.Write('\n');
                    }
                }
            }
        }
    }
}
