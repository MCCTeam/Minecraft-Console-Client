using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Net;
using MinecraftClient.ChatBots;
using MinecraftClient.Protocol;
using MinecraftClient.Proxy;
using MinecraftClient.Protocol.Handlers.Forge;
using MinecraftClient.Mapping;
using MinecraftClient.Inventory;

namespace MinecraftClient
{
    /// <summary>
    /// The main client class, used to connect to a Minecraft server.
    /// </summary>
    public class McClient : IMinecraftComHandler
    {
        public static int ReconnectionAttemptsLeft = 0;

        private static readonly List<string> cmd_names = new List<string>();
        private static readonly Dictionary<string, Command> cmds = new Dictionary<string, Command>();
        private readonly Dictionary<Guid, string> onlinePlayers = new Dictionary<Guid, string>();

        private readonly List<ChatBot> bots = new List<ChatBot>();
        private static readonly List<ChatBot> botsOnHold = new List<ChatBot>();
        private static Dictionary<int, Container> inventories = new Dictionary<int, Container>();

        private readonly Dictionary<string, List<ChatBot>> registeredBotPluginChannels = new Dictionary<string, List<ChatBot>>();
        private readonly List<string> registeredServerPluginChannels = new List<String>();

        private bool terrainAndMovementsEnabled;
        private bool terrainAndMovementsRequested = false;
        private bool inventoryHandlingEnabled;
        private bool inventoryHandlingRequested = false;
        private bool entityHandlingEnabled;

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
        private DateTime lastKeepAlive;
        private object lastKeepAliveLock = new object();
        private int respawnTicks = 0;
        private int gamemode = 0;

        private int playerEntityID;

        // player health and hunger
        private float playerHealth;
        private int playerFoodSaturation;
        private int playerLevel;
        private int playerTotalExperience;
        private byte CurrentSlot = 0;

        // Entity handling
        private Dictionary<int, Entity> entities = new Dictionary<int, Entity>();

        // server TPS
        private long lastAge = 0;
        private DateTime lastTime;
        private Double serverTPS = 0;

        public int GetServerPort() { return port; }
        public string GetServerHost() { return host; }
        public string GetUsername() { return username; }
        public string GetUserUUID() { return uuid; }
        public string GetSessionID() { return sessionid; }
        public Location GetCurrentLocation() { return location; }
        public World GetWorld() { return world; }
        public Double GetServerTPS() { return serverTPS; }
        public float GetHealth() { return playerHealth; }
        public int GetSaturation() { return playerFoodSaturation; }
        public int GetLevel() { return playerLevel; }
        public int GetTotalExperience() { return playerTotalExperience; }
        public byte GetCurrentSlot() { return CurrentSlot; }
        public int GetGamemode() { return gamemode; }

