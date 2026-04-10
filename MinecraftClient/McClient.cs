using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
using MinecraftClient.Physics;
using MinecraftClient.Protocol;
using MinecraftClient.Protocol.Handlers;
using MinecraftClient.Protocol.Handlers.Forge;
using MinecraftClient.Protocol.Message;
using MinecraftClient.Protocol.ProfileKey;
using MinecraftClient.Protocol.Session;
using MinecraftClient.Proxy;
using MinecraftClient.Scripting;
using Sentry;
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
        private readonly Lock tabListHeaderFooterLock = new();
        private string tabListHeader = string.Empty;
        private string tabListFooter = string.Empty;

        private readonly Queue<string> chatQueue = new();
        private static DateTime nextMessageSendTime = DateTime.MinValue;

        private readonly Queue<Action> threadTasks = new();
        private readonly Lock threadTasksLock = new();
        private readonly Lock recipeBookLock = new();
        private readonly Lock achievementsLock = new();

        private readonly List<ChatBot> bots = new();
        private static readonly List<ChatBot> botsOnHold = new();
        private static readonly Dictionary<int, Container> inventories = new();
        private readonly Dictionary<string, RecipeBookRecipeEntry> unlockedRecipes = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Achievement> achievements = new(StringComparer.Ordinal);
        private string? activeAdvancementTab;

        private readonly Dictionary<string, List<ChatBot>> registeredBotPluginChannels = new();
        private readonly List<string> registeredServerPluginChannels = new();

        private bool terrainAndMovementsEnabled;
        private bool terrainAndMovementsRequested = false;
        private bool inventoryHandlingEnabled;
        private bool inventoryHandlingRequested = false;
        private bool entityHandlingEnabled;

        private readonly Lock locationLock = new();
        private bool locationReceived = false;
        private readonly World world = new();
        private Queue<Location>? path;
        private Location location;
        private float? _yaw; // Used for calculation ONLY!!! Doesn't reflect the client yaw
        private float? _pitch; // Used for calculation ONLY!!! Doesn't reflect the client pitch
        private float playerYaw;
        private float playerPitch;
        private readonly PlayerPhysics playerPhysics = new();
        private readonly MovementInput physicsInput = new();
        private bool physicsInitialized = false;
        private Location? pathTarget; // Current waypoint for physics-driven pathfinding
        public enum MovementType { Sneak, Walk, Sprint }
        private int sequenceId; // User for player block synchronization (Aka. digging, placing blocks, etc..)
        private bool CanSendMessage = false;

        private string host;
        private int port;
        private readonly int protocolversion;
        private readonly string username;
        private Guid uuid;
        private string uuidStr;
        private readonly string sessionid;
        private readonly PlayerKeyPair? playerKeyPair;
        private DateTime lastKeepAlive;
        private readonly Lock lastKeepAliveLock = new();
        private int respawnTicks = 0;
        private int gamemode = 0;
        private bool isSupportPreviewsChat;
        private EnchantmentData? lastEnchantment = null;

        private int playerEntityID;

        private readonly Lock DigLock = new();
        private Tuple<Location, Direction>? LastDigPosition;
        private int RemainingDiggingTime = 0;

        // player health and hunger
        private float playerHealth;
        private int playerFoodSaturation;
        private int playerLevel;
        private int playerTotalExperience;
        private byte CurrentSlot = 0;

        // player effects
        private readonly Dictionary<Effects, EffectData> playerEffects = new();

        // player attributes (e.g., block_break_speed, mining_efficiency, submerged_mining_speed)
        private readonly Dictionary<string, double> playerAttributes = new();

        // scoreboard teams (key = team name)
        private readonly Dictionary<string, PlayerTeam> teams = new(StringComparer.Ordinal);
        
        // Sneaking
        public bool IsSneaking { get; set; } = false;
        private bool isUnderSlab = false;
        private DateTime nextSneakingUpdate = DateTime.Now;

        // Entity handling
        private readonly Dictionary<int, Entity> entities = new();
        private readonly Lock signDataLock = new();
        private readonly Dictionary<(int x, int y, int z), (string material, string typeLabel, string[] frontText, string[] backText, bool isWaxed)> knownSigns = new();

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
        
        // Cookies
        private Dictionary<string, byte[]> Cookies { get; set; } = new();

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

        /// <summary>
        /// Get the player's active effects
        /// </summary>
        /// <returns>Dictionary of active effects</returns>
        public Dictionary<Effects, EffectData> GetPlayerEffects()
        {
            return new Dictionary<Effects, EffectData>(playerEffects);
        }

        /// <summary>
        /// Get a snapshot of all known scoreboard teams.
        /// </summary>
        /// <returns>Dictionary mapping team name to <see cref="PlayerTeam"/></returns>
        public Dictionary<string, PlayerTeam> GetTeams()
        {
            lock (teams)
                return new Dictionary<string, PlayerTeam>(teams, StringComparer.Ordinal);
        }

        /// <summary>
        /// Get the team that contains the given player/entity name, or <c>null</c> if not found.
        /// </summary>
        public PlayerTeam? GetPlayerTeam(string playerName)
        {
            lock (teams)
            {
                foreach (var team in teams.Values)
                    if (team.Members.Contains(playerName))
                        return team;
                return null;
            }
        }

        public int GetLevel() { return playerLevel; }
        public int GetTotalExperience() { return playerTotalExperience; }
        public byte GetCurrentSlot() { return CurrentSlot; }
        public int GetGamemode() { return gamemode; }
        public bool GetNetworkPacketCaptureEnabled() { return networkPacketCaptureEnabled; }
        public int GetProtocolVersion() { return protocolversion; }
        public ILogger GetLogger() { return Log; }
        public int GetPlayerEntityID() { return playerEntityID; }
        public List<ChatBot> GetLoadedChatBots() { return new List<ChatBot>(bots); }
        public void GetCookie(string key, out byte[]? data) => Cookies.TryGetValue(key, out data);
        public void SetCookie(string key, byte[] data) => Cookies[key] = data;
        public void DeleteCookie(string key) => Cookies.Remove(key, out var data);
        public (Location location, string material, string typeLabel, string[] frontText, string[] backText, bool isWaxed)[] GetKnownSigns()
        {
            lock (signDataLock)
            {
                return knownSigns
                    .Select(pair => (
                        location: new Location(pair.Key.x, pair.Key.y, pair.Key.z),
                        material: pair.Value.material,
                        typeLabel: pair.Value.typeLabel,
                        frontText: (string[])pair.Value.frontText.Clone(),
                        backText: (string[])pair.Value.backText.Clone(),
                        isWaxed: pair.Value.isWaxed))
                    .ToArray();
            }
        }

        TcpClient client = null!;
        IMinecraftCom handler = null!;
        SessionToken _sessionToken;
        CancellationTokenSource? cmdprompt = null;
        Tuple<Thread, CancellationTokenSource>? timeoutdetector = null;
        private int transferInProgress = 0;
        private bool consoleReadThreadOwned = false;
        private bool consoleHandlersAttached = false;

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
            _sessionToken = session;

            Log = Settings.Config.Logging.LogToFile
                ? new FileLogLogger(Config.AppVar.ExpandVars(Settings.Config.Logging.LogFile), Settings.Config.Logging.PrependTimestamp)
                : new FilteredLogger();
            Log.DebugEnabled = Config.Logging.DebugMessages;
            Log.InfoEnabled = Config.Logging.InfoMessages;
            Log.ChatEnabled = Config.Logging.ChatMessages;
            Log.WarnEnabled = Config.Logging.WarningMessages;
            Log.ErrorEnabled = Config.Logging.ErrorMessages;

            // SENTRY: Send our client version and server version to Sentry
            SentrySdk.ConfigureScope(scope =>
            {
                scope.SetTag("Protocol Version", protocolversion.ToString());
                scope.SetTag("Minecraft Version", ProtocolHandler.ProtocolVersion2MCVer(protocolversion));
                scope.SetTag("MCC Build", Program.BuildInfo is null ? "Debug" : Program.BuildInfo);
                    
                if (forgeInfo is not null)
                    scope.SetTag("Forge Version", forgeInfo.Version.ToString());

                scope.Contexts["Server Information"] = new
                {
                    ProtocolVersion = protocolversion,
                    MinecraftVersion = ProtocolHandler.ProtocolVersion2MCVer(protocolversion),
                    ForgeInfo = forgeInfo?.Version
                };
                
                scope.Contexts["Client Configuration"] = new 
                {
                    TerrainAndMovementsEnabled = terrainAndMovementsEnabled,
                    InventoryHandlingEnabled = inventoryHandlingEnabled,
                    EntityHandlingEnabled = entityHandlingEnabled
                };
            });
            
            SentrySdk.StartSession();
            
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

                        StartConsoleSession();
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
            if (timeoutdetector is not null)
            {
                timeoutdetector.Item2.Cancel();
                timeoutdetector = null;
            }

            if (!Config.ChatBot.AutoRelog.Enabled)
            {
                if (ReconnectionAttemptsLeft > 0)
                {
                    Log.Info(string.Format(Translations.mcc_reconnect, ReconnectionAttemptsLeft));
                    Thread.Sleep(5000);
                    ReconnectionAttemptsLeft--;
                    Program.Restart();
                }
                else if (InternalConfig.InteractiveMode)
                {
                    StopConsoleSession();
                    Program.HandleFailure();
                }

                throw new Exception("Initialization failed.");
            } 
            else
            {
                // AutoRelog is enabled - invoke its static handler to trigger reconnection.
                // Use the same "Connection has been lost" message that OnConnectionLost uses
                // for ConnectionLost, so it matches the default Kick_Messages.
                if (AutoRelog.OnDisconnectStatic(ChatBot.DisconnectReason.ConnectionLost, Translations.mcc_disconnect_lost))
                    return; // AutoRelog is triggering a restart

                // AutoRelog chose not to reconnect (e.g., message didn't match
                // kick messages and Ignore_Kick_Message is false, or retry limit reached)
                if (InternalConfig.InteractiveMode)
                {
                    StopConsoleSession();
                    Program.HandleFailure();
                }

                throw new Exception("Initialization failed.");
            }
        }
        
        public void Transfer(string newHost, int newPort)
        {
            // Do not block here: a new handler can start processing packets before the
            // previous transfer call fully unwinds, and waiting can deadlock main-thread work.
            if (Interlocked.CompareExchange(ref transferInProgress, 1, 0) != 0)
            {
                Log.Warn($"Ignoring overlapping transfer to {newHost}:{newPort} because another transfer is still in progress.");
                return;
            }

            IMinecraftCom oldHandler = handler;
            TcpClient oldClient = client;
            string resolvedHost = newHost;
            int resolvedPort = newPort;

            try
            {
                ResolveTransferAddress(ref resolvedHost, ref resolvedPort);
                Log.Info($"Initiating a transfer to: {resolvedHost}:{resolvedPort}");
                
                // Unload bots
                UnloadAllBots();
                bots.Clear();

                ResetStateForTransfer();
                
                // Retire the old handler so its updater exits without reporting a stale disconnect.
                oldHandler.Dispose();
                oldClient.Close();

                host = resolvedHost;
                port = resolvedPort;
                UpdateKeepAlive();

                // Establish new connection
                client = ProxyHandler.NewTcpClient(resolvedHost, resolvedPort);
                client.ReceiveBufferSize = 1024 * 1024;
                client.ReceiveTimeout = Config.Main.Advanced.TcpTimeout * 1000;

                // Reinitialize the protocol handler
                handler = Protocol.ProtocolHandler.GetProtocolHandler(client, protocolversion, null, this);
                Log.Info($"Connected to {resolvedHost}:{resolvedPort}");

                // Retry login process
                if (handler.Login(playerKeyPair, _sessionToken, isTransfer: true))
                {
                    foreach (var bot in botsOnHold)
                        BotLoad(bot, false);
                    botsOnHold.Clear();

                    UpdateKeepAlive();
                    Log.Info($"Successfully transferred connection and logged in to {resolvedHost}:{resolvedPort}.");

                    StartConsoleSession();
                }
                else
                {
                    Log.Error("Failed to login to the new host.");
                    throw new Exception("Login failed after transfer.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Transfer to {resolvedHost}:{resolvedPort} failed: {ex.Message}");

                try
                {
                    handler.Dispose();
                }
                catch
                {
                }

                try
                {
                    client.Close();
                }
                catch
                {
                }

                // Handle reconnection attempts
                if (timeoutdetector is not null)
                {
                    timeoutdetector.Item2.Cancel();
                    timeoutdetector = null;
                }

                if (ReconnectionAttemptsLeft > 0)
                {
                    Log.Info($"Reconnecting... Attempts left: {ReconnectionAttemptsLeft}");
                    Thread.Sleep(5000);
                    ReconnectionAttemptsLeft--;
                    Program.Restart();
                }
                else if (InternalConfig.InteractiveMode)
                {
                    StopConsoleSession();
                    Program.HandleFailure();
                }

                throw new Exception("Transfer failed and reconnection attempts exhausted.", ex);
            }
            finally
            {
                Interlocked.Exchange(ref transferInProgress, 0);
            }
        }

        private static void ResolveTransferAddress(ref string host, ref int port)
        {
            if (Config.Main.Advanced.ResolveSrvRecords == MainConfigHelper.MainConfig.AdvancedConfig.ResolveSrvRecordType.no
                || port != 25565
                || IPAddress.TryParse(host, out _))
            {
                return;
            }

            ushort resolvedPort = (ushort)port;
            ProtocolHandler.MinecraftServiceLookup(ref host, ref resolvedPort);
            port = resolvedPort;
        }

        private void StartConsoleSession()
        {
            cmdprompt = new CancellationTokenSource();

            if (!consoleReadThreadOwned)
            {
                ConsoleIO.Backend.BeginReadThread();
                consoleReadThreadOwned = true;
            }

            if (!consoleHandlersAttached)
            {
                ConsoleIO.Backend.MessageReceived += ConsoleReaderOnMessageReceived;
                ConsoleIO.Backend.OnInputChange += ConsoleIO.AutocompleteHandler;
                consoleHandlersAttached = true;
            }
        }

        private void StopConsoleSession()
        {
            if (consoleHandlersAttached)
            {
                ConsoleIO.Backend.MessageReceived -= ConsoleReaderOnMessageReceived;
                ConsoleIO.Backend.OnInputChange -= ConsoleIO.AutocompleteHandler;
                consoleHandlersAttached = false;
            }

            if (consoleReadThreadOwned)
            {
                ConsoleIO.Backend.StopReadThread();
                consoleReadThreadOwned = false;
            }
        }

        private void ResetStateForTransfer()
        {
            ClearTasks();
            ConsoleIO.CancelAutocomplete();
            SetCanSendMessage(false);

            locationReceived = false;
            physicsInitialized = false;
            isUnderSlab = false;
            path = null;
            pathTarget = null;
            _yaw = null;
            _pitch = null;
            LastDigPosition = null;
            RemainingDiggingTime = 0;
            nextSneakingUpdate = DateTime.Now;

            physicsInput.Reset();
            world.Clear();
            entities.Clear();
            ClearKnownSigns();
            ClearInventories();
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
            if (Config.ChatBot.ReplayCapture.Enabled) { BotLoad(new ReplayCapture()); }
            if (Config.ChatBot.ScriptScheduler.Enabled) { BotLoad(new ScriptScheduler()); }
            if (Config.ChatBot.TelegramBridge.Enabled) { BotLoad(new TelegramBridge()); }
            if (Config.ChatBot.ItemsCollector.Enabled) { BotLoad(new ItemsCollector()); }
            if (Config.ChatBot.DiscordRpc.Enabled) { BotLoad(new DiscordRpc()); }
            if (Config.ChatBot.McpServer.Enabled) { BotLoad(new McpServer()); }
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MCC_FILE_INPUT")))
                BotLoad(new FileInputBot());
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
        /// Called 20 times per second by the protocol handler
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
                    if (!physicsInitialized)
                    {
                        BlockShapes.Initialize();
                        playerPhysics.SetPosition(location.X, location.Y, location.Z);
                        playerPhysics.Yaw = playerYaw;
                        playerPhysics.Pitch = playerPitch;
                        playerPhysics.DebugLog = msg => Log.Debug(msg);
                        physicsInitialized = true;
                    }

                    // Navigate pathfinding: set input based on current path
                    UpdatePathfindingInput();

                    // Sync yaw/pitch if explicitly set (by commands/bots)
                    if (_yaw is not null) playerPhysics.Yaw = _yaw.Value;
                    if (_pitch is not null) playerPhysics.Pitch = _pitch.Value;

                    // Update environment flags (water, lava, climbable)
                    playerPhysics.UpdateEnvironment(world);

                    // Apply movement input
                    playerPhysics.ApplyInput(physicsInput);

                    // Run one physics tick
                    playerPhysics.Tick(world);

                    // Sync back to MCC location
                    location = new Location(
                        playerPhysics.Position.X,
                        playerPhysics.Position.Y,
                        playerPhysics.Position.Z);

                    playerYaw = _yaw ?? playerYaw;
                    playerPitch = _pitch ?? playerPitch;

                    // Send position packet
                    handler.SendLocationUpdate(location, playerPhysics.OnGround, playerPhysics.HorizontalCollision, _yaw, _pitch);

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

            // Check for expired effects
            if (playerEffects.Count > 0)
            {
                var expiredEffects = playerEffects
                    .Where(e => e.Value.IsExpired)
                    .Select(e => e.Key)
                    .ToList();

                foreach (var effect in expiredEffects)
                {
                    if (!playerEffects.Remove(effect, out var effectData))
                        continue;

                    ConsoleIO.WriteLine(string.Format(Translations.bot_effect_expired, effectData.GetDisplayName()));

                    if (entities.TryGetValue(playerEntityID, out var playerEntity))
                        playerEntity.ActiveEffects.Remove(effect);
                }
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
                    if (--RemainingDiggingTime == 0 && LastDigPosition is not null)
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
            instance = null;

            DispatchBotEvent(bot => bot.OnDisconnect(ChatBot.DisconnectReason.UserLogout, ""));

            botsOnHold.Clear();
            botsOnHold.AddRange(bots);

            if (handler is not null)
            {
                handler.Disconnect();
                handler.Dispose();
            }

            if (cmdprompt is not null)
            {
                cmdprompt.Cancel();
                cmdprompt = null;
            }

            if (timeoutdetector is not null)
            {
                timeoutdetector.Item2.Cancel();
                timeoutdetector = null;
            }

            if (client is not null)
                client.Close();
        }

        /// <summary>
        /// When connection has been lost, login was denied or played was kicked from the server
        /// </summary>
        public void OnConnectionLost(ChatBot.DisconnectReason reason, string message)
        {
            instance = null;

            ConsoleIO.CancelAutocomplete();

            handler.Dispose();

            world.Clear();
            ClearKnownSigns();

            if (timeoutdetector is not null)
            {
                if (timeoutdetector is not null && Thread.CurrentThread != timeoutdetector.Item1)
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

            SentrySdk.EndSession();
            
            if (!will_restart)
            {
                StopConsoleSession();
                Program.HandleFailure(null, false, reason);
            }
        }

        #endregion

        #region Command prompt and internal MCC commands

        private void ConsoleReaderOnMessageReceived(object? sender, string e)
        {

            if (client.Client is null)
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
        /// Get the console message handler delegate for re-attaching after TUI mode.
        /// </summary>
        public EventHandler<string> GetConsoleMessageHandler()
        {
            return ConsoleReaderOnMessageReceived;
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
                    && Config.Main.Advanced.InternalCmdChar == MainConfigHelper.MainConfig.AdvancedConfig.InternalCmdCharType.none
                    && text[0] == '/')
                {
                    SendText(text);
                }
                else if (text.Length > 2
                    && Config.Main.Advanced.InternalCmdChar != MainConfigHelper.MainConfig.AdvancedConfig.InternalCmdCharType.none
                    && text[0] == Config.Main.Advanced.InternalCmdChar.ToChar()
                    && text[1] == '/')
                {
                    SendText(text[1..]);
                }
                else if (text.Length > 0)
                {
                    if (Config.Main.Advanced.InternalCmdChar == MainConfigHelper.MainConfig.AdvancedConfig.InternalCmdCharType.none
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
                if (handler is not null)
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
                DispatchBotEvent(bot => bot.Initialize(), [b]);
            if (handler is not null)
                DispatchBotEvent(bot => bot.AfterGameJoined(), [b]);
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
            b.UnregisterChatBotCommands();

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
                ClearUnlockedRecipes();
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
        /// Gets the horizontal direction of the takeoff.
        /// </summary>
        /// <returns>Return direction of view</returns>
        public Direction GetHorizontalFacing()
        {
            return DirectionExtensions.FromRotation(GetYaw());
        }

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
            return [(char)167, (char)127]; // Minecraft color code and ASCII code DEL
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
        /// Get all unlocked recipe book recipe identifiers.
        /// </summary>
        /// <returns>Unlocked recipe identifiers sorted alphabetically</returns>
        public RecipeBookRecipeEntry[] GetUnlockedRecipes()
        {
            lock (recipeBookLock)
            {
                return unlockedRecipes.Values.OrderBy(static recipe => recipe.CommandId, StringComparer.Ordinal).ToArray();
            }
        }

        /// <summary>
        /// Get all achievements/advancements known to the client.
        /// </summary>
        /// <returns>Snapshot of all achievements</returns>
        public Achievement[] GetAchievements()
        {
            lock (achievementsLock)
            {
                return [.. achievements.Values];
            }
        }

        /// <summary>
        /// Get only completed achievements/advancements.
        /// </summary>
        /// <returns>Snapshot of completed achievements</returns>
        public Achievement[] GetUnlockedAchievements()
        {
            lock (achievementsLock)
            {
                return achievements.Values.Where(static a => a.IsCompleted).ToArray();
            }
        }

        /// <summary>
        /// Get only incomplete achievements/advancements.
        /// </summary>
        /// <returns>Snapshot of locked achievements</returns>
        public Achievement[] GetLockedAchievements()
        {
            lock (achievementsLock)
            {
                return achievements.Values.Where(static a => !a.IsCompleted).ToArray();
            }
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
        /// Get the currently active inventory if it supports recipe book crafting.
        /// </summary>
        /// <returns>Active recipe book inventory, or null if the active inventory does not support recipe book crafting</returns>
        public Container? GetActiveRecipeBookInventory()
        {
            if (InvokeRequired)
                return InvokeOnMainThread(() => GetActiveRecipeBookInventory());

            if (inventories.Count == 0)
                return null;

            Container activeInventory = inventories.MaxBy(static pair => pair.Key).Value;
            return SupportsRecipeBook(activeInventory.Type) ? activeInventory : null;
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

        internal TabListSnapshot GetTabListSnapshot()
        {
            List<(Guid Uuid, string Name, string DisplayName, int Gamemode, int Ping, int TabListOrder, bool Listed)> players;
            lock (onlinePlayers)
            {
                players = onlinePlayers
                    .Select(static pair => (
                        pair.Key,
                        pair.Value.Name,
                        pair.Value.DisplayName ?? string.Empty,
                        pair.Value.Gamemode,
                        pair.Value.Ping,
                        pair.Value.TabListOrder,
                        pair.Value.Listed))
                    .ToList();
            }

            Dictionary<string, PlayerTeam> teamSnapshot;
            lock (teams)
            {
                teamSnapshot = teams.ToDictionary(
                    static pair => pair.Key,
                    static pair =>
                    {
                        var sourceTeam = pair.Value;
                        var copy = new PlayerTeam
                        {
                            Name = sourceTeam.Name,
                            DisplayName = sourceTeam.DisplayName,
                            AllowFriendlyFire = sourceTeam.AllowFriendlyFire,
                            SeeFriendlyInvisibles = sourceTeam.SeeFriendlyInvisibles,
                            NameTagVisibility = sourceTeam.NameTagVisibility,
                            CollisionRule = sourceTeam.CollisionRule,
                            Color = sourceTeam.Color,
                            Prefix = sourceTeam.Prefix,
                            Suffix = sourceTeam.Suffix
                        };

                        foreach (string member in sourceTeam.Members)
                            copy.Members.Add(member);

                        return copy;
                    },
                    StringComparer.OrdinalIgnoreCase);
            }

            string header;
            string footer;
            lock (tabListHeaderFooterLock)
            {
                header = tabListHeader;
                footer = tabListFooter;
            }

            var entries = players
                .Select(player =>
                {
                    PlayerTeam? team = teamSnapshot.Values.FirstOrDefault(
                        team => team.Members.Contains(player.Name));

                    string displayName = !string.IsNullOrWhiteSpace(player.DisplayName)
                        ? player.DisplayName
                        : TabListFormatter.FormatTeamMemberName(player.Name, team);

                    return new TabListEntry(
                        player.Uuid,
                        player.Name,
                        displayName,
                        team?.Name ?? string.Empty,
                        !string.IsNullOrWhiteSpace(team?.DisplayName) ? team.DisplayName : team?.Name ?? string.Empty,
                        player.Gamemode,
                        player.Ping,
                        player.TabListOrder,
                        player.Listed);
                })
                .ToList();

            return new TabListSnapshot(header, footer, entries);
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
                    handler.SendLocationUpdate(goal, Movement.IsOnGround(world, goal), false, _yaw, _pitch);
                    return true;
                }
                else
                {
                    pathTarget = null;
                    path = Movement.CalculatePath(world, location, goal, allowUnsafe, maxOffset, minOffset, timeout ?? TimeSpan.FromSeconds(5));
                    return path is not null;
                }
            }
        }

        /// <summary>
        /// Navigate to a goal using the new A* pathfinder.
        /// Runs the search, converts the result into the legacy Queue path, and starts movement.
        /// Returns a description of the result for UI feedback.
        /// </summary>
        public (bool success, string message) MoveToAStar(Location goal, long timeoutMs = 5000)
        {
            lock (locationLock)
            {
                var ctx = new Pathing.Core.CalculationContext(world);
                var finder = new Pathing.Core.AStarPathFinder();
                finder.DebugLog = msg => Log.Debug(msg);

                int sx = (int)Math.Floor(location.X);
                int sy = (int)Math.Floor(location.Y);
                int sz = (int)Math.Floor(location.Z);

                // If floored Y lands inside a solid block (e.g. player on top of it), step up
                if (!ctx.CanWalkThrough(sx, sy, sz) && ctx.CanWalkThrough(sx, sy + 1, sz))
                    sy++;

                int gx = (int)Math.Floor(goal.X);
                int gy = (int)Math.Floor(goal.Y);
                int gz = (int)Math.Floor(goal.Z);

                if (!ctx.CanWalkThrough(gx, gy, gz) && ctx.CanWalkThrough(gx, gy + 1, gz))
                    gy++;

                Log.Info($"[Goto] A* search from ({sx},{sy},{sz}) to ({gx},{gy},{gz}) " +
                         $"[raw pos=({location.X:F2},{location.Y:F2},{location.Z:F2})]");

                using var cts = new CancellationTokenSource();
                var result = finder.Calculate(ctx, sx, sy, sz,
                    new Pathing.Goals.GoalBlock(gx, gy, gz), cts.Token, timeoutMs);

                Log.Info($"[Goto] A* result: {result.Status}, nodes={result.NodesExplored}, " +
                         $"time={result.ElapsedMs}ms, path length={result.Path.Count}");

                if (result.Status == Pathing.Core.PathStatus.Failed || result.Path.Count < 2)
                {
                    return (false, string.Format(Translations.cmd_goto_failed,
                        result.NodesExplored, result.ElapsedMs));
                }

                var queue = new Queue<Location>();
                for (int i = 1; i < result.Path.Count; i++)
                {
                    var node = result.Path[i];
                    queue.Enqueue(new Location(node.X + 0.5, node.Y, node.Z + 0.5));
                }

                Log.Info($"[Goto] Path waypoints: {queue.Count}");
                int logCount = 0;
                foreach (var wp in queue)
                {
                    Log.Debug($"[Goto]   wp[{logCount}] = ({wp.X:F1},{wp.Y:F1},{wp.Z:F1})");
                    logCount++;
                }

                pathTarget = null;
                path = queue;

                string statusStr = result.Status == Pathing.Core.PathStatus.Partial ? " (partial)" : "";
                return (true, string.Format(Translations.cmd_goto_success,
                    queue.Count, result.NodesExplored, result.ElapsedMs, statusStr));
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

            if (!CanSendMessage)
            {
                Log.Warn(Translations.mcc_send_text_not_connected);
                return;
            }

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
            Item newItem = item.CloneWithCount(item.Count);
            inventory.Items[newSlotId] = newItem;
            inventory.Items.Remove(slotId);

            changedSlots.Add(new Tuple<short, Item?>((short)newSlotId, newItem));
            changedSlots.Add(new Tuple<short, Item?>((short)slotId, null));
        }

        private static bool IsServerManagedOutputSlot(Container inventory, int slotId)
        {
            return (inventory.Type, slotId) switch
            {
                (ContainerType.PlayerInventory, 0) => true,
                (ContainerType.Crafting, 0) => true,
                (ContainerType.Anvil, 2) => true,
                (ContainerType.BlastFurnace, 2) => true,
                (ContainerType.Furnace, 2) => true,
                (ContainerType.Smoker, 2) => true,
                (ContainerType.Grindstone, 2) => true,
                (ContainerType.Cartography, 2) => true,
                (ContainerType.Merchant, 2) => true,
                (ContainerType.Stonecutter, 1) => true,
                (ContainerType.Loom, 3) => true,
                (ContainerType.SmightingTable, 3) => true,
                _ => false
            };
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
            if (inventory is not null)
            {
                switch (action)
                {
                    case WindowActionType.LeftClick:
                        // Check if cursor have item (slot -1)
                        if (playerInventory.Items.ContainsKey(-1))
                        {
                            // Result slots are server-managed and cannot accept cursor items directly.
                            if (IsServerManagedOutputSlot(inventory, slotId))
                                break;

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
                                if (IsServerManagedOutputSlot(inventory, slotId))
                                    break;

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
                            if (IsServerManagedOutputSlot(inventory, slotId))
                                break;

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
                                Item itemClone = itemTmp.CloneWithCount(1);
                                inventory.Items[slotId] = itemClone;
                                playerInventory.Items[-1].Count--;
                            }
                        }
                        else
                        {
                            // Check target slot have item?
                            if (inventory.Items.ContainsKey(slotId))
                            {
                                if (IsServerManagedOutputSlot(inventory, slotId))
                                {
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
                                        playerInventory.Items[-1] = itemTmp.CloneWithCount(itemTmp.Count / 2);
                                        inventory.Items[slotId].Count = itemTmp.Count / 2;
                                    }
                                    else
                                    {
                                        // Cannot be evenly divided. item count on cursor is always larger than item on inventory
                                        Item itemTmp = inventory.Items[slotId];
                                        playerInventory.Items[-1] = itemTmp.CloneWithCount((itemTmp.Count + 1) / 2);
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
                        if (IsServerManagedOutputSlot(inventory, slotId))
                            break;
                        if (item is not null)
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
                                    else if (item is not null && false /* Check if wearable */)
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
                                    else if (item is not null && item.Count == 1 && (item.Type == ItemType.NetheriteIngot ||
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
                                    else if (item is not null && false /* Check if it can be burned */)
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
                                    else if (item is not null && item.Type == ItemType.BlazePowder)
                                    {
                                        lower2upper = true;
                                        if (!inventory.Items.ContainsKey(4) || inventory.Items[4].Count < 64)
                                            upperStartSlot = upperEndSlot = 4;
                                        else
                                            upperStartSlot = upperEndSlot = 3;
                                    }
                                    else if (item is not null && false /* Check if it can be used for alchemy */)
                                    {
                                        lower2upper = true;
                                        upperStartSlot = upperEndSlot = 3;
                                    }
                                    else if (item is not null && (item.Type == ItemType.Potion || item.Type == ItemType.GlassBottle))
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
                                    else if (item is not null && item.Type == ItemType.LapisLazuli)
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
                                    else if (item is not null && false /* Check */)
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
                                    else if (item is not null && false /* Check for availability for staining */)
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
                                    else if (item is not null && false /* Check if it is available for trading */)
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
                                    else if (item is not null && item.Type == ItemType.FilledMap)
                                    {
                                        lower2upper = true;
                                        upperStartSlot = upperEndSlot = 0;
                                    }
                                    else if (item is not null && item.Type == ItemType.Map)
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
                                    else if (item is not null && false /* Check if it is available for stone cutteing */)
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
            ClearUnlockedRecipes();
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
                return type switch
                {
                    InteractType.Interact => handler.SendInteractEntity(entityID, (int)type, (int)hand),
                    InteractType.InteractAt => handler.SendInteractEntity(
                        EntityID: entityID,
                        type: (int)type,
                        X: (float)entities[entityID].Location.X,
                        Y: (float)entities[entityID].Location.Y,
                        Z: (float)entities[entityID].Location.Z,
                        hand: (int)hand),
                    _ => handler.SendInteractEntity(entityID, (int)type),
                };
            }
            
            return false;
        }

        /// <summary>
        /// Place the block at hand in the Minecraft world
        /// </summary>
        /// <param name="location">Location to place block to</param>
        /// <param name="blockFace">Block face (e.g. Direction.Down when clicking on the block below to place this block)</param>
        /// <param name="lookAtBlock">Also look at the block before interacting</param>
        /// <returns>TRUE if successfully placed</returns>
        public bool PlaceBlock(Location location, Direction blockFace, Hand hand = Hand.MainHand, bool lookAtBlock = false)
        {
            return InvokeOnMainThread(() =>
            {
                if (lookAtBlock)
                {
                    UpdateLocation(GetCurrentLocation(), location.ToCenter());
                    handler.SendLocationUpdate(GetCurrentLocation(), Movement.IsOnGround(world, GetCurrentLocation()), false, _yaw, _pitch);
                }
                return handler.SendPlayerBlockPlacement((int)hand, location, blockFace, sequenceId++);
            });
        }


        /// <summary>
        /// Attempt to dig a block at the specified location
        /// </summary>
        /// <param name="location">Location of block to dig</param>
        /// <param name="swingArms">Also perform the "arm swing" animation</param>
        /// <param name="lookAtBlock">Also look at the block before digging</param>
        public bool DigBlock(Location location, Direction blockFace, bool swingArms = true, bool lookAtBlock = true, double duration = 0)
        {
            // TODO select best face from current player location

            if (!GetTerrainEnabled())
                return false;

            if (InvokeRequired)
                return InvokeOnMainThread(() => DigBlock(location, blockFace, swingArms, lookAtBlock, duration));

            lock (DigLock)
            {
                if (RemainingDiggingTime > 0 && LastDigPosition is not null)
                {
                    handler.SendPlayerDigging(1, LastDigPosition.Item1, LastDigPosition.Item2, sequenceId++);
                    Log.Info(string.Format(Translations.cmd_dig_cancel, LastDigPosition.Item1));
                }

                // Look at block before attempting to break it
                if (lookAtBlock)
                    UpdateLocation(GetCurrentLocation(), location);

                // Auto-compute dig duration for survival/adventure mode when not explicitly supplied
                if (duration <= 0 && protocolversion >= Protocol18Handler.MC_1_8_Version
                    && gamemode is 0 or 2) // Survival or Adventure
                {
                    duration = ComputeAutoDigDuration(location);
                }

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
        /// Compute the automatic dig duration in seconds for a block, based on held tool,
        /// enchantments, effects, attributes, and player state.
        /// Returns 0 for instant-break blocks.
        /// </summary>
        private double ComputeAutoDigDuration(Location location)
        {
            try
            {
                Block block = world.GetBlock(location);
                Material blockMaterial = block.Type;

                if (blockMaterial == Material.Air)
                    return 0;

                // Get held item from player inventory
                Item? heldItem = null;
                Item? helmetItem = null;
                if (inventories.TryGetValue(0, out var playerInv))
                {
                    int hotbarSlot = 36 + CurrentSlot; // Hotbar slots are 36-44
                    playerInv.Items.TryGetValue(hotbarSlot, out heldItem);
                    playerInv.Items.TryGetValue(5, out helmetItem); // Slot 5 = helmet
                }

                int ticks = MiningCalculator.ComputeDigTicks(
                    blockMaterial,
                    heldItem,
                    helmetItem,
                    playerEffects,
                    playerAttributes,
                    playerPhysics.InWater,
                    playerPhysics.OnGround,
                    protocolversion);

                if (ticks <= 0)
                    return 0;

                return (double)ticks / Settings.ClientTicksPerSecond;
            }
            catch
            {
                return 0;
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
        /// Drop the currently selected hotbar item like a real player pressing Q or Ctrl+Q.
        /// </summary>
        /// <param name="dropEntireStack">TRUE to drop the whole stack, FALSE to drop one item</param>
        /// <returns>TRUE if the packet was sent</returns>
        public bool DropSelectedItem(bool dropEntireStack)
        {
            if (InvokeRequired)
                return InvokeOnMainThread(() => DropSelectedItem(dropEntireStack));

            Location actionLocation = GetCurrentLocation().ToFloor();
            int status = dropEntireStack ? 3 : 4;
            bool sent = handler.SendPlayerDigging(status, actionLocation, Direction.Down, sequenceId++);
            if (sent)
                ApplySelectedItemDropPrediction(dropEntireStack);

            return sent;
        }

        private void ApplySelectedItemDropPrediction(bool dropEntireStack)
        {
            if (!inventories.TryGetValue(0, out Container? playerInventory))
                return;

            int selectedSlotId = CurrentSlot + 36;
            if (!playerInventory.Items.TryGetValue(selectedSlotId, out Item? heldItem) || heldItem.IsEmpty)
                return;

            if (dropEntireStack || heldItem.Count <= 1)
                playerInventory.Items.Remove(selectedSlotId);
            else heldItem.Count--;

            DispatchBotEvent(bot => bot.OnInventoryUpdate(0));
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

        /// <summary>
        /// Send a recipe book craft request for the currently active crafting inventory.
        /// </summary>
        /// <param name="recipeId">Recipe identifier to craft</param>
        /// <param name="makeAll">True to craft as many items as possible</param>
        /// <returns>True if the packet was sent</returns>
        public bool SendPlaceRecipe(string recipeId, bool makeAll)
        {
            if (InvokeRequired)
                return InvokeOnMainThread(() => SendPlaceRecipe(recipeId, makeAll));

            if (protocolversion < Protocol18Handler.MC_1_13_Version)
                return false;

            Container? activeInventory = GetActiveRecipeBookInventory();
            if (activeInventory is null)
                return false;

            string normalizedRecipeId = NormalizeRecipeArgument(recipeId, protocolversion);
            if (normalizedRecipeId.Length == 0)
                return false;

            return handler.SendPlaceRecipe(activeInventory.ID, normalizedRecipeId, makeAll);
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

            if (botList is not null)
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
            if (protocolversion < Protocol18Handler.MC_1_19_3_Version || playerKeyPair is null || !isOnlineMode)
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
                && playerKeyPair is not null && isOnlineMode)
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
            ClearKnownSigns();
            ClearInventories();
            DispatchBotEvent(bot => bot.OnRespawn());
        }

        /// <summary>
        /// Drive the physics engine input based on the current A* path.
        /// Converts discrete waypoint pathfinding into continuous movement input.
        /// </summary>
        private void UpdatePathfindingInput()
        {
            physicsInput.Reset();

            // Advance waypoints when reached
            if (pathTarget is not null && ReachedWaypoint(pathTarget.Value))
                AdvanceWaypoint();

            // First target from a fresh path
            if (pathTarget is null && path is not null && path.Count > 0)
                AdvanceWaypoint();

            if (pathTarget is not null)
            {
                // Look-ahead: if this is a vertical-only waypoint and the next requires
                // horizontal movement, merge them once we're close enough vertically.
                // This handles the ladder-to-platform transition.
                if (path is not null && path.Count > 0)
                {
                    var target = pathTarget.Value;
                    double dx = target.X - location.X;
                    double dz = target.Z - location.Z;
                    double dy = target.Y - location.Y;
                    double horizDistSq = dx * dx + dz * dz;

                    bool isVerticalWaypoint = horizDistSq < 0.5 && Math.Abs(dy) > 0.3;
                    if (isVerticalWaypoint)
                    {
                        var next = path.Peek();
                        double ndx = next.X - target.X;
                        double ndz = next.Z - target.Z;
                        bool nextIsHorizontal = ndx * ndx + ndz * ndz > 0.3;

                        // Skip to next waypoint early if we're within 1 block of the target Y
                        // and the next move requires horizontal movement
                        if (nextIsHorizontal && Math.Abs(dy) < 1.0)
                        {
                            AdvanceWaypoint();
                        }
                    }
                }

                SetInputToward(pathTarget.Value);
            }
        }

        private void AdvanceWaypoint()
        {
            if (path is not null && path.Count > 0)
            {
                pathTarget = path.Dequeue();
                if (Config.Main.Advanced.MoveHeadWhileWalking)
                    UpdateLocation(location, pathTarget.Value + new Location(0, 1, 0));
            }
            else
            {
                pathTarget = null;
                path = null;
            }
        }

        /// <summary>
        /// Check if the player has approximately reached a waypoint.
        /// Uses both horizontal and vertical distance for climb/descend waypoints.
        /// </summary>
        private bool ReachedWaypoint(Location target)
        {
            double dx = target.X - location.X;
            double dz = target.Z - location.Z;
            double dy = target.Y - location.Y;
            double horizDistSq = dx * dx + dz * dz;

            // Vertical waypoint (climbing/falling): require reaching target Y level
            if (horizDistSq < 0.5 && Math.Abs(dy) > 0.8)
                return false;

            return horizDistSq < 0.25 && Math.Abs(dy) < 0.8;
        }

        /// <summary>
        /// Set movement input to walk toward a target location.
        /// Calculates the yaw needed and sets Forward + Sprint.
        /// </summary>
        private void SetInputToward(Location target)
        {
            double dx = target.X - location.X;
            double dz = target.Z - location.Z;
            double dy = target.Y - location.Y;
            double distSqr = dx * dx + dz * dz;

            // Climbing: target is above/below with small horizontal offset
            if (playerPhysics.OnClimbable && Math.Abs(dy) > 0.5 && distSqr < 1.0)
            {
                if (dy > 0)
                {
                    physicsInput.Jump = true;
                    // Push against the wall for HorizontalCollision-triggered climbing
                    if (distSqr > 0.01)
                    {
                        float yaw = (float)(-Math.Atan2(dx, dz) / Math.PI * 180.0);
                        if (yaw < 0) yaw += 360;
                        playerPhysics.Yaw = yaw;
                        playerYaw = yaw;
                        physicsInput.Forward = true;
                    }
                    else
                    {
                        physicsInput.Forward = true;
                    }
                }
                else
                {
                    physicsInput.Sneak = false;
                }
                return;
            }

            // Non-climbing vertical jump
            if (distSqr < 0.1 && dy > 0.5 && playerPhysics.OnGround)
            {
                physicsInput.Jump = true;
                return;
            }

            if (distSqr < 0.01)
            {
                // Vertically aligned but need to reach different Y: set Jump when on ground
                if (dy > 0.3 && playerPhysics.OnGround)
                    physicsInput.Jump = true;
                return;
            }

            // Calculate yaw to face target
            float targetYaw = (float)(-Math.Atan2(dx, dz) / Math.PI * 180.0);
            if (targetYaw < 0) targetYaw += 360;
            playerPhysics.Yaw = targetYaw;
            playerYaw = targetYaw;

            physicsInput.Forward = true;

            // Jump if target is above and we're on ground
            if (dy > 0.5 && playerPhysics.OnGround)
                physicsInput.Jump = true;

            // Map MovementSpeed setting: 1=sneak, 2-4=walk, 5=sprint
            if (Config.Main.Advanced.MovementSpeed >= 5)
                physicsInput.Sprint = true;
            else if (Config.Main.Advanced.MovementSpeed <= 1)
                physicsInput.Sneak = true;
        }

        /// <summary>
        /// Check if the client is currently processing a Movement.
        /// </summary>
        /// <returns>true if a movement is currently handled</returns>
        public bool ClientIsMoving()
        {
            return terrainAndMovementsEnabled && locationReceived && path is not null && path.Count > 0;
        }

        /// <summary>
        /// Get the current goal
        /// </summary>
        /// <returns>Current goal of movement. Location.Zero if not set.</returns>
        public Location GetCurrentMovementGoal()
        {
            return (ClientIsMoving() || path is null) ? Location.Zero : path.Last();
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

                // Sync physics engine position
                if (physicsInitialized)
                {
                    playerPhysics.Teleport(this.location.X, this.location.Y, this.location.Z);
                }
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
        /// Send the current player position and look angles to the server.
        /// </summary>
        /// <returns>TRUE if the update packet was sent</returns>
        public bool SendLocationUpdate()
        {
            if (InvokeRequired)
                return InvokeOnMainThread(SendLocationUpdate);

            Location current = GetCurrentLocation();
            bool onGround = physicsInitialized ? playerPhysics.OnGround : Movement.IsOnGround(world, current);
            bool horizontalCollision = physicsInitialized && playerPhysics.HorizontalCollision;
            return handler.SendLocationUpdate(current, onGround, horizontalCollision, _yaw, _pitch);
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
                        if (Config.Signature.ShowModifiedChat && message.unsignedContent is not null)
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

                if (ConsoleIO.Backend is Tui.TuiConsoleBackend
                    && Tui.ContainerViewBase.HasTuiSupport(inventory.Type)
                    && Tui.InventoryTuiHost.CanLaunch)
                {
                    Tui.InventoryTuiHost.Launch(this, inventoryID);
                }
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

            Tui.InventoryTuiHost.NotifyInventoryClosed(inventoryID);
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
                    var topEnchantmentLevelRequirement = inventory.Properties[0];
                    var middleEnchantmentLevelRequirement = inventory.Properties[1];
                    var bottomEnchantmentLevelRequirement = inventory.Properties[2];

                    var topEnchantment = EnchantmentMapping.GetEnchantmentById(
                        GetProtocolVersion(),
                        inventory.Properties[4]);

                    var middleEnchantment = EnchantmentMapping.GetEnchantmentById(
                        GetProtocolVersion(),
                        inventory.Properties[5]);

                    var bottomEnchantment = EnchantmentMapping.GetEnchantmentById(
                        GetProtocolVersion(),
                        inventory.Properties[6]);

                    var topEnchantmentLevel = inventory.Properties[7];
                    var middleEnchantmentLevel = inventory.Properties[8];
                    var bottomEnchantmentLevel = inventory.Properties[9];

                    var sb = new StringBuilder();

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
                    if (item is not null)
                        inventories[0].Items[-1] = item;
                    else
                        inventories[0].Items.Remove(-1);
                }
            }
            else
            {
                if (inventories.ContainsKey(inventoryID))
                {
                    if (item is null || item.IsEmpty)
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
            Entity? entity = null;
            if (entities.TryGetValue(entityid, out var trackedEntity))
            {
                entity = trackedEntity;
            }

            var effectData = new EffectData(effect, amplifier, duration, flags);
            entity?.ActiveEffects[effect] = effectData;

            if (entityid == playerEntityID)
            {
                playerEffects.TryGetValue(effect, out var previousPlayerEffect);
                playerEffects[effect] = effectData;

                bool shouldAnnounceEffectGain = previousPlayerEffect is null
                    || previousPlayerEffect.Amplifier != amplifier
                    || (effectData.IsInfinite && !previousPlayerEffect.IsInfinite)
                    || (!effectData.IsInfinite && duration > previousPlayerEffect.RemainingTicks + 20);

                if (shouldAnnounceEffectGain)
                {
                    ConsoleIO.WriteLine(string.Format(Translations.bot_effect_gained,
                        effectData.GetDisplayNameWithArticle(), effectData.GetInitialDurationText()));
                }
            }

            if (entity is not null)
                DispatchBotEvent(bot => bot.OnEntityEffect(entity, effect, amplifier, duration, flags));
        }

        /// <summary>
        /// Called when an entity has an effect removed
        /// </summary>
        /// <param name="entityid">Entity ID</param>
        /// <param name="effect">Effect that was removed</param>
        public void OnRemoveEntityEffect(int entityid, Effects effect)
        {
            Entity? entity = null;
            EffectData? removedEffectData = null;

            if (entities.TryGetValue(entityid, out var trackedEntity))
            {
                entity = trackedEntity;
                if (entity.ActiveEffects.Remove(effect, out var entityEffectData))
                    removedEffectData = entityEffectData;
            }

            if (entityid == playerEntityID && playerEffects.Remove(effect, out var playerEffectData))
                removedEffectData ??= playerEffectData;

            if (entityid == playerEntityID && removedEffectData is not null)
                ConsoleIO.WriteLine(string.Format(Translations.bot_effect_expired, removedEffectData.GetDisplayName()));

            if (entity is not null)
                DispatchBotEvent(bot => bot.OnRemoveEntityEffect(entity, effect));
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
                if (item is not null)
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
        /// Called when an entity velocity update is received.
        /// </summary>
        /// <param name="entityID">Entity ID</param>
        /// <param name="velocityX">Velocity on X axis (blocks/tick)</param>
        /// <param name="velocityY">Velocity on Y axis (blocks/tick)</param>
        /// <param name="velocityZ">Velocity on Z axis (blocks/tick)</param>
        public void OnEntityVelocity(int entityID, double velocityX, double velocityY, double velocityZ)
        {
            if (entities.TryGetValue(entityID, out Entity? entity))
                DispatchBotEvent(bot => bot.OnEntityVelocity(entity, velocityX, velocityY, velocityZ));
        }

        /// <summary>
        /// Called when a sound packet is received.
        /// </summary>
        /// <param name="soundName">Sound key when available, otherwise null</param>
        /// <param name="location">Sound location when available</param>
        /// <param name="category">Sound category id from packet</param>
        /// <param name="volume">Sound volume</param>
        /// <param name="pitch">Sound pitch</param>
        /// <param name="entityID">Source entity id for entity sound packets, if any</param>
        public void OnSoundEffect(string? soundName, Location? location, int category, float volume, float pitch,
            int? entityID)
        {
            Entity? sourceEntity = null;
            Location? resolvedLocation = location;

            if (entityID is int id && entities.TryGetValue(id, out Entity? entity))
            {
                sourceEntity = entity;
                resolvedLocation ??= entity.Location;
            }

            DispatchBotEvent(bot => bot.OnSoundEffect(soundName, resolvedLocation, category, volume, pitch,
                sourceEntity));
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
                foreach (var kvp in prop)
                    playerAttributes[kvp.Key] = kvp.Value;

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
                double tps = tickDiff / (currentTime - lastTime).TotalSeconds;
                lastAge = WorldAge;
                lastTime = currentTime;
                if (tps > 0)
                {
                    // A Minecraft server cannot genuinely exceed 20 TPS; values above 20 are
                    // caused by packet-timing jitter. Clamp instead of discarding so that a
                    // healthy server averages to 20 rather than being biased downward.
                    tps = Math.Min(tps, 20.0);
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
                    respawnTicks = Settings.ClientTicksPerSecond;
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
        /// Called when Soreboard Objective
        /// </summary>
        /// <param name="objectiveName">objective name</param>
        /// <param name="mode">0 to create the scoreboard. 1 to remove the scoreboard. 2 to update the display text.</param>
        /// <param name="objectiveValue">Only if mode is 0 or 2. The text to be displayed for the score</param>
        /// <param name="type">Only if mode is 0 or 2. 0 = "integer", 1 = "hearts".</param>
        /// <param name="numberFormat">Number format: 0 - blank, 1 - styled, 2 - fixed</param>
        public void OnScoreboardObjective(string objectiveName, byte mode, string objectiveValue, int type, int numberFormat)
        {
            var json = objectiveValue;
            objectiveValue = ChatParser.ParseText(objectiveValue);
            DispatchBotEvent(bot => bot.OnScoreboardObjective(objectiveName, mode, objectiveValue, type, json, numberFormat));
        }

        /// <summary>
        /// Called when DisplayScoreboard
        /// </summary>
        /// <param name="entityName">The entity whose score this is. For players, this is their username; for other entities, it is their UUID.</param>
        /// <param name="action">0 to create/update an item. 1 to remove an item.</param>
        /// <param name="objectiveName">The name of the objective the score belongs to</param>
        /// <param name="objectiveDisplayName">The name of the objective the score belongs to, but with chat formatting</param>
        /// <param name="objectiveValue">The score to be displayed next to the entry. Only sent when Action does not equal 1.</param>
        /// <param name="numberFormat">Number format: 0 - blank, 1 - styled, 2 - fixed</param>
        public void OnUpdateScore(string entityName, int action, string objectiveName, string objectiveDisplayName, int objectiveValue, int numberFormat)
        {
            DispatchBotEvent(bot => bot.OnUpdateScore(entityName, action, objectiveName, objectiveDisplayName, objectiveValue, numberFormat));
        }

        /// <summary>
        /// Called when a Teams packet is received. Updates the internal team state and notifies bots.
        /// </summary>
        public void OnTeam(string teamName, byte method, string displayName, byte friendlyFlags,
            string nameTagVisibility, string collisionRule, int color,
            string prefix, string suffix, List<string> players)
        {
            lock (teams)
            {
                switch (method)
                {
                    case 0: // create
                        var newTeam = new PlayerTeam
                        {
                            Name = teamName,
                            DisplayName = displayName,
                            AllowFriendlyFire = (friendlyFlags & 0x01) != 0,
                            SeeFriendlyInvisibles = (friendlyFlags & 0x02) != 0,
                            NameTagVisibility = nameTagVisibility,
                            CollisionRule = collisionRule,
                            Color = color,
                            Prefix = prefix,
                            Suffix = suffix
                        };
                        foreach (var p in players)
                            newTeam.Members.Add(p);
                        teams[teamName] = newTeam;
                        break;

                    case 1: // remove
                        teams.Remove(teamName);
                        break;

                    case 2: // update parameters
                        if (!teams.TryGetValue(teamName, out var updateTeam))
                        {
                            updateTeam = new PlayerTeam { Name = teamName };
                            teams[teamName] = updateTeam;
                        }
                        updateTeam.DisplayName = displayName;
                        updateTeam.AllowFriendlyFire = (friendlyFlags & 0x01) != 0;
                        updateTeam.SeeFriendlyInvisibles = (friendlyFlags & 0x02) != 0;
                        updateTeam.NameTagVisibility = nameTagVisibility;
                        updateTeam.CollisionRule = collisionRule;
                        updateTeam.Color = color;
                        updateTeam.Prefix = prefix;
                        updateTeam.Suffix = suffix;
                        break;

                    case 3: // add players
                        if (!teams.TryGetValue(teamName, out var addTeam))
                        {
                            addTeam = new PlayerTeam { Name = teamName };
                            teams[teamName] = addTeam;
                        }
                        foreach (var p in players)
                            addTeam.Members.Add(p);
                        break;

                    case 4: // remove players
                        if (teams.TryGetValue(teamName, out var removeTeam))
                            foreach (var p in players)
                                removeTeam.Members.Remove(p);
                        break;
                }
            }
            DispatchBotEvent(bot => bot.OnTeam(teamName, method, displayName, friendlyFlags,
                nameTagVisibility, collisionRule, color, prefix, suffix, players));
        }


        /// <summary>
        /// Called when the client received the Tab Header and Footer
        /// </summary>
        /// <param name="header">Header</param>
        /// <param name="footer">Footer</param>
        public void OnTabListHeaderAndFooter(string header, string footer)
        {
            lock (tabListHeaderFooterLock)
            {
                tabListHeader = header;
                tabListFooter = footer;
            }
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
                
                if (entity.Type.ContainsItem() && metadata.TryGetValue(itemEntityMetadataFieldIndex, out object? itemObj) && itemObj is not null && itemObj.GetType() == typeof(Item))
                {
                    Item item = (Item)itemObj;
                    if (item is null)
                        entity.Item = new Item(ItemType.Air, 0, null);
                    else entity.Item = item;
                }
                if (metadata.TryGetValue(6, out object? poseObj) && poseObj is not null && poseObj.GetType() == typeof(Int32))
                {
                    entity.Pose = (EntityPose)poseObj;
                }
                if (metadata.TryGetValue(2, out object? nameObj) && nameObj is not null && nameObj.GetType() == typeof(string))
                {
                    string name = nameObj.ToString() ?? string.Empty;
                    entity.CustomNameJson = name;
                    entity.CustomName = ChatParser.ParseText(name);
                }
                if (metadata.TryGetValue(3, out object? nameVisableObj) && nameVisableObj is not null && nameVisableObj.GetType() == typeof(bool))
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
            if (!IsSignMaterial(block.Type))
                RemoveKnownSign(location);
            DispatchBotEvent(bot => bot.OnBlockChange(location, block));
        }

        public void OnBlockEntityData(Location location, Dictionary<string, object>? nbt)
        {
            UpdateKnownSign(location, nbt);
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

        public void OnRecipeBookAdd(RecipeBookRecipeEntry[] recipes, bool replace)
        {
            lock (recipeBookLock)
            {
                if (replace)
                    unlockedRecipes.Clear();

                foreach (RecipeBookRecipeEntry recipe in recipes)
                {
                    // Guard against malformed server packets that send empty display IDs.
                    if (!string.IsNullOrWhiteSpace(recipe.CommandId))
                        unlockedRecipes[recipe.CommandId] = recipe;
                }
            }
        }

        public void OnRecipeBookRemove(string[] recipeIds)
        {
            lock (recipeBookLock)
            {
                foreach (string recipeId in recipeIds)
                {
                    if (!string.IsNullOrWhiteSpace(recipeId))
                        unlockedRecipes.Remove(recipeId);
                }
            }
        }

        public void OnAchievementsUpdate(IReadOnlyList<Achievement> added, IReadOnlyList<string> removedIds, bool reset)
        {
            lock (achievementsLock)
            {
                if (reset)
                    achievements.Clear();

                // Remove entries
                foreach (string id in removedIds)
                    achievements.Remove(id);

                // Add/update entries. For progress-only updates (no definition),
                // merge with existing definition if available.
                foreach (Achievement entry in added)
                {
                    if (entry.Title is null && achievements.TryGetValue(entry.Id, out Achievement? existing))
                    {
                        // Progress-only update - merge with existing definition
                        bool isCompleted = ComputeAchievementCompleted(existing.Requirements, entry.CriteriaProgress);
                        achievements[entry.Id] = existing with { IsCompleted = isCompleted, CriteriaProgress = entry.CriteriaProgress };
                    }
                    else
                    {
                        achievements[entry.Id] = entry;
                    }
                }
            }

            DispatchBotEvent(bot => bot.OnAchievementUpdate(added, removedIds, reset));
        }

        public void OnSelectAdvancementTab(string? tabId)
        {
            activeAdvancementTab = tabId;
        }

        /// <summary>
        /// Compute whether an achievement is completed based on AND-of-ORs requirements.
        /// </summary>
        private static bool ComputeAchievementCompleted(IReadOnlyList<IReadOnlyList<string>> requirements, IReadOnlyDictionary<string, bool> criteria)
        {
            if (requirements.Count == 0)
                return true;

            foreach (IReadOnlyList<string> group in requirements)
            {
                bool groupSatisfied = false;
                foreach (string criterion in group)
                {
                    if (criteria.TryGetValue(criterion, out bool done) && done)
                    {
                        groupSatisfied = true;
                        break;
                    }
                }
                if (!groupSatisfied)
                    return false;
            }
            return true;
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

        private void ClearKnownSigns()
        {
            lock (signDataLock)
            {
                knownSigns.Clear();
            }
        }

        private void RemoveKnownSign(Location location)
        {
            var key = ToBlockKey(location);
            lock (signDataLock)
            {
                knownSigns.Remove(key);
            }
        }

        private void UpdateKnownSign(Location location, Dictionary<string, object>? nbt)
        {
            var key = ToBlockKey(location);
            var block = world.GetBlock(new Location(key.x, key.y, key.z));
            if (!IsSignMaterial(block.Type) || !TryExtractSignText(nbt, out string[] frontText, out string[] backText, out bool isWaxed))
            {
                lock (signDataLock)
                {
                    knownSigns.Remove(key);
                }

                return;
            }

            lock (signDataLock)
            {
                knownSigns[key] = (block.Type.ToString(), block.GetTypeString(), frontText, backText, isWaxed);
            }
        }

        private static bool TryExtractSignText(Dictionary<string, object>? nbt, out string[] frontText, out string[] backText, out bool isWaxed)
        {
            frontText = ExtractSignLines(nbt, "front_text");
            backText = ExtractSignLines(nbt, "back_text");
            if (frontText.Length == 0 && backText.Length == 0)
                frontText = ExtractLegacySignLines(nbt);

            isWaxed = nbt is not null
                && nbt.TryGetValue("is_waxed", out object? waxedValue)
                && waxedValue is bool waxed
                && waxed;
            return frontText.Length > 0 || backText.Length > 0;
        }

        private static string[] ExtractSignLines(Dictionary<string, object>? nbt, string sideKey)
        {
            if (nbt is null
                || !nbt.TryGetValue(sideKey, out object? sideValue)
                || sideValue is not Dictionary<string, object> sideData
                || !sideData.TryGetValue("messages", out object? messagesValue)
                || messagesValue is not object[] messages)
            {
                return [];
            }

            return messages
                .Take(4)
                .Select(ConvertSignMessage)
                .ToArray();
        }

        private static string[] ExtractLegacySignLines(Dictionary<string, object>? nbt)
        {
            if (nbt is null)
                return [];

            List<string> lines = new(4);
            for (int i = 1; i <= 4; i++)
            {
                if (nbt.TryGetValue($"Text{i}", out object? value))
                    lines.Add(ConvertSignMessage(value));
            }

            return lines.ToArray();
        }

        private static string ConvertSignMessage(object? value)
        {
            try
            {
                return value switch
                {
                    null => string.Empty,
                    string text => ParseMaybeJsonText(text),
                    Dictionary<string, object> nbt => ChatParser.ParseText(nbt),
                    object[] items => string.Concat(items.Select(ConvertSignMessage)),
                    _ => value.ToString() ?? string.Empty
                };
            }
            catch
            {
                return value?.ToString() ?? string.Empty;
            }
        }

        private static string ParseMaybeJsonText(string text)
        {
            string trimmed = text.Trim();
            if ((trimmed.StartsWith("{", StringComparison.Ordinal) && trimmed.EndsWith("}", StringComparison.Ordinal))
                || (trimmed.StartsWith("[", StringComparison.Ordinal) && trimmed.EndsWith("]", StringComparison.Ordinal)))
            {
                try
                {
                    return ChatParser.ParseText(trimmed);
                }
                catch
                {
                }
            }

            return text;
        }

        private static bool IsSignMaterial(Material material)
        {
            return material.ToString().Contains("Sign", StringComparison.Ordinal);
        }

        private static (int x, int y, int z) ToBlockKey(Location location)
        {
            Location blockLocation = location.ToFloor();
            return ((int)blockLocation.X, (int)blockLocation.Y, (int)blockLocation.Z);
        }

        private static bool SupportsRecipeBook(ContainerType containerType)
        {
            return containerType switch
            {
                ContainerType.PlayerInventory or
                ContainerType.Crafting or
                ContainerType.Furnace or
                ContainerType.BlastFurnace or
                ContainerType.Smoker or
                ContainerType.Stonecutter => true,
                _ => false,
            };
        }

        private void ClearUnlockedRecipes()
        {
            lock (recipeBookLock)
            {
                unlockedRecipes.Clear();
            }
        }

        /// <summary>
        /// Normalize a recipe argument for the target protocol version.
        /// Legacy recipe-book packets use identifiers and default to the minecraft namespace.
        /// 1.21.2+ recipe-book packets use numeric recipe display ids and should be left trimmed-only.
        /// </summary>
        internal static string NormalizeRecipeArgument(string recipeId, int protocolVersion)
        {
            return protocolVersion >= Protocol18Handler.MC_1_21_2_Version
                ? recipeId.Trim()
                : NormalizeRecipeId(recipeId);
        }

        private static string NormalizeRecipeId(string recipeId)
        {
            string trimmedRecipeId = recipeId.Trim();
            if (trimmedRecipeId.Length == 0)
                return string.Empty;

            return trimmedRecipeId.Contains(':', StringComparison.Ordinal)
                ? trimmedRecipeId
                : "minecraft:" + trimmedRecipeId;
        }

        #endregion
    }
}
