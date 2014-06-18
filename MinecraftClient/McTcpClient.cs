using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Net;
using MinecraftClient.Protocol;
using MinecraftClient.Proxy;

namespace MinecraftClient
{
    /// <summary>
    /// The main client class, used to connect to a Minecraft server.
    /// </summary>

    public class McTcpClient : IMinecraftComHandler
    {
        private static List<string> cmd_names = new List<string>();
        private static Dictionary<string, Command> cmds = new Dictionary<string, Command>();
        private List<ChatBot> bots = new List<ChatBot>();
        private static List<ChatBots.Script> scripts_on_hold = new List<ChatBots.Script>();
        public void BotLoad(ChatBot b) { b.SetHandler(this); bots.Add(b); b.Initialize(); Settings.SingleCommand = ""; }
        public void BotUnLoad(ChatBot b) { bots.RemoveAll(item => object.ReferenceEquals(item, b)); }
        public void BotClear() { bots.Clear(); }

        public static int AttemptsLeft = 0;

        private string host;
        private int port;
        private string username;
        private string uuid;
        private string sessionid;

        public int getServerPort() { return port; }
        public string getServerHost() { return host; }
        public string getUsername() { return username; }
        public string getUserUUID() { return uuid; }
        public string getSessionID() { return sessionid; }
        
        TcpClient client;
        IMinecraftCom handler;

        /// <summary>
        /// Starts the main chat client
        /// </summary>
        /// <param name="username">The chosen username of a premium Minecraft Account</param>
        /// <param name="uuid">The player's UUID for online-mode authentication</param>
        /// <param name="sessionID">A valid sessionID obtained after logging in</param>
        /// <param name="server_ip">The server IP</param>
        /// <param name="port">The server port to use</param>
        /// <param name="protocolversion">Minecraft protocol version to use</param>

        public McTcpClient(string username, string uuid, string sessionID, int protocolversion, string server_ip, short port)
        {
            StartClient(username, uuid, sessionID, server_ip, port, protocolversion, false, "");
        }

        /// <summary>
        /// Starts the main chat client in single command sending mode
        /// </summary>
        /// <param name="username">The chosen username of a premium Minecraft Account</param>
        /// <param name="uuid">The player's UUID for online-mode authentication</param>
        /// <param name="sessionID">A valid sessionID obtained after logging in</param>
        /// <param name="server_ip">The server IP</param>
        /// <param name="port">The server port to use</param>
        /// <param name="protocolversion">Minecraft protocol version to use</param>
        /// <param name="command">The text or command to send.</param>

        public McTcpClient(string username, string uuid, string sessionID, string server_ip, short port, int protocolversion, string command)
        {
            StartClient(username, uuid, sessionID, server_ip, port, protocolversion, true, command);
        }

        /// <summary>
        /// Starts the main chat client, wich will login to the server using the MinecraftCom class.
        /// </summary>
        /// <param name="user">The chosen username of a premium Minecraft Account</param>
        /// <param name="sessionID">A valid sessionID obtained with MinecraftCom.GetLogin()</param>
        /// <param name="server_ip">The server IP</param>
        /// <param name="port">The server port to use</param>
        /// <param name="protocolversion">Minecraft protocol version to use</param>
        /// <param name="uuid">The player's UUID for online-mode authentication</param>
        /// <param name="singlecommand">If set to true, the client will send a single command and then disconnect from the server</param>
        /// <param name="command">The text or command to send. Will only be sent if singlecommand is set to true.</param>

