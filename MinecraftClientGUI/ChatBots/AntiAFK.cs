using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// This bot sends a command every 60 seconds in order to stay non-afk.
    /// </summary>

    public class AntiAFK : ChatBot
    {
        private int count;
        private int timeping;

        /// <summary>
        /// This bot sends a /ping command every X seconds in order to stay non-afk.
        /// </summary>
        /// <param name="pingparam">Time amount between each ping (10 = 1s, 600 = 1 minute, etc.)</param>

        public AntiAFK(int pingparam)
        {
            count = 0;
            timeping = pingparam;
            if (timeping < 10) { timeping = 10; } //To avoid flooding
        }

        public override void Update()
        {
            count++;
            if (count == timeping)
            {
                SendText(Settings.AntiAFK_Command);
                count = 0;
            }
        }
    }
}
