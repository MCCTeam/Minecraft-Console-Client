using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Brigadier.NET;
using Brigadier.NET.Exceptions;
using MinecraftClient.ChatBots;
using MinecraftClient.CommandHandler;
using MinecraftClient.CommandHandler.Patch;
using MinecraftClient.Commands;
using MinecraftClient.Inventory;
using MinecraftClient.Logger;
using MinecraftClient.Mapping;
using MinecraftClient.Protocol;
using MinecraftClient.Protocol.Handlers;
using MinecraftClient.Protocol.Handlers.Forge;
using MinecraftClient.Protocol.Message;
using MinecraftClient.Protocol.ProfileKey;
using MinecraftClient.Protocol.Session;
using MinecraftClient.Proxy;
using MinecraftClient.Scripting;
using static MinecraftClient.Settings;

namespace MinecraftClient
{
    /// <summary>
    /// The main client class, used to connect to a Minecraft server.
    /// </summary>
    public class McClient : IMinecraftComHandler
    {
        public static int ReconnectionAttemptsLeft = 0;

        public static CommandDispatcher<CmdResult> dispatcher = new();
        private readonly Dictionary<Guid, PlayerInfo> onlinePlayers = new();

        private static bool commandsLoaded = false;

        private readonly Queue<string> chatQueue = new();
        private static DateTime nextMessageSendTime = DateTime.MinValue;

        private readonly Queue<Action> threadTasks = new();
        private readonly object threadTasksLock = new();

        private readonly List<ChatBot> bots = new();
        private static readonly List<ChatBot> botsOnHold = new();
        private static readonly Dictionary<int, Container> inventories = new();

        private readonly Dictionary<string, List<ChatBot>> registeredBotPluginChannels = new();
        private readonly List<string> registeredServerPluginChannels = new();

        private bool terrainAndMovementsEnabled;
        private bool terrainAndMovementsRequested = false;
        private bool inventoryHandlingEnabled;
        private bool inventoryHandlingRequested = false;
        private bool entityHandlingEnabled;

        private readonly object locationLock = new();
        private bool locationReceived = false;
        private readonly World world = new();
        private Queue<Location>? steps;
        private Queue<Location>? path;
        private Location location;
        private float? _yaw; // Used for calculation ONLY!!! Doesn't reflect the client yaw
        private float? _pitch; // Used for calculation ONLY!!! Doesn't reflect the client pitch
        private float playerYaw;
        private float playerPitch;
        private double motionY;
        public enum MovementType { Sneak, Walk, Sprint }
        private int sequenceId; // User for player block synchronization (Aka. digging, placing blocks, etc..)
        private bool CanSendMessage = false;

        private readonly string host;
        private readonly int port;
        private readonly int protocolversion;
        private readonly string username;
        private Guid uuid;
        private string uuidStr;
        private readonly string sessionid;
        private readonly PlayerKeyPair? playerKeyPair;
        private DateTime lastKeepAlive;
        private readonly object lastKeepAliveLock = new();
        private int respawnTicks = 0;
        private int gamemode = 0;
        private bool isSupportPreviewsChat;
        private EnchantmentData? lastEnchantment = null;

        private int playerEntityID;

        private object DigLock = new();
        private Tuple<Location, Direction>? LastDigPosition;
        private int RemainingDiggingTime = 0;

        // player health and hunger
        private float playerHealth;
        private int playerFoodSaturation;
        private int playerLevel;
        private int playerTotalExperience;
        private byte CurrentSlot = 0;
        
        // Sneaking
        public bool IsSneaking { get; set; } = false;
        private bool isUnderSlab = false;
        private DateTime nextSneakingUpdate = DateTime.Now;

        // Entity handling
        private readonly Dictionary<int, Entity> entities = new();

        // server TPS
        private long lastAge = 0;
        private DateTime lastTime;
        private double serverTPS = 0;
        private double averageTPS = 20;
        private const int maxSamples = 5;
        private readonly List<double> tpsSamples = new(maxSamples);
        private double sampleSum = 0;

        // ChatBot OnNetworkPacket event
        private bool networkPacketCaptureEnabled = false;

        public int GetServerPort() { return port; }
        public string GetServerHost() { return host; }
        public string GetUsername() { return username; }
        public Guid GetUserUuid() { return uuid; }
        public string GetUserUuidStr() { return uuidStr; }
        public string GetSessionID() { return sessionid; }
        public Location GetCurrentLocation() { return location; }
        public float GetYaw() { return playerYaw; }
        public int GetSequenceId() { return sequenceId; }
        public float GetPitch() { return playerPitch; }
        public World GetWorld() { return world; }
        public double GetServerTPS() { return averageTPS; }
        public bool GetIsSupportPreviewsChat() { return isSupportPreviewsChat; }
        public float GetHealth() { return playerHealth; }
        public int GetSaturation() { return playerFoodSaturation; }
        public int GetLevel() { return playerLevel; }
        public int GetTotalExperience() { return playerTotalExperience; }
        public byte GetCurrentSlot() { return CurrentSlot; }
        public int GetGamemode() { return gamemode; }
        public bool GetNetworkPacketCaptureEnabled() { return networkPacketCaptureEnabled; }
        public int GetProtocolVersion() { return protocolversion; }
        public ILogger GetLogger() { return Log; }
        public int GetPlayerEntityID() { return playerEntityID; }
        public List<ChatBot> GetLoadedChatBots() { return new List<ChatBot>(bots); }

        readonly TcpClient client;
        readonly IMinecraftCom handler;
        CancellationTokenSource? cmdprompt = null;
        Tuple<Thread, CancellationTokenSource>? timeoutdetector = null;

        public ILogger Log;
        
        private static IMinecraftComHandler? instance;
        public static IMinecraftComHandler? Instance => instance;
        /// <summary>
        /// Starts the main chat client, wich will login to the server using the MinecraftCom class.
        /// </summary>
        /// <param name="session">A valid session obtained with MinecraftCom.GetLogin()</param>
        /// <param name="playerKeyPair">Key for message signing</param>
        /// <param name="server_ip">The server IP</param>
        /// <param name="port">The server port to use</param>
        /// <param name="protocolversion">Minecraft protocol version to use</param>
        /// <param name="forgeInfo">ForgeInfo item stating that Forge is enabled</param>
        public McClient(SessionToken session, PlayerKeyPair? playerKeyPair, string server_ip, ushort port, int protocolversion, ForgeInfo? forgeInfo)
        {
            CmdResult.currentHandler = this;
            instance = this;
            
            terrainAndMovementsEnabled = Config.Main.Advanced.TerrainAndMovements;
            inventoryHandlingEnabled = Config.Main.Advanced.InventoryHandling;
            entityHandlingEnabled = Config.Main.Advanced.EntityHandling;

            sessionid = session.ID;
            if (!Guid.TryParse(session.PlayerID, out uuid))
                uuid = Guid.Empty;
            uuidStr = session.PlayerID;
            username = session.PlayerName;
            host = server_ip;
            this.port = port;
            this.protocolversion = protocolversion;
            this.playerKeyPair = playerKeyPair;

            Log = Settings.Config.Logging.LogToFile
                ? new FileLogLogger(Config.AppVar.ExpandVars(Settings.Config.Logging.LogFile), Settings.Config.Logging.PrependTimestamp)
                : new FilteredLogger();
            Log.DebugEnabled = Config.Logging.DebugMessages;
            Log.InfoEnabled = Config.Logging.InfoMessages;
            Log.ChatEnabled = Config.Logging.ChatMessages;
            Log.WarnEnabled = Config.Logging.WarningMessages;
            Log.ErrorEnabled = Config.Logging.ErrorMessages;

            /* Load commands from Commands namespace */
            LoadCommands();

            if (botsOnHold.Count == 0)
                RegisterBots();

            try
            {
                client = ProxyHandler.NewTcpClient(host, port);
                client.ReceiveBufferSize = 1024 * 1024;
                client.ReceiveTimeout = Config.Main.Advanced.TcpTimeout * 1000; // Default: 30 seconds
                handler = Protocol.ProtocolHandler.GetProtocolHandler(client, protocolversion, forgeInfo, this);
                Log.Info(Translations.mcc_version_supported);

                timeoutdetector = new(new Thread(new ParameterizedThreadStart(TimeoutDetector)), new CancellationTokenSource());
                timeoutdetector.Item1.Name = "MCC Connection timeout detector";
                timeoutdetector.Item1.Start(timeoutdetector.Item2.Token);

                try
                {
                    if (handler.Login(this.playerKeyPair, session))
                    {
                        foreach (ChatBot bot in botsOnHold)
                            BotLoad(bot, false);
                        botsOnHold.Clear();

                        Log.Info(string.Format(Translations.mcc_joined, Config.Main.Advanced.InternalCmdChar.ToLogString()));

                        cmdprompt = new CancellationTokenSource();
                        ConsoleInteractive.ConsoleReader.BeginReadThread();
                        ConsoleInteractive.ConsoleReader.MessageReceived += ConsoleReaderOnMessageReceived;
                        ConsoleInteractive.ConsoleReader.OnInputChange += ConsoleIO.AutocompleteHandler;
                    }
                    else
                    {
                        Log.Error(Translations.error_login_failed);
                        goto Retry;
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e.GetType().Name + ": " + e.Message);
                    Log.Error(Translations.error_join);
                    goto Retry;
                }
            }
            catch (SocketException e)
            {
                Log.Error(e.Message);
                Log.Error(Translations.error_connect);
                goto Retry;
            }

            return;

        Retry:
            if (timeoutdetector != null)
            {
                timeoutdetector.Item2.Cancel();
                timeoutdetector = null;
            }
            if (ReconnectionAttemptsLeft > 0)
            {
                Log.Info(string.Format(Translations.mcc_reconnect, ReconnectionAttemptsLeft));
                Thread.Sleep(5000);
                ReconnectionAttemptsLeft--;
                Program.Restart();
            }
            else if (InternalConfig.InteractiveMode)
            {
                ConsoleInteractive.ConsoleReader.StopReadThread();
                ConsoleInteractive.ConsoleReader.MessageReceived -= ConsoleReaderOnMessageReceived;
                ConsoleInteractive.ConsoleReader.OnInputChange -= ConsoleIO.AutocompleteHandler;
                Program.HandleFailure();
            }

            throw new Exception("Initialization failed.");
        }

