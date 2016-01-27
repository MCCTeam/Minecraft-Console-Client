﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Net;
using MinecraftClient.Protocol;
using MinecraftClient.Proxy;
using MinecraftClient.Protocol.Handlers.Forge;
using MinecraftClient.Mapping;

namespace MinecraftClient
{
    /// <summary>
    /// The main client class, used to connect to a Minecraft server.
    /// </summary>

    public class McTcpClient : IMinecraftComHandler
    {
        public static int ReconnectionAttemptsLeft = 0;

        private static readonly List<string> cmd_names = new List<string>();
        private static readonly Dictionary<string, Command> cmds = new Dictionary<string, Command>();
        private readonly Dictionary<Guid, string> onlinePlayers = new Dictionary<Guid, string>();

        private readonly List<ChatBot> bots = new List<ChatBot>();
        private static readonly List<ChatBots.Script> scripts_on_hold = new List<ChatBots.Script>();
        public void BotLoad(ChatBot b) { b.SetHandler(this); bots.Add(b); b.Initialize(); Settings.SingleCommand = ""; }
        public void BotUnLoad(ChatBot b) { bots.RemoveAll(item => object.ReferenceEquals(item, b)); }
        public void BotClear() { bots.Clear(); }

        private object locationLock = new object();
        private bool locationReceived = false;
        private World world = new World();
        private Queue<Location> steps;
        private Queue<Location> path;
        private Location location;

        private string host;
        private int port;
        private string username;
        private string uuid;
        private string sessionid;

        public int GetServerPort() { return port; }
        public string GetServerHost() { return host; }
        public string GetUsername() { return username; }
        public string GetUserUUID() { return uuid; }
        public string GetSessionID() { return sessionid; }
        public Location GetCurrentLocation() { return location; }
        public World GetWorld() { return world; }

        TcpClient client;
        IMinecraftCom handler;
        Thread cmdprompt;

        /// <summary>
        /// Starts the main chat client
        /// </summary>
        /// <param name="username">The chosen username of a premium Minecraft Account</param>
        /// <param name="uuid">The player's UUID for online-mode authentication</param>
        /// <param name="sessionID">A valid sessionID obtained after logging in</param>
        /// <param name="server_ip">The server IP</param>
        /// <param name="port">The server port to use</param>
        /// <param name="protocolversion">Minecraft protocol version to use</param>

        public McTcpClient(string username, string uuid, string sessionID, int protocolversion, ForgeInfo forgeInfo, string server_ip, ushort port)
        {
            StartClient(username, uuid, sessionID, server_ip, port, protocolversion, forgeInfo, false, "");
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

        public McTcpClient(string username, string uuid, string sessionID, string server_ip, ushort port, int protocolversion, ForgeInfo forgeInfo, string command)
        {
            StartClient(username, uuid, sessionID, server_ip, port, protocolversion, forgeInfo, true, command);
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

        private void StartClient(string user, string uuid, string sessionID, string server_ip, ushort port, int protocolversion, ForgeInfo forgeInfo, bool singlecommand, string command)
        {
            bool retry = false;
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
                if (Settings.ChatLog_Enabled) { BotLoad(new ChatBots.ChatLog(Settings.ExpandVars(Settings.ChatLog_File), Settings.ChatLog_Filter, Settings.ChatLog_DateTime)); }
                if (Settings.PlayerLog_Enabled) { BotLoad(new ChatBots.PlayerListLogger(Settings.PlayerLog_Delay, Settings.ExpandVars(Settings.PlayerLog_File))); }
                if (Settings.AutoRelog_Enabled) { BotLoad(new ChatBots.AutoRelog(Settings.AutoRelog_Delay, Settings.AutoRelog_Retries)); }
                if (Settings.ScriptScheduler_Enabled) { BotLoad(new ChatBots.ScriptScheduler(Settings.ExpandVars(Settings.ScriptScheduler_TasksFile))); }
                if (Settings.RemoteCtrl_Enabled) { BotLoad(new ChatBots.RemoteControl()); }
                if (Settings.AutoRespond_Enabled) { BotLoad(new ChatBots.AutoRespond(Settings.AutoRespond_Matches)); }
            }

            try
            {
                client = ProxyHandler.newTcpClient(host, port);
                client.ReceiveBufferSize = 1024 * 1024;
                handler = Protocol.ProtocolHandler.getProtocolHandler(client, protocolversion, forgeInfo, this);
                Console.WriteLine("Version is supported.\nLogging in...");

                try
                {
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
                            foreach (ChatBot bot in scripts_on_hold)
                                bot.SetHandler(this);
                            bots.AddRange(scripts_on_hold);
                            scripts_on_hold.Clear();

                            Console.WriteLine("Server was successfully joined.\nType '"
                                + (Settings.internalCmdChar == ' ' ? "" : "" + Settings.internalCmdChar)
                                + "quit' to leave the server.");

                            cmdprompt = new Thread(new ThreadStart(CommandPrompt));
                            cmdprompt.Name = "MCC Command prompt";
                            cmdprompt.Start();
                        }
                    }
                }
                catch (Exception e)
                {
                    ConsoleIO.WriteLineFormatted("§8" + e.Message);
                    Console.WriteLine("Failed to join this server.");
                    retry = true;
                }
            }
            catch (SocketException e)
            {
                ConsoleIO.WriteLineFormatted("§8" + e.Message);
                Console.WriteLine("Failed to connect to this IP.");
                retry = true;
            }

