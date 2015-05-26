using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MinecraftClient.ChatBots
{
    class AutoRespond : ChatBot
    {
        private string[] respondon = new string[0];
        private string[] torespond = new string[0];

        //Initalize the bot
        public override void Initialize()
        {
            respondon = LoadDistinctEntriesFromFile(Settings.Respond_MatchesFile);
            torespond = LoadDistinctEntriesFromFile(Settings.Respond_RespondFile);
            ConsoleIO.WriteLine("Auto Respond Bot Sucessfully loaded!");
        }

        public override void GetText(string text)
        {
            //Remove colour codes
            text = getVerbatim(text).ToLower();
            //Check text to see if bot should respond
            foreach (string alert in respondon.Where(alert => text.Contains(alert)))
            {
                //Find what to respond with
                for (int x = 0; x < respondon.Length; x++)
                {
                    if (respondon[x].ToString().Contains(alert))
                    {
                        //Respond
                        SendText(torespond[x].ToString());
                    }
                }
            }
        }
    }
}