        // get bots list for unloading them by commands
        public List<ChatBot> GetLoadedChatBots()
        {
            return bots;
        }

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
        public McClient(string username, string uuid, string sessionID, int protocolversion, ForgeInfo forgeInfo, string server_ip, ushort port)
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
        public McClient(string username, string uuid, string sessionID, string server_ip, ushort port, int protocolversion, ForgeInfo forgeInfo, string command)
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
            entityHandlingEnabled = Settings.EntityHandling;

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
                    if (Settings.AutoAttack_Enabled) { BotLoad(new ChatBots.AutoAttack()); }
                    if (Settings.AutoFishing_Enabled) { BotLoad(new ChatBots.AutoFishing()); }
                    if (Settings.AutoEat_Enabled) { BotLoad(new ChatBots.AutoEat(Settings.AutoEat_hungerThreshold)); }

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
                    else
                    {
                        Console.WriteLine("Failed to login to this server.");
                        retry = true;
                    }
                }
                catch (Exception e)
                {
                    ConsoleIO.WriteLineFormatted("§8" + e.GetType().Name + ": " + e.Message);
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
                                    ConsoleIO.WriteLogLine(response_msg);
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
        /// <param name="response_msg">May contain a confirmation or error message after processing the command, or "" otherwise.</param>
        /// <param name="localVars">Local variables passed along with the command</param>
        /// <returns>TRUE if the command was indeed an internal MCC command</returns>
        public bool PerformInternalCommand(string command, ref string response_msg, Dictionary<string, object> localVars = null)
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
                            ConsoleIO.WriteLogLine(e.Message);
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
                response_msg = cmds[command_name].Run(this, command, localVars);
                foreach (ChatBot bot in bots.ToArray())
                {
                    try
                    {
                        bot.OnInternalCommand(command_name, string.Join(" ",Command.getArgs(command)),response_msg);
                    }
                    catch (Exception e)
                    {
                        if (!(e is ThreadAbortException))
                        {
                            ConsoleIO.WriteLogLine("OnInternalCommand: Got error from " + bot.ToString() + ": " + e.ToString());
                        }
                        else throw; //ThreadAbortException should not be caught
                    }
                }
            }
            else
            {
                response_msg = "Unknown command '" + command_name + "'. Use '" + (Settings.internalCmdChar == ' ' ? "" : "" + Settings.internalCmdChar) + "help' for help.";
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Disconnect the client from the server (initiated from MCC)
        /// </summary>
        public void Disconnect()
        {
            foreach (ChatBot bot in bots.ToArray())
            {
                try
                {
                    bot.OnDisconnect(ChatBot.DisconnectReason.UserLogout, "");
                }
                catch (Exception e)
                {
                    if (!(e is ThreadAbortException))
                    {
                        ConsoleIO.WriteLogLine("OnDisconnect: Got error from " + bot.ToString() + ": " + e.ToString());
                    }
                    else throw; //ThreadAbortException should not be caught
                }
            }

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
            {
                timeoutdetector.Abort();
                timeoutdetector = null;
            }

            Thread.Sleep(1000);

            if (client != null)
                client.Close();
        }

        /// <summary>
        /// When connection has been lost, login was denied or played was kicked from the server
        /// </summary>
        public void OnConnectionLost(ChatBot.DisconnectReason reason, string message)
        {
            world.Clear();

            if (timeoutdetector != null)
            {
                timeoutdetector.Abort();
                timeoutdetector = null;
            }

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

                case ChatBot.DisconnectReason.UserLogout:
                    throw new InvalidOperationException("User-initiated logout should be done by calling Disconnect()");
            }

            foreach (ChatBot bot in bots.ToArray())
            {
                try
                {
                    will_restart |= bot.OnDisconnect(reason, message);
                }
                catch (Exception e)
                {
                    if (!(e is ThreadAbortException))
                    {
                        ConsoleIO.WriteLogLine("OnDisconnect: Got error from " + bot.ToString() + ": " + e.ToString());
                    }
                    else throw; //ThreadAbortException should not be caught
                }
            }

            if (!will_restart)
                Program.HandleFailure();
        }

        /// <summary>
        /// Called ~10 times per second by the protocol handler
        /// </summary>
        public void OnUpdate()
        {
            foreach (ChatBot bot in bots.ToArray())
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
                        ConsoleIO.WriteLogLine("Update: Got error from " + bot.ToString() + ": " + e.ToString());
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

            if (Settings.AutoRespawn && respawnTicks > 0)
            {
                respawnTicks--;
                if (respawnTicks == 0)
                    SendRespawnPacket();
            }
        }

