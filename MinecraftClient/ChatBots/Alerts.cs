﻿using System;
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
        /// Import alerts from the specified file
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private static string[] FromFile(string file)
        {
            if (File.Exists(file))
            {
                //Read all lines from file, remove lines with no text, convert to lowercase,
                //remove duplicate entries, convert to a string array, and return the result.
                return File.ReadAllLines(file)
                        .Where(line => !String.IsNullOrWhiteSpace(line))
                        .Select(line => line.ToLower())
                        .Distinct().ToArray();
            }
            else
            {
                LogToConsole("File not found: " + Settings.Alerts_MatchesFile);
                return new string[0];
            }
        }

        /// <summary>
        /// Intitialize the Alerts bot
        /// </summary>
        public override void Initialize()
        {
            dictionary = FromFile(Settings.Alerts_MatchesFile);
            excludelist = FromFile(Settings.Alerts_ExcludesFile);
        }

        /// <summary>
        /// Process text received from the server to display alerts
        /// </summary>
        /// <param name="text">Received text</param>
        public override void GetText(string text)
        {
            //Remove color codes and convert to lowercase
            text = getVerbatim(text).ToLower();

            //Proceed only if no exclusions are found in text
            if (!excludelist.Any(exclusion => text.Contains(exclusion)))
            {
                //Show an alert for each alert item found in text, if any
                foreach (string alert in dictionary.Where(alert => text.Contains(alert)))
                {
                    if (Settings.Alerts_Beep_Enabled)
                        Console.Beep(); //Text found !

                    if (ConsoleIO.basicIO) //Using a GUI? Pass text as is.
                        ConsoleIO.WriteLine(text.Replace(alert, "§c" + alert + "§r"));

                    else //Using Consome Prompt : Print text with alert highlighted
                    {
                        string[] splitted = text.Split(new string[] { alert }, StringSplitOptions.None);

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
                        if(!Settings.useDefaultBackground)
                        {
                            Console.BackgroundColor = ConsoleColor.Black;
                        }
                        else 
                        {
                            Console.ResetColor();
                        }                        
                        Console.ForegroundColor = ConsoleColor.Gray;
                        ConsoleIO.Write('\n');
                    }
                }
            }
        }
    }
}