        private void StartClient(string user, string uuid, string sessionID, string server_ip, short port, int protocolversion, bool singlecommand, string command)
        {
            this.sessionid = sessionID;
            this.uuid = uuid;
            this.username = user;
            this.host = server_ip;
            this.port = port;

            if (!singlecommand)
            {
                if (Settings.AntiAFK_Enabled) { BotLoad(new ChatBots.AntiAFK(Settings.AntiAFK_Delay)); }
                if (Settings.Hangman_Enabled) { BotLoad(new ChatBots.HangmanGame(Settings.Hangman_English)); }
                if (Settings.Alerts_Enabled) { BotLoad(new ChatBots.Alerts()); }
                if (Settings.ChatLog_Enabled) { BotLoad(new ChatBots.ChatLog(Settings.expandVars(Settings.ChatLog_File), Settings.ChatLog_Filter, Settings.ChatLog_DateTime)); }
                if (Settings.PlayerLog_Enabled) { BotLoad(new ChatBots.PlayerListLogger(Settings.PlayerLog_Delay, Settings.expandVars(Settings.PlayerLog_File))); }
                if (Settings.AutoRelog_Enabled) { BotLoad(new ChatBots.AutoRelog(Settings.AutoRelog_Delay, Settings.AutoRelog_Retries)); }
                if (Settings.ScriptScheduler_Enabled) { BotLoad(new ChatBots.ScriptScheduler(Settings.expandVars(Settings.ScriptScheduler_TasksFile))); }
                if (Settings.RemoteCtrl_Enabled) { BotLoad(new ChatBots.RemoteControl()); }
            }

            try
            {
                client = ProxyHandler.newTcpClient(host, port);
                client.ReceiveBufferSize = 1024 * 1024;
                handler = Protocol.ProtocolHandler.getProtocolHandler(client, protocolversion, this);
                Console.WriteLine("Version is supported.\nLogging in...");
                
                if (handler.Login())
                {
                    if (singlecommand)
                    {
                        handler.SendChatMessage(command);
                        ConsoleIO.WriteLineFormatted("§7Command §8" + command + "§7 sent.");
                        Thread.Sleep(5000);
                        handler.Disconnect();
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        foreach (ChatBot bot in scripts_on_hold) { bots.Add(bot); }
                        scripts_on_hold.Clear();
                        Console.WriteLine("Server was successfully joined.\nType '"
                            + (Settings.internalCmdChar == ' ' ? "" : "" + Settings.internalCmdChar)
                            + "quit' to leave the server.");
                        StartTalk();
                    }
                }
            }
            catch (SocketException)
            {
                Console.WriteLine("Failed to connect to this IP.");
                if (AttemptsLeft > 0)
                {
                    ChatBot.LogToConsole("Waiting 5 seconds (" + AttemptsLeft + " attempts left)...");
                    Thread.Sleep(5000); AttemptsLeft--; Program.Restart();
                }
                else if (!singlecommand) { Console.ReadLine(); }
            }
        }

        /// <summary>
        /// Allows the user to send chat messages, commands, and to leave the server.
        /// </summary>

        private void StartTalk()
        {
            try
            {
                string text = "";
                Thread.Sleep(500);
                handler.SendRespawnPacket();

                while (client.Client.Connected)
                {
                    text = ConsoleIO.ReadLine();
                    if (ConsoleIO.basicIO && text.Length > 0 && text[0] == (char)0x00)
                    {
                        //Process a request from the GUI
                        string[] command = text.Substring(1).Split((char)0x00);
                        switch (command[0].ToLower())
                        {
                            case "autocomplete":
                                if (command.Length > 1) { ConsoleIO.WriteLine((char)0x00 + "autocomplete" + (char)0x00 + handler.AutoComplete(command[1])); }
                                else Console.WriteLine((char)0x00 + "autocomplete" + (char)0x00);
                                break;
                        }
                    }
                    else
                    {
                        text = text.Trim();
                        if (text.Length > 0)
                        {
                            if (Settings.internalCmdChar == ' ' || text[0] == Settings.internalCmdChar)
                            {
                                string response_msg = "";
                                string command = Settings.internalCmdChar == ' ' ? text : text.Substring(1);
                                if (!performInternalCommand(Settings.expandVars(command), ref response_msg) && Settings.internalCmdChar == '/')
                                {
                                    SendChatMessage(text);
                                }
                                else if (response_msg.Length > 0)
                                {
                                    ConsoleIO.WriteLineFormatted("§8MCC: " + response_msg);
                                }
                            }
                            else SendChatMessage(text);
                        }
                    }
                }
            }
            catch (IOException) { }
        }

        /// <summary>
        /// Perform an internal MCC command (not a server command, use SendChatMessage() instead for that!)
        /// </summary>
        /// <param name="command">The command</param>
        /// <param name="interactive_mode">Set to true if command was sent by the user using the command prompt</param>
        /// <param name="response_msg">May contain a confirmation or error message after processing the command, or "" otherwise.</param>
        /// <returns>TRUE if the command was indeed an internal MCC command</returns>

