using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// This bot make the console beep on some specified words. Useful to detect when someone is talking to you, for example.
    /// </summary>

    public class Alerts : ChatBot
    {
        private string[] dictionary = new string[0];
        private string[] excludelist = new string[0];

        public override void Initialize()
        {
            if (System.IO.File.Exists(Settings.Alerts_MatchesFile))
            {
                dictionary = System.IO.File.ReadAllLines(Settings.Alerts_MatchesFile);

                for (int i = 0; i < dictionary.Length; i++)
                {
                    dictionary[i] = dictionary[i].ToLower();
                }
            }
            else LogToConsole("File not found: " + Settings.Alerts_MatchesFile);

            if (System.IO.File.Exists(Settings.Alerts_ExcludesFile))
            {
                excludelist = System.IO.File.ReadAllLines(Settings.Alerts_ExcludesFile);

                for (int i = 0; i < excludelist.Length; i++)
                {
                    excludelist[i] = excludelist[i].ToLower();
                }
            }
            else LogToConsole("File not found : " + Settings.Alerts_ExcludesFile);
        }

        public override void GetText(string text)
        {
            text = getVerbatim(text);
            string comp = text.ToLower();
            foreach (string alert in dictionary)
            {
                if (comp.Contains(alert))
                {
                    bool ok = true;

                    foreach (string exclusion in excludelist)
                    {
                        if (comp.Contains(exclusion))
                        {
                            ok = false;
                            break;
                        }
                    }

                    if (ok)
                    {
                        if (Settings.Alerts_Beep_Enabled) { Console.Beep(); } //Text found !

                        if (ConsoleIO.basicIO) { ConsoleIO.WriteLine(comp.Replace(alert, "§c" + alert + "§r")); }
                        else
                        {

                            #region Displaying the text with the interesting part highlighted

                            Console.BackgroundColor = ConsoleColor.DarkGray;
                            Console.ForegroundColor = ConsoleColor.White;

                            //Will be used for text displaying
                            string[] temp = comp.Split(alert.Split(','), StringSplitOptions.RemoveEmptyEntries);
                            int p = 0;

                            //Special case : alert in the beginning of the text
                            string test = "";
                            for (int i = 0; i < alert.Length; i++)
                            {
                                test += comp[i];
                            }
                            if (test == alert)
                            {
                                Console.BackgroundColor = ConsoleColor.Yellow;
                                Console.ForegroundColor = ConsoleColor.Red;
                                for (int i = 0; i < alert.Length; i++)
                                {
                                    ConsoleIO.Write(text[p]);
                                    p++;
                                }
                            }

                            //Displaying the rest of the text
                            for (int i = 0; i < temp.Length; i++)
                            {
                                Console.BackgroundColor = ConsoleColor.DarkGray;
                                Console.ForegroundColor = ConsoleColor.White;
                                for (int j = 0; j < temp[i].Length; j++)
                                {
                                    ConsoleIO.Write(text[p]);
                                    p++;
                                }
                                Console.BackgroundColor = ConsoleColor.Yellow;
                                Console.ForegroundColor = ConsoleColor.Red;
                                try
                                {
                                    for (int j = 0; j < alert.Length; j++)
                                    {
                                        ConsoleIO.Write(text[p]);
                                        p++;
                                    }
                                }
                                catch (IndexOutOfRangeException) { }
                            }
                            Console.BackgroundColor = ConsoleColor.Black;
                            Console.ForegroundColor = ConsoleColor.Gray;
                            ConsoleIO.Write('\n');

                            #endregion

                        }
                    }
                }
            }
        }
    }
}
