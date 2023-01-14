using MinecraftClient.Scripting;

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
                LogToConsole(string.Format(Translations.bot_testBot_told, username, message));
            }
            else if (IsChatMessage(text, ref message, ref username))
            {
                LogToConsole(string.Format(Translations.bot_testBot_said, username, message));
            }
        }
    }
}
