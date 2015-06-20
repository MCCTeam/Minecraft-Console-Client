using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// This bot sends a /list command every X seconds and save the result.
    /// </summary>

    public class PlayerListLogger : ChatBot
    {
        private int count;
        private int timeping;
        private string file;

        /// <summary>
        /// This bot sends a  /list command every X seconds and save the result.
        /// </summary>
        /// <param name="pingparam">Time amount between each list ping (10 = 1s, 600 = 1 minute, etc.)</param>

        public PlayerListLogger(int pingparam, string filetosavein)
        {
            count = 0;
            file = filetosavein;
            timeping = pingparam;
            if (timeping < 10) { timeping = 10; } //To avoid flooding

        }

        public override void Update()
        {
            count++;
            if (count == timeping)
            {
                SendText("/list");
                count = 0;
            }
        }

        public override void GetText(string text)
        {
            if (text.Contains("Joueurs en ligne") || text.Contains("Connected:") || text.Contains("online:"))
            {
                LogToConsole("Saving Player List");
                DateTime now = DateTime.Now;
                string TimeStamp = "[" + now.Year + '/' + now.Month + '/' + now.Day + ' ' + now.Hour + ':' + now.Minute + ']';
                System.IO.File.AppendAllText(file, TimeStamp + "\n" + GetVerbatim(text) + "\n\n");
            }
        }
    }
}
