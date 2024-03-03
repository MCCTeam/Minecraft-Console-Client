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
using MinecraftClient.Logger;

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

        private static bool commandsLoaded = false;

        private Queue<string> chatQueue = new Queue<string>();
        private static DateTime nextMessageSendTime = DateTime.MinValue;

        private Queue<Action> threadTasks = new Queue<Action>();
        private object threadTasksLock = new object();

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
        private float? _yaw; // Used for calculation ONLY!!! Doesn't reflect the client yaw
        private float? _pitch; // Used for calculation ONLY!!! Doesn't reflect the client pitch
        private float playerYaw;
        private float playerPitch;
        private double motionY;

        private string host;
        private int port;
        private int protocolversion;
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
        private double serverTPS = 0;
        private double averageTPS = 20;
        private const int maxSamples = 5;
        private List<double> tpsSamples = new List<double>(maxSamples);
        private double sampleSum = 0;

        // players latency
        private Dictionary<string, int> playersLatency = new Dictionary<string, int>();

        // ChatBot OnNetworkPacket event
        private bool networkPacketCaptureEnabled = false;

        public int GetServerPort() { return port; }
        public string GetServerHost() { return host; }
        public string GetUsername() { return username; }
        public string GetUserUUID() { return uuid; }
        public string GetSessionID() { return sessionid; }
        public Location GetCurrentLocation() { return location; }
        public float GetYaw() { return playerYaw; }
        public float GetPitch() { return playerPitch; }
        public World GetWorld() { return world; }
        public Double GetServerTPS() { return averageTPS; }
        public float GetHealth() { return playerHealth; }
        public int GetSaturation() { return playerFoodSaturation; }
        public int GetLevel() { return playerLevel; }
        public int GetTotalExperience() { return playerTotalExperience; }
        public byte GetCurrentSlot() { return CurrentSlot; }
        public int GetGamemode() { return gamemode; }
        public bool GetNetworkPacketCaptureEnabled() { return networkPacketCaptureEnabled; }
        public int GetProtocolVersion() { return protocolversion; }
        public ILogger GetLogger() { return this.Log; }
        public int GetPlayerEntityID() { return playerEntityID; }
        public List<ChatBot> GetLoadedChatBots() { return new List<ChatBot>(bots); }

        TcpClient client;
        IMinecraftCom handler;
        Thread cmdprompt;
        Thread timeoutdetector;

        public ILogger Log;

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
            this.protocolversion = protocolversion;

            this.Log = Settings.LogToFile
                ? new FileLogLogger(Settings.ExpandVars(Settings.LogFile), Settings.PrependTimestamp)
                : new FilteredLogger();
            Log.DebugEnabled = Settings.DebugMessages;
            Log.InfoEnabled = Settings.InfoMessages;
            Log.ChatEnabled = Settings.ChatMessages;
            Log.WarnEnabled = Settings.WarningMessages;
            Log.ErrorEnabled = Settings.ErrorMessages;

            if (!singlecommand)
            {
                /* Load commands from Commands namespace */
                LoadCommands();

                if (botsOnHold.Count == 0)
                {
                    if (Settings.AntiAFK_Enabled) { BotLoad(new ChatBots.AntiAFK(Settings.AntiAFK_Delay)); }
                    if (Settings.Hangman_Enabled) { BotLoad(new ChatBots.HangmanGame(Settings.Hangman_English)); }
                    if (Settings.Alerts_Enabled) { BotLoad(new ChatBots.Alerts()); }
                    if (Settings.ChatLog_Enabled) { BotLoad(new ChatBots.ChatLog(Settings.ExpandVars(Settings.ChatLog_File), Settings.ChatLog_Filter, Settings.ChatLog_DateTime)); }
                    if (Settings.PlayerLog_Enabled) { BotLoad(new ChatBots.PlayerListLogger(Settings.PlayerLog_Delay, Settings.ExpandVars(Settings.PlayerLog_File))); }
                    if (Settings.AutoRelog_Enabled) { BotLoad(new ChatBots.AutoRelog(Settings.AutoRelog_Delay_Min, Settings.AutoRelog_Delay_Max, Settings.AutoRelog_Retries)); }
                    if (Settings.ScriptScheduler_Enabled) { BotLoad(new ChatBots.ScriptScheduler(Settings.ExpandVars(Settings.ScriptScheduler_TasksFile))); }
                    if (Settings.RemoteCtrl_Enabled) { BotLoad(new ChatBots.RemoteControl()); }
                    if (Settings.AutoRespond_Enabled) { BotLoad(new ChatBots.AutoRespond(Settings.AutoRespond_Matches)); }
                    if (Settings.AutoAttack_Enabled) { BotLoad(new ChatBots.AutoAttack(Settings.AutoAttack_Mode, Settings.AutoAttack_Priority, Settings.AutoAttack_OverrideAttackSpeed, Settings.AutoAttack_CooldownSeconds)); }
                    if (Settings.AutoFishing_Enabled) { BotLoad(new ChatBots.AutoFishing()); }
                    if (Settings.AutoEat_Enabled) { BotLoad(new ChatBots.AutoEat(Settings.AutoEat_hungerThreshold)); }
                    if (Settings.Mailer_Enabled) { BotLoad(new ChatBots.Mailer()); }
                    if (Settings.AutoCraft_Enabled) { BotLoad(new AutoCraft(Settings.AutoCraft_configFile)); }
                    if (Settings.AutoDrop_Enabled) { BotLoad(new AutoDrop(Settings.AutoDrop_Mode, Settings.AutoDrop_items)); }
                    if (Settings.ReplayMod_Enabled) { BotLoad(new ReplayCapture(Settings.ReplayMod_BackupInterval)); }

                    //Add your ChatBot here by uncommenting and adapting
                    //BotLoad(new ChatBots.YourBot());
                }
            }

            try
            {
                client = ProxyHandler.newTcpClient(host, port);
                client.ReceiveBufferSize = 1024 * 1024;
                client.ReceiveTimeout = 30000; // 30 seconds
                handler = Protocol.ProtocolHandler.GetProtocolHandler(client, protocolversion, forgeInfo, this);
                Log.Info(Translations.Get("mcc.version_supported"));

                if (!singlecommand)
                {
                    timeoutdetector = new Thread(new ThreadStart(TimeoutDetector));
                    timeoutdetector.Name = "MCC Connection timeout detector";
                    timeoutdetector.Start();
                }

                try
                {
                    if (handler.Login())
                    {
                        if (singlecommand)
                        {
                            handler.SendChatMessage(command);
                            Log.Info(Translations.Get("mcc.single_cmd", command));
                            Thread.Sleep(5000);
                            handler.Disconnect();
                            Thread.Sleep(1000);
                        }
                        else
                        {
                            foreach (ChatBot bot in botsOnHold)
                                BotLoad(bot, false);
                            botsOnHold.Clear();

                            Log.Info(Translations.Get("mcc.joined", (Settings.internalCmdChar == ' ' ? "" : "" + Settings.internalCmdChar)));

                            cmdprompt = new Thread(new ThreadStart(CommandPrompt));
                            cmdprompt.Name = "MCC Command prompt";
                            cmdprompt.Start();
                        }
                    }
                    else
                    {
                        Log.Error(Translations.Get("error.login_failed"));
                        retry = true;
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e.GetType().Name + ": " + e.Message);
                    Log.Error(Translations.Get("error.join"));
                    retry = true;
                }
            }
            catch (SocketException e)
            {
                Log.Error(e.Message);
                Log.Error(Translations.Get("error.connect"));
                retry = true;
            }

            if (retry)
            {
                if (timeoutdetector != null)
                {
                    timeoutdetector.Abort();
                    timeoutdetector = null;
                }
                if (ReconnectionAttemptsLeft > 0)
                {
                    Log.Info(Translations.Get("mcc.reconnect", ReconnectionAttemptsLeft));
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
        /// Called ~10 times per second by the protocol handler
        /// </summary>
        public void OnUpdate()
        {
            foreach (ChatBot bot in bots.ToArray())
            {
                try
                {
                    bot.Update();
                    bot.UpdateInternal();
                }
                catch (Exception e)
                {
                    if (!(e is ThreadAbortException))
                    {
                        Log.Warn("Update: Got error from " + bot.ToString() + ": " + e.ToString());
                    }
                    else throw; //ThreadAbortException should not be caught
                }
            }

            lock (chatQueue)
            {
                if (chatQueue.Count > 0 && nextMessageSendTime < DateTime.Now)
                {
                    string text = chatQueue.Dequeue();
                    handler.SendChatMessage(text);
                    nextMessageSendTime = DateTime.Now + Settings.messageCooldown;
                }
            }

            if (terrainAndMovementsEnabled && locationReceived)
            {
                lock (locationLock)
                {
                    for (int i = 0; i < 2; i++) //Needs to run at 20 tps; MCC runs at 10 tps
                    {
                        if (_yaw == null || _pitch == null)
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
                        playerYaw = _yaw == null ? playerYaw : _yaw.Value;
                        playerPitch = _pitch == null ? playerPitch : _pitch.Value;
                        handler.SendLocationUpdate(location, Movement.IsOnGround(world, location), _yaw, _pitch);
                    }
                    // First 2 updates must be player position AND look, and player must not move (to conform with vanilla)
                    // Once yaw and pitch have been sent, switch back to location-only updates (without yaw and pitch)
                    _yaw = null;
                    _pitch = null;
                }
            }

            if (Settings.AutoRespawn && respawnTicks > 0)
            {
                respawnTicks--;
                if (respawnTicks == 0)
                    SendRespawnPacket();
            }

            lock (threadTasksLock)
            {
                while (threadTasks.Count > 0)
                {
                    Action taskToRun = threadTasks.Dequeue();
                    taskToRun();
                }
            }
        }

        #region Connection Lost and Disconnect from Server

        /// <summary>
        /// Periodically checks for server keepalives and consider that connection has been lost if the last received keepalive is too old.
        /// </summary>
        private void TimeoutDetector()
        {
            UpdateKeepAlive();
            do
            {
                Thread.Sleep(TimeSpan.FromSeconds(15));
                lock (lastKeepAliveLock)
                {
                    if (lastKeepAlive.AddSeconds(30) < DateTime.Now)
                    {
                        OnConnectionLost(ChatBot.DisconnectReason.ConnectionLost, Translations.Get("error.timeout"));
                        return;
                    }
                }
            }
            while (true);
        }

        /// <summary>
        /// Update last keep alive to current time
        /// </summary>
        private void UpdateKeepAlive()
        {
            lock (lastKeepAliveLock)
            {
                lastKeepAlive = DateTime.Now;
            }
        }

        /// <summary>
        /// Disconnect the client from the server (initiated from MCC)
        /// </summary>
        public void Disconnect()
        {
            DispatchBotEvent(bot => bot.OnDisconnect(ChatBot.DisconnectReason.UserLogout, ""));

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
                if (Thread.CurrentThread != timeoutdetector)
                    timeoutdetector.Abort();
                timeoutdetector = null;
            }

            bool will_restart = false;

            switch (reason)
            {
                case ChatBot.DisconnectReason.ConnectionLost:
                    message = Translations.Get("mcc.disconnect.lost");
                    Log.Info(message);
                    break;

                case ChatBot.DisconnectReason.InGameKick:
                    Log.Info(Translations.Get("mcc.disconnect.server"));
                    Log.Info(message);
                    break;

                case ChatBot.DisconnectReason.LoginRejected:
                    Log.Info(Translations.Get("mcc.disconnect.login"));
                    Log.Info(message);
                    break;

                case ChatBot.DisconnectReason.UserLogout:
                    throw new InvalidOperationException(Translations.Get("exception.user_logout"));
            }

            //Process AutoRelog last to make sure other bots can perform their cleanup tasks first (issue #1517)
            List<ChatBot> onDisconnectBotList = bots.Where(bot => !(bot is AutoRelog)).ToList();
            onDisconnectBotList.AddRange(bots.Where(bot => bot is AutoRelog));

            foreach (ChatBot bot in onDisconnectBotList)
            {
                try
                {
                    will_restart |= bot.OnDisconnect(reason, message);
                }
                catch (Exception e)
                {
                    if (!(e is ThreadAbortException))
                    {
                        Log.Warn("OnDisconnect: Got error from " + bot.ToString() + ": " + e.ToString());
                    }
                    else throw; //ThreadAbortException should not be caught
                }
            }

            if (!will_restart)
                Program.HandleFailure();
        }

        #endregion

        #region Command prompt and internal MCC commands

        /// <summary>
        /// Allows the user to send chat messages, commands, and leave the server.
        /// </summary>
        private void CommandPrompt()
        {
            try
            {
                Thread.Sleep(500);
                while (client.Client.Connected)
                {
                    string text = ConsoleIO.ReadLine();
                    InvokeOnMainThread(() => HandleCommandPromptText(text));
                }
            }
            catch (IOException) { }
            catch (NullReferenceException) { }
        }

        /// <summary>
        /// Allows the user to send chat messages, commands, and leave the server.
        /// Process text from the MCC command prompt on the main thread.
        /// </summary>
        private void HandleCommandPromptText(string text)
        {
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
                            Log.Info(response_msg);
                        }
                    }
                    else SendText(text);
                }
            }
        }

        /// <summary>
        /// Register a custom console command
        /// </summary>
        /// <param name="cmdName">Name of the command</param>
        /// <param name="cmdDesc">Description/usage of the command</param>
        /// <param name="callback">Method for handling the command</param>
        /// <returns>True if successfully registered</returns>
        public bool RegisterCommand(string cmdName, string cmdDesc, string cmdUsage, ChatBot.CommandRunner callback)
        {
            if (cmds.ContainsKey(cmdName.ToLower()))
            {
                return false;
            }
            else
            {
                Command cmd = new ChatBot.ChatBotCommand(cmdName, cmdDesc, cmdUsage, callback);
                cmds.Add(cmdName.ToLower(), cmd);
                cmd_names.Add(cmdName.ToLower());
                return true;
            }
        }

        /// <summary>
        /// Unregister a console command
        /// </summary>
        /// <remarks>
        /// There is no check for the command is registered by above method or is embedded command.
        /// Which mean this can unload any command
        /// </remarks>
        /// <param name="cmdName">The name of command to be unregistered</param>
        /// <returns></returns>
        public bool UnregisterCommand(string cmdName)
        {
            if (cmds.ContainsKey(cmdName.ToLower()))
            {
                cmds.Remove(cmdName.ToLower());
                cmd_names.Remove(cmdName.ToLower());
                return true;
            }
            else return false;
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
            /* Process the provided command */

            string command_name = command.Split(' ')[0].ToLower();
            if (command_name == "help")
            {
                if (Command.hasArg(command))
                {
                    string help_cmdname = Command.getArgs(command)[0].ToLower();
                    if (help_cmdname == "help")
                    {
                        response_msg = Translations.Get("icmd.help");
                    }
                    else if (cmds.ContainsKey(help_cmdname))
                    {
                        response_msg = cmds[help_cmdname].GetCmdDescTranslated();
                    }
                    else response_msg = Translations.Get("icmd.unknown", command_name);
                }
                else response_msg = Translations.Get("icmd.list", String.Join(", ", cmd_names.ToArray()), Settings.internalCmdChar);
            }
            else if (cmds.ContainsKey(command_name))
            {
                response_msg = cmds[command_name].Run(this, command, localVars);
                foreach (ChatBot bot in bots.ToArray())
                {
                    try
                    {
                        bot.OnInternalCommand(command_name, string.Join(" ", Command.getArgs(command)), response_msg);
                    }
                    catch (Exception e)
                    {
                        if (!(e is ThreadAbortException))
                        {
                            Log.Warn(Translations.Get("icmd.error", bot.ToString(), e.ToString()));
                        }
                        else throw; //ThreadAbortException should not be caught
                    }
                }
            }
            else
            {
                response_msg = Translations.Get("icmd.unknown", command_name);
                return false;
            }

            return true;
        }

        public void LoadCommands()
        {
            /* Load commands from the 'Commands' namespace */

            if (!commandsLoaded)
            {
                Type[] cmds_classes = Program.GetTypesInNamespace("MinecraftClient.Commands");
                foreach (Type type in cmds_classes)
                {
                    if (type.IsSubclassOf(typeof(Command)))
                    {
                        try
                        {
                            Command cmd = (Command)Activator.CreateInstance(type);
                            cmds[cmd.CmdName.ToLower()] = cmd;
                            cmd_names.Add(cmd.CmdName.ToLower());
                            foreach (string alias in cmd.getCMDAliases())
                                cmds[alias.ToLower()] = cmd;
                        }
                        catch (Exception e)
                        {
                            Log.Warn(e.Message);
                        }
                    }
                }
                commandsLoaded = true;
            }
        }

        #endregion

        #region Thread-Invoke: Cross-thread method calls

        /// <summary>
        /// Invoke a task on the main thread, wait for completion and retrieve return value.
        /// </summary>
        /// <param name="task">Task to run with any type or return value</param>
        /// <returns>Any result returned from task, result type is inferred from the task</returns>
        /// <example>bool result = InvokeOnMainThread(methodThatReturnsAbool);</example>
        /// <example>bool result = InvokeOnMainThread(() => methodThatReturnsAbool(argument));</example>
        /// <example>int result = InvokeOnMainThread(() => { yourCode(); return 42; });</example>
        /// <typeparam name="T">Type of the return value</typeparam>
        public T InvokeOnMainThread<T>(Func<T> task)
        {
            if (!InvokeRequired)
            {
                return task();
            }
            else
            {
                TaskWithResult<T> taskWithResult = new TaskWithResult<T>(task);
                lock (threadTasksLock)
                {
                    threadTasks.Enqueue(taskWithResult.ExecuteSynchronously);
                }
                return taskWithResult.WaitGetResult();
            }
        }

        /// <summary>
        /// Invoke a task on the main thread and wait for completion
        /// </summary>
        /// <param name="task">Task to run without return value</param>
        /// <example>InvokeOnMainThread(methodThatReturnsNothing);</example>
        /// <example>InvokeOnMainThread(() => methodThatReturnsNothing(argument));</example>
        /// <example>InvokeOnMainThread(() => { yourCode(); });</example>
        public void InvokeOnMainThread(Action task)
        {
            InvokeOnMainThread(() => { task(); return true; });
        }

        /// <summary>
        /// Check if running on a different thread and InvokeOnMainThread is required
        /// </summary>
        /// <returns>True if calling thread is not the main thread</returns>
        public bool InvokeRequired
        {
            get
            {
                int callingThreadId = Thread.CurrentThread.ManagedThreadId;
                if (handler != null)
                {
                    return handler.GetNetReadThreadId() != callingThreadId;
                }
                else
                {
                    // net read thread (main thread) not yet ready
                    return false;
                }
            }
        }

        #endregion

        #region Management: Load/Unload ChatBots and Enable/Disable settings

        /// <summary>
        /// Load a new bot
        /// </summary>
        public void BotLoad(ChatBot b, bool init = true)
        {
            if (InvokeRequired)
            {
                InvokeOnMainThread(() => BotLoad(b, init));
                return;
            }

            b.SetHandler(this);
            bots.Add(b);
            if (init)
                DispatchBotEvent(bot => bot.Initialize(), new ChatBot[] { b });
            if (this.handler != null)
                DispatchBotEvent(bot => bot.AfterGameJoined(), new ChatBot[] { b });
            Settings.SingleCommand = "";
        }

        /// <summary>
        /// Unload a bot
        /// </summary>
        public void BotUnLoad(ChatBot b)
        {
            if (InvokeRequired)
            {
                InvokeOnMainThread(() => BotUnLoad(b));
                return;
            }

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
            InvokeOnMainThread(bots.Clear);
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
        /// Get entity handling status
        /// </summary>
        /// <returns></returns>
        /// <remarks>Entity Handling cannot be enabled in runtime (or after joining server)</remarks>
        public bool GetEntityHandlingEnabled()
        {
            return entityHandlingEnabled;
        }

        /// <summary>
        /// Enable or disable Terrain and Movements.
        /// Please note that Enabling will be deferred until next relog, respawn or world change.
        /// </summary>
        /// <param name="enabled">Enabled</param>
        /// <returns>TRUE if the setting was applied immediately, FALSE if delayed.</returns>
        public bool SetTerrainEnabled(bool enabled)
        {
            if (InvokeRequired)
                return InvokeOnMainThread(() => SetTerrainEnabled(enabled));

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
            if (InvokeRequired)
                return InvokeOnMainThread(() => SetInventoryEnabled(enabled));

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
        /// Enable or disable Entity handling.
        /// Please note that Enabling will be deferred until next relog.
        /// </summary>
        /// <param name="enabled">Enabled</param>
        /// <returns>TRUE if the setting was applied immediately, FALSE if delayed.</returns>
        public bool SetEntityHandlingEnabled(bool enabled)
        {
            if (InvokeRequired)
                return InvokeOnMainThread(() => SetEntityHandlingEnabled(enabled));

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

        /// <summary>
        /// Enable or disable network packet event calling.
        /// </summary>
        /// <remarks>
        /// Enable this may increase memory usage.
        /// </remarks>
        /// <param name="enabled"></param>
        public void SetNetworkPacketCaptureEnabled(bool enabled)
        {
            if (InvokeRequired)
            {
                InvokeOnMainThread(() => SetNetworkPacketCaptureEnabled(enabled));
                return;
            }

            networkPacketCaptureEnabled = enabled;
        }

        #endregion

        #region Getters: Retrieve data for use in other methods or ChatBots

        /// <summary>
        /// Get max length for chat messages
        /// </summary>
        /// <returns>Max length, in characters</returns>
        public int GetMaxChatMessageLength()
        {
            return handler.GetMaxChatMessageLength();
        }

        /// <summary>
        /// Get a list of disallowed characters in chat
        /// </summary>
        /// <returns></returns>
        public static char[] GetDisallowedChatCharacters()
        {
            return new char[] { (char)167, (char)127 }; // Minecraft color code and ASCII code DEL
        }

        /// <summary>
        /// Get all inventories. ID 0 is the player inventory.
        /// </summary>
        /// <returns>All inventories</returns>
        public Dictionary<int, Container> GetInventories()
        {
            return inventories;
        }

        /// <summary>
        /// Get all Entities
        /// </summary>
        /// <returns>All Entities</returns>
        public Dictionary<int, Entity> GetEntities()
        {
            return entities;
        }

        /// <summary>
        /// Get all players latency
        /// </summary>
        /// <returns>All players latency</returns>
        public Dictionary<string, int> GetPlayersLatency()
        {
            return playersLatency;
        }

        /// <summary>
        /// Get client player's inventory items
        /// </summary>
        /// <param name="inventoryID">Window ID of the requested inventory</param>
        /// <returns> Item Dictionary indexed by Slot ID (Check wiki.vg for slot ID)</returns>
        public Container GetInventory(int inventoryID)
        {
            if (InvokeRequired)
                return InvokeOnMainThread(() => GetInventory(inventoryID));

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
        /// <param name="allowDirectTeleport">Allow non-vanilla direct teleport instead of computing path, but may cause invalid moves and/or trigger anti-cheat plugins</param>
        /// <returns>True if a path has been found</returns>
        public bool MoveTo(Location location, bool allowUnsafe = false, bool allowDirectTeleport = false)
        {
            lock (locationLock)
            {
                if (allowDirectTeleport)
                {
                    // 1-step path to the desired location without checking anything
                    UpdateLocation(location, location); // Update yaw and pitch to look at next step
                    handler.SendLocationUpdate(location, Movement.IsOnGround(world, location), _yaw, _pitch);
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
        public void SendText(string text)
        {
            lock (chatQueue)
            {
                if (String.IsNullOrEmpty(text))
                    return;
                int maxLength = handler.GetMaxChatMessageLength();
                if (text.Length > maxLength) //Message is too long?
                {
                    if (text[0] == '/')
                    {
                        //Send the first 100/256 chars of the command
                        text = text.Substring(0, maxLength);
                        chatQueue.Enqueue(text);
                    }
                    else
                    {
                        //Split the message into several messages
                        while (text.Length > maxLength)
                        {
                            chatQueue.Enqueue(text.Substring(0, maxLength));
                            text = text.Substring(maxLength, text.Length - maxLength);
                        }
                        chatQueue.Enqueue(text);
                    }
                }
                else chatQueue.Enqueue(text);
            }
        }

        /// <summary>
        /// Allow to respawn after death
        /// </summary>
        /// <returns>True if packet successfully sent</returns>
        public bool SendRespawnPacket()
        {
            if (InvokeRequired)
                return InvokeOnMainThread<bool>(SendRespawnPacket);

            return handler.SendRespawnPacket();
        }

        /// <summary>
        /// Registers the given plugin channel for the given bot.
        /// </summary>
        /// <param name="channel">The channel to register.</param>
        /// <param name="bot">The bot to register the channel for.</param>
        public void RegisterPluginChannel(string channel, ChatBot bot)
        {
            if (InvokeRequired)
            {
                InvokeOnMainThread(() => RegisterPluginChannel(channel, bot));
                return;
            }

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
            if (InvokeRequired)
            {
                InvokeOnMainThread(() => UnregisterPluginChannel(channel, bot));
                return;
            }

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
            if (InvokeRequired)
                return InvokeOnMainThread(() => SendPluginChannelMessage(channel, data, sendEvenIfNotRegistered));

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
            return InvokeOnMainThread(() => handler.SendEntityAction(playerEntityID, (int)entityAction));
        }

        /// <summary>
        /// Use the item currently in the player's hand
        /// </summary>
        /// <returns>TRUE if the item was successfully used</returns>
        public bool UseItemOnHand()
        {
            return InvokeOnMainThread(() => handler.SendUseItem(0));
        }

        /// <summary>
        /// Click a slot in the specified window
        /// </summary>
        /// <returns>TRUE if the slot was successfully clicked</returns>
        public bool DoWindowAction(int windowId, int slotId, WindowActionType action)
        {
            if (InvokeRequired)
                return InvokeOnMainThread(() => DoWindowAction(windowId, slotId, action));

            Item item = null;
            if (inventories.ContainsKey(windowId) && inventories[windowId].Items.ContainsKey(slotId))
                item = inventories[windowId].Items[slotId];

            // Inventory update must be after sending packet
            bool result = handler.SendWindowAction(windowId, slotId, action, item);

            // Update our inventory base on action type
            var inventory = GetInventory(windowId);
            var playerInventory = GetInventory(0);
            if (inventory != null)
            {
                switch (action)
                {
                    case WindowActionType.LeftClick:
                        // Check if cursor have item (slot -1)
                        if (playerInventory.Items.ContainsKey(-1))
                        {
                            // When item on cursor and clicking slot 0, nothing will happen
                            if (slotId == 0) break;

                            // Check target slot also have item?
                            if (inventory.Items.ContainsKey(slotId))
                            {
                                // Check if both item are the same?
                                if (inventory.Items[slotId].Type == playerInventory.Items[-1].Type)
                                {
                                    int maxCount = inventory.Items[slotId].Type.StackCount();
                                    // Check item stacking
                                    if ((inventory.Items[slotId].Count + playerInventory.Items[-1].Count) <= maxCount)
                                    {
                                        // Put cursor item to target
                                        inventory.Items[slotId].Count += playerInventory.Items[-1].Count;
                                        playerInventory.Items.Remove(-1);
                                    }
                                    else
                                    {
                                        // Leave some item on cursor
                                        playerInventory.Items[-1].Count -= (maxCount - inventory.Items[slotId].Count);
                                        inventory.Items[slotId].Count = maxCount;
                                    }
                                }
                                else
                                {
                                    // Swap two items
                                    var itemTmp = playerInventory.Items[-1];
                                    playerInventory.Items[-1] = inventory.Items[slotId];
                                    inventory.Items[slotId] = itemTmp;
                                }
                            }
                            else
                            {
                                // Put cursor item to target
                                inventory.Items[slotId] = playerInventory.Items[-1];
                                playerInventory.Items.Remove(-1);
                            }
                        }
                        else
                        {
                            // Check target slot have item?
                            if (inventory.Items.ContainsKey(slotId))
                            {
                                // When taking item from slot 0, server will update us
                                if (slotId == 0) break;

                                // Put target slot item to cursor
                                playerInventory.Items[-1] = inventory.Items[slotId];
                                inventory.Items.Remove(slotId);
                            }
                        }
                        break;
                    case WindowActionType.RightClick:
                        // Check if cursor have item (slot -1)
                        if (playerInventory.Items.ContainsKey(-1))
                        {
                            // When item on cursor and clicking slot 0, nothing will happen
                            if (slotId == 0) break;

                            // Check target slot have item?
                            if (inventory.Items.ContainsKey(slotId))
                            {
                                // Check if both item are the same?
                                if (inventory.Items[slotId].Type == playerInventory.Items[-1].Type)
                                {
                                    // Check item stacking
                                    if (inventory.Items[slotId].Count < inventory.Items[slotId].Type.StackCount())
                                    {
                                        // Drop 1 item count from cursor
                                        playerInventory.Items[-1].Count--;
                                        inventory.Items[slotId].Count++;
                                    }
                                }
                                else
                                {
                                    // Swap two items
                                    var itemTmp = playerInventory.Items[-1];
                                    playerInventory.Items[-1] = inventory.Items[slotId];
                                    inventory.Items[slotId] = itemTmp;
                                }
                            }
                            else
                            {
                                // Drop 1 item count from cursor
                                var itemTmp = playerInventory.Items[-1];
                                var itemClone = new Item(itemTmp.Type, 1, itemTmp.NBT);
                                inventory.Items[slotId] = itemClone;
                                playerInventory.Items[-1].Count--;
                            }
                        }
                        else
                        {
                            // Check target slot have item?
                            if (inventory.Items.ContainsKey(slotId))
                            {
                                if (slotId == 0)
                                {
                                    // no matter how many item in slot 0, only 1 will be taken out
                                    // Also server will update us
                                    break;
                                }
                                if (inventory.Items[slotId].Count == 1)
                                {
                                    // Only 1 item count. Put it to cursor
                                    playerInventory.Items[-1] = inventory.Items[slotId];
                                    inventory.Items.Remove(slotId);
                                }
                                else
                                {
                                    // Take half of the item stack to cursor
                                    if (inventory.Items[slotId].Count % 2 == 0)
                                    {
                                        // Can be evenly divided
                                        Item itemTmp = inventory.Items[slotId];
                                        playerInventory.Items[-1] = new Item(itemTmp.Type, itemTmp.Count / 2, itemTmp.NBT);
                                        inventory.Items[slotId].Count = itemTmp.Count / 2;
                                    }
                                    else
                                    {
                                        // Cannot be evenly divided. item count on cursor is always larger than item on inventory
                                        Item itemTmp = inventory.Items[slotId];
                                        playerInventory.Items[-1] = new Item(itemTmp.Type, (itemTmp.Count + 1) / 2, itemTmp.NBT);
                                        inventory.Items[slotId].Count = (itemTmp.Count - 1) / 2;
                                    }
                                }
                            }
                        }
                        break;
                    case WindowActionType.ShiftClick:
                        if (slotId == 0) break;
                        if (inventory.Items.ContainsKey(slotId))
                        {
                            /* Target slot have item */

                            int upperStartSlot = 9;
                            int upperEndSlot = 35;

                            switch (inventory.Type)
                            {
                                case ContainerType.PlayerInventory:
                                    upperStartSlot = 9;
                                    upperEndSlot = 35;
                                    break;
                                case ContainerType.Crafting:
                                    upperStartSlot = 1;
                                    upperEndSlot = 9;
                                    break;
                                    // TODO: Define more container type here
                            }

                            // Cursor have item or not doesn't matter
                            // If hotbar already have same item, will put on it first until every stack are full
                            // If no more same item , will put on the first empty slot (smaller slot id)
                            // If inventory full, item will not move
                            if (slotId <= upperEndSlot)
                            {
                                // Clicked slot is on upper side inventory, put it to hotbar
                                // Now try to find same item and put on them
                                var itemsClone = playerInventory.Items.ToDictionary(entry => entry.Key, entry => entry.Value);
                                foreach (KeyValuePair<int, Item> _item in itemsClone)
                                {
                                    if (_item.Key <= upperEndSlot) continue;

                                    int maxCount = _item.Value.Type.StackCount();
                                    if (_item.Value.Type == inventory.Items[slotId].Type && _item.Value.Count < maxCount)
                                    {
                                        // Put item on that stack
                                        int spaceLeft = maxCount - _item.Value.Count;
                                        if (inventory.Items[slotId].Count <= spaceLeft)
                                        {
                                            // Can fit into the stack
                                            inventory.Items[_item.Key].Count += inventory.Items[slotId].Count;
                                            inventory.Items.Remove(slotId);
                                        }
                                        else
                                        {
                                            inventory.Items[slotId].Count -= spaceLeft;
                                            inventory.Items[_item.Key].Count = inventory.Items[_item.Key].Type.StackCount();
                                        }
                                    }
                                }
                                if (inventory.Items[slotId].Count > 0)
                                {
                                    int[] emptySlots = inventory.GetEmpytSlots();
                                    int emptySlot = -2;
                                    foreach (int slot in emptySlots)
                                    {
                                        if (slot <= upperEndSlot) continue;
                                        emptySlot = slot;
                                        break;
                                    }
                                    if (emptySlot != -2)
                                    {
                                        var itemTmp = inventory.Items[slotId];
                                        inventory.Items[emptySlot] = new Item(itemTmp.Type, itemTmp.Count, itemTmp.NBT);
                                        inventory.Items.Remove(slotId);
                                    }
                                }
                            }
                            else
                            {
                                // Clicked slot is on hotbar, put it to upper inventory
                                // Now try to find same item and put on them
                                var itemsClone = playerInventory.Items.ToDictionary(entry => entry.Key, entry => entry.Value);
                                foreach (KeyValuePair<int, Item> _item in itemsClone)
                                {
                                    if (_item.Key < upperStartSlot) continue;
                                    if (_item.Key >= upperEndSlot) break;

                                    int maxCount = _item.Value.Type.StackCount();
                                    if (_item.Value.Type == inventory.Items[slotId].Type && _item.Value.Count < maxCount)
                                    {
                                        // Put item on that stack
                                        int spaceLeft = maxCount - _item.Value.Count;
                                        if (inventory.Items[slotId].Count <= spaceLeft)
                                        {
                                            // Can fit into the stack
                                            inventory.Items[_item.Key].Count += inventory.Items[slotId].Count;
                                            inventory.Items.Remove(slotId);
                                        }
                                        else
                                        {
                                            inventory.Items[slotId].Count -= spaceLeft;
                                            inventory.Items[_item.Key].Count = inventory.Items[_item.Key].Type.StackCount();
                                        }
                                    }
                                }
                                if (inventory.Items[slotId].Count > 0)
                                {
                                    int[] emptySlots = inventory.GetEmpytSlots();
                                    int emptySlot = -2;
                                    foreach (int slot in emptySlots)
                                    {
                                        if (slot < upperStartSlot) continue;
                                        if (slot >= upperEndSlot) break;
                                        emptySlot = slot;
                                        break;
                                    }
                                    if (emptySlot != -2)
                                    {
                                        var itemTmp = inventory.Items[slotId];
                                        inventory.Items[emptySlot] = new Item(itemTmp.Type, itemTmp.Count, itemTmp.NBT);
                                        inventory.Items.Remove(slotId);
                                    }
                                }
                            }
                        }
                        break;
                    case WindowActionType.DropItem:
                        if (inventory.Items.ContainsKey(slotId))
                            inventory.Items[slotId].Count--;

                        if (inventory.Items[slotId].Count <= 0)
                            inventory.Items.Remove(slotId);
                        break;
                    case WindowActionType.DropItemStack:
                        inventory.Items.Remove(slotId);
                        break;
                }
            }

            return result;
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
            return InvokeOnMainThread(() => handler.SendCreativeInventoryAction(slot, itemType, count, nbt));
        }

        /// <summary>
        /// Plays animation (Player arm swing)
        /// </summary>
        /// <param name="animation">0 for left arm, 1 for right arm</param>
        /// <returns>TRUE if animation successfully done</returns>
        public bool DoAnimation(int animation)
        {
            return InvokeOnMainThread(() => handler.SendAnimation(animation, playerEntityID));
        }

        /// <summary>
        /// Close the specified inventory window
        /// </summary>
        /// <param name="windowId">Window ID</param>
        /// <returns>TRUE if the window was successfully closed</returns>
        /// <remarks>Sending close window for inventory 0 can cause server to update our inventory if there are any item in the crafting area</remarks>
        public bool CloseInventory(int windowId)
        {
            if (InvokeRequired)
                return InvokeOnMainThread(() => CloseInventory(windowId));

            if (inventories.ContainsKey(windowId))
            {
                if (windowId != 0)
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
            if (!inventoryHandlingEnabled)
                return false;

            if (InvokeRequired)
                return InvokeOnMainThread<bool>(ClearInventories);

            inventories.Clear();
            inventories[0] = new Container(0, ContainerType.PlayerInventory, "Player Inventory");
            return true;
        }

        /// <summary>
        /// Interact with an entity
        /// </summary>
        /// <param name="EntityID"></param>
        /// <param name="type">0: interact, 1: attack, 2: interact at</param>
        /// <param name="hand">Hand.MainHand or Hand.OffHand</param>
        /// <returns>TRUE if interaction succeeded</returns>
        public bool InteractEntity(int entityID, int type, Hand hand = Hand.MainHand)
        {
            if (InvokeRequired)
                return InvokeOnMainThread(() => InteractEntity(entityID, type, hand));

            if (entities.ContainsKey(entityID))
            {
                if (type == 0)
                {
                    return handler.SendInteractEntity(entityID, type, (int)hand);
                }
                else
                {
                    return handler.SendInteractEntity(entityID, type);
                }
            }
            else return false;
        }

        /// <summary>
        /// Place the block at hand in the Minecraft world
        /// </summary>
        /// <param name="location">Location to place block to</param>
        /// <param name="blockFace">Block face (e.g. Direction.Down when clicking on the block below to place this block)</param>
        /// <returns>TRUE if successfully placed</returns>
        public bool PlaceBlock(Location location, Direction blockFace, Hand hand = Hand.MainHand)
        {
            return InvokeOnMainThread(() => handler.SendPlayerBlockPlacement((int)hand, location, blockFace));
        }

        /// <summary>
        /// Attempt to dig a block at the specified location
        /// </summary>
        /// <param name="location">Location of block to dig</param>
        /// <param name="swingArms">Also perform the "arm swing" animation</param>
        /// <param name="lookAtBlock">Also look at the block before digging</param>
        public bool DigBlock(Location location, bool swingArms = true, bool lookAtBlock = true)
        {
            if (!GetTerrainEnabled())
                return false;

            if (InvokeRequired)
                return InvokeOnMainThread(() => DigBlock(location, swingArms, lookAtBlock));

            // TODO select best face from current player location
            Direction blockFace = Direction.Down;

            // Look at block before attempting to break it
            if (lookAtBlock)
                UpdateLocation(GetCurrentLocation(), location);

            // Send dig start and dig end, will need to wait for server response to know dig result
            // See https://wiki.vg/How_to_Write_a_Client#Digging for more details
            return handler.SendPlayerDigging(0, location, blockFace)
                && (!swingArms || DoAnimation((int)Hand.MainHand))
                && handler.SendPlayerDigging(2, location, blockFace);
        }

        /// <summary>
        /// Change active slot in the player inventory
        /// </summary>
        /// <param name="slot">Slot to activate (0 to 8)</param>
        /// <returns>TRUE if the slot was changed</returns>
        public bool ChangeSlot(short slot)
        {
            if (slot < 0 || slot > 8)
                return false;

            if (InvokeRequired)
                return InvokeOnMainThread(() => ChangeSlot(slot));

            CurrentSlot = Convert.ToByte(slot);
            return handler.SendHeldItemChange(slot);
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
            return InvokeOnMainThread(() => handler.SendUpdateSign(location, line1, line2, line3, line4));
        }

        /// <summary>
        /// Select villager trade
        /// </summary>
        /// <param name="selectedSlot">The slot of the trade, starts at 0.</param>
        public bool SelectTrade(int selectedSlot)
        {
            return InvokeOnMainThread(() => handler.SelectTrade(selectedSlot));
        }

        /// <summary>
        /// Update command block
        /// </summary>
        /// <param name="location">command block location</param>
        /// <param name="command">command</param>
        /// <param name="mode">command block mode</param>
        /// <param name="flags">command block flags</param>
        public bool UpdateCommandBlock(Location location, string command, CommandBlockMode mode, CommandBlockFlags flags)
        {
            return InvokeOnMainThread(() => handler.UpdateCommandBlock(location, command, mode, flags));
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
                        Log.Error(parentMethodName + ": Got error from " + bot.ToString() + ": " + e.ToString());
                    }
                    else throw; //ThreadAbortException should not be caught here as in can happen when disconnecting from server
                }
            }
        }

        /// <summary>
        /// Called when a network packet received or sent
        /// </summary>
        /// <remarks>
        /// Only called if <see cref="networkPacketEventEnabled"/> is set to True
        /// </remarks>
        /// <param name="packetID">Packet ID</param>
        /// <param name="packetData">A copy of Packet Data</param>
        /// <param name="isLogin">The packet is login phase or playing phase</param>
        /// <param name="isInbound">The packet is received from server or sent by client</param>
        public void OnNetworkPacket(int packetID, List<byte> packetData, bool isLogin, bool isInbound)
        {
            DispatchBotEvent(bot => bot.OnNetworkPacket(packetID, packetData, isLogin, isInbound));
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


            if (inventoryHandlingRequested)
            {
                inventoryHandlingRequested = false;
                inventoryHandlingEnabled = true;
                Log.Info(Translations.Get("extra.inventory_enabled"));
            }

            ClearInventories();

            DispatchBotEvent(bot => bot.AfterGameJoined());
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
                Log.Info(Translations.Get("extra.terrainandmovement_enabled"));
            }

            if (terrainAndMovementsEnabled)
            {
                world.Clear();
            }

            entities.Clear();
            ClearInventories();
            DispatchBotEvent(bot => bot.OnRespawn());
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
            this._yaw = yaw;
            this._pitch = pitch;
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
                    throw new ArgumentException(Translations.Get("exception.unknown_direction"), "direction");
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
            UpdateKeepAlive();

            List<string> links = new List<string>();
            string json = null;

            if (isJson)
            {
                json = text;
                text = ChatParser.ParseText(json, links);
            }

            Log.Chat(text);

            if (Settings.DisplayChatLinks)
                foreach (string link in links)
                    Log.Chat(Translations.Get("mcc.link", link), false);

            DispatchBotEvent(bot => bot.GetText(text));
            DispatchBotEvent(bot => bot.GetText(text, json));
        }

        /// <summary>
        /// Received a connection keep-alive from the server
        /// </summary>
        public void OnServerKeepAlive()
        {
            UpdateKeepAlive();
        }

        /// <summary>
        /// When an inventory is opened
        /// </summary>
        /// <param name="inventory">The inventory</param>
        /// <param name="inventoryID">Inventory ID</param>
        public void OnInventoryOpen(int inventoryID, Container inventory)
        {
            inventories[inventoryID] = inventory;

            if (inventoryID != 0)
            {
                Log.Info(Translations.Get("extra.inventory_open", inventoryID, inventory.Title));
                Log.Info(Translations.Get("extra.inventory_interact"));
                DispatchBotEvent(bot => bot.OnInventoryOpen(inventoryID));
            }
        }

        /// <summary>
        /// When an inventory is close
        /// </summary>
        /// <param name="inventoryID">Inventory ID</param>
        public void OnInventoryClose(int inventoryID)
        {
            if (inventories.ContainsKey(inventoryID))
            {
                if (inventoryID == 0)
                    inventories[0].Items.Clear(); // Don't delete player inventory
                else
                    inventories.Remove(inventoryID);
            }

            if (inventoryID != 0)
            {
                Log.Info(Translations.Get("extra.inventory_close", inventoryID));
                DispatchBotEvent(bot => bot.OnInventoryClose(inventoryID));
            }
        }

        /// <summary>
        /// When received window items from server.
        /// </summary>
        /// <param name="inventoryID">Inventory ID</param>
        /// <param name="itemList">Item list, key = slot ID, value = Item information</param>
        public void OnWindowItems(byte inventoryID, Dictionary<int, Inventory.Item> itemList)
        {
            if (inventories.ContainsKey(inventoryID))
            {
                inventories[inventoryID].Items = itemList;
                DispatchBotEvent(bot => bot.OnInventoryUpdate(inventoryID));
            }
        }

        /// <summary>
        /// When a slot is set inside window items
        /// </summary>
        /// <param name="inventoryID">Window ID</param>
        /// <param name="slotID">Slot ID</param>
        /// <param name="item">Item (may be null for empty slot)</param>
        public void OnSetSlot(byte inventoryID, short slotID, Item item)
        {
            // Handle inventoryID -2 - Add item to player inventory without animation
            if (inventoryID == 254)
                inventoryID = 0;
            // Handle cursor item
            if (inventoryID == 255 && slotID == -1)
            {
                inventoryID = 0; // Prevent key not found for some bots relied to this event
                if (inventories.ContainsKey(0))
                {
                    if (item != null)
                        inventories[0].Items[-1] = item;
                    else
                        inventories[0].Items.Remove(-1);
                }
            }
            else
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
            DispatchBotEvent(bot => bot.OnInventoryUpdate(inventoryID));
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

            DispatchBotEvent(bot => bot.OnPlayerJoin(uuid, name));
        }

        /// <summary>
        /// Triggered when a player has left the game
        /// </summary>
        /// <param name="uuid">UUID of the player</param>
        public void OnPlayerLeave(Guid uuid)
        {
            string username = null;

            lock (onlinePlayers)
            {
                if (onlinePlayers.ContainsKey(uuid))
                {
                    username = onlinePlayers[uuid];
                    onlinePlayers.Remove(uuid);
                }
            }

            DispatchBotEvent(bot => bot.OnPlayerLeave(uuid, username));
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
                Entity entity = entities[entityid];
                if (entity.Equipment.ContainsKey(slot))
                    entity.Equipment.Remove(slot);
                if (item != null)
                    entity.Equipment[slot] = item;
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
        /// Called when the status of an entity have been changed
        /// </summary>
        /// <param name="entityID">Entity ID</param>
        /// <param name="status">Status ID</param>
        public void OnEntityStatus(int entityID, byte status)
        {
            if (entityID == playerEntityID)
            {
                DispatchBotEvent(bot => bot.OnPlayerStatus(status));
            }
        }

        /// <summary>
        /// Called when server sent a Time Update packet.
        /// </summary>
        /// <param name="WorldAge"></param>
        /// <param name="TimeOfDay"></param>
        public void OnTimeUpdate(long WorldAge, long TimeOfDay)
        {
            // TimeUpdate sent every server tick hence used as timeout detect
            UpdateKeepAlive();
            // calculate server tps
            if (lastAge != 0)
            {
                DateTime currentTime = DateTime.Now;
                long tickDiff = WorldAge - lastAge;
                Double tps = tickDiff / (currentTime - lastTime).TotalSeconds;
                lastAge = WorldAge;
                lastTime = currentTime;
                if (tps <= 20 && tps > 0)
                {
                    // calculate average tps
                    if (tpsSamples.Count >= maxSamples)
                    {
                        // full
                        sampleSum -= tpsSamples[0];
                        tpsSamples.RemoveAt(0);
                    }
                    tpsSamples.Add(tps);
                    sampleSum += tps;
                    averageTPS = sampleSum / tpsSamples.Count;
                    serverTPS = tps;
                    DispatchBotEvent(bot => bot.OnServerTpsUpdate(tps));
                }
            }
            else
            {
                lastAge = WorldAge;
                lastTime = DateTime.Now;
            }
            DispatchBotEvent(bot => bot.OnTimeUpdate(WorldAge, TimeOfDay));
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
                    Log.Info(Translations.Get("mcc.player_dead_respawn"));
                    respawnTicks = 10;
                }
                else
                {
                    Log.Info(Translations.Get("mcc.player_dead"));
                }
                DispatchBotEvent(bot => bot.OnDeath());
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
                playersLatency[playerName] = latency;
                foreach (KeyValuePair<int, Entity> ent in entities)
                {
                    if (ent.Value.UUID == uuid && ent.Value.Name == playerName)
                    {
                        ent.Value.Latency = latency;
                        DispatchBotEvent(bot => bot.OnLatencyUpdate(ent.Value, playerName, uuid, latency));
                        break;
                    }
                }
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

        /// <summary>
        /// Called when the health of an entity changed
        /// </summary>
        /// <param name="entityID">Entity ID</param>
        /// <param name="health">The health of the entity</param>
        public void OnEntityHealth(int entityID, float health)
        {
            if (entities.ContainsKey(entityID))
            {
                entities[entityID].Health = health;
                DispatchBotEvent(bot => bot.OnEntityHealth(entities[entityID], health));
            }
        }

        /// <summary>
        /// Called when the metadata of an entity changed
        /// </summary>
        /// <param name="entityID">Entity ID</param>
        /// <param name="metadata">The metadata of the entity</param>
        public void OnEntityMetadata(int entityID, Dictionary<int, object> metadata)
        {
            if (entities.ContainsKey(entityID))
            {
                Entity entity = entities[entityID];
                entity.Metadata = metadata;
                if (entity.Type.ContainsItem() && metadata.ContainsKey(7) && metadata[7] != null && metadata[7].GetType() == typeof(Item))
                {
                    Item item = (Item)metadata[7];
                    if (item == null)
                        entity.Item = new Item(ItemType.Air, 0, null);
                    else entity.Item = item;
                }
                if (metadata.ContainsKey(6) && metadata[6] != null && metadata[6].GetType() == typeof(Int32))
                {
                    entity.Pose = (EntityPose)metadata[6];
                }
                if (metadata.ContainsKey(2) && metadata[2] != null && metadata[2].GetType() == typeof(string))
                {
                    entity.CustomNameJson = metadata[2].ToString();
                    entity.CustomName = ChatParser.ParseText(metadata[2].ToString());
                }
                if (metadata.ContainsKey(3) && metadata[3] != null && metadata[3].GetType() == typeof(bool))
                {
                    entity.IsCustomNameVisible = bool.Parse(metadata[3].ToString());
                }
                DispatchBotEvent(bot => bot.OnEntityMetadata(entity, metadata));
            }
        }

        /// <summary>
        /// Called when tradeList is recieved after interacting with villager
        /// </summary>
        /// <param name="windowID">Window ID</param>
        /// <param name="trades">List of trades.</param>
        /// <param name="villagerInfo">Contains Level, Experience, IsRegularVillager and CanRestock .</param>
        public void OnTradeList(int windowID, List<VillagerTrade> trades, VillagerInfo villagerInfo)
        {
            DispatchBotEvent(bot => bot.OnTradeList(windowID, trades, villagerInfo));
        }

        /// <summary>
        /// Will be called every player break block in gamemode 0
        /// </summary>
        /// <param name="entityId">Player ID</param>
        /// <param name="location">Block location</param>
        /// <param name="stage">Destroy stage, maximum 255</param>
        public void OnBlockBreakAnimation(int entityId, Location location, byte stage)
        {
            if (entities.ContainsKey(entityId))
            {
                Entity entity = entities[entityId];
                DispatchBotEvent(bot => bot.OnBlockBreakAnimation(entity, location, stage));
            }
        }

        /// <summary>
        /// Will be called every animations of the hit and place block
        /// </summary>
        /// <param name="entityID">Player ID</param>
        /// <param name="animation">0 = LMB, 1 = RMB (RMB Corrent not work)</param>
        public void OnEntityAnimation(int entityID, byte animation)
        {
            if (entities.ContainsKey(entityID))
            {
                Entity entity = entities[entityID];
                DispatchBotEvent(bot => bot.OnEntityAnimation(entity, animation));
            }
        }
        #endregion
    }
}