            if (retry)
            {
                if (ReconnectionAttemptsLeft > 0)
                {
                    ConsoleIO.WriteLogLine("Waiting 5 seconds (" + ReconnectionAttemptsLeft + " attempts left)...");
                    Thread.Sleep(5000); ReconnectionAttemptsLeft--; Program.Restart();
                }
                else if (!singlecommand && Settings.interactiveMode)
                {
                    Program.HandleFailure();
                }
            }
        }

        /// <summary>
        /// Allows the user to send chat messages, commands, and to leave the server.
        /// </summary>

        private void CommandPrompt()
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
                                if (!PerformInternalCommand(Settings.ExpandVars(command), ref response_msg) && Settings.internalCmdChar == '/')
                                {
                                    SendText(text);
                                }
                                else if (response_msg.Length > 0)
                                {
                                    ConsoleIO.WriteLineFormatted("§8MCC: " + response_msg);
                                }
                            }
                            else SendText(text);
                        }
                    }
                }
            }
            catch (IOException) { }
            catch (NullReferenceException) { }
        }

        /// <summary>
        /// Perform an internal MCC command (not a server command, use SendText() instead for that!)
        /// </summary>
        /// <param name="command">The command</param>
        /// <param name="interactive_mode">Set to true if command was sent by the user using the command prompt</param>
        /// <param name="response_msg">May contain a confirmation or error message after processing the command, or "" otherwise.</param>
        /// <returns>TRUE if the command was indeed an internal MCC command</returns>

        public bool PerformInternalCommand(string command, ref string response_msg)
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
                response_msg = "Unknown command '" + command_name + "'. Use '" + (Settings.internalCmdChar == ' ' ? "" : "" + Settings.internalCmdChar) + "help' for help.";
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

            if (handler != null)
            {
                handler.Disconnect();
                handler.Dispose();
            }

            if (cmdprompt != null)
                cmdprompt.Abort();

            Thread.Sleep(1000);

            if (client != null)
                client.Close();
        }

        /// <summary>
        /// Called when a server was successfully joined
        /// </summary>

        public void OnGameJoined()
        {
            if (!String.IsNullOrWhiteSpace(Settings.BrandInfo))
                handler.SendBrandInfo(Settings.BrandInfo.Trim());
        }

        /// <summary>
        /// Called when the server sends a new player location,
        /// or if a ChatBot whishes to update the player's location.
        /// </summary>
        /// <param name="location">The new location</param>
        /// <param name="relative">If true, the location is relative to the current location</param>

        public void UpdateLocation(Location location, bool relative)
        {
            lock (locationLock)
            {
                if (relative)
                {
                    this.location += location;
                }
                else this.location = location;
                locationReceived = true;
            }
        }

        /// <summary>
        /// Called when the server sends a new player location,
        /// or if a ChatBot whishes to update the player's location.
        /// </summary>
        /// <param name="location">The new location</param>
        /// <param name="relative">If true, the location is relative to the current location</param>

        public void UpdateLocation(Location location)
        {
            UpdateLocation(location, false);
        }

        /// <summary>
        /// Move to the specified location
        /// </summary>
        /// <param name="location">Location to reach</param>
        /// <param name="allowUnsafe">Allow possible but unsafe locations</param>
        /// <returns>True if a path has been found</returns>
        public bool MoveTo(Location location, bool allowUnsafe = false)
        {
            lock (locationLock)
            {
                if (Movement.GetAvailableMoves(world, this.location, allowUnsafe).Contains(location))
                    path = new Queue<Location>(new[] { location });
                else path = Movement.CalculatePath(world, this.location, location, allowUnsafe);
                return path != null;
            }
        }

        /// <summary>
        /// Received some text from the server
        /// </summary>
        /// <param name="text">Text received</param>

        public void OnTextReceived(string text)
        {
            ConsoleIO.WriteLineFormatted(text, false);
            for (int i = 0; i < bots.Count; i++)
            {
                try
                {
                    bots[i].GetText(text);
                }
                catch (Exception e)
                {
                    if (!(e is ThreadAbortException))
                    {
                        ConsoleIO.WriteLineFormatted("§8GetText: Got error from " + bots[i].ToString() + ": " + e.ToString());
                    }
                    else throw; //ThreadAbortException should not be caught
                }
            }
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

            if (!will_restart)
                Program.HandleFailure();
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
                    if (!(e is ThreadAbortException))
                    {
                        ConsoleIO.WriteLineFormatted("§8Update: Got error from " + bots[i].ToString() + ": " + e.ToString());
                    }
                    else throw; //ThreadAbortException should not be caught
                }
            }

            if (Settings.TerrainAndMovements && locationReceived)
            {
                lock (locationLock)
                {
                    for (int i = 0; i < 2; i++) //Needs to run at 20 tps; MCC runs at 10 tps
                    {
                        if (steps != null && steps.Count > 0)
                            location = steps.Dequeue();
                        else if (path != null && path.Count > 0)
                            steps = Movement.Move2Steps(location, path.Dequeue());
                        else location = Movement.HandleGravity(world, location);
                        handler.SendLocationUpdate(location, Movement.IsOnGround(world, location));
                    }
                }
            }
        }

        /// <summary>
        /// Send a chat message or command to the server
        /// </summary>
        /// <param name="text">Text to send to the server</param>
        /// <returns>True if the text was sent with no error</returns>

        public bool SendText(string text)
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
                        if (Settings.splitMessageDelay.TotalSeconds > 0)
                            Thread.Sleep(Settings.splitMessageDelay);
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

        /// <summary>
        /// Triggered when a new player joins the game
        /// </summary>
        /// <param name="uuid">UUID of the player</param>
        /// <param name="name">Name of the player</param>

        public void OnPlayerJoin(Guid uuid, string name)
        {
            //Ignore TabListPlus placeholders
            if (name.StartsWith("0000tab#"))
                return;

            lock (onlinePlayers)
            {
                onlinePlayers[uuid] = name;
            }
        }

        /// <summary>
        /// Triggered when a player has left the game
        /// </summary>
        /// <param name="uuid">UUID of the player</param>

        public void OnPlayerLeave(Guid uuid)
        {
            lock (onlinePlayers)
            {
                onlinePlayers.Remove(uuid);
            }
        }

        /// <summary>
        /// Get a set of online player names
        /// </summary>
        /// <returns>Online player names</returns>

        public string[] GetOnlinePlayers()
        {
            lock (onlinePlayers)
            {
                return onlinePlayers.Values.Distinct().ToArray();
            }
        }
    }
}
