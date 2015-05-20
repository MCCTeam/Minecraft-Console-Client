using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace MinecraftClient.ChatBots
{
    class Auto_Respond : ChatBot
    {
        private String[] respondon = new String[0];
        private String[] torespond = new String[0];

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
                LogToConsole("File not found: " + file);
                return new string[0];
            }
        }

        //Initalize the bot
        public override void Initialize()
        {
            respondon = FromFile(Settings.Respond_MatchesFile);
            torespond = FromFile(Settings.Respond_RespondFile);
            ConsoleIO.WriteLine("Auto Respond Bot Sucessfully loaded!");
        }

        public override void GetText(string text)
        {
            //Remove colour codes
            text = getVerbatim(text).ToLower();

            //Check is the message is from the bot
            if (text.Contains("<" + Settings.Username.ToLower() + ">"))
            {
                //Message is from the bot, ignore the message.
            }
            else
            {
                //Check if user names should be ignored
                Regex regex = new Regex(@"\<[^\)]+\>");
                if (Settings.Respond_IgnoreUserName)
                {
                    text = regex.Replace(text, "");
                }
                //Check text to see if bot should respond
                foreach (string alert in respondon.Where(alert => text.Contains(alert)))
                {
                    //Find what to respond with
                    for (int x = 0; x < respondon.Length; x++)
                    {
                        if (respondon[x].ToString().Contains(alert))
                        {
                            SendText(torespond[x]);
                        }
                    }
                }
            }
        }
    }
}
