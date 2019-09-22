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
        private static readonly List<ChatBot> botsOnHold = new List<ChatBot>();
        private static List<Inventory> inventories = new List<Inventory>();

        private readonly Dictionary<string, List<ChatBot>> registeredBotPluginChannels = new Dictionary<string, List<ChatBot>>();
        private readonly List<string> registeredServerPluginChannels = new List<String>();

        private bool terrainAndMovementsEnabled;
        private bool terrainAndMovementsRequested = false;
        private bool inventoryHandlingEnabled;
        private bool inventoryHandlingRequested = false;

        private object locationLock = new object();
        private bool locationReceived = false;
        private World world = new World();
        private Queue<Location> steps;
        private Queue<Location> path;
        private Location location;
        private float? yaw;
        private float? pitch;
        private double motionY;

        private string host;
        private int port;
        private string username;
        private string uuid;
        private string sessionid;
        private Inventory playerInventory;
        private DateTime lastKeepAlive;
        private object lastKeepAliveLock = new object();

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
        Thread timeoutdetector;

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
            terrainAndMovementsEnabled = Settings.TerrainAndMovements;
            inventoryHandlingEnabled = Settings.InventoryHandling;

            bool retry = false;
            this.sessionid = sessionID;
            this.uuid = uuid;
            this.username = user;
            this.host = server_ip;
            this.port = port;

            if (!singlecommand)
            {
                if (botsOnHold.Count == 0)
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
                    //Add your ChatBot here by uncommenting and adapting
                    //BotLoad(new ChatBots.YourBot());
                }
            }

            try
            {
                client = ProxyHandler.newTcpClient(host, port);
                client.ReceiveBufferSize = 1024 * 1024;
                handler = Protocol.ProtocolHandler.GetProtocolHandler(client, protocolversion, forgeInfo, this);
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
                            foreach (ChatBot bot in botsOnHold)
                                BotLoad(bot, false);
                            botsOnHold.Clear();

                            Console.WriteLine("Server was successfully joined.\nType '"
                                + (Settings.internalCmdChar == ' ' ? "" : "" + Settings.internalCmdChar)
                                + "quit' to leave the server.");

                            cmdprompt = new Thread(new ThreadStart(CommandPrompt));
                            cmdprompt.Name = "MCC Command prompt";
                            cmdprompt.Start();

                            timeoutdetector = new Thread(new ThreadStart(TimeoutDetector));
                            timeoutdetector.Name = "MCC Connection timeout detector";
                            timeoutdetector.Start();
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
                    Thread.Sleep(5000);
                    ReconnectionAttemptsLeft--;
                    Program.Restart();
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
                    if (ConsoleIO.BasicIO && text.Length > 0 && text[0] == (char)0x00)
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
        /// Periodically checks for server keepalives and consider that connection has been lost if the last received keepalive is too old.
        /// </summary>
        private void TimeoutDetector()
        {
            lock (lastKeepAliveLock)
            {
                lastKeepAlive = DateTime.Now;
            }
            do
            {
                Thread.Sleep(TimeSpan.FromSeconds(15));
                lock (lastKeepAliveLock)
                {
                    if (lastKeepAlive.AddSeconds(30) < DateTime.Now)
                    {
                        OnConnectionLost(ChatBot.DisconnectReason.ConnectionLost, "Connection Timeout");
                    }
                }
            }
            while (true);
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
                else response_msg = "help <cmdname>. Available commands: " + String.Join(", ", cmd_names.ToArray()) + ". For server help, use '" + Settings.internalCmdChar + "send /help' instead.";
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
                bot.OnDisconnect(ChatBot.DisconnectReason.ConnectionLost, "Disconnected");

            botsOnHold.Clear();
            botsOnHold.AddRange(bots);

            if (handler != null)
            {
                handler.Disconnect();
                handler.Dispose();
            }

            if (cmdprompt != null)
                cmdprompt.Abort();

            if (timeoutdetector != null)
                timeoutdetector.Abort();

            Thread.Sleep(1000);

            if (client != null)
                client.Close();
        }

        /// <summary>
        /// Load a new bot
        /// </summary>
        public void BotLoad(ChatBot b, bool init = true)
        {
            b.SetHandler(this);
            bots.Add(b);
            if (init)
                b.Initialize();
            if (this.handler != null)
                b.AfterGameJoined();
            Settings.SingleCommand = "";
        }

        /// <summary>
        /// Unload a bot
        /// </summary>
        public void BotUnLoad(ChatBot b)
        {
            bots.RemoveAll(item => object.ReferenceEquals(item, b));

            // ToList is needed to avoid an InvalidOperationException from modfiying the list while it's being iterated upon.
            var botRegistrations = registeredBotPluginChannels.Where(entry => entry.Value.Contains(b)).ToList();
            foreach (var entry in botRegistrations)
            {
                UnregisterPluginChannel(entry.Key, b);
            }
        }

        /// <summary>
        /// Clear bots
        /// </summary>
        public void BotClear()
        {
            bots.Clear();
        }

        /// <summary>
        /// Called when a server was successfully joined
        /// </summary>
        public void OnGameJoined()
        {
            if (!String.IsNullOrWhiteSpace(Settings.BrandInfo))
                handler.SendBrandInfo(Settings.BrandInfo.Trim());
            if (Settings.MCSettings_Enabled)
                handler.SendClientSettings(
                    Settings.MCSettings_Locale,
                    Settings.MCSettings_RenderDistance,
                    Settings.MCSettings_Difficulty,
                    Settings.MCSettings_ChatMode,
                    Settings.MCSettings_ChatColors,
                    Settings.MCSettings_Skin_All,
                    Settings.MCSettings_MainHand);
            foreach (ChatBot bot in bots)
                bot.AfterGameJoined();
            if (inventoryHandlingRequested)
            {
                inventoryHandlingRequested = false;
                inventoryHandlingEnabled = true;
                ConsoleIO.WriteLogLine("Inventory handling is now enabled.");
            }
        }

        /// <summary>
        /// Called when the player respawns, which happens on login, respawn and world change.
        /// </summary>
        public void OnRespawn()
        {
            if (terrainAndMovementsRequested)
            {
                terrainAndMovementsEnabled = true;
                terrainAndMovementsRequested = false;
                ConsoleIO.WriteLogLine("Terrain and Movements is now enabled.");
            }

            if (terrainAndMovementsEnabled)
            {
                world.Clear();
            }
        }

        /// <summary>
        /// Get Terrain and Movements status.
        /// </summary>
        public bool GetTerrainEnabled()
        {
            return terrainAndMovementsEnabled;
        }

        /// <summary>
        /// Get Inventory Handling Mode
        /// </summary>
        public bool GetInventoryEnabled()
        {
            return inventoryHandlingEnabled;
        }

        /// <summary>
        /// Enable or disable Terrain and Movements.
        /// Please note that Enabling will be deferred until next relog, respawn or world change.
        /// </summary>
        /// <param name="enabled">Enabled</param>
        /// <returns>TRUE if the setting was applied immediately, FALSE if delayed.</returns>
        public bool SetTerrainEnabled(bool enabled)
        {
            if (enabled)
            {
                if (!terrainAndMovementsEnabled)
                {
                    terrainAndMovementsRequested = true;
                    return false;
                }
            }
            else
            {
                terrainAndMovementsEnabled = false;
                terrainAndMovementsRequested = false;
                locationReceived = false;
                world.Clear();
            }
            return true;
        }

        /// <summary>
        /// Enable or disable Inventories.
        /// Please note that Enabling will be deferred until next relog.
        /// </summary>
        /// <param name="enabled">Enabled</param>
        /// <returns>TRUE if the setting was applied immediately, FALSE if delayed.</returns>
        public bool SetInventoryEnabled(bool enabled)
        {
            if (enabled)
            {
                if (!inventoryHandlingEnabled)
                {
                    inventoryHandlingRequested = true;
                    return false;
                }
            }
            else
            {
                inventoryHandlingEnabled = false;
                inventoryHandlingRequested = false;
                inventories.Clear();
                playerInventory = null;
            }
            return true;
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
        /// <param name="yaw">Yaw to look at</param>
        /// <param name="pitch">Pitch to look at</param>
        public void UpdateLocation(Location location, float yaw, float pitch)
        {
            this.yaw = yaw;
            this.pitch = pitch;
            UpdateLocation(location, false);
        }

        /// <summary>
        /// Called when the server sends a new player location,
        /// or if a ChatBot whishes to update the player's location.
        /// </summary>
        /// <param name="location">The new location</param>
        /// <param name="lookAt">Block coordinates to look at</param>
        public void UpdateLocation(Location location, Location lookAtLocation)
        {
            double dx = lookAtLocation.X - (location.X - 0.5);
            double dy = lookAtLocation.Y - (location.Y + 1);
            double dz = lookAtLocation.Z - (location.Z - 0.5);

            double r = Math.Sqrt(dx * dx + dy * dy + dz * dz);

            float yaw = Convert.ToSingle(-Math.Atan2(dx, dz) / Math.PI * 180);
            float pitch = Convert.ToSingle(-Math.Asin(dy / r) / Math.PI * 180);
            if (yaw < 0) yaw += 360;

            UpdateLocation(location, yaw, pitch);
        }

        /// <summary>
        /// Called when the server sends a new player location,
        /// or if a ChatBot whishes to update the player's location.
        /// </summary>
        /// <param name="location">The new location</param>
        /// <param name="direction">Direction to look at</param>
        public void UpdateLocation(Location location, Direction direction)
        {
            float yaw = 0;
            float pitch = 0;

            switch (direction)
            {
                case Direction.Up:
                    pitch = -90;
                    break;
                case Direction.Down:
                    pitch = 90;
                    break;
                case Direction.East:
                    yaw = 270;
                    break;
                case Direction.West:
                    yaw = 90;
                    break;
                case Direction.North:
                    yaw = 180;
                    break;
                case Direction.South:
                    break;
                default:
                    throw new ArgumentException("Unknown direction", "direction");
            }

            UpdateLocation(location, yaw, pitch);
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
        /// <param name="isJson">TRUE if the text is JSON-Encoded</param>
        /// <param name="links">Links embedded in text</param>
        public void OnTextReceived(string text, bool isJson)
        {
            lock (lastKeepAliveLock)
            {
                lastKeepAlive = DateTime.Now;
            }
            List<string> links = new List<string>();
            string json = null;
            if (isJson)
            {
                json = text;
                text = ChatParser.ParseText(json, links);
            }
            ConsoleIO.WriteLineFormatted(text, true);
            if (Settings.DisplayChatLinks)
                foreach (string link in links)
                    ConsoleIO.WriteLineFormatted("§8MCC: Link: " + link, false);
            foreach (ChatBot bot in bots.ToArray())
            {
                try
                {
                    bot.GetText(text);
                    if (bots.Contains(bot))
                        bot.GetText(text, json);
                }
                catch (Exception e)
                {
                    if (!(e is ThreadAbortException))
                    {
                        ConsoleIO.WriteLineFormatted("§8GetText: Got error from " + bot.ToString() + ": " + e.ToString());
                    }
                    else throw; //ThreadAbortException should not be caught
                }
            }
        }

        /// <summary>
        /// Received a connection keep-alive from the server
        /// </summary>
        public void OnServerKeepAlive()
        {
            lock (lastKeepAliveLock)
            {
                lastKeepAlive = DateTime.Now;
            }
        }

        /// <summary>
        /// When an inventory is opened
        /// </summary>
        /// <param name="inventory">Location to reach</param>
        public void OnInventoryOpen(Inventory inventory)
        {
            //TODO: Handle Inventory
            if (!inventories.Contains(inventory))
            {
                inventories.Add(inventory);
            }
        }

        /// <summary>
        /// When an inventory is close
        /// </summary>
        /// <param name="inventoryID">Location to reach</param>
        public void OnInventoryClose(byte inventoryID)
        {
            for (int i = 0; i < inventories.Count; i++)
            {
                Inventory inventory = inventories[i];
                if (inventory == null) continue;
                if (inventory.id == inventoryID)
                {
                    inventories.Remove(inventory);
                    return;
                }
            }
        }

        /// <summary>
        /// When connection has been lost
        /// </summary>
        public void OnConnectionLost(ChatBot.DisconnectReason reason, string message)
        {
            world.Clear();

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
            foreach (var bot in bots.ToArray())
            {
                try
                {
                    bot.Update();
                    bot.ProcessQueuedText();
                }
                catch (Exception e)
                {
                    if (!(e is ThreadAbortException))
                    {
                        ConsoleIO.WriteLineFormatted("§8Update: Got error from " + bot.ToString() + ": " + e.ToString());
                    }
                    else throw; //ThreadAbortException should not be caught
                }
            }

            if (terrainAndMovementsEnabled && locationReceived)
            {
                lock (locationLock)
                {
                    for (int i = 0; i < 2; i++) //Needs to run at 20 tps; MCC runs at 10 tps
                    {
                        if (yaw == null || pitch == null)
                        {
                            if (steps != null && steps.Count > 0)
                            {
                                location = steps.Dequeue();
                            }
                            else if (path != null && path.Count > 0)
                            {
                                Location next = path.Dequeue();
                                steps = Movement.Move2Steps(location, next, ref motionY);
                                UpdateLocation(location, next + new Location(0, 1, 0)); // Update yaw and pitch to look at next step
                            }
                            else
                            {
                                location = Movement.HandleGravity(world, location, ref motionY);
                            }
                        }
                        handler.SendLocationUpdate(location, Movement.IsOnGround(world, location), yaw, pitch);
                    }
                    // First 2 updates must be player position AND look, and player must not move (to conform with vanilla)
                    // Once yaw and pitch have been sent, switch back to location-only updates (without yaw and pitch)
                    yaw = null;
                    pitch = null;
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
            int maxLength = handler.GetMaxChatMessageLength();
            if (text.Length > maxLength) //Message is too long?
            {
                if (text[0] == '/')
                {
                    //Send the first 100/256 chars of the command
                    text = text.Substring(0, maxLength);
                    return handler.SendChatMessage(text);
                }
                else
                {
                    //Send the message splitted into several messages
                    while (text.Length > maxLength)
                    {
                        handler.SendChatMessage(text.Substring(0, maxLength));
                        text = text.Substring(maxLength, text.Length - maxLength);
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
            //Ignore placeholders eg 0000tab# from TabListPlus
            if (!ChatBot.IsValidName(name))
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

        /// <summary>
        /// Get a dictionary of online player names and their corresponding UUID
        /// </summary>
        /// <returns>
        ///     dictionary of online player whereby
        ///     UUID represents the key
        ///     playername represents the value</returns>
        public Dictionary<string, string> GetOnlinePlayersWithUUID()
        {
            Dictionary<string, string> uuid2Player = new Dictionary<string, string>();
            lock (onlinePlayers)
            {
                foreach (Guid key in onlinePlayers.Keys)
                {
                    uuid2Player.Add(key.ToString(), onlinePlayers[key]);
                }
            }
            return uuid2Player;
        }

        /// <summary>
        /// Registers the given plugin channel for the given bot.
        /// </summary>
        /// <param name="channel">The channel to register.</param>
        /// <param name="bot">The bot to register the channel for.</param>
        public void RegisterPluginChannel(string channel, ChatBot bot)
        {
            if (registeredBotPluginChannels.ContainsKey(channel))
            {
                registeredBotPluginChannels[channel].Add(bot);
            }
            else
            {
                List<ChatBot> bots = new List<ChatBot>();
                bots.Add(bot);
                registeredBotPluginChannels[channel] = bots;
                SendPluginChannelMessage("REGISTER", Encoding.UTF8.GetBytes(channel), true);
            }
        }

        /// <summary>
        /// Unregisters the given plugin channel for the given bot.
        /// </summary>
        /// <param name="channel">The channel to unregister.</param>
        /// <param name="bot">The bot to unregister the channel for.</param>
        public void UnregisterPluginChannel(string channel, ChatBot bot)
        {
            if (registeredBotPluginChannels.ContainsKey(channel))
            {
                List<ChatBot> registeredBots = registeredBotPluginChannels[channel];
                registeredBots.RemoveAll(item => object.ReferenceEquals(item, bot));
                if (registeredBots.Count == 0)
                {
                    registeredBotPluginChannels.Remove(channel);
                    SendPluginChannelMessage("UNREGISTER", Encoding.UTF8.GetBytes(channel), true);
                }
            }
        }

        /// <summary>
        /// Sends a plugin channel packet to the server.  See http://wiki.vg/Plugin_channel for more information
        /// about plugin channels.
        /// </summary>
        /// <param name="channel">The channel to send the packet on.</param>
        /// <param name="data">The payload for the packet.</param>
        /// <param name="sendEvenIfNotRegistered">Whether the packet should be sent even if the server or the client hasn't registered it yet.</param>
        /// <returns>Whether the packet was sent: true if it was sent, false if there was a connection error or it wasn't registered.</returns>
        public bool SendPluginChannelMessage(string channel, byte[] data, bool sendEvenIfNotRegistered = false)
        {
            if (!sendEvenIfNotRegistered)
            {
                if (!registeredBotPluginChannels.ContainsKey(channel))
                {
                    return false;
                }
                if (!registeredServerPluginChannels.Contains(channel))
                {
                    return false;
                }
            }
            return handler.SendPluginChannelPacket(channel, data);
        }

        /// <summary>
        /// Called when a plugin channel message was sent from the server.
        /// </summary>
        /// <param name="channel">The channel the message was sent on</param>
        /// <param name="data">The data from the channel</param>
        public void OnPluginChannelMessage(string channel, byte[] data)
        {
            if (channel == "REGISTER")
            {
                string[] channels = Encoding.UTF8.GetString(data).Split('\0');
                foreach (string chan in channels)
                {
                    if (!registeredServerPluginChannels.Contains(chan))
                    {
                        registeredServerPluginChannels.Add(chan);
                    }
                }
            }
            if (channel == "UNREGISTER")
            {
                string[] channels = Encoding.UTF8.GetString(data).Split('\0');
                foreach (string chan in channels)
                {
                    registeredServerPluginChannels.Remove(chan);
                }
            }

            if (registeredBotPluginChannels.ContainsKey(channel))
            {
                foreach (ChatBot bot in registeredBotPluginChannels[channel])
                {
                    bot.OnPluginMessage(channel, data);
                }
            }
        }
    }
}
