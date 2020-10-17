using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// Example of message receiving.
    /// </summary>

    public class TestBot : ChatBot
    {
        public override void GetText(string text)
        {
            string message = "";
            string username = "";
            text = GetVerbatim(text);

            if (IsPrivateMessage(text, ref message, ref username))
            {
                LogToConsoleTranslated("bot.testBot.told", username, message);
            }
            else if (IsChatMessage(text, ref message, ref username))
            {
                LogToConsoleTranslated("bot.testBot.said", username, message);
            }
        }
    }
}
