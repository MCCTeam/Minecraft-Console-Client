using System;
using System.Threading.Tasks;
using MinecraftClient.Scripting;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// Example of message receiving.
    /// </summary>

    public class TestBot : ChatBot
    {
        //public override Tuple<McClientEventType, Func<object?, Task>>[]? InitializeEventCallbacks()
        //{
        //    return new Tuple<McClientEventType, Func<object?, Task>>[]
        //    {
        //        new(McClientEventType.ClientTick, async (object? o) =>
        //        {
        //            await Task.CompletedTask;
        //            LogToConsole("test aaa");
        //            throw new Exception("dwadwa");
        //        })
        //    };
        //}

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
