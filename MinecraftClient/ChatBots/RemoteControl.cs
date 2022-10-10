using System;
using Tomlet.Attributes;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// Allow to perform operations using whispers to the bot
    /// </summary>

    public class RemoteControl : ChatBot
    {
        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "RemoteControl";

            public bool Enabled = false;

            public bool AutoTpaccept = true;

            public bool AutoTpaccept_Everyone = false;

            public void OnSettingUpdate() { }
        }

        public override void GetText(string text)
        {
            text = GetVerbatim(text).Trim();
            string command = "", sender = "";
            if (IsPrivateMessage(text, ref command, ref sender) && Settings.Config.Main.Advanced.BotOwners.Contains(sender.ToLower().Trim()))
            {
                string? response = "";
                PerformInternalCommand(command, ref response);
                response = GetVerbatim(response);
                foreach (char disallowedChar in McClient.GetDisallowedChatCharacters())
                {
                    response = response.Replace(disallowedChar.ToString(), String.Empty);
                }
                if (response.Length > 0)
                {
                    SendPrivateMessage(sender, response);
                }
            }
            else if (Config.AutoTpaccept
                && IsTeleportRequest(text, ref sender)
                && (Config.AutoTpaccept_Everyone || Settings.Config.Main.Advanced.BotOwners.Contains(sender.ToLower().Trim())))
            {
                SendText("/tpaccept");
            }
        }
    }
}
