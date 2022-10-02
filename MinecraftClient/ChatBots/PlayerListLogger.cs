using System;
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
        private readonly int timeping;
        private readonly string file;

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
                DateTime now = DateTime.Now;

                LogDebugToConsole("Saving Player List");

                StringBuilder sb = new();
                sb.AppendLine(string.Format("[{0}/{1}/{2} {3}:{4}]", now.Year, now.Month, now.Day, now.Hour, now.Minute));
                sb.AppendLine(string.Join(", ", GetOnlinePlayers())).AppendLine();
                System.IO.File.AppendAllText(file, sb.ToString());

                count = 0;
            }
        }
    }
}
