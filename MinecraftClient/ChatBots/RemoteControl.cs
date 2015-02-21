using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// Allow to perform operations using whispers to the bot
    /// </summary>

    public class RemoteControl : ChatBot
    {
        public override void GetText(string text)
        {
            text = getVerbatim(text);
            string command = "", sender = "";
            if (isPrivateMessage(text, ref command, ref sender) && Settings.Bots_Owners.Contains(sender.ToLower().Trim()))
            {
                string response = "";
                if(command.StartsWith(Settings.internalCmdChar.ToString()))
                    performInternalCommand(command.Substring(1), ref response);
                else
                    SendText(command);
                    
                if (response.Length > 0)
                {
                    SendPrivateMessage(sender, response);
                }
            }
            else if (Settings.RemoteCtrl_AutoTpaccept
                && isTeleportRequest(text, ref sender)
                && (Settings.RemoteCtrl_AutoTpaccept_Everyone || Settings.Bots_Owners.Contains(sender.ToLower().Trim())))
            {
                SendText("/tpaccept");
            }
        }
    }
}
