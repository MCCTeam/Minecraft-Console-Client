// Use this template only when the user explicitly requests a built-in MCC bot.

using MinecraftClient.Scripting;
using Tomlet.Attributes;

namespace MinecraftClient.ChatBots
{
    public class ExampleBot : ChatBot
    {
        private const string BotName = "ExampleBot";

        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            public bool Enabled = false;

            public void OnSettingUpdate()
            {
            }
        }

        public override void Initialize()
        {
            LogToConsole(BotName, "Initialized.");
        }

        public override void AfterGameJoined()
        {
        }

        public override void GetText(string text)
        {
            text = GetVerbatim(text);

            string message = "";
            string username = "";

            if (IsPrivateMessage(text, ref message, ref username))
            {
            }
            else if (IsChatMessage(text, ref message, ref username))
            {
            }
        }

        public override void OnUnload()
        {
        }

        public override bool OnDisconnect(DisconnectReason reason, string message)
        {
            return false;
        }
    }
}