        /// <summary>
        /// Register bots
        /// </summary>
        private void RegisterBots(bool reload = false)
        {
            if (Config.ChatBot.Alerts.Enabled) { BotLoad(new Alerts()); }
            if (Config.ChatBot.AntiAFK.Enabled) { BotLoad(new AntiAFK()); }
            if (Config.ChatBot.AutoAttack.Enabled) { BotLoad(new AutoAttack()); }
            if (Config.ChatBot.AutoCraft.Enabled) { BotLoad(new AutoCraft()); }
            if (Config.ChatBot.AutoDig.Enabled) { BotLoad(new AutoDig()); }
            if (Config.ChatBot.AutoDrop.Enabled) { BotLoad(new AutoDrop()); }
            if (Config.ChatBot.AutoEat.Enabled) { BotLoad(new AutoEat()); }
            if (Config.ChatBot.AutoFishing.Enabled) { BotLoad(new AutoFishing()); }
            if (Config.ChatBot.AutoRelog.Enabled) { BotLoad(new AutoRelog()); }
            if (Config.ChatBot.AutoRespond.Enabled) { BotLoad(new AutoRespond()); }
            if (Config.ChatBot.ChatLog.Enabled) { BotLoad(new ChatLog()); }
            if (Config.ChatBot.DiscordBridge.Enabled) { BotLoad(new DiscordBridge()); }
            if (Config.ChatBot.Farmer.Enabled) { BotLoad(new Farmer()); }
            if (Config.ChatBot.FollowPlayer.Enabled) { BotLoad(new FollowPlayer()); }
            if (Config.ChatBot.HangmanGame.Enabled) { BotLoad(new HangmanGame()); }
            if (Config.ChatBot.Mailer.Enabled) { BotLoad(new Mailer()); }
            if (Config.ChatBot.Map.Enabled) { BotLoad(new Map()); }
            if (Config.ChatBot.PlayerListLogger.Enabled) { BotLoad(new PlayerListLogger()); }
            if (Config.ChatBot.RemoteControl.Enabled) { BotLoad(new RemoteControl()); }
            if (Config.ChatBot.ReplayCapture.Enabled && reload) { BotLoad(new ReplayCapture()); }
            if (Config.ChatBot.ScriptScheduler.Enabled) { BotLoad(new ScriptScheduler()); }
            if (Config.ChatBot.TelegramBridge.Enabled) { BotLoad(new TelegramBridge()); }
            if (Config.ChatBot.ItemsCollector.Enabled) { BotLoad(new ItemsCollector()); }
            if (Config.ChatBot.WebSocketBot.Enabled) { BotLoad(new WebSocketBot()); }
            //Add your ChatBot here by uncommenting and adapting
            //BotLoad(new ChatBots.YourBot());
        }

        /// <summary>
        /// Retrieve messages from the queue and send.
        /// Note: requires external locking.
        /// </summary>
        private void TrySendMessageToServer()
        {
            if (!CanSendMessage)
                return;

            while (chatQueue.Count > 0 && nextMessageSendTime < DateTime.Now)
            {
                string text = chatQueue.Dequeue();
                handler.SendChatMessage(text, playerKeyPair);
                nextMessageSendTime = DateTime.Now + TimeSpan.FromSeconds(Config.Main.Advanced.MessageCooldown);
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
                    if (e is not ThreadAbortException)
                        Log.Warn("Update: Got error from " + bot.ToString() + ": " + e.ToString());
                    else
                        throw; //ThreadAbortException should not be caught
                }
            }

            if (nextSneakingUpdate < DateTime.Now)
            {
                if (world.GetBlock(new Location(location.X, location.Y + 1, location.Z)).IsTopSlab(protocolversion) && !IsSneaking)
                {
                    isUnderSlab = true;
                    SendEntityAction(EntityActionType.StartSneaking);
                }
                else
                {
                    if (isUnderSlab && !IsSneaking)
                    {
                        isUnderSlab = false;
                        SendEntityAction(EntityActionType.StopSneaking);
                    }
                }

                nextSneakingUpdate = DateTime.Now.AddMilliseconds(300);
            }

            lock (chatQueue)
            {
                TrySendMessageToServer();
            }

