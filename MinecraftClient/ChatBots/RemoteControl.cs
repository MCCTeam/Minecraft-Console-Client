using System;
using MinecraftClient.CommandHandler;
using MinecraftClient.Scripting;
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
                CmdResult response = new();
                PerformInternalCommand(command, ref response);
                SendPrivateMessage(sender, response.ToString());
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