        public bool performInternalCommand(string command, ref string response_msg)
        {
            /* Load commands from the 'Commands' namespace */

            if (cmds.Count == 0)
            {
                Type[] cmds_classes = Program.GetTypesInNamespace("MinecraftClient.Commands");
                foreach (Type type in cmds_classes)
                {
                    if (type.IsSubclassOf(typeof(Command)))
                    {
                        try
                        {
                            Command cmd = (Command)Activator.CreateInstance(type);
                            cmds[cmd.CMDName.ToLower()] = cmd;
                            cmd_names.Add(cmd.CMDName.ToLower());
                            foreach (string alias in cmd.getCMDAliases())
                                cmds[alias.ToLower()] = cmd;
                        }
                        catch (Exception e)
                        {
                            ConsoleIO.WriteLine(e.Message);
                        }
                    }
                }
            }

            /* Process the provided command */

            string command_name = command.Split(' ')[0].ToLower();
            if (command_name == "help")
            {
                if (Command.hasArg(command))
                {
                    string help_cmdname = Command.getArgs(command)[0].ToLower();
                    if (help_cmdname == "help")
                    {
                        response_msg = "help <cmdname>: show brief help about a command.";
                    }
                    else if (cmds.ContainsKey(help_cmdname))
                    {
                        response_msg = cmds[help_cmdname].CMDDesc;
                    }
                    else response_msg = "Unknown command '" + command_name + "'. Use 'help' for command list.";
                }
                else response_msg = "help <cmdname>. Available commands: " + String.Join(", ", cmd_names.ToArray());
            }
            else if (cmds.ContainsKey(command_name))
            {
                response_msg = cmds[command_name].Run(this, command);
            }
            else
            {
                response_msg = "Unknown command '" + command_name + "'. Use 'help' for help.";
                return false;
            }
            return true;
        }

        /// <summary>
        /// Disconnect the client from the server
        /// </summary>

        public void Disconnect()
        {
            foreach (ChatBot bot in bots)
                if (bot is ChatBots.Script)
                    scripts_on_hold.Add((ChatBots.Script)bot);

            handler.Disconnect();
            handler.Dispose();
            Thread.Sleep(1000);

            if (client != null) { client.Close(); }
        }

        /// <summary>
        /// Received some text from the server
        /// </summary>
        /// <param name="text">Text received</param>

        public void OnTextReceived(string text)
        {
            ConsoleIO.WriteLineFormatted(text, false);
            foreach (ChatBot bot in bots)
                bot.GetText(text);
        }

        /// <summary>
        /// When connection has been lost
        /// </summary>

        public void OnConnectionLost(ChatBot.DisconnectReason reason, string message)
        {
            bool will_restart = false;

            switch (reason)
            {
                case ChatBot.DisconnectReason.ConnectionLost:
                    message = "Connection has been lost.";
                    ConsoleIO.WriteLine(message);
                    break;

                case ChatBot.DisconnectReason.InGameKick:
                    ConsoleIO.WriteLine("Disconnected by Server :");
                    ConsoleIO.WriteLineFormatted(message);
                    break;

                case ChatBot.DisconnectReason.LoginRejected:
                    ConsoleIO.WriteLine("Login failed :");
                    ConsoleIO.WriteLineFormatted(message);
                    break;
            }

            foreach (ChatBot bot in bots)
                will_restart |= bot.OnDisconnect(reason, message);

            if (!will_restart) { Program.ReadLineReconnect(); }
        }

        /// <summary>
        /// Called ~10 times per second by the protocol handler
        /// </summary>

        public void OnUpdate()
        {
            for (int i = 0; i < bots.Count; i++)
            {
                try
                {
                    bots[i].Update();
                }
                catch (Exception e)
                {
                    ConsoleIO.WriteLineFormatted("§8Got error from " + bots[i].ToString() + ": " + e.ToString());
                }
            }
        }

        /// <summary>
        /// Send a chat message or command to the server
        /// </summary>
        /// <param name="text">Text to send to the server</param>
        /// <returns>True if the text was sent with no error</returns>

        public bool SendChatMessage(string text)
        {
            if (text.Length > 100) //Message is too long?
            {
                if (text[0] == '/')
                {
                    //Send the first 100 chars of the command
                    text = text.Substring(0, 100);
                    return handler.SendChatMessage(text);
                }
                else
                {
                    //Send the message splitted into several messages
                    while (text.Length > 100)
                    {
                        handler.SendChatMessage(text.Substring(0, 100));
                        text = text.Substring(100, text.Length - 100);
                    }
                    return handler.SendChatMessage(text);
                }
            }
            else return handler.SendChatMessage(text);
        }

        /// <summary>
        /// Allow to respawn after death
        /// </summary>
        /// <returns>True if packet successfully sent</returns>

        public bool SendRespawnPacket()
        {
            return handler.SendRespawnPacket();
        }
    }
}
