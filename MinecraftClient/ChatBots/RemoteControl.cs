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
            if (isPrivateMessage(text, ref command, ref sender) && Settings.Bots_Owners.Contains(sender.ToLower()))
            {
                string cmd_name = command.Split(' ')[0];
                switch (cmd_name.ToLower())
                {
                    case "exit":
                        DisconnectAndExit();
                        break;
                    case "reco":
                        ReconnectToTheServer();
                        break;
                    case "script":
                        if (command.Length >= 8)
                            RunScript(command.Substring(7), sender);
                        break;
                    case "send":
                        if (command.Length >= 6)
                            SendText(command.Substring(5));
                        break;
                    case "connect":
                        if (command.Length >= 9)
                        {
                            Settings.setServerIP(command.Substring(8));
                            ReconnectToTheServer();
                        }
                        break;
                    case "help":
                        if (command.Length >= 6)
                        {
                            string help_cmd_name = command.Substring(5).ToLower();
                            switch (help_cmd_name)
                            {
                                case "exit": SendPrivateMessage(sender, "exit: disconnect from the server."); break;
                                case "reco": SendPrivateMessage(sender, "reco: restart and reconnct to the server."); break;
                                case "script": SendPrivateMessage(sender, "script <scriptname>: run a script file."); break;
                                case "send": SendPrivateMessage(sender, "send <text>: send a chat message or command."); break;
                                case "connect": SendPrivateMessage(sender, "connect <serverip>: connect to the specified server."); break;
                                case "help": SendPrivateMessage(sender, "help <cmdname>: show brief help about a command."); break;
                                default: SendPrivateMessage(sender, "help: unknown command '" + help_cmd_name + "'."); break;
                            }
                        }
                        else SendPrivateMessage(sender, "help <cmdname>. Available commands: exit, reco, script, send, connect.");
                        break;
                    default:
                        SendPrivateMessage(sender, "Unknown command '" + cmd_name + "'. Use 'help' for help.");
                        break;
                }
            }
        }
    }
}