        #region Management: Load/Unload ChatBots and Enable/Disable settings

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
            }
            return true;
        }

        /// <summary>
        /// Get entity handling status
        /// </summary>
        /// <returns></returns>
        /// <remarks>Entity Handling cannot be enabled in runtime (or after joining server)</remarks>
        public bool GetEntityHandlingEnabled()
        {
            return entityHandlingEnabled;
        }

        /// <summary>
        /// Enable or disable Entity handling.
        /// Please note that Enabling will be deferred until next relog.
        /// </summary>
        /// <param name="enabled">Enabled</param>
        /// <returns>TRUE if the setting was applied immediately, FALSE if delayed.</returns>
        public bool SetEntityHandlingEnabled(bool enabled)
        {
            if (!enabled)
            {
                if (entityHandlingEnabled)
                {
                    entityHandlingEnabled = false;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                // Entity Handling cannot be enabled in runtime (or after joining server)
                return false;
            }
        }

        #endregion

        #region Getters: Retrieve data for use in other methods or ChatBots

        /// <summary>
        /// Get all inventories. ID 0 is the player inventory.
        /// </summary>
        /// <returns>All inventories</returns>
        public Dictionary<int, Container> GetInventories()
        {
            return inventories;
        }
        
        /// <summary>
        /// Get all Entityes
        /// </summary>
        /// <returns>All Entities</returns>
        public Dictionary<int, Entity> GetEntities()
        {
            return entities;
        }

        /// <summary>
        /// Get client player's inventory items
        /// </summary>
        /// <param name="inventoryID">Window ID of the requested inventory</param>
        /// <returns> Item Dictionary indexed by Slot ID (Check wiki.vg for slot ID)</returns>
        public Container GetInventory(int inventoryID)
        {
            if (inventories.ContainsKey(inventoryID))
                return inventories[inventoryID];
            return null;
        }

        /// <summary>
        /// Get client player's inventory items
        /// </summary>
        /// <returns> Item Dictionary indexed by Slot ID (Check wiki.vg for slot ID)</returns>
        public Container GetPlayerInventory()
        {
            return GetInventory(0);
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
        /// <returns>Dictionay of online players, key is UUID, value is Player name</returns>
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

        #endregion

        #region Action methods: Perform an action on the Server

        /// <summary>
        /// Move to the specified location
        /// </summary>
        /// <param name="location">Location to reach</param>
        /// <param name="allowUnsafe">Allow possible but unsafe locations thay may hurt the player: lava, cactus...</param>
        /// <param name="allowSmallTeleport">Allow non-vanilla small teleport instead of computing path, but may cause invalid moves and/or trigger anti-cheat plugins</param>
        /// <returns>True if a path has been found</returns>
        public bool MoveTo(Location location, bool allowUnsafe = false, bool allowSmallTeleport = false)
        {
            lock (locationLock)
            {
                if (allowSmallTeleport && location.DistanceSquared(this.location) <= 32)
                {
                    // Allow small teleport within a range of 8 blocks. 1-step path to the desired location without checking anything
                    UpdateLocation(location, location); // Update yaw and pitch to look at next step
                    handler.SendLocationUpdate(location, Movement.IsOnGround(world, location), yaw, pitch);
                    return true;
                }
                else
                {
                    // Calculate path through pathfinding. Path contains a list of 1-block movement that will be divided into steps
                    if (Movement.GetAvailableMoves(world, this.location, allowUnsafe).Contains(location))
                        path = new Queue<Location>(new[] { location });
                    else path = Movement.CalculatePath(world, this.location, location, allowUnsafe);
                    return path != null;
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
        /// Send the Entity Action packet with the Specified ID
        /// </summary>
        /// <returns>TRUE if the item was successfully used</returns>
        public bool SendEntityAction(EntityActionType entityAction)
        {
            return handler.SendEntityAction(playerEntityID, (int)entityAction);
        }

        /// <summary>
        /// Use the item currently in the player's hand
        /// </summary>
        /// <returns>TRUE if the item was successfully used</returns>
        public bool UseItemOnHand()
        {
            return handler.SendUseItem(0);
        }

        /// <summary>
        /// Click a slot in the specified window
        /// </summary>
        /// <returns>TRUE if the slot was successfully clicked</returns>
        public bool DoWindowAction(int windowId, int slotId, WindowActionType action)
        {
            Item item = null;
            if (inventories.ContainsKey(windowId) && inventories[windowId].Items.ContainsKey(slotId))
                item = inventories[windowId].Items[slotId];

            return handler.SendWindowAction(windowId, slotId, action, item);
        }

        /// <summary>
        /// Give Creative Mode items into regular/survival Player Inventory
        /// </summary>
        /// <remarks>(obviously) requires to be in creative mode</remarks>
        /// <param name="slot">Destination inventory slot</param>
        /// <param name="itemType">Item type</param>
        /// <param name="count">Item count</param>
        /// <param name="nbt">Item NBT</param>
        /// <returns>TRUE if item given successfully</returns>
        public bool DoCreativeGive(int slot, ItemType itemType, int count, Dictionary<string, object> nbt = null)
        {
            return handler.SendCreativeInventoryAction(slot, itemType, count, nbt);
        }

        /// <summary>
        /// Plays animation (Player arm swing)
        /// </summary>
        /// <param name="animation">0 for left arm, 1 for right arm</param>
        /// <returns>TRUE if animation successfully done</returns>
        public bool DoAnimation(int animation)
        {
            return handler.SendAnimation(animation, playerEntityID);
        }

        /// <summary>
        /// Close the specified inventory window
        /// </summary>
        /// <param name="windowId">Window ID</param>
        /// <returns>TRUE if the window was successfully closed</returns>
        public bool CloseInventory(int windowId)
        {
            if (windowId != 0 && inventories.ContainsKey(windowId))
            {
                inventories.Remove(windowId);
                return handler.SendCloseWindow(windowId);
            }
            return false;
        }

        /// <summary>
        /// Clean all inventory
        /// </summary>
        /// <returns>TRUE if the uccessfully clear</returns>
        public bool ClearInventories()
        {
            if (inventoryHandlingEnabled)
            {
                inventories.Clear();
                inventories[0] = new Container(0, ContainerType.PlayerInventory, "Player Inventory");
                return true;
            }
            else { return false; }
        }

        /// <summary>
        /// Interact with an entity
        /// </summary>
        /// <param name="EntityID"></param>
        /// <param name="type">0: interact, 1: attack, 2: interact at</param>
        /// <param name="hand">Hand.MainHand or Hand.OffHand</param>
        /// <returns>TRUE if interaction succeeded</returns>
        public bool InteractEntity(int EntityID, int type, Hand hand = Hand.MainHand)
        {
            if (entities.ContainsKey(EntityID))
            {
                if (type == 0)
                {
                    return handler.SendInteractEntity(EntityID, type, (int)hand);
                }
                else
                {
                    return handler.SendInteractEntity(EntityID, type);
                }
            }
            else { return false; }
        }

        /// <summary>
        /// Place the block at hand in the Minecraft world
        /// </summary>
        /// <param name="location">Location to place block to</param>
        /// <param name="blockFace">Block face (e.g. Direction.Down when clicking on the block below to place this block)</param>
        /// <returns>TRUE if successfully placed</returns>
        public bool PlaceBlock(Location location, Direction blockFace, Hand hand = Hand.MainHand)
        {
            return handler.SendPlayerBlockPlacement((int)hand, location, blockFace);
        }

        /// <summary>
        /// Attempt to dig a block at the specified location
        /// </summary>
        /// <param name="location">Location of block to dig</param>
        public bool DigBlock(Location location)
        {
            if (GetTerrainEnabled())
            {
                // TODO select best face from current player location
                Direction blockFace = Direction.Down;

                // Look at block before attempting to break it
                UpdateLocation(GetCurrentLocation(), location);

                // Send dig start and dig end, will need to wait for server response to know dig result
                // See https://wiki.vg/How_to_Write_a_Client#Digging for more details
                return handler.SendPlayerDigging(0, location, blockFace)
                    && handler.SendPlayerDigging(2, location, blockFace);
            }
            else return false;
        }

        /// <summary>
        /// Change active slot in the player inventory
        /// </summary>
        /// <param name="slot">Slot to activate (0 to 8)</param>
        /// <returns>TRUE if the slot was changed</returns>
        public bool ChangeSlot(short slot)
        {
            if (slot >= 0 && slot <= 8)
            {
                CurrentSlot = Convert.ToByte(slot);
                return handler.SendHeldItemChange(slot);
            }
            else return false;
        }

        /// <summary>
        /// Update sign text
        /// </summary>
        /// <param name="location">sign location</param>
        /// <param name="line1">text one</param>
        /// <param name="line2">text two</param>
        /// <param name="line3">text three</param>
        /// <param name="line4">text1 four</param>
        public bool UpdateSign(Location location, string line1, string line2, string line3, string line4)
        {
            // TODO Open sign editor first https://wiki.vg/Protocol#Open_Sign_Editor
            return handler.SendUpdateSign(location, line1, line2, line3, line4);
        }

        #endregion

        #region Event handlers: An event occurs on the Server

        /// <summary>
        /// Dispatch a ChatBot event with automatic exception handling
        /// </summary>
        /// <example>
        /// Example for calling SomeEvent() on all bots at once:
        /// DispatchBotEvent(bot => bot.SomeEvent());
        /// </example>
        /// <param name="action">Action to execute on each bot</param>
        /// <param name="botList">Only fire the event for the specified bot list (default: all bots)</param>
        private void DispatchBotEvent(Action<ChatBot> action, IEnumerable<ChatBot> botList = null)
        {
            ChatBot[] selectedBots;

            if (botList != null)
            {
                selectedBots = botList.ToArray();
            }
            else
            {
                selectedBots = bots.ToArray();
            }

            foreach (ChatBot bot in selectedBots)
            {
                try
                {
                    action(bot);
                }
                catch (Exception e)
                {
                    if (!(e is ThreadAbortException))
                    {
                        //Retrieve parent method name to determine which event caused the exception
                        System.Diagnostics.StackFrame frame = new System.Diagnostics.StackFrame(1);
                        System.Reflection.MethodBase method = frame.GetMethod();
                        string parentMethodName = method.Name;

                        //Display a meaningful error message to help debugging the ChatBot
                        ConsoleIO.WriteLogLine(parentMethodName + ": Got error from " + bot.ToString() + ": " + e.ToString());
                    }
                    else throw; //ThreadAbortException should not be caught here as in can happen when disconnecting from server
                }
            }
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

            DispatchBotEvent(bot => bot.AfterGameJoined());

            if (inventoryHandlingRequested)
            {
                inventoryHandlingRequested = false;
                inventoryHandlingEnabled = true;
                ConsoleIO.WriteLogLine("Inventory handling is now enabled.");
            }

            ClearInventories();
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

            ClearInventories();
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
        /// <param name="lookAtLocation">Block coordinates to look at</param>
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
        /// Received some text from the server
        /// </summary>
        /// <param name="text">Text received</param>
        /// <param name="isJson">TRUE if the text is JSON-Encoded</param>
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
                    ConsoleIO.WriteLogLine("Link: " + link, false);

            DispatchBotEvent(bot => bot.GetText(text));
            DispatchBotEvent(bot => bot.GetText(text, json));
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
        public void OnInventoryOpen(int inventoryID, Container inventory)
        {
            inventories[inventoryID] = inventory;

            if (inventoryID != 0)
            {
                ConsoleIO.WriteLogLine("Inventory # " + inventoryID + " opened: " + inventory.Title);
                ConsoleIO.WriteLogLine("Use /inventory to interact with it.");
            }
        }

        /// <summary>
        /// When an inventory is close
        /// </summary>
        /// <param name="inventoryID">Location to reach</param>
        public void OnInventoryClose(int inventoryID)
        {
            if (inventories.ContainsKey(inventoryID))
                inventories.Remove(inventoryID);

            if (inventoryID != 0)
                ConsoleIO.WriteLogLine("Inventory # " + inventoryID + " closed.");
        }

        /// <summary>
        /// When received window items from server.
        /// </summary>
        /// <param name="inventoryID">Inventory ID</param>
        /// <param name="itemList">Item list, key = slot ID, value = Item information</param>
        public void OnWindowItems(byte inventoryID, Dictionary<int, Inventory.Item> itemList)
        {
            if (inventories.ContainsKey(inventoryID))
                inventories[inventoryID].Items = itemList;
        }

        /// <summary>
        /// When a slot is set inside window items
        /// </summary>
        /// <param name="inventoryID">Window ID</param>
        /// <param name="slotID">Slot ID</param>
        /// <param name="item">Item (may be null for empty slot)</param>
        public void OnSetSlot(byte inventoryID, short slotID, Item item)
        {
            if (inventories.ContainsKey(inventoryID))
            {
                if (item == null || item.IsEmpty)
                {
                    if (inventories[inventoryID].Items.ContainsKey(slotID))
                        inventories[inventoryID].Items.Remove(slotID);
                }
                else inventories[inventoryID].Items[slotID] = item;
            }
        }

        /// <summary>
        /// Set client player's ID for later receiving player's own properties
        /// </summary>
        /// <param name="EntityID">Player Entity ID</param>
        public void OnReceivePlayerEntityID(int EntityID)
        {
            playerEntityID = EntityID;
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
                DispatchBotEvent(bot => bot.OnPluginMessage(channel, data), registeredBotPluginChannels[channel]);
            }
        }

        /// <summary>
        /// Called when an entity spawned
        /// </summary>
        public void OnSpawnEntity(Entity entity)
        {
            // The entity should not already exist, but if it does, let's consider the previous one is being destroyed
            if (entities.ContainsKey(entity.ID))
                OnDestroyEntities(new[] { entity.ID });

            entities.Add(entity.ID, entity);
            DispatchBotEvent(bot => bot.OnEntitySpawn(entity));
        }
        
        /// <summary>
        /// Called when an entity effects
        /// </summary>
        public void OnEntityEffect(int entityid, Effects effect, int amplifier, int duration, byte flags)
        {
            if (entities.ContainsKey(entityid))
                DispatchBotEvent(bot => bot.OnEntityEffect(entities[entityid], effect, amplifier, duration, flags));
        }

        /// <summary>
        /// Called when a player spawns or enters the client's render distance
        /// </summary>
        public void OnSpawnPlayer(int entityID, Guid uuid, Location location, byte Yaw, byte Pitch)
        {
            string playerName = null;
            if (onlinePlayers.ContainsKey(uuid))
                playerName = onlinePlayers[uuid];
            Entity playerEntity = new Entity(entityID, EntityType.Player, location, uuid, playerName);
            OnSpawnEntity(playerEntity);
        }

        /// <summary>
        /// Called on Entity Equipment
        /// </summary>
        /// <param name="entityid"> Entity ID</param>
        /// <param name="slot"> Equipment slot. 0: main hand, 1: off hand, 2–5: armor slot (2: boots, 3: leggings, 4: chestplate, 5: helmet)</param>
        /// <param name="item"> Item)</param>
        public void OnEntityEquipment(int entityid, int slot, Item item)
        {
            if (entities.ContainsKey(entityid))
            {
                DispatchBotEvent(bot => bot.OnEntityEquipment(entities[entityid], slot, item));
            }
        }

        /// <summary>
        /// Called when the Game Mode has been updated for a player
        /// </summary>
        /// <param name="playername">Player Name</param>
        /// <param name="uuid">Player UUID (Empty for initial gamemode on login)</param>
        /// <param name="gamemode">New Game Mode (0: Survival, 1: Creative, 2: Adventure, 3: Spectator).</param>
        public void OnGamemodeUpdate(Guid uuid, int gamemode)
        {
            // Initial gamemode on login
            if (uuid == Guid.Empty)
                this.gamemode = gamemode;

            // Further regular gamemode change events
            if (onlinePlayers.ContainsKey(uuid))
            {
                string playerName = onlinePlayers[uuid];
                if (playerName == this.username)
                    this.gamemode = gamemode;
                DispatchBotEvent(bot => bot.OnGamemodeUpdate(playerName, uuid, gamemode));
            }
        }

        /// <summary>
        /// Called when entities dead/despawn.
        /// </summary>
        public void OnDestroyEntities(int[] Entities)
        {
            foreach (int a in Entities)
            {
                if (entities.ContainsKey(a))
                {
                    DispatchBotEvent(bot => bot.OnEntityDespawn(entities[a]));
                    entities.Remove(a);
                }
            }
        }

        /// <summary>
        /// Called when an entity's position changed within 8 block of its previous position.
        /// </summary>
        /// <param name="EntityID"></param>
        /// <param name="Dx"></param>
        /// <param name="Dy"></param>
        /// <param name="Dz"></param>
        /// <param name="onGround"></param>
        public void OnEntityPosition(int EntityID, Double Dx, Double Dy, Double Dz, bool onGround)
        {
            if (entities.ContainsKey(EntityID))
            {
                Location L = entities[EntityID].Location;
                L.X += Dx;
                L.Y += Dy;
                L.Z += Dz;
                entities[EntityID].Location = L;
                DispatchBotEvent(bot => bot.OnEntityMove(entities[EntityID]));
            }

        }

        /// <summary>
        /// Called when an entity moved over 8 block.
        /// </summary>
        /// <param name="EntityID"></param>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="Z"></param>
        /// <param name="onGround"></param>
        public void OnEntityTeleport(int EntityID, Double X, Double Y, Double Z, bool onGround)
        {
            if (entities.ContainsKey(EntityID))
            {
                Location location = new Location(X, Y, Z);
                entities[EntityID].Location = location;
                DispatchBotEvent(bot => bot.OnEntityMove(entities[EntityID]));
            }
        }

        /// <summary>
        /// Called when received entity properties from server.
        /// </summary>
        /// <param name="EntityID"></param>
        /// <param name="prop"></param>
        public void OnEntityProperties(int EntityID, Dictionary<string, Double> prop)
        {
            if (EntityID == playerEntityID)
            {
                DispatchBotEvent(bot => bot.OnPlayerProperty(prop));
            }
        }

        /// <summary>
        /// Called when server sent a Time Update packet.
        /// </summary>
        /// <param name="WorldAge"></param>
        /// <param name="TimeOfDay"></param>
        public void OnTimeUpdate(long WorldAge, long TimeOfDay)
        {
            // calculate server tps
            if (lastAge != 0)
            {
                DateTime currentTime = DateTime.Now;
                long tickDiff = WorldAge - lastAge;
                Double tps = tickDiff / (currentTime - lastTime).TotalSeconds;
                lastAge = WorldAge;
                lastTime = currentTime;
                if (tps <= 20.0 && tps >= 0.0 && serverTPS != tps)
                {
                    serverTPS = tps;
                    DispatchBotEvent(bot => bot.OnServerTpsUpdate(tps));
                }
            }
            else
            {
                lastAge = WorldAge;
                lastTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Called when client player's health changed, e.g. getting attack
        /// </summary>
        /// <param name="health">Player current health</param>
        public void OnUpdateHealth(float health, int food)
        {
            playerHealth = health;
            playerFoodSaturation = food;

            if (health <= 0)
            {
                if (Settings.AutoRespawn)
                {
                    ConsoleIO.WriteLogLine("You are dead. Automatically respawning after 1 second.");
                    respawnTicks = 10;
                }
                else
                {
                    ConsoleIO.WriteLogLine("You are dead. Type /respawn to respawn.");
                }
            }

            DispatchBotEvent(bot => bot.OnHealthUpdate(health, food));
        }

        /// <summary>
        /// Called when experience updates
        /// </summary>
        /// <param name="Experiencebar">Between 0 and 1</param>
        /// <param name="Level">Level</param>
        /// <param name="TotalExperience">Total Experience</param>
        public void OnSetExperience(float Experiencebar, int Level, int TotalExperience)
        {
            playerLevel = Level;
            playerTotalExperience = TotalExperience;
            DispatchBotEvent(bot => bot.OnSetExperience(Experiencebar, Level, TotalExperience));
        }

        /// <summary>
        /// Called when and explosion occurs on the server
        /// </summary>
        /// <param name="location">Explosion location</param>
        /// <param name="strength">Explosion strength</param>
        /// <param name="affectedBlocks">Amount of affected blocks</param>
        public void OnExplosion(Location location, float strength, int affectedBlocks)
        {
            DispatchBotEvent(bot => bot.OnExplosion(location, strength, affectedBlocks));
        }

        /// <summary>
        /// Called when Latency is updated
        /// </summary>
        /// <param name="uuid">player uuid</param>
        /// <param name="latency">Latency</param>
        public void OnLatencyUpdate(Guid uuid, int latency)
        {
            string playerName = null;
            if (onlinePlayers.ContainsKey(uuid))
            {
                playerName = onlinePlayers[uuid];
                DispatchBotEvent(bot => bot.OnLatencyUpdate(playerName, uuid, latency));
            }
        }

        /// <summary>
        /// Called when held item change
        /// </summary>
        /// <param name="slot"> item slot</param>
        public void OnHeldItemChange(byte slot)
        {
            CurrentSlot = slot;
            DispatchBotEvent(bot => bot.OnHeldItemChange(slot));
        }

        /// <summary>
        /// Called map data
        /// </summary>
        /// <param name="mapid"></param>
        /// <param name="scale"></param>
        /// <param name="trackingposition"></param>
        /// <param name="locked"></param>
        /// <param name="iconcount"></param>
        public void OnMapData(int mapid, byte scale, bool trackingposition, bool locked, int iconcount)
        {
            DispatchBotEvent(bot => bot.OnMapData(mapid, scale, trackingposition, locked, iconcount));
        }

        /// <summary>
        /// Received some Title from the server
        /// <param name="action"> 0 = set title, 1 = set subtitle, 3 = set action bar, 4 = set times and display, 4 = hide, 5 = reset</param>
        /// <param name="titletext"> title text</param>
        /// <param name="subtitletext"> suntitle text</param>
        /// <param name="actionbartext"> action bar text</param>
        /// <param name="fadein"> Fade In</param>
        /// <param name="stay"> Stay</param>
        /// <param name="fadeout"> Fade Out</param>
        /// <param name="json"> json text</param>
        public void OnTitle(int action, string titletext, string subtitletext, string actionbartext, int fadein, int stay, int fadeout, string json)
        {
            DispatchBotEvent(bot => bot.OnTitle(action, titletext, subtitletext, actionbartext, fadein, stay, fadeout, json));
        }
        
        /// <summary>
        /// Called when coreboardObjective
        /// </summary>
        /// <param name="objectivename">objective name</param>
        /// <param name="mode">0 to create the scoreboard. 1 to remove the scoreboard. 2 to update the display text.</param>
        /// <param name="objectivevalue">Only if mode is 0 or 2. The text to be displayed for the score</param>
        /// <param name="type">Only if mode is 0 or 2. 0 = "integer", 1 = "hearts".</param>
        public void OnScoreboardObjective(string objectivename, byte mode, string objectivevalue, int type)
        {
            string json = objectivevalue;
            objectivevalue = ChatParser.ParseText(objectivevalue);
            DispatchBotEvent(bot => bot.OnScoreboardObjective(objectivename, mode, objectivevalue, type, json));
        }
        
        /// <summary>
        /// Called when DisplayScoreboard
        /// </summary>
        /// <param name="entityname">The entity whose score this is. For players, this is their username; for other entities, it is their UUID.</param>
        /// <param name="action">0 to create/update an item. 1 to remove an item.</param>
        /// <param name="objectivename">The name of the objective the score belongs to</param>
        /// <param name="value">he score to be displayed next to the entry. Only sent when Action does not equal 1.</param>
        public void OnUpdateScore(string entityname, byte action, string objectivename, int value)
        {
            DispatchBotEvent(bot => bot.OnUpdateScore(entityname, action, objectivename, value));
        }
        #endregion
    }
}
