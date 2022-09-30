using System;
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
                string[] playerList = GetOnlinePlayers();

                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < playerList.Length; i++)
                {
                    sb.Append(playerList[i]);

                    // Do not add a comma after the last username
                    if (i != playerList.Length - 1)
                        sb.Append(", ");
                }

                LogDebugToConsole("Saving Player List");

                DateTime now = DateTime.Now;
                string TimeStamp = "[" + now.Year + '/' + now.Month + '/' + now.Day + ' ' + now.Hour + ':' + now.Minute + ']';
                System.IO.File.AppendAllText(file, TimeStamp + "\n" + sb.ToString() + "\n\n");

                count = 0;
            }
        }
    }
}
