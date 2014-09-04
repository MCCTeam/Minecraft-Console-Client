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
            text = getVerbatim(text);

            if (isPrivateMessage(text, ref message, ref username))
            {
                ConsoleIO.WriteLine("Bot: " + username + " told me : " + message);
            }
            else if (isChatMessage(text, ref message, ref username))
            {
                ConsoleIO.WriteLine("Bot: " + username + " said : " + message);
            }
        }
    }
}