            if (terrainAndMovementsEnabled && locationReceived)
            {
                lock (locationLock)
                {
                    for (int i = 0; i < Config.Main.Advanced.MovementSpeed; i++) //Needs to run at 20 tps; MCC runs at 10 tps
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

                                if (Config.Main.Advanced.MoveHeadWhileWalking) // Disable head movements to avoid anti-cheat triggers
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

            if (Config.Main.Advanced.AutoRespawn && respawnTicks > 0)
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

            lock (DigLock)
            {
                if (RemainingDiggingTime > 0)
                {
                    if (--RemainingDiggingTime == 0 && LastDigPosition != null)
                    {
                        handler.SendPlayerDigging(2, LastDigPosition.Item1, LastDigPosition.Item2, sequenceId++);
                        Log.Info(string.Format(Translations.cmd_dig_end, LastDigPosition.Item1));
                    }
                    else
                    {
                        DoAnimation((int)Hand.MainHand);
                    }
                }
            }
        }

        #region Connection Lost and Disconnect from Server

        /// <summary>
        /// Periodically checks for server keepalives and consider that connection has been lost if the last received keepalive is too old.
        /// </summary>
        private void TimeoutDetector(object? o)
        {
            UpdateKeepAlive();
            do
            {
                Thread.Sleep(TimeSpan.FromSeconds(15));

                if (((CancellationToken)o!).IsCancellationRequested)
                    return;

                lock (lastKeepAliveLock)
                {
                    if (lastKeepAlive.AddSeconds(Config.Main.Advanced.TcpTimeout) < DateTime.Now)
                    {
                        if (((CancellationToken)o!).IsCancellationRequested)
                            return;

                        OnConnectionLost(ChatBot.DisconnectReason.ConnectionLost, Translations.error_timeout);
                        return;
                    }
                }
            }
            while (!((CancellationToken)o!).IsCancellationRequested);
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
            {
                cmdprompt.Cancel();
                cmdprompt = null;
            }

            if (timeoutdetector != null)
            {
                timeoutdetector.Item2.Cancel();
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
            ConsoleIO.CancelAutocomplete();

            handler.Dispose();

            world.Clear();

            if (timeoutdetector != null)
            {
                if (timeoutdetector != null && Thread.CurrentThread != timeoutdetector.Item1)
                    timeoutdetector.Item2.Cancel();
                timeoutdetector = null;
            }

            bool will_restart = false;

            switch (reason)
            {
                case ChatBot.DisconnectReason.ConnectionLost:
                    message = Translations.mcc_disconnect_lost;
                    Log.Info(message);
                    break;

                case ChatBot.DisconnectReason.InGameKick:
                    Log.Info(Translations.mcc_disconnect_server);
                    Log.Info(message);
                    break;

                case ChatBot.DisconnectReason.LoginRejected:
                    Log.Info(Translations.mcc_disconnect_login);
                    Log.Info(message);
                    break;

                case ChatBot.DisconnectReason.UserLogout:
                    throw new InvalidOperationException(Translations.exception_user_logout);
            }

            //Process AutoRelog last to make sure other bots can perform their cleanup tasks first (issue #1517)
            List<ChatBot> onDisconnectBotList = bots.Where(bot => bot is not AutoRelog).ToList();
            onDisconnectBotList.AddRange(bots.Where(bot => bot is AutoRelog));

            foreach (ChatBot bot in onDisconnectBotList)
            {
                try
                {
                    will_restart |= bot.OnDisconnect(reason, message);
                }
                catch (Exception e)
                {
                    if (e is not ThreadAbortException)
                    {
                        Log.Warn("OnDisconnect: Got error from " + bot.ToString() + ": " + e.ToString());
                    }
                    else throw; //ThreadAbortException should not be caught
                }
            }

            if (!will_restart)
            {
                ConsoleInteractive.ConsoleReader.StopReadThread();
                ConsoleInteractive.ConsoleReader.MessageReceived -= ConsoleReaderOnMessageReceived;
                ConsoleInteractive.ConsoleReader.OnInputChange -= ConsoleIO.AutocompleteHandler;
                Program.HandleFailure();
            }
        }

        #endregion

        #region Command prompt and internal MCC commands

        private void ConsoleReaderOnMessageReceived(object? sender, string e)
        {

            if (client.Client == null)
                return;

            if (client.Client.Connected)
            {
                new Thread(() =>
                {
                    InvokeOnMainThread(() => HandleCommandPromptText(e));
                }).Start();
            }
            else
                return;
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
                string[] command = text[1..].Split((char)0x00);
                switch (command[0].ToLower())
                {
                    case "autocomplete":
                        int id = handler.AutoComplete(command[1]);
                        while (!ConsoleIO.AutoCompleteDone) { Thread.Sleep(100); }
                        if (command.Length > 1) { ConsoleIO.WriteLine((char)0x00 + "autocomplete" + (char)0x00 + ConsoleIO.AutoCompleteResult); }
                        else ConsoleIO.WriteLine((char)0x00 + "autocomplete" + (char)0x00);
                        break;
                }
            }
            else
            {
                text = text.Trim();

                if (text.Length > 1
                    && Config.Main.Advanced.InternalCmdChar == MainConfigHealper.MainConfig.AdvancedConfig.InternalCmdCharType.none
                    && text[0] == '/')
                {
                    SendText(text);
                }
                else if (text.Length > 2
                    && Config.Main.Advanced.InternalCmdChar != MainConfigHealper.MainConfig.AdvancedConfig.InternalCmdCharType.none
                    && text[0] == Config.Main.Advanced.InternalCmdChar.ToChar()
                    && text[1] == '/')
                {
                    SendText(text[1..]);
                }
                else if (text.Length > 0)
                {
                    if (Config.Main.Advanced.InternalCmdChar == MainConfigHealper.MainConfig.AdvancedConfig.InternalCmdCharType.none
                        || text[0] == Config.Main.Advanced.InternalCmdChar.ToChar())
                    {
                        CmdResult result = new();
                        string command = Config.Main.Advanced.InternalCmdChar.ToChar() == ' ' ? text : text[1..];
                        if (!PerformInternalCommand(Config.AppVar.ExpandVars(command), ref result, Settings.Config.AppVar.GetVariables()) && Config.Main.Advanced.InternalCmdChar.ToChar() == '/')
                        {
                            SendText(text);
                        }
                        else if (result.status != CmdResult.Status.NotRun && (result.status != CmdResult.Status.Done || !string.IsNullOrWhiteSpace(result.result)))
                        {
                            Log.Info(result);
                        }
                    }
                    else
                    {
                        SendText(text);
                    }
                }
            }
        }

        /// <summary>
        /// Perform an internal MCC command (not a server command, use SendText() instead for that!)
        /// </summary>
        /// <param name="command">The command</param>
        /// <param name="response_msg">May contain a confirmation or error message after processing the command, or "" otherwise.</param>
        /// <param name="localVars">Local variables passed along with the command</param>
        /// <returns>TRUE if the command was indeed an internal MCC command</returns>
        public bool PerformInternalCommand(string command, ref CmdResult result, Dictionary<string, object>? localVars = null)
        {
            /* Process the provided command */
            ParseResults<CmdResult> parse;
            try
            {
                parse = dispatcher.Parse(command, result);
            }
            catch (Exception e)
            {
                Log.Debug("Parse fail: " + e.GetFullMessage());
                return false;
            }

            try
            {
                dispatcher.Execute(parse);

                foreach (ChatBot bot in bots.ToArray())
                {
                    try
                    {
                        bot.OnInternalCommand(command, string.Join(" ", Command.GetArgs(command)), result);
                    }
                    catch (Exception e)
                    {
                        if (e is not ThreadAbortException)
                        {
                            Log.Warn(string.Format(Translations.icmd_error, bot.ToString() ?? string.Empty, e.ToString()));
                        }
                        else throw; //ThreadAbortException should not be caught
                    }
                }

                return true;
            }
            catch (CommandSyntaxException e)
            {
                if (parse.Context.Nodes.Count == 0)
                {
                    return false;
                }
                else
                {
                    Log.Info("§e" + e.Message ?? e.StackTrace ?? "Incorrect argument.");
                    Log.Info(dispatcher.GetAllUsageString(parse.Context.Nodes[0].Node.Name, false));
                    return true;
                }
            }
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
                            Command cmd = (Command)Activator.CreateInstance(type)!;
                            cmd.RegisterCommand(dispatcher);
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

        /// <summary>
        /// Reload settings and bots
        /// </summary>
        /// <param name="hard">Marks if bots need to be hard reloaded</param>
        public void ReloadSettings()
        {
            Program.ReloadSettings(true);
            ReloadBots();
        }

        /// <summary>
        /// Reload loaded bots (Only builtin bots)
        /// </summary>
        public void ReloadBots()
        {
            UnloadAllBots();
            RegisterBots(true);

            if (client.Client.Connected)
                bots.ForEach(bot => bot.AfterGameJoined());
        }

        /// <summary>
        /// Unload All Bots
        /// </summary>
        public void UnloadAllBots()
        {
            foreach (ChatBot bot in GetLoadedChatBots())
                BotUnLoad(bot);
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
                TaskWithResult<T> taskWithResult = new(task);
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
        /// Clear all tasks
        /// </summary>
        public void ClearTasks()
        {
            lock (threadTasksLock)
            {
                threadTasks.Clear();
            }
        }

        /// <summary>
        /// Check if running on a different thread and InvokeOnMainThread is required
        /// </summary>
        /// <returns>True if calling thread is not the main thread</returns>
        public bool InvokeRequired
        {
            get
            {
                int callingThreadId = Environment.CurrentManagedThreadId;
                if (handler != null)
                {
                    return handler.GetNetMainThreadId() != callingThreadId;
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
            if (handler != null)
                DispatchBotEvent(bot => bot.AfterGameJoined(), new ChatBot[] { b });
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

            b.OnUnload();

            bots.RemoveAll(item => ReferenceEquals(item, b));

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
        /// <returns>Ladt Enchantments</returns>
        public EnchantmentData? GetLastEnchantments()
        {
            return lastEnchantment;
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
            Dictionary<string, int> playersLatency = new();
            foreach (var player in onlinePlayers)
                playersLatency.Add(player.Value.Name, player.Value.Ping);
            return playersLatency;
        }

        /// <summary>
        /// Get client player's inventory items
        /// </summary>
        /// <param name="inventoryID">Window ID of the requested inventory</param>
        /// <returns> Item Dictionary indexed by Slot ID (Check wiki.vg for slot ID)</returns>
        public Container? GetInventory(int inventoryID)
        {
            if (InvokeRequired)
                return InvokeOnMainThread(() => GetInventory(inventoryID));

            if (inventories.TryGetValue(inventoryID, out Container? inv))
                return inv;
            else
                return null;
        }

        /// <summary>
        /// Get client player's inventory items
        /// </summary>
        /// <returns> Item Dictionary indexed by Slot ID (Check wiki.vg for slot ID)</returns>
        public Container GetPlayerInventory()
        {
            return GetInventory(0)!;
        }

        /// <summary>
        /// Get a set of online player names
        /// </summary>
        /// <returns>Online player names</returns>
        public string[] GetOnlinePlayers()
        {
            lock (onlinePlayers)
            {
                string[] playerNames = new string[onlinePlayers.Count];
                int idx = 0;
                foreach (var player in onlinePlayers)
                    playerNames[idx++] = player.Value.Name;
                return playerNames;
            }
        }

        /// <summary>
        /// Get a dictionary of online player names and their corresponding UUID
        /// </summary>
        /// <returns>Dictionay of online players, key is UUID, value is Player name</returns>
        public Dictionary<string, string> GetOnlinePlayersWithUUID()
        {
            Dictionary<string, string> uuid2Player = new();
            lock (onlinePlayers)
            {
                foreach (Guid key in onlinePlayers.Keys)
                {
                    uuid2Player.Add(key.ToString(), onlinePlayers[key].Name);
                }
            }
            return uuid2Player;
        }

        /// <summary>
        /// Get player info from uuid
        /// </summary>
        /// <param name="uuid">Player's UUID</param>
        /// <returns>Player info</returns>
        public PlayerInfo? GetPlayerInfo(Guid uuid)
        {
            if (onlinePlayers.TryGetValue(uuid, out PlayerInfo? player))
                return player;
            else
                return null;
        }

        public PlayerKeyPair? GetPlayerKeyPair()
        {
            return playerKeyPair;
        }

        #endregion

        #region Action methods: Perform an action on the Server

        /// <summary>
        /// Move to the specified location
        /// </summary>
        /// <param name="goal">Location to reach</param>
        /// <param name="allowUnsafe">Allow possible but unsafe locations thay may hurt the player: lava, cactus...</param>
        /// <param name="allowDirectTeleport">Allow non-vanilla direct teleport instead of computing path, but may cause invalid moves and/or trigger anti-cheat plugins</param>
        /// <param name="maxOffset">If no valid path can be found, also allow locations within specified distance of destination</param>
        /// <param name="minOffset">Do not get closer of destination than specified distance</param>
        /// <param name="timeout">How long to wait until the path is evaluated (default: 5 seconds)</param>
        /// <remarks>When location is unreachable, computation will reach timeout, then optionally fallback to a close location within maxOffset</remarks>
        /// <returns>True if a path has been found</returns>
        public bool MoveTo(Location goal, bool allowUnsafe = false, bool allowDirectTeleport = false, int maxOffset = 0, int minOffset = 0, TimeSpan? timeout = null)
        {
            lock (locationLock)
            {
                if (allowDirectTeleport)
                {
                    // 1-step path to the desired location without checking anything
                    UpdateLocation(goal, goal); // Update yaw and pitch to look at next step
                    handler.SendLocationUpdate(goal, Movement.IsOnGround(world, goal), _yaw, _pitch);
                    return true;
                }
                else
                {
                    // Calculate path through pathfinding. Path contains a list of 1-block movement that will be divided into steps
                    path = Movement.CalculatePath(world, location, goal, allowUnsafe, maxOffset, minOffset, timeout ?? TimeSpan.FromSeconds(5));
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
            if (String.IsNullOrEmpty(text))
                return;

            int maxLength = handler.GetMaxChatMessageLength();

            lock (chatQueue)
            {
                if (text.Length > maxLength) //Message is too long?
                {
                    if (text[0] == '/')
                    {
                        //Send the first 100/256 chars of the command
                        text = text[..maxLength];
                        chatQueue.Enqueue(text);
                    }
                    else
                    {
                        //Split the message into several messages
                        while (text.Length > maxLength)
                        {
                            chatQueue.Enqueue(text[..maxLength]);
                            text = text[maxLength..];
                        }
                        chatQueue.Enqueue(text);
                    }
                }
                else
                    chatQueue.Enqueue(text);

                TrySendMessageToServer();
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
                List<ChatBot> bots = new()
                {
                    bot
                };
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
            return InvokeOnMainThread(() => handler.SendUseItem(0, sequenceId++));
        }

        /// <summary>
        /// Use the item currently in the player's left hand
        /// </summary>
        /// <returns>TRUE if the item was successfully used</returns>
        public bool UseItemOnLeftHand()
        {
            return InvokeOnMainThread(() => handler.SendUseItem(1, sequenceId++));
        }

        /// <summary>
        /// Try to merge a slot
        /// </summary>
        /// <param name="inventory">The container where the item is located</param>
        /// <param name="item">Items to be processed</param>
        /// <param name="slotId">The ID of the slot of the item to be processed</param>
        /// <param name="curItem">The slot that was put down</param>
        /// <param name="curId">The ID of the slot being put down</param>
        /// <param name="changedSlots">Record changes</param>
        /// <returns>Whether to fully merge</returns>
        private static bool TryMergeSlot(Container inventory, Item item, int slotId, Item curItem, int curId, List<Tuple<short, Item?>> changedSlots)
        {
            int spaceLeft = curItem.Type.StackCount() - curItem.Count;
            if (curItem.Type == item!.Type && spaceLeft > 0)
            {
                // Put item on that stack
                if (item.Count <= spaceLeft)
                {
                    // Can fit into the stack
                    item.Count = 0;
                    curItem.Count += item.Count;

                    changedSlots.Add(new Tuple<short, Item?>((short)curId, curItem));
                    changedSlots.Add(new Tuple<short, Item?>((short)slotId, null));

                    inventory.Items.Remove(slotId);
                    return true;
                }
                else
                {
                    item.Count -= spaceLeft;
                    curItem.Count += spaceLeft;

                    changedSlots.Add(new Tuple<short, Item?>((short)curId, curItem));
                }
            }
            return false;
        }

        /// <summary>
        /// Store items in a new slot
        /// </summary>
        /// <param name="inventory">The container where the item is located</param>
        /// <param name="item">Items to be processed</param>
        /// <param name="slotId">The ID of the slot of the item to be processed</param>
        /// <param name="newSlotId">ID of the new slot</param>
        /// <param name="changedSlots">Record changes</param>
        private static void StoreInNewSlot(Container inventory, Item item, int slotId, int newSlotId, List<Tuple<short, Item?>> changedSlots)
        {
            Item newItem = new(item.Type, item.Count, item.NBT);
            inventory.Items[newSlotId] = newItem;
            inventory.Items.Remove(slotId);

            changedSlots.Add(new Tuple<short, Item?>((short)newSlotId, newItem));
            changedSlots.Add(new Tuple<short, Item?>((short)slotId, null));
        }

        /// <summary>
        /// Click a slot in the specified window
        /// </summary>
        /// <returns>TRUE if the slot was successfully clicked</returns>
        public bool DoWindowAction(int windowId, int slotId, WindowActionType action)
        {
            if (InvokeRequired)
                return InvokeOnMainThread(() => DoWindowAction(windowId, slotId, action));

            Item? item = null;
            if (inventories.ContainsKey(windowId) && inventories[windowId].Items.ContainsKey(slotId))
                item = inventories[windowId].Items[slotId];

            List<Tuple<short, Item?>> changedSlots = new(); // List<Slot ID, Changed Items>

            // Update our inventory base on action type
            Container inventory = GetInventory(windowId)!;
            Container playerInventory = GetInventory(0)!;
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
                                    (inventory.Items[slotId], playerInventory.Items[-1]) = (playerInventory.Items[-1], inventory.Items[slotId]);
                                }
                            }
                            else
                            {
                                // Put cursor item to target
                                inventory.Items[slotId] = playerInventory.Items[-1];
                                playerInventory.Items.Remove(-1);
                            }

                            if (inventory.Items.ContainsKey(slotId))
                                changedSlots.Add(new Tuple<short, Item?>((short)slotId, inventory.Items[slotId]));
                            else
                                changedSlots.Add(new Tuple<short, Item?>((short)slotId, null));
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

                                changedSlots.Add(new Tuple<short, Item?>((short)slotId, null));
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
                                    (inventory.Items[slotId], playerInventory.Items[-1]) = (playerInventory.Items[-1], inventory.Items[slotId]);
                                }
                            }
                            else
                            {
                                // Drop 1 item count from cursor
                                Item itemTmp = playerInventory.Items[-1];
                                Item itemClone = new(itemTmp.Type, 1, itemTmp.NBT);
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
                        if (inventory.Items.ContainsKey(slotId))
                            changedSlots.Add(new Tuple<short, Item?>((short)slotId, inventory.Items[slotId]));
                        else
                            changedSlots.Add(new Tuple<short, Item?>((short)slotId, null));
                        break;
                    case WindowActionType.ShiftClick:
                    case WindowActionType.ShiftRightClick:
                        if (slotId == 0) break;
                        if (item != null)
                        {
                            /* Target slot have item */

                            bool lower2upper = false, upper2backpack = false, backpack2hotbar = false; // mutual exclusion
                            bool hotbarFirst = true; // Used when upper2backpack = true
                            int upperStartSlot = 9;
                            int upperEndSlot = 35;
                            int lowerStartSlot = 36;

                            switch (inventory.Type)
                            {
                                case ContainerType.PlayerInventory:
                                    if (slotId >= 0 && slotId <= 8 || slotId == 45)
                                    {
                                        if (slotId != 0)
                                            hotbarFirst = false;
                                        upper2backpack = true;
                                        lowerStartSlot = 9;
                                    }
                                    else if (item != null && false /* Check if wearable */)
                                    {
                                        lower2upper = true;
                                        // upperStartSlot = ?;
                                        // upperEndSlot = ?;
                                        // Todo: Distinguish the type of equipment
                                    }
                                    else
                                    {
                                        if (slotId >= 9 && slotId <= 35)
                                        {
                                            backpack2hotbar = true;
                                            lowerStartSlot = 36;
                                        }
                                        else
                                        {
                                            lower2upper = true;
                                            upperStartSlot = 9;
                                            upperEndSlot = 35;
                                        }
                                    }
                                    break;
                                case ContainerType.Generic_9x1:
                                    if (slotId >= 0 && slotId <= 8)
                                    {
                                        upper2backpack = true;
                                        lowerStartSlot = 9;
                                    }
                                    else
                                    {
                                        lower2upper = true;
                                        upperStartSlot = 0;
                                        upperEndSlot = 8;
                                    }
                                    break;
                                case ContainerType.Generic_9x2:
                                    if (slotId >= 0 && slotId <= 17)
                                    {
                                        upper2backpack = true;
                                        lowerStartSlot = 18;
                                    }
                                    else
                                    {
                                        lower2upper = true;
                                        upperStartSlot = 0;
                                        upperEndSlot = 17;
                                    }
                                    break;
                                case ContainerType.Generic_9x3:
                                case ContainerType.ShulkerBox:
                                    if (slotId >= 0 && slotId <= 26)
                                    {
                                        upper2backpack = true;
                                        lowerStartSlot = 27;
                                    }
                                    else
                                    {
                                        lower2upper = true;
                                        upperStartSlot = 0;
                                        upperEndSlot = 26;
                                    }
                                    break;
                                case ContainerType.Generic_9x4:
                                    if (slotId >= 0 && slotId <= 35)
                                    {
                                        upper2backpack = true;
                                        lowerStartSlot = 36;
                                    }
                                    else
                                    {
                                        lower2upper = true;
                                        upperStartSlot = 0;
                                        upperEndSlot = 35;
                                    }
                                    break;
                                case ContainerType.Generic_9x5:
                                    if (slotId >= 0 && slotId <= 44)
                                    {
                                        upper2backpack = true;
                                        lowerStartSlot = 45;
                                    }
                                    else
                                    {
                                        lower2upper = true;
                                        upperStartSlot = 0;
                                        upperEndSlot = 44;
                                    }
                                    break;
                                case ContainerType.Generic_9x6:
                                    if (slotId >= 0 && slotId <= 53)
                                    {
                                        upper2backpack = true;
                                        lowerStartSlot = 54;
                                    }
                                    else
                                    {
                                        lower2upper = true;
                                        upperStartSlot = 0;
                                        upperEndSlot = 53;
                                    }
                                    break;
                                case ContainerType.Generic_3x3:
                                    if (slotId >= 0 && slotId <= 8)
                                    {
                                        upper2backpack = true;
                                        lowerStartSlot = 9;
                                    }
                                    else
                                    {
                                        lower2upper = true;
                                        upperStartSlot = 0;
                                        upperEndSlot = 8;
                                    }
                                    break;
                                case ContainerType.Anvil:
                                    if (slotId >= 0 && slotId <= 2)
                                    {
                                        if (slotId >= 0 && slotId <= 1)
                                            hotbarFirst = false;
                                        upper2backpack = true;
                                        lowerStartSlot = 3;
                                    }
                                    else
                                    {
                                        lower2upper = true;
                                        upperStartSlot = 0;
                                        upperEndSlot = 1;
                                    }
                                    break;
                                case ContainerType.Beacon:
                                    if (slotId == 0)
                                    {
                                        hotbarFirst = false;
                                        upper2backpack = true;
                                        lowerStartSlot = 1;
                                    }
                                    else if (item != null && item.Count == 1 && (item.Type == ItemType.NetheriteIngot ||
                                        item.Type == ItemType.Emerald || item.Type == ItemType.Diamond || item.Type == ItemType.GoldIngot ||
                                        item.Type == ItemType.IronIngot) && !inventory.Items.ContainsKey(0))
                                    {
                                        lower2upper = true;
                                        upperStartSlot = 0;
                                        upperEndSlot = 0;
                                    }
                                    else
                                    {
                                        if (slotId >= 1 && slotId <= 27)
                                        {
                                            backpack2hotbar = true;
                                            lowerStartSlot = 28;
                                        }
                                        else
                                        {
                                            lower2upper = true;
                                            upperStartSlot = 1;
                                            upperEndSlot = 27;
                                        }
                                    }
                                    break;
                                case ContainerType.BlastFurnace:
                                case ContainerType.Furnace:
                                case ContainerType.Smoker:
                                    if (slotId >= 0 && slotId <= 2)
                                    {
                                        if (slotId >= 0 && slotId <= 1)
                                            hotbarFirst = false;
                                        upper2backpack = true;
                                        lowerStartSlot = 3;
                                    }
                                    else if (item != null && false /* Check if it can be burned */)
                                    {
                                        lower2upper = true;
                                        upperStartSlot = 0;
                                        upperEndSlot = 0;
                                    }
                                    else
                                    {
                                        if (slotId >= 3 && slotId <= 29)
                                        {
                                            backpack2hotbar = true;
                                            lowerStartSlot = 30;
                                        }
                                        else
                                        {
                                            lower2upper = true;
                                            upperStartSlot = 3;
                                            upperEndSlot = 29;
                                        }
                                    }
                                    break;
                                case ContainerType.BrewingStand:
                                    if (slotId >= 0 && slotId <= 3)
                                    {
                                        upper2backpack = true;
                                        lowerStartSlot = 5;
                                    }
                                    else if (item != null && item.Type == ItemType.BlazePowder)
                                    {
                                        lower2upper = true;
                                        if (!inventory.Items.ContainsKey(4) || inventory.Items[4].Count < 64)
                                            upperStartSlot = upperEndSlot = 4;
                                        else
                                            upperStartSlot = upperEndSlot = 3;
                                    }
                                    else if (item != null && false /* Check if it can be used for alchemy */)
                                    {
                                        lower2upper = true;
                                        upperStartSlot = upperEndSlot = 3;
                                    }
                                    else if (item != null && (item.Type == ItemType.Potion || item.Type == ItemType.GlassBottle))
                                    {
                                        lower2upper = true;
                                        upperStartSlot = 0;
                                        upperEndSlot = 2;
                                    }
                                    else
                                    {
                                        if (slotId >= 5 && slotId <= 31)
                                        {
                                            backpack2hotbar = true;
                                            lowerStartSlot = 32;
                                        }
                                        else
                                        {
                                            lower2upper = true;
                                            upperStartSlot = 5;
                                            upperEndSlot = 31;
                                        }
                                    }
                                    break;
                                case ContainerType.Crafting:
                                    if (slotId >= 0 && slotId <= 9)
                                    {
                                        if (slotId >= 1 && slotId <= 9)
                                            hotbarFirst = false;
                                        upper2backpack = true;
                                        lowerStartSlot = 10;
                                    }
                                    else
                                    {
                                        lower2upper = true;
                                        upperStartSlot = 1;
                                        upperEndSlot = 9;
                                    }
                                    break;
                                case ContainerType.Enchantment:
                                    if (slotId >= 0 && slotId <= 1)
                                    {
                                        upper2backpack = true;
                                        lowerStartSlot = 5;
                                    }
                                    else if (item != null && item.Type == ItemType.LapisLazuli)
                                    {
                                        lower2upper = true;
                                        upperStartSlot = upperEndSlot = 1;
                                    }
                                    else
                                    {
                                        lower2upper = true;
                                        upperStartSlot = 0;
                                        upperEndSlot = 0;
                                    }
                                    break;
                                case ContainerType.Grindstone:
                                    if (slotId >= 0 && slotId <= 2)
                                    {
                                        if (slotId >= 0 && slotId <= 1)
                                            hotbarFirst = false;
                                        upper2backpack = true;
                                        lowerStartSlot = 3;
                                    }
                                    else if (item != null && false /* Check */)
                                    {
                                        lower2upper = true;
                                        upperStartSlot = 0;
                                        upperEndSlot = 1;
                                    }
                                    else
                                    {
                                        lower2upper = true;
                                        upperStartSlot = 0;
                                        upperEndSlot = 1;
                                    }
                                    break;
                                case ContainerType.Hopper:
                                    if (slotId >= 0 && slotId <= 4)
                                    {
                                        upper2backpack = true;
                                        lowerStartSlot = 5;
                                    }
                                    else
                                    {
                                        lower2upper = true;
                                        upperStartSlot = 0;
                                        upperEndSlot = 4;
                                    }
                                    break;
                                case ContainerType.Lectern:
                                    return false;
                                // break;
                                case ContainerType.Loom:
                                    if (slotId >= 0 && slotId <= 3)
                                    {
                                        if (slotId >= 0 && slotId <= 5)
                                            hotbarFirst = false;
                                        upper2backpack = true;
                                        lowerStartSlot = 4;
                                    }
                                    else if (item != null && false /* Check for availability for staining */)
                                    {
                                        lower2upper = true;
                                        // upperStartSlot = ?;
                                        // upperEndSlot = ?;
                                    }
                                    else
                                    {
                                        if (slotId >= 4 && slotId <= 30)
                                        {
                                            backpack2hotbar = true;
                                            lowerStartSlot = 31;
                                        }
                                        else
                                        {
                                            lower2upper = true;
                                            upperStartSlot = 4;
                                            upperEndSlot = 30;
                                        }
                                    }
                                    break;
                                case ContainerType.Merchant:
                                    if (slotId >= 0 && slotId <= 2)
                                    {
                                        if (slotId >= 0 && slotId <= 1)
                                            hotbarFirst = false;
                                        upper2backpack = true;
                                        lowerStartSlot = 3;
                                    }
                                    else if (item != null && false /* Check if it is available for trading */)
                                    {
                                        lower2upper = true;
                                        upperStartSlot = 0;
                                        upperEndSlot = 1;
                                    }
                                    else
                                    {
                                        if (slotId >= 3 && slotId <= 29)
                                        {
                                            backpack2hotbar = true;
                                            lowerStartSlot = 30;
                                        }
                                        else
                                        {
                                            lower2upper = true;
                                            upperStartSlot = 3;
                                            upperEndSlot = 29;
                                        }
                                    }
                                    break;
                                case ContainerType.Cartography:
                                    if (slotId >= 0 && slotId <= 2)
                                    {
                                        if (slotId >= 0 && slotId <= 1)
                                            hotbarFirst = false;
                                        upper2backpack = true;
                                        lowerStartSlot = 3;
                                    }
                                    else if (item != null && item.Type == ItemType.FilledMap)
                                    {
                                        lower2upper = true;
                                        upperStartSlot = upperEndSlot = 0;
                                    }
                                    else if (item != null && item.Type == ItemType.Map)
                                    {
                                        lower2upper = true;
                                        upperStartSlot = upperEndSlot = 1;
                                    }
                                    else
                                    {
                                        if (slotId >= 3 && slotId <= 29)
                                        {
                                            backpack2hotbar = true;
                                            lowerStartSlot = 30;
                                        }
                                        else
                                        {
                                            lower2upper = true;
                                            upperStartSlot = 3;
                                            upperEndSlot = 29;
                                        }
                                    }
                                    break;
                                case ContainerType.Stonecutter:
                                    if (slotId >= 0 && slotId <= 1)
                                    {
                                        if (slotId == 0)
                                            hotbarFirst = false;
                                        upper2backpack = true;
                                        lowerStartSlot = 2;
                                    }
                                    else if (item != null && false /* Check if it is available for stone cutteing */)
                                    {
                                        lower2upper = true;
                                        upperStartSlot = 0;
                                        upperEndSlot = 0;
                                    }
                                    else
                                    {
                                        if (slotId >= 2 && slotId <= 28)
                                        {
                                            backpack2hotbar = true;
                                            lowerStartSlot = 29;
                                        }
                                        else
                                        {
                                            lower2upper = true;
                                            upperStartSlot = 2;
                                            upperEndSlot = 28;
                                        }
                                    }
                                    break;
                                // TODO: Define more container type here
                                default:
                                    return false;
                            }

                            // Cursor have item or not doesn't matter
                            // If hotbar already have same item, will put on it first until every stack are full
                            // If no more same item , will put on the first empty slot (smaller slot id)
                            // If inventory full, item will not move
                            int itemCount = inventory.Items[slotId].Count;
                            if (lower2upper)
                            {
                                int firstEmptySlot = -1;
                                for (int i = upperStartSlot; i <= upperEndSlot; ++i)
                                {
                                    if (inventory.Items.TryGetValue(i, out Item? curItem))
                                    {
                                        if (TryMergeSlot(inventory, item!, slotId, curItem, i, changedSlots))
                                            break;
                                    }
                                    else if (firstEmptySlot == -1)
                                        firstEmptySlot = i;
                                }
                                if (item!.Count > 0)
                                {
                                    if (firstEmptySlot != -1)
                                        StoreInNewSlot(inventory, item, slotId, firstEmptySlot, changedSlots);
                                    else if (item.Count != itemCount)
                                        changedSlots.Add(new Tuple<short, Item?>((short)slotId, inventory.Items[slotId]));
                                }
                            }
                            else if (upper2backpack)
                            {
                                int hotbarEnd = lowerStartSlot + 4 * 9 - 1;
                                if (hotbarFirst)
                                {
                                    int lastEmptySlot = -1;
                                    for (int i = hotbarEnd; i >= lowerStartSlot; --i)
                                    {
                                        if (inventory.Items.TryGetValue(i, out Item? curItem))
                                        {
                                            if (TryMergeSlot(inventory, item!, slotId, curItem, i, changedSlots))
                                                break;
                                        }
                                        else if (lastEmptySlot == -1)
                                            lastEmptySlot = i;
                                    }
                                    if (item!.Count > 0)
                                    {
                                        if (lastEmptySlot != -1)
                                            StoreInNewSlot(inventory, item, slotId, lastEmptySlot, changedSlots);
                                        else if (item.Count != itemCount)
                                            changedSlots.Add(new Tuple<short, Item?>((short)slotId, inventory.Items[slotId]));
                                    }
                                }
                                else
                                {
                                    int firstEmptySlot = -1;
                                    for (int i = lowerStartSlot; i <= hotbarEnd; ++i)
                                    {
                                        if (inventory.Items.TryGetValue(i, out Item? curItem))
                                        {
                                            if (TryMergeSlot(inventory, item!, slotId, curItem, i, changedSlots))
                                                break;
                                        }
                                        else if (firstEmptySlot == -1)
                                            firstEmptySlot = i;
                                    }
                                    if (item!.Count > 0)
                                    {
                                        if (firstEmptySlot != -1)
                                            StoreInNewSlot(inventory, item, slotId, firstEmptySlot, changedSlots);
                                        else if (item.Count != itemCount)
                                            changedSlots.Add(new Tuple<short, Item?>((short)slotId, inventory.Items[slotId]));
                                    }
                                }
                            }
                            else if (backpack2hotbar)
                            {
                                int hotbarEnd = lowerStartSlot + 1 * 9 - 1;

                                int firstEmptySlot = -1;
                                for (int i = lowerStartSlot; i <= hotbarEnd; ++i)
                                {
                                    if (inventory.Items.TryGetValue(i, out Item? curItem))
                                    {
                                        if (TryMergeSlot(inventory, item!, slotId, curItem, i, changedSlots))
                                            break;
                                    }
                                    else if (firstEmptySlot == -1)
                                        firstEmptySlot = i;
                                }
                                if (item!.Count > 0)
                                {
                                    if (firstEmptySlot != -1)
                                        StoreInNewSlot(inventory, item, slotId, firstEmptySlot, changedSlots);
                                    else if (item.Count != itemCount)
                                        changedSlots.Add(new Tuple<short, Item?>((short)slotId, inventory.Items[slotId]));
                                }
                            }
                        }
                        break;
                    case WindowActionType.DropItem:
                        if (inventory.Items.ContainsKey(slotId))
                        {
                            inventory.Items[slotId].Count--;
                            changedSlots.Add(new Tuple<short, Item?>((short)slotId, inventory.Items[slotId]));
                        }

                        if (inventory.Items[slotId].Count <= 0)
                        {
                            inventory.Items.Remove(slotId);
                            changedSlots.Add(new Tuple<short, Item?>((short)slotId, null));
                        }

                        break;
                    case WindowActionType.DropItemStack:
                        inventory.Items.Remove(slotId);
                        changedSlots.Add(new Tuple<short, Item?>((short)slotId, null));
                        break;
                }
            }

            return handler.SendWindowAction(windowId, slotId, action, item, changedSlots, inventories[windowId].StateID);
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
        public bool DoCreativeGive(int slot, ItemType itemType, int count, Dictionary<string, object>? nbt = null)
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
                bool result = handler.SendCloseWindow(windowId);
                DispatchBotEvent(bot => bot.OnInventoryClose(windowId));
                return result;
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
        /// <param name="type">Type of interaction (interact, attack...)</param>
        /// <param name="hand">Hand.MainHand or Hand.OffHand</param>
        /// <returns>TRUE if interaction succeeded</returns>
        public bool InteractEntity(int entityID, InteractType type, Hand hand = Hand.MainHand)
        {
            if (InvokeRequired)
                return InvokeOnMainThread(() => InteractEntity(entityID, type, hand));

            if (entities.ContainsKey(entityID))
            {
                switch (type)
                {
                    case InteractType.Interact:
                        return handler.SendInteractEntity(entityID, (int)type, (int)hand);
                    
                    case InteractType.InteractAt:
                        return handler.SendInteractEntity(
                            EntityID: entityID, 
                            type: (int)type, 
                            X: (float)entities[entityID].Location.X, 
                            Y: (float)entities[entityID].Location.Y, 
                            Z: (float)entities[entityID].Location.Z, 
                            hand: (int)hand);
                    
                    default:
                        return handler.SendInteractEntity(entityID, (int)type);
                }
            }
            
            return false;
        }

        /// <summary>
        /// Place the block at hand in the Minecraft world
        /// </summary>
        /// <param name="location">Location to place block to</param>
        /// <param name="blockFace">Block face (e.g. Direction.Down when clicking on the block below to place this block)</param>
        /// <returns>TRUE if successfully placed</returns>
        public bool PlaceBlock(Location location, Direction blockFace, Hand hand = Hand.MainHand)
        {
            return InvokeOnMainThread(() => handler.SendPlayerBlockPlacement((int)hand, location, blockFace, sequenceId++));
        }

        /// <summary>
        /// Attempt to dig a block at the specified location
        /// </summary>
        /// <param name="location">Location of block to dig</param>
        /// <param name="swingArms">Also perform the "arm swing" animation</param>
        /// <param name="lookAtBlock">Also look at the block before digging</param>
        public bool DigBlock(Location location, bool swingArms = true, bool lookAtBlock = true, double duration = 0)
        {
            if (!GetTerrainEnabled())
                return false;

            if (InvokeRequired)
                return InvokeOnMainThread(() => DigBlock(location, swingArms, lookAtBlock, duration));

            // TODO select best face from current player location
            Direction blockFace = Direction.Down;

            lock (DigLock)
            {
                if (RemainingDiggingTime > 0 && LastDigPosition != null)
                {
                    handler.SendPlayerDigging(1, LastDigPosition.Item1, LastDigPosition.Item2, sequenceId++);
                    Log.Info(string.Format(Translations.cmd_dig_cancel, LastDigPosition.Item1));
                }

                // Look at block before attempting to break it
                if (lookAtBlock)
                    UpdateLocation(GetCurrentLocation(), location);

                // Send dig start and dig end, will need to wait for server response to know dig result
                // See https://wiki.vg/How_to_Write_a_Client#Digging for more details
                bool result = handler.SendPlayerDigging(0, location, blockFace, sequenceId++)
                    && (!swingArms || DoAnimation((int)Hand.MainHand));

                if (duration <= 0)
                    result &= handler.SendPlayerDigging(2, location, blockFace, sequenceId++);
                else
                {
                    LastDigPosition = new(location, blockFace);
                    RemainingDiggingTime = Settings.DoubleToTick(duration);
                }

                return result;
            }
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

        /// <summary>
        /// Teleport to player in spectator mode
        /// </summary>
        /// <param name="entity">Player to teleport to</param>
        /// Teleporting to other entityies is NOT implemented yet
        public bool Spectate(Entity entity)
        {
            if (entity.Type == EntityType.Player)
            {
                return SpectateByUUID(entity.UUID);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Teleport to player/entity in spectator mode
        /// </summary>
        /// <param name="UUID">UUID of player/entity to teleport to</param>
        public bool SpectateByUUID(Guid UUID)
        {
            if (GetGamemode() == 3)
            {
                if (InvokeRequired)
                    return InvokeOnMainThread(() => SpectateByUUID(UUID));
                return handler.SendSpectate(UUID);
            }
            else
            {
                return false;
            }
        }
        
        /// <summary>
        /// Send the server a command to type in the item name in the Anvil inventory when it's open.
        /// </summary>
        /// <param name="itemName">The new item name</param>
        public bool SendRenameItem(string itemName)
        {
            if (inventories.Count == 0)
                return false;

            if (inventories.Values.ToList().Last().Type != ContainerType.Anvil)
                return false;
            
            return handler.SendRenameItem(itemName);
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
        private void DispatchBotEvent(Action<ChatBot> action, IEnumerable<ChatBot>? botList = null)
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
                    if (e is not ThreadAbortException)
                    {
                        //Retrieve parent method name to determine which event caused the exception
                        System.Diagnostics.StackFrame frame = new(1);
                        System.Reflection.MethodBase method = frame.GetMethod()!;
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
        public void OnGameJoined(bool isOnlineMode)
        {
            if (protocolversion < Protocol18Handler.MC_1_19_3_Version || playerKeyPair == null || !isOnlineMode)
                SetCanSendMessage(true);
            else
                SetCanSendMessage(false);

            string? bandString = Config.Main.Advanced.BrandInfo.ToBrandString();
            if (!String.IsNullOrWhiteSpace(bandString))
                handler.SendBrandInfo(bandString.Trim());

            if (Config.MCSettings.Enabled)
                handler.SendClientSettings(
                    Config.MCSettings.Locale,
                    Config.MCSettings.RenderDistance,
                    (byte)Config.MCSettings.Difficulty,
                    (byte)Config.MCSettings.ChatMode,
                    Config.MCSettings.ChatColors,
                    Config.MCSettings.Skin.GetByte(),
                    (byte)Config.MCSettings.MainHand);

            if (protocolversion >= Protocol18Handler.MC_1_19_3_Version
                && playerKeyPair != null && isOnlineMode)
                handler.SendPlayerSession(playerKeyPair);

            if (inventoryHandlingRequested)
            {
                inventoryHandlingRequested = false;
                inventoryHandlingEnabled = true;
                Log.Info(Translations.extra_inventory_enabled);
            }

            ClearInventories();

            DispatchBotEvent(bot => bot.AfterGameJoined());

            ConsoleIO.InitCommandList(dispatcher);
        }

        /// <summary>
        /// Called when the player respawns, which happens on login, respawn and world change.
        /// </summary>
        public void OnRespawn()
        {
            ClearTasks();

            if (terrainAndMovementsRequested)
            {
                terrainAndMovementsEnabled = true;
                terrainAndMovementsRequested = false;
                Log.Info(Translations.extra_terrainandmovement_enabled);
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
        /// Check if the client is currently processing a Movement.
        /// </summary>
        /// <returns>true if a movement is currently handled</returns>
        public bool ClientIsMoving()
        {
            return terrainAndMovementsEnabled && locationReceived && ((steps != null && steps.Count > 0) || (path != null && path.Count > 0));
        }

        /// <summary>
        /// Get the current goal
        /// </summary>
        /// <returns>Current goal of movement. Location.Zero if not set.</returns>
        public Location GetCurrentMovementGoal()
        {
            return (ClientIsMoving() || path == null) ? Location.Zero : path.Last();
        }

        /// <summary>
        /// Cancels the current movement
        /// </summary>
        /// <returns>True if there was an active path</returns>
        public bool CancelMovement()
        {
            bool success = ClientIsMoving();
            path = null;
            return success;
        }

        /// <summary>
        /// Change the amount of sent movement packets per time
        /// </summary>
        /// <param name="newSpeed">Set a new walking type</param>
        public void SetMovementSpeed(MovementType newSpeed)
        {
            switch (newSpeed)
            {
                case MovementType.Sneak:
                    // https://minecraft.wiki/w/Sneaking#Effects - Sneaking  1.31m/s
                    Config.Main.Advanced.MovementSpeed = 2;
                    break;
                case MovementType.Walk:
                    // https://minecraft.wiki/w/Walking#Usage - Walking 4.317 m/s
                    Config.Main.Advanced.MovementSpeed = 4;
                    break;
                case MovementType.Sprint:
                    // https://minecraft.wiki/w/Sprinting#Usage - Sprinting 5.612 m/s
                    Config.Main.Advanced.MovementSpeed = 5;
                    break;
            }
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
            _yaw = yaw;
            _pitch = pitch;
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
                    throw new ArgumentException(Translations.exception_unknown_direction, nameof(direction));
            }

            UpdateLocation(location, yaw, pitch);
        }

        /// <summary>
        /// Received chat/system message from the server
        /// </summary>
        /// <param name="message">Message received</param>
        public void OnTextReceived(ChatMessage message)
        {
            UpdateKeepAlive();

            List<string> links = new();
            string messageText;

            // Used for 1.19+ to mark: system message, legal / illegal signature
            string color = string.Empty;

            if (message.isSignedChat)
            {
                if (!Config.Signature.ShowIllegalSignedChat && !message.isSystemChat && !(bool)message.isSignatureLegal!)
                    return;
                messageText = ChatParser.ParseSignedChat(message, links);
                
                if (message.isSystemChat)
                {
                    if (Config.Signature.MarkSystemMessage)
                        color = "§7▌§r";     // Background Gray
                }
                else
                {
                    if ((bool)message.isSignatureLegal!)
                    {
                        if (Config.Signature.ShowModifiedChat && message.unsignedContent != null)
                        {
                            if (Config.Signature.MarkModifiedMsg)
                                color = "§6▌§r"; // Background Yellow
                        }
                        else
                        {
                            if (Config.Signature.MarkLegallySignedMsg)
                                color = "§2▌§r"; // Background Green
                        }
                    }
                    else
                    {
                        if (Config.Signature.MarkIllegallySignedMsg)
                            color = "§4▌§r"; // Background Red
                    }
                }
            }
            else
            {
                if (message.isJson)
                    messageText = ChatParser.ParseText(message.content, links);
                else
                    messageText = message.content;
            }

            Log.Chat(color + messageText);

            if (Config.Main.Advanced.ShowChatLinks)
                foreach (string link in links)
                    Log.Chat(string.Format(Translations.mcc_link, link));

            DispatchBotEvent(bot => bot.GetText(messageText));
            DispatchBotEvent(bot => bot.GetText(messageText, message.content));
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
                Log.Info(string.Format(Translations.extra_inventory_open, inventoryID, inventory.Title));
                Log.Info(Translations.extra_inventory_interact);
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
                Log.Info(string.Format(Translations.extra_inventory_close, inventoryID));
                DispatchBotEvent(bot => bot.OnInventoryClose(inventoryID));
            }
        }

        /// <summary>
        /// When received window properties from server.
        /// Used for Frunaces, Enchanting Table, Beacon, Brewing stand, Stone cutter, Loom and Lectern
        /// More info about: https://wiki.vg/Protocol#Set_Container_Property
        /// </summary>
        /// <param name="inventoryID">Inventory ID</param>
        /// <param name="propertyId">Property ID</param>
        /// <param name="propertyValue">Property Value</param>
        public void OnWindowProperties(byte inventoryID, short propertyId, short propertyValue)
        {
            if (!inventories.ContainsKey(inventoryID))
                return;

            Container inventory = inventories[inventoryID];

            if (inventory.Properties.ContainsKey(propertyId))
                inventory.Properties.Remove(propertyId);

            inventory.Properties.Add(propertyId, propertyValue);

            DispatchBotEvent(bot => bot.OnInventoryProperties(inventoryID, propertyId, propertyValue));

            if (inventory.Type == ContainerType.Enchantment)
            {
                // We got the last property for enchantment
                if (propertyId == 9 && propertyValue != -1)
                {
                    short topEnchantmentLevelRequirement = inventory.Properties[0];
                    short middleEnchantmentLevelRequirement = inventory.Properties[1];
                    short bottomEnchantmentLevelRequirement = inventory.Properties[2];

                    Enchantment topEnchantment = EnchantmentMapping.GetEnchantmentById(
                        GetProtocolVersion(),
                        inventory.Properties[4]);

                    Enchantment middleEnchantment = EnchantmentMapping.GetEnchantmentById(
                        GetProtocolVersion(),
                        inventory.Properties[5]);

                    Enchantment bottomEnchantment = EnchantmentMapping.GetEnchantmentById(
                        GetProtocolVersion(),
                        inventory.Properties[6]);

                    short topEnchantmentLevel = inventory.Properties[7];
                    short middleEnchantmentLevel = inventory.Properties[8];
                    short bottomEnchantmentLevel = inventory.Properties[9];

                    StringBuilder sb = new();

                    sb.AppendLine(Translations.Enchantment_enchantments_available + ":");

                    sb.AppendLine(Translations.Enchantment_tops_slot + ":\t"
                        + EnchantmentMapping.GetEnchantmentName(topEnchantment) + " "
                        + EnchantmentMapping.ConvertLevelToRomanNumbers(topEnchantmentLevel) + " ("
                        + topEnchantmentLevelRequirement + " " + Translations.Enchantment_levels + ")");

                    sb.AppendLine(Translations.Enchantment_middle_slot + ":\t"
                        + EnchantmentMapping.GetEnchantmentName(middleEnchantment) + " "
                        + EnchantmentMapping.ConvertLevelToRomanNumbers(middleEnchantmentLevel) + " ("
                        + middleEnchantmentLevelRequirement + " " + Translations.Enchantment_levels + ")");

                    sb.AppendLine(Translations.Enchantment_bottom_slot + ":\t"
                        + EnchantmentMapping.GetEnchantmentName(bottomEnchantment) + " "
                        + EnchantmentMapping.ConvertLevelToRomanNumbers(bottomEnchantmentLevel) + " ("
                        + bottomEnchantmentLevelRequirement + " " + Translations.Enchantment_levels + ")");

                    Log.Info(sb.ToString());

                    lastEnchantment = new();
                    lastEnchantment.TopEnchantment = topEnchantment;
                    lastEnchantment.MiddleEnchantment = middleEnchantment;
                    lastEnchantment.BottomEnchantment = bottomEnchantment;

                    lastEnchantment.Seed = inventory.Properties[3];

                    lastEnchantment.TopEnchantmentLevel = topEnchantmentLevel;
                    lastEnchantment.MiddleEnchantmentLevel = middleEnchantmentLevel;
                    lastEnchantment.BottomEnchantmentLevel = bottomEnchantmentLevel;

                    lastEnchantment.TopEnchantmentLevelRequirement = topEnchantmentLevelRequirement;
                    lastEnchantment.MiddleEnchantmentLevelRequirement = middleEnchantmentLevelRequirement;
                    lastEnchantment.BottomEnchantmentLevelRequirement = bottomEnchantmentLevelRequirement;


                    DispatchBotEvent(bot => bot.OnEnchantments(
                        // Enchantments
                        topEnchantment,
                        middleEnchantment,
                        bottomEnchantment,

                        // Enchantment levels
                        topEnchantmentLevel,
                        middleEnchantmentLevel,
                        bottomEnchantmentLevel,

                        // Required levels for enchanting
                        topEnchantmentLevelRequirement,
                        middleEnchantmentLevelRequirement,
                        bottomEnchantmentLevelRequirement));

                    DispatchBotEvent(bot => bot.OnEnchantments(lastEnchantment));
                }
            }
        }

        /// <summary>
        /// When received window items from server.
        /// </summary>
        /// <param name="inventoryID">Inventory ID</param>
        /// <param name="itemList">Item list, key = slot ID, value = Item information</param>
        public void OnWindowItems(byte inventoryID, Dictionary<int, Inventory.Item> itemList, int stateId)
        {
            if (inventories.ContainsKey(inventoryID))
            {
                inventories[inventoryID].Items = itemList;
                inventories[inventoryID].StateID = stateId;
                DispatchBotEvent(bot => bot.OnInventoryUpdate(inventoryID));
            }
        }

        /// <summary>
        /// When a slot is set inside window items
        /// </summary>
        /// <param name="inventoryID">Window ID</param>
        /// <param name="slotID">Slot ID</param>
        /// <param name="item">Item (may be null for empty slot)</param>
        public void OnSetSlot(byte inventoryID, short slotID, Item? item, int stateId)
        {
            if (inventories.ContainsKey(inventoryID))
                inventories[inventoryID].StateID = stateId;

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
        /// <param name="player">player info</param>
        public void OnPlayerJoin(PlayerInfo player)
        {
            //Ignore placeholders eg 0000tab# from TabListPlus
            if (Config.Main.Advanced.IgnoreInvalidPlayerName && !ChatBot.IsValidName(player.Name))
                return;

            if (player.Name == username)
            {
                // 1.19+ offline server is possible to return different uuid
                uuid = player.Uuid;
                uuidStr = player.Uuid.ToString().Replace("-", string.Empty);
            }

            lock (onlinePlayers)
            {
                onlinePlayers[player.Uuid] = player;
            }

            DispatchBotEvent(bot => bot.OnPlayerJoin(player.Uuid, player.Name));
        }

        /// <summary>
        /// Triggered when a player has left the game
        /// </summary>
        /// <param name="uuid">UUID of the player</param>
        public void OnPlayerLeave(Guid uuid)
        {
            string? username = null;

            lock (onlinePlayers)
            {
                if (onlinePlayers.ContainsKey(uuid))
                {
                    username = onlinePlayers[uuid].Name;
                    onlinePlayers.Remove(uuid);
                }
            }

            DispatchBotEvent(bot => bot.OnPlayerLeave(uuid, username));
        }

        // <summary>
        /// This method is called when a player has been killed by another entity
        /// </summary>
        /// <param name="playerEntity">Victim's entity</param>
        /// <param name="killerEntity">Killer's entity</param>
        public void OnPlayerKilled(int killerEntityId, string chatMessage)
        {
            if (!entities.ContainsKey(killerEntityId))
                return;

            DispatchBotEvent(bot => bot.OnKilled(entities[killerEntityId], chatMessage));
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
        public void OnEntityEffect(int entityid, Effects effect, int amplifier, int duration, byte flags, bool hasFactorData, Dictionary<string, object>? factorCodec)
        {
            if (entities.ContainsKey(entityid))
                DispatchBotEvent(bot => bot.OnEntityEffect(entities[entityid], effect, amplifier, duration, flags));
        }

        /// <summary>
        /// Called when a player spawns or enters the client's render distance
        /// </summary>
        public void OnSpawnPlayer(int entityID, Guid uuid, Location location, byte yaw, byte pitch)
        {
            Entity playerEntity;
            if (onlinePlayers.TryGetValue(uuid, out PlayerInfo? player))
            {
                playerEntity = new(entityID, EntityType.Player, location, uuid, player.Name, yaw, pitch);
                player.entity = playerEntity;
            }
            else
                playerEntity = new(entityID, EntityType.Player, location, uuid, null, yaw, pitch);
            OnSpawnEntity(playerEntity);
        }

        /// <summary>
        /// Called on Entity Equipment
        /// </summary>
        /// <param name="entityid"> Entity ID</param>
        /// <param name="slot"> Equipment slot. 0: main hand, 1: off hand, 2-5: armor slot (2: boots, 3: leggings, 4: chestplate, 5: helmet)</param>
        /// <param name="item"> Item)</param>
        public void OnEntityEquipment(int entityid, int slot, Item? item)
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
                string playerName = onlinePlayers[uuid].Name;
                if (playerName == username)
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
                if (entities.TryGetValue(a, out Entity? entity))
                {
                    DispatchBotEvent(bot => bot.OnEntityDespawn(entity));
                    entities.Remove(a);
                }
            }
        }

        /// <summary>
        /// Called when an entity's position changed within 8 block of its previous position with rotation.
        /// </summary>
        /// <param name="EntityID"></param>
        /// <param name="Dx"></param>
        /// <param name="Dy"></param>
        /// <param name="Dz"></param>
        /// <param name="yaw"></param>
        /// <param name="pitch"></param>
        /// <param name="onGround"></param>
        public void OnEntityPosition(int EntityID, Double Dx, Double Dy, Double Dz, float yaw, float pitch, bool onGround)
        {
            if (entities.ContainsKey(EntityID))
            {
                Location L = entities[EntityID].Location;
                L.X += Dx;
                L.Y += Dy;
                L.Z += Dz;
                entities[EntityID].Location = L;
                entities[EntityID].Yaw = yaw;
                entities[EntityID].Pitch = pitch;
                DispatchBotEvent(bot => bot.OnEntityMove(entities[EntityID]));
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
        /// Called when an entity's rotation changed.
        /// </summary>
        /// <param name="EntityID"></param>
        /// <param name="yaw"></param>
        /// <param name="pitch"></param>
        /// <param name="onGround"></param>
        public void OnEntityRotation(int EntityID, float yaw, float pitch, bool onGround)
        {
            if (entities.ContainsKey(EntityID))
            {
                entities[EntityID].Yaw = yaw;
                entities[EntityID].Pitch = pitch;
                DispatchBotEvent(bot => bot.OnEntityRotate(entities[EntityID]));
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
                Location location = new(X, Y, Z);
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
                if (Config.Main.Advanced.AutoRespawn)
                {
                    Log.Info(Translations.mcc_player_dead_respawn);
                    respawnTicks = 10;
                }
                else
                {
                    Log.Info(string.Format(Translations.mcc_player_dead, Config.Main.Advanced.InternalCmdChar.ToLogString()));
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
            if (onlinePlayers.ContainsKey(uuid))
            {
                PlayerInfo player = onlinePlayers[uuid];
                player.Ping = latency;
                string playerName = player.Name;
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
        /// Called when an update of the map is sent by the server, take a look at https://wiki.vg/Protocol#Map_Data for more info on the fields
        /// Map format and colors: https://minecraft.wiki/w/Map_item_format
        /// </summary>
        /// <param name="mapid">Map ID of the map being modified</param>
        /// <param name="scale">A scale of the Map, from 0 for a fully zoomed-in map (1 block per pixel) to 4 for a fully zoomed-out map (16 blocks per pixel)</param>
        /// <param name="trackingposition">Specifies whether player and item frame icons are shown </param>
        /// <param name="locked">True if the map has been locked in a cartography table </param>
        /// <param name="icons">A list of MapIcon objects of map icons, send only if trackingPosition is true</param>
        /// <param name="columnsUpdated">Numbs of columns that were updated (map width) (NOTE: If it is 0, the next fields are not used/are set to default values of 0 and null respectively)</param>
        /// <param name="rowsUpdated">Map height</param>
        /// <param name="mapCoulmnX">x offset of the westernmost column</param>
        /// <param name="mapRowZ">z offset of the northernmost row</param>
        /// <param name="colors">a byte array of colors on the map</param>
        public void OnMapData(int mapid, byte scale, bool trackingPosition, bool locked, List<MapIcon> icons, byte columnsUpdated, byte rowsUpdated, byte mapCoulmnX, byte mapCoulmnZ, byte[]? colors)
        {
            DispatchBotEvent(bot => bot.OnMapData(mapid, scale, trackingPosition, locked, icons, columnsUpdated, rowsUpdated, mapCoulmnX, mapCoulmnZ, colors));
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
        public void OnUpdateScore(string entityname, int action, string objectivename, int value)
        {
            DispatchBotEvent(bot => bot.OnUpdateScore(entityname, action, objectivename, value));
        }
        
        /// <summary>
        /// Called when the client received the Tab Header and Footer
        /// </summary>
        /// <param name="header">Header</param>
        /// <param name="footer">Footer</param>
        public void OnTabListHeaderAndFooter(string header, string footer)
        {
            DispatchBotEvent(bot => bot.OnTabListHeaderAndFooter(header, footer));
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
        public void OnEntityMetadata(int entityID, Dictionary<int, object?> metadata)
        {
            if (entities.ContainsKey(entityID))
            {
                Entity entity = entities[entityID];
                entity.Metadata = metadata;
                int itemEntityMetadataFieldIndex = protocolversion < Protocol18Handler.MC_1_17_Version ? 7 : 8;
                
                if (entity.Type.ContainsItem() && metadata.TryGetValue(itemEntityMetadataFieldIndex, out object? itemObj) && itemObj != null && itemObj.GetType() == typeof(Item))
                {
                    Item item = (Item)itemObj;
                    if (item == null)
                        entity.Item = new Item(ItemType.Air, 0, null);
                    else entity.Item = item;
                }
                if (metadata.TryGetValue(6, out object? poseObj) && poseObj != null && poseObj.GetType() == typeof(Int32))
                {
                    entity.Pose = (EntityPose)poseObj;
                }
                if (metadata.TryGetValue(2, out object? nameObj) && nameObj != null && nameObj.GetType() == typeof(string))
                {
                    string name = nameObj.ToString() ?? string.Empty;
                    entity.CustomNameJson = name;
                    entity.CustomName = ChatParser.ParseText(name);
                }
                if (metadata.TryGetValue(3, out object? nameVisableObj) && nameVisableObj != null && nameVisableObj.GetType() == typeof(bool))
                {
                    entity.IsCustomNameVisible = bool.Parse(nameVisableObj.ToString() ?? string.Empty);
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

        /// <summary>
        /// Will be called when a Synchronization sequence is recevied, this sequence need to be sent when breaking or placing blocks
        /// </summary>
        /// <param name="sequenceId">Sequence ID</param>
        public void OnBlockChangeAck(int sequenceId)
        {
            this.sequenceId = sequenceId;
        }

        /// <summary>
        /// This method is called when the protocol handler receives server data
        /// </summary>
        /// <param name="hasMotd">Indicates if the server has a motd message</param>
        /// <param name="motd">Server MOTD message</param>
        /// <param name="hasIcon">Indicates if the server has a an icon</param>
        /// <param name="iconBase64">Server icon in Base 64 format</param>
        /// <param name="previewsChat">Indicates if the server previews chat</param>
        public void OnServerDataRecived(bool hasMotd, string motd, bool hasIcon, string iconBase64, bool previewsChat)
        {
            isSupportPreviewsChat = previewsChat;
        }

        /// <summary>
        /// This method is called when the protocol handler receives "Set Display Chat Preview" packet
        /// </summary>
        /// <param name="previewsChat">Indicates if the server previews chat</param>
        public void OnChatPreviewSettingUpdate(bool previewsChat)
        {
            isSupportPreviewsChat = previewsChat;
        }

        /// <summary>
        /// This method is called when the protocol handler receives "Login Success" packet
        /// </summary>
        /// <param name="uuid">The player's UUID received from the server</param>
        /// <param name="userName">The player's username received from the server</param>
        /// <param name="playerProperty">Tuple<Name, Value, Signature(empty if there is no signature)></param>
        public void OnLoginSuccess(Guid uuid, string userName, Tuple<string, string, string>[]? playerProperty)
        {
            //string UUID = uuid.ToString().Replace("-", String.Empty);
            //Log.Info("now UUID = " + this.uuid);
            //Log.Info("new UUID = " + UUID);
            ////handler.SetUserUUID(UUID);

        }

        /// <summary>
        /// Used for a wide variety of game events, from weather to bed use to gamemode to demo messages.
        /// </summary>
        /// <param name="reason">Event type</param>
        /// <param name="value">Depends on Reason</param>
        public void OnGameEvent(byte reason, float value)
        {
            switch (reason)
            {
                case 7:
                    DispatchBotEvent(bot => bot.OnRainLevelChange(value));
                    break;
                case 8:
                    DispatchBotEvent(bot => bot.OnThunderLevelChange(value));
                    break;
            }
        }

        /// <summary>
        /// Called when a block is changed.
        /// </summary>
        /// <param name="location">The location of the block.</param>
        /// <param name="block">The block</param>
        public void OnBlockChange(Location location, Block block)
        {
            world.SetBlock(location, block);
            DispatchBotEvent(bot => bot.OnBlockChange(location, block));
        }

        /// <summary>
        /// Called when "AutoComplete" completes.
        /// </summary>
        /// <param name="transactionId">The number of this result.</param>
        /// <param name="result">All commands.</param>
        public void OnAutoCompleteDone(int transactionId, string[] result)
        {
            ConsoleIO.OnAutoCompleteDone(transactionId, result);
        }

        public void SetCanSendMessage(bool canSendMessage)
        {
            CanSendMessage = canSendMessage;
            Log.Debug("CanSendMessage = " + canSendMessage);
        }

        /// <summary>
        /// Send a click container button packet to the server.
        /// Used for Enchanting table, Lectern, stone cutter and loom
        /// </summary>
        /// <param name="windowId">Id of the window being clicked</param>
        /// <param name="buttonId">Id of the clicked button</param>
        /// <returns>True if packet was successfully sent</returns>

        public bool ClickContainerButton(int windowId, int buttonId)
        {
            return handler.ClickContainerButton(windowId, buttonId);
        }

        #endregion
    }
}
