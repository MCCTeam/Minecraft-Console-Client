﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Brigadier.NET;
using Brigadier.NET.Exceptions;
using MinecraftClient.ChatBots;
using MinecraftClient.CommandHandler;
using MinecraftClient.CommandHandler.Patch;
using MinecraftClient.Inventory;
using MinecraftClient.Logger;
using MinecraftClient.EntityHandler;
using MinecraftClient.Mapping;
using MinecraftClient.Protocol;
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

        private readonly ConcurrentQueue<string> chatQueue = new();
        private DateTime nextMessageSendTime = DateTime.MinValue;

        private readonly object inventoryLock = new();
        private readonly Dictionary<int, Container> inventories = new();

        private readonly List<string> registeredServerPluginChannels = new();
        private readonly Dictionary<string, List<ChatBot>> registeredBotPluginChannels = new();

        private bool terrainAndMovementsEnabled;
        private bool terrainAndMovementsRequested = false;
        private bool inventoryHandlingEnabled;
        private bool inventoryHandlingRequested = false;
        private bool entityHandlingEnabled;

        private static SemaphoreSlim locationLock = new(1, 1);
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

        private readonly string host;
        private readonly int port;
        private int protocolversion;
        private string username;
        private Guid uuid;
        private string uuidStr;
        private string sessionId;
        private PlayerKeyPair? playerKeyPair;
        private DateTime lastKeepAlive;
        private int respawnTicks = 0;
        private int gamemode = 0;
        private bool isSupportPreviewsChat;
        private EnchantmentData? lastEnchantment = null;

        private int playerEntityID;

        // player health and hunger
        private float playerHealth;
        private int playerFoodSaturation;
        private int playerLevel;
        private int playerTotalExperience;
        private byte CurrentSlot = 0;

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

        // ChatBot
        private ChatBot[] chatbots = Array.Empty<ChatBot>();
        private static ChatBot[] botsOnHold = Array.Empty<ChatBot>();
        private bool OldChatBotUpdateTrigger = false;
        private bool networkPacketCaptureEnabled = false;

        // ChatBot Async Events
        private static int EventTypeCount = typeof(McClientEventType).GetFields().Length;
        private static SemaphoreSlim EventCallbackWriteLock = new(1, 1);
        private static Task[][] ChatbotEventTasks = new Task[EventTypeCount][];
        private static Task[] WaitChatbotExecuteTask = new Task[EventTypeCount];
        private static SemaphoreSlim[] ChatbotEventTaskLocks = new SemaphoreSlim[EventTypeCount];
        private static Func<object?, Task>[][] ChatbotEvents = new Func<object?, Task>[EventTypeCount][];
        private static Dictionary<ChatBot, List<Tuple<McClientEventType, Func<object?, Task>>>> ChatbotRegisteredEvents = new();

        public int GetServerPort() { return port; }
        public string GetServerHost() { return host; }
        public string GetUsername() { return username; }
        public Guid GetUserUuid() { return uuid; }
        public string GetUserUuidStr() { return uuidStr; }
        public string GetSessionID() { return sessionId; }
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
        public ChatBot[] GetLoadedChatBots() { return chatbots; }

        private TcpClient? tcpClient;
        private IMinecraftCom? handler;
        private readonly CancellationTokenSource CancelTokenSource;

        public ILogger Log;

        public static void LoadCommandsAndChatbots()
        {
            for (int i = 0; i < EventTypeCount; ++i)
            {
                ChatbotEventTaskLocks[i] = new(1, 1);
                WaitChatbotExecuteTask[i] = Task.CompletedTask;
            }

            /* Load commands from the 'Commands' namespace */
            Type[] cmds_classes = Program.GetTypesInNamespace("MinecraftClient.Commands");
            foreach (Type type in cmds_classes)
            {
                if (type.IsSubclassOf(typeof(Command)))
                {
                    Command cmd = (Command)Activator.CreateInstance(type)!;
                    cmd.RegisterCommand(dispatcher);
                }
            }

            /* Load ChatBots */
            botsOnHold = GetChatbotsToRegister();
            foreach (ChatBot bot in botsOnHold)
                bot.Initialize();

            InitializeChatbotEventCallbacks(botsOnHold).Wait();
        }

        /// <summary>
        /// Starts the main chat client, wich will login to the server using the MinecraftCom class.
        /// </summary>
        /// <param name="session">A valid session obtained with MinecraftCom.GetLogin()</param>
        /// <param name="playerKeyPair">Key for message signing</param>
        /// <param name="serverHost">The server IP</param>
        /// <param name="serverPort">The server port to use</param>
        /// <param name="protocolversion">Minecraft protocol version to use</param>
        /// <param name="forgeInfo">ForgeInfo item stating that Forge is enabled</param>
        public McClient(string serverHost, ushort serverPort, CancellationTokenSource cancelTokenSource)
        {
            CancelTokenSource = cancelTokenSource;

            CmdResult.currentHandler = this;
            terrainAndMovementsEnabled = Config.Main.Advanced.TerrainAndMovements;
            inventoryHandlingEnabled = Config.Main.Advanced.InventoryHandling;
            entityHandlingEnabled = Config.Main.Advanced.EntityHandling;

            host = serverHost;
            port = serverPort;

            uuid = Guid.Empty;
            uuidStr = string.Empty;
            username = string.Empty;
            sessionId = string.Empty;
            playerKeyPair = null;
            protocolversion = 0;

            Log = Config.Logging.LogToFile
                ? new FileLogLogger(Config.AppVar.ExpandVars(Settings.Config.Logging.LogFile), Settings.Config.Logging.PrependTimestamp)
                : new FilteredLogger();
            Log.DebugEnabled = Config.Logging.DebugMessages;
            Log.InfoEnabled = Config.Logging.InfoMessages;
            Log.ChatEnabled = Config.Logging.ChatMessages;
            Log.WarnEnabled = Config.Logging.WarningMessages;
            Log.ErrorEnabled = Config.Logging.ErrorMessages;

            ClearInventories();

            chatbots = botsOnHold;
            botsOnHold = Array.Empty<ChatBot>();
            foreach (ChatBot bot in chatbots)
                bot.SetHandler(this);
        }

        public async Task Login(HttpClient httpClient, SessionToken session, PlayerKeyPair? playerKeyPair, int protocolversion, ForgeInfo? forgeInfo)
        {
            sessionId = session.ID;
            if (!Guid.TryParse(session.PlayerID, out uuid))
                uuid = Guid.Empty;
            uuidStr = session.PlayerID;
            username = session.PlayerName;
            this.playerKeyPair = playerKeyPair;

            this.protocolversion = protocolversion;

            try
            {
                tcpClient = ProxyHandler.NewTcpClient(host, port, ProxyHandler.ClientType.Ingame);
                tcpClient.ReceiveBufferSize = 1024 * 1024;
                tcpClient.ReceiveTimeout = Config.Main.Advanced.TcpTimeout * 1000; // Default: 30 seconds

                handler = ProtocolHandler.GetProtocolHandler(CancelTokenSource.Token, tcpClient, protocolversion, forgeInfo, this);
                Log.Info(Translations.mcc_version_supported);

                _ = Task.Run(TimeoutDetector, CancelTokenSource.Token);

                try
                {
                    if (await handler.Login(httpClient, this.playerKeyPair, session))
                    {
                        DispatchBotEvent(bot => bot.AfterGameJoined());
                        await TriggerEvent(McClientEventType.GameJoin, null);
                        return;
                    }

                    Log.Error(Translations.error_login_failed);
                }
                catch (Exception e)
                {
                    Log.Error($"{e.GetType().Name}: {e.Message}");
                    if (e.StackTrace != null)
                        Log.Error(e.StackTrace);
                    Log.Error(Translations.error_join);
                }
            }
            catch (SocketException e)
            {
                Log.Error(e.Message);
                Log.Error(Translations.error_connect);
            }

            if (ReconnectionAttemptsLeft > 0)
            {
                Log.Info(string.Format(Translations.mcc_reconnect, ReconnectionAttemptsLeft));
                Thread.Sleep(5000);
                ReconnectionAttemptsLeft--;
                Program.SetRestart();
            }
            else
            {
                Program.SetExit();
            }

            throw new Exception("Initialization failed.");
        }

        public async Task StartUpdating()
        {
            Log.Info(string.Format(Translations.mcc_joined, Config.Main.Advanced.InternalCmdChar.ToLogString()));

            ConsoleInteractive.ConsoleReader.MessageReceived += ConsoleReaderOnMessageReceived;
            ConsoleInteractive.ConsoleReader.OnInputChange += ConsoleIO.AutocompleteHandler;
            ConsoleInteractive.ConsoleReader.BeginReadThread();

            await handler!.StartUpdating();

            ConsoleInteractive.ConsoleReader.MessageReceived -= ConsoleReaderOnMessageReceived;
            ConsoleInteractive.ConsoleReader.OnInputChange -= ConsoleIO.AutocompleteHandler;
            ConsoleInteractive.ConsoleReader.StopReadThread();

            ConsoleIO.CancelAutocomplete();
            ConsoleIO.WriteLine(string.Empty);
        }

        /// <summary>
        /// Register bots
        /// </summary>
        private static ChatBot[] GetChatbotsToRegister(bool reload = false)
        {
            List<ChatBot> chatbotList = new();

            if (Config.ChatBot.Alerts.Enabled) { chatbotList.Add(new Alerts()); }
            if (Config.ChatBot.AntiAFK.Enabled) { chatbotList.Add(new AntiAFK()); }
            if (Config.ChatBot.AutoAttack.Enabled) { chatbotList.Add(new AutoAttack()); }
            if (Config.ChatBot.AutoCraft.Enabled) { chatbotList.Add(new AutoCraft()); }
            if (Config.ChatBot.AutoDig.Enabled) { chatbotList.Add(new AutoDig()); }
            if (Config.ChatBot.AutoDrop.Enabled) { chatbotList.Add(new AutoDrop()); }
            if (Config.ChatBot.AutoEat.Enabled) { chatbotList.Add(new AutoEat()); }
            if (Config.ChatBot.AutoFishing.Enabled) { chatbotList.Add(new AutoFishing()); }
            if (Config.ChatBot.AutoRelog.Enabled) { chatbotList.Add(new AutoRelog()); }
            if (Config.ChatBot.AutoRespond.Enabled) { chatbotList.Add(new AutoRespond()); }
            if (Config.ChatBot.ChatLog.Enabled) { chatbotList.Add(new ChatLog()); }
            if (Config.ChatBot.DiscordBridge.Enabled) { chatbotList.Add(new DiscordBridge()); }
            if (Config.ChatBot.Farmer.Enabled) { chatbotList.Add(new Farmer()); }
            if (Config.ChatBot.FollowPlayer.Enabled) { chatbotList.Add(new FollowPlayer()); }
            if (Config.ChatBot.HangmanGame.Enabled) { chatbotList.Add(new HangmanGame()); }
            if (Config.ChatBot.Mailer.Enabled) { chatbotList.Add(new Mailer()); }
            if (Config.ChatBot.Map.Enabled) { chatbotList.Add(new Map()); }
            if (Config.ChatBot.PlayerListLogger.Enabled) { chatbotList.Add(new PlayerListLogger()); }
            if (Config.ChatBot.RemoteControl.Enabled) { chatbotList.Add(new RemoteControl()); }
            // if (Config.ChatBot.ReplayCapture.Enabled && reload) { chatbotList.Add(new ReplayCapture()); }
            if (Config.ChatBot.ScriptScheduler.Enabled) { chatbotList.Add(new ScriptScheduler()); }
            if (Config.ChatBot.TelegramBridge.Enabled) { chatbotList.Add(new TelegramBridge()); }
            // Add your ChatBot here by uncommenting and adapting
            // chatbotList.Add(new ChatBots.YourBot());
            chatbotList.Add(new TestBot());

            return chatbotList.ToArray();
        }

        /// <summary>
        /// Retrieve messages from the queue and send.
        /// Note: requires external locking.
        /// </summary>
        private async Task TrySendMessageToServer()
        {
            if (handler != null)
            {
                while (nextMessageSendTime < DateTime.Now && chatQueue.TryDequeue(out string? text))
                {
                    await handler.SendChatMessage(text, playerKeyPair);
                    nextMessageSendTime = DateTime.Now + TimeSpan.FromSeconds(Config.Main.Advanced.MessageCooldown);
                }
            }
        }

        /// <summary>
        /// Called ~20 times per second by the protocol handler
        /// </summary>
        public async Task OnUpdate()
        {
            OldChatBotUpdateTrigger = !OldChatBotUpdateTrigger;
            foreach (ChatBot bot in chatbots)
            {
                await bot.OnClientTickAsync();
                if (OldChatBotUpdateTrigger)
                {
                    try
                    {
                        bot.Update();
                        bot.UpdateInternal();
                    }
                    catch (Exception e)
                    {
                        Log.Warn("Update: Got error from " + bot.ToString() + ": " + e.ToString());
                    }
                }
            }

            await TrySendMessageToServer();

            if (terrainAndMovementsEnabled && locationReceived)
            {
                for (int i = 0; i < Config.Main.Advanced.MovementSpeed / 2; i++) //Needs to run at 20 tps; MCC runs at 10 tps
                {
                    await locationLock.WaitAsync();
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
                    locationLock.Release();
                    await handler!.SendLocationUpdate(location, Movement.IsOnGround(world, location), _yaw, _pitch);
                }
                // First 2 updates must be player position AND look, and player must not move (to conform with vanilla)
                // Once yaw and pitch have been sent, switch back to location-only updates (without yaw and pitch)
                _yaw = null;
                _pitch = null;
            }

            if (Config.Main.Advanced.AutoRespawn && respawnTicks > 0)
            {
                if (--respawnTicks == 0)
                    await SendRespawnPacketAsync();
            }

            await TriggerEvent(McClientEventType.ClientTick, null);
        }

        #region Connection Lost and Disconnect from Server

        /// <summary>
        /// Periodically checks for server keepalives and consider that connection has been lost if the last received keepalive is too old.
        /// </summary>
        private async Task TimeoutDetector()
        {
            UpdateKeepAlive();
            using PeriodicTimer periodicTimer = new(TimeSpan.FromSeconds(Config.Main.Advanced.TcpTimeout));
            try
            {
                while (await periodicTimer.WaitForNextTickAsync(CancelTokenSource.Token) && !CancelTokenSource.IsCancellationRequested)
                {
                    if (lastKeepAlive.AddSeconds(Config.Main.Advanced.TcpTimeout) < DateTime.Now)
                    {
                        OnConnectionLost(ChatBot.DisconnectReason.ConnectionLost, Translations.error_timeout);
                        return;
                    }
                }
            }
            catch (AggregateException e)
            {
                if (e.InnerException is not OperationCanceledException)
                    throw;
            }
            catch (OperationCanceledException) { }
        }

        /// <summary>
        /// Update last keep alive to current time
        /// </summary>
        private void UpdateKeepAlive()
        {
            lastKeepAlive = DateTime.Now;
        }

        /// <summary>
        /// Disconnect the client from the server (initiated from MCC)
        /// </summary>
        public void Disconnect()
        {
            for (int i = 0; i < EventTypeCount; ++i)
            {
                ChatbotEventTaskLocks[i].Wait();
                WaitChatbotExecuteTask[i].Wait();
                ChatbotEventTaskLocks[i].Release();
            }

            DispatchBotEvent(bot => bot.OnDisconnect(ChatBot.DisconnectReason.UserLogout, string.Empty));

            TriggerEvent(McClientEventType.ClientDisconnect,
                new Tuple<ChatBot.DisconnectReason, string>(ChatBot.DisconnectReason.UserLogout, string.Empty)).Wait();

            WaitChatbotExecuteTask[(int)McClientEventType.ClientDisconnect].Wait();

            botsOnHold = chatbots;
            chatbots = Array.Empty<ChatBot>();

            if (handler != null)
            {
                handler.Disconnect();
                handler.Dispose();
            }

            tcpClient?.Close();
        }

        /// <summary>
        /// When connection has been lost, login was denied or played was kicked from the server
        /// </summary>
        public void OnConnectionLost(ChatBot.DisconnectReason reason, string message)
        {
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

            // Process AutoRelog last to make sure other bots can perform their cleanup tasks first (issue #1517)
            List<ChatBot> onDisconnectBotList = chatbots.Where(bot => bot is not AutoRelog).ToList();
            onDisconnectBotList.AddRange(chatbots.Where(bot => bot is AutoRelog));

            int restartDelay = -1;
            foreach (ChatBot bot in onDisconnectBotList)
            {
                try
                {
                    restartDelay = Math.Max(restartDelay, bot.OnDisconnect(reason, message));
                }
                catch (Exception e)
                {
                    if (e is not ThreadAbortException)
                    {
                        Log.Warn("OnDisconnect: Got error from " + bot.ToString() + ": " + e.ToString());
                    }
                    else throw; // ThreadAbortException should not be caught
                }
            }

            if (restartDelay < 0)
                Program.SetExit(handleFailure: true);
            else
                Program.SetRestart(restartDelay, true);

            handler!.Dispose();

            world.Clear();
        }

        #endregion

        #region ChatBot event callback

        private async Task TriggerEvent(McClientEventType eventType, object? parameter)
        {
            int eventId = (int)eventType;
            Func<object?, Task>[] eventList = ChatbotEvents[eventId];
            if (eventList.Length > 0)
            {
                await ChatbotEventTaskLocks[eventId].WaitAsync();
                await WaitChatbotExecuteTask[eventId];
                for (int i = 0; i < eventList.Length; ++i)
                    ChatbotEventTasks[eventId][i] = eventList[i](parameter);
                WaitChatbotExecuteTask[eventId] = WaitTaskAndHandleException(eventType);
                ChatbotEventTaskLocks[eventId].Release();
            }
        }

        private async Task WaitTaskAndHandleException(McClientEventType eventType)
        {
            Task[] taskList = ChatbotEventTasks[(int)eventType];
            for (int i = 0; i < taskList.Length; ++i)
            {
                try
                {
                    await taskList[i];
                }
                catch (Exception exception)
                {
                    Log.Error(string.Format(Translations.mcc_chatbot_event_exception, eventType.ToString(), exception.ToString()));
                }
            }
        }

        private static async Task InitializeChatbotEventCallbacks(IEnumerable<ChatBot> chatbotList)
        {
            List<Func<object?, Task>>[] tmpCallbackList = new List<Func<object?, Task>>[EventTypeCount];
            for (int i = 0; i < EventTypeCount; ++i)
                tmpCallbackList[i] = new();

            foreach (ChatBot bot in chatbotList)
            {
                Tuple<McClientEventType, Func<object?, Task>>[]? botEvents = bot.InitializeEventCallbacks();
                if (botEvents != null)
                {
                    ChatbotRegisteredEvents[bot] = new(botEvents);
                    foreach ((McClientEventType eventType, Func<object?, Task> callback) in botEvents)
                        tmpCallbackList[(int)eventType].Add(callback);
                }
                else
                {
                    ChatbotRegisteredEvents[bot] = new();
                }
            }

            await EventCallbackWriteLock.WaitAsync();
            for (int i = 0; i < EventTypeCount; ++i)
            {
                ChatbotEvents[i] = tmpCallbackList[i].ToArray();
                await UpdateChatbotEventTasksArray(i);
            }
            EventCallbackWriteLock.Release();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="eventType"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static async Task RegisterEventCallback(ChatBot bot, McClientEventType eventType, Func<object?, Task> callback)
        {
            int eventId = (int)eventType;
            await EventCallbackWriteLock.WaitAsync();

            ChatbotEvents[eventId] = new List<Func<object?, Task>>(ChatbotEvents[eventId]) { callback }.ToArray();
            if (ChatbotRegisteredEvents.TryGetValue(bot, out var botEvents))
                botEvents.Add(new(eventType, callback));
            else
                ChatbotRegisteredEvents[bot] = new() { new(eventType, callback) };
            await UpdateChatbotEventTasksArray(eventId);
            EventCallbackWriteLock.Release();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="eventType"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static async Task UnregisterEventCallback(ChatBot bot, McClientEventType eventType, Func<object?, Task> callback)
        {
            int eventId = (int)eventType;
            await EventCallbackWriteLock.WaitAsync();

            List<Func<object?, Task>> newList = new(ChatbotEvents[eventId]);
            newList.RemoveAll(c => c == callback);
            ChatbotEvents[eventId] = newList.ToArray();
            if (ChatbotRegisteredEvents.TryGetValue(bot, out var botEvents))
                botEvents.RemoveAll(c => c.Item1 == eventType && c.Item2 == callback);
            await UpdateChatbotEventTasksArray(eventId);
            EventCallbackWriteLock.Release();
        }

        public static async Task UnregisterChatbotEventCallback(ChatBot bot)
        {
            await EventCallbackWriteLock.WaitAsync();
            if (ChatbotRegisteredEvents.TryGetValue(bot, out var botEvents))
            {
                foreach ((McClientEventType eventType, Func<object?, Task> callback) in botEvents)
                {
                    int eventId = (int)eventType;
                    List<Func<object?, Task>> newList = new(ChatbotEvents[eventId]);
                    newList.RemoveAll(c => c == callback);
                    ChatbotEvents[eventId] = newList.ToArray();
                    await UpdateChatbotEventTasksArray(eventId);
                }
                ChatbotRegisteredEvents.Remove(bot);
            }
            EventCallbackWriteLock.Release();
        }

        private static async Task UpdateChatbotEventTasksArray(int eventId)
        {
            await ChatbotEventTaskLocks[eventId].WaitAsync();
            await WaitChatbotExecuteTask[eventId];
            ChatbotEventTasks[eventId] = new Task[ChatbotEvents[eventId].Length];
            ChatbotEventTaskLocks[eventId].Release();
        }

        #endregion

        #region Command prompt and internal MCC commands

        private void ConsoleReaderOnMessageReceived(object? sender, string text)
        {

            if (tcpClient!.Client == null)
                return;

            if (tcpClient.Client.Connected)
                Task.Run(async () => { await HandleCommandPromptText(text); });
            else
                return;
        }

        /// <summary>
        /// Allows the user to send chat messages, commands, and leave the server.
        /// Process text from the MCC command prompt on the main thread.
        /// </summary>
        private async Task HandleCommandPromptText(string text)
        {
            if (ConsoleIO.BasicIO && text.Length > 0 && text[0] == (char)0x00)
            {
                //Process a request from the GUI
                string[] command = text[1..].Split((char)0x00);
                switch (command[0].ToLower())
                {
                    case "autocomplete":
                        int id = await handler!.AutoComplete(command[1]);
                        while (!ConsoleIO.AutoCompleteDone) { await Task.Delay(100); }
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
                    await SendTextAsync(text);
                }
                else if (text.Length > 2
                    && Config.Main.Advanced.InternalCmdChar != MainConfigHealper.MainConfig.AdvancedConfig.InternalCmdCharType.none
                    && text[0] == Config.Main.Advanced.InternalCmdChar.ToChar()
                    && text[1] == '/')
                {
                    await SendTextAsync(text[1..]);
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
                            await SendTextAsync(text);
                        }
                        else if (result.status != CmdResult.Status.NotRun && (result.status != CmdResult.Status.Done || !string.IsNullOrWhiteSpace(result.result)))
                        {
                            Log.Info(result);
                        }
                    }
                    else
                    {
                        await SendTextAsync(text);
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

                foreach (ChatBot bot in chatbots)
                {
                    try
                    {
                        bot.OnInternalCommand(command, string.Join(' ', Command.GetArgs(command)), result);
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

        /// <summary>
        /// Reload settings and bots
        /// </summary>
        /// <param name="hard">Marks if bots need to be hard reloaded</param>
        public async Task ReloadSettings()
        {
            Program.ReloadSettings(true);
            await ReloadBots();
        }

        /// <summary>
        /// Reload loaded bots (Only builtin bots)
        /// </summary>
        public async Task ReloadBots()
        {
            await UnloadAllBots();

            ChatBot[] bots = GetChatbotsToRegister(true);

            foreach (ChatBot bot in bots)
            {
                bot.SetHandler(this);
                bot.Initialize();
            }

            await InitializeChatbotEventCallbacks(bots);

            if (handler != null)
                foreach (ChatBot bot in bots)
                    bot.AfterGameJoined();

            chatbots = bots;
        }

        /// <summary>
        /// Unload All Bots
        /// </summary>
        public async Task UnloadAllBots()
        {
            foreach (ChatBot bot in chatbots)
                bot.OnUnload();
            chatbots = Array.Empty<ChatBot>();
            registeredBotPluginChannels.Clear();

            for (int i = 0; i < ChatbotEvents.Length; ++i)
                ChatbotEvents[i] = Array.Empty<Func<object?, Task>>();
            ChatbotRegisteredEvents.Clear();

            await Task.CompletedTask;
        }

        #endregion

        #region Management: Load/Unload ChatBots and Enable/Disable settings

        /// <summary>
        /// Load a new bot
        /// </summary>
        public async Task BotLoad(ChatBot bot, bool init = true)
        {
            bot.SetHandler(this);
            chatbots = new List<ChatBot>(chatbots) { bot }.ToArray();
            if (init)
            {
                bot.Initialize();
                await InitializeChatbotEventCallbacks(new ChatBot[] { bot });
            }
            if (handler != null)
                bot.AfterGameJoined();
        }

        /// <summary>
        /// Unload a bot
        /// </summary>
        public async Task BotUnLoad(ChatBot bot)
        {
            List<ChatBot> botList = new();
            botList.AddRange(from botInList in chatbots
                             where !ReferenceEquals(botInList, bot)
                             select botInList);
            chatbots = botList.ToArray();

            bot.OnUnload();
            await UnregisterChatbotEventCallback(bot);

            // ToList is needed to avoid an InvalidOperationException from modfiying the list while it's being iterated upon.
            var botRegistrations = registeredBotPluginChannels.Where(entry => entry.Value.Contains(bot)).ToList();
            foreach (var entry in botRegistrations)
            {
                await UnregisterPluginChannelAsync(entry.Key, bot);
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

        /// <summary>
        /// Enable or disable network packet event calling.
        /// </summary>
        /// <remarks>
        /// Enable this may increase memory usage.
        /// </remarks>
        /// <param name="enabled"></param>
        public void SetNetworkPacketCaptureEnabled(bool enabled)
        {
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
            return handler!.GetMaxChatMessageLength();
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
            lock (onlinePlayers)
            {
                if (onlinePlayers.TryGetValue(uuid, out PlayerInfo? playerInfo))
                    return playerInfo;
                else
                    return null;
            }
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
        public async Task<bool> MoveToAsync(Location goal, bool allowUnsafe = false, bool allowDirectTeleport = false, int maxOffset = 0, int minOffset = 0, TimeSpan? timeout = null)
        {
            if (handler == null)
                return false;

            if (allowDirectTeleport)
            {
                await locationLock.WaitAsync();
                // 1-step path to the desired location without checking anything
                UpdateLocation(goal, goal); // Update yaw and pitch to look at next step
                await handler.SendLocationUpdate(goal, Movement.IsOnGround(world, goal), _yaw, _pitch);
                locationLock.Release();
                return true;
            }
            else
            {
                // Calculate path through pathfinding. Path contains a list of 1-block movement that will be divided into steps
                path = await Movement.CalculatePath(world, location, goal, allowUnsafe, maxOffset, minOffset, timeout ?? TimeSpan.FromSeconds(5));
                return path != null;
            }
        }

        /// <summary>
        /// Send a chat message or command to the server
        /// </summary>
        /// <param name="text">Text to send to the server</param>
        public async Task SendTextAsync(string text)
        {
            if (handler == null)
                return;

            if (string.IsNullOrEmpty(text))
                return;

            int maxLength = handler!.GetMaxChatMessageLength();

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
                    if (!string.IsNullOrEmpty(text))
                        chatQueue.Enqueue(text);
                }
            }
            else
            {
                chatQueue.Enqueue(text);
            }

            await TrySendMessageToServer();
        }

        /// <summary>
        /// Allow to respawn after death
        /// </summary>
        /// <returns>True if packet successfully sent</returns>
        public async Task<bool> SendRespawnPacketAsync()
        {
            if (handler == null)
                return false;
            return await handler.SendRespawnPacket();
        }

        /// <summary>
        /// Registers the given plugin channel for the given bot.
        /// </summary>
        /// <param name="channel">The channel to register.</param>
        /// <param name="bot">The bot to register the channel for.</param>
        public async Task RegisterPluginChannelAsync(string channel, ChatBot bot)
        {
            if (registeredBotPluginChannels.TryGetValue(channel, out List<ChatBot>? channelList))
            {
                channelList.Add(bot);
            }
            else
            {
                List<ChatBot> bots = new() { bot };
                registeredBotPluginChannels[channel] = bots;
                await SendPluginChannelMessageAsync("REGISTER", Encoding.UTF8.GetBytes(channel), true);
            }
        }

        /// <summary>
        /// Unregisters the given plugin channel for the given bot.
        /// </summary>
        /// <param name="channel">The channel to unregister.</param>
        /// <param name="bot">The bot to unregister the channel for.</param>
        public async Task UnregisterPluginChannelAsync(string channel, ChatBot bot)
        {
            if (registeredBotPluginChannels.TryGetValue(channel, out List<ChatBot>? channelList))
            {
                List<ChatBot> registeredBots = channelList;
                registeredBots.RemoveAll(item => ReferenceEquals(item, bot));
                if (registeredBots.Count == 0)
                {
                    registeredBotPluginChannels.Remove(channel);
                    await SendPluginChannelMessageAsync("UNREGISTER", Encoding.UTF8.GetBytes(channel), true);
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
        public async Task<bool> SendPluginChannelMessageAsync(string channel, byte[] data, bool sendEvenIfNotRegistered = false)
        {
            if (handler == null)
                return false;

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
            return await handler.SendPluginChannelPacket(channel, data);
        }

        /// <summary>
        /// Send the Entity Action packet with the Specified ID
        /// </summary>
        /// <returns>TRUE if the item was successfully used</returns>
        public async Task<bool> SendEntityActionAsync(EntityActionType entityAction)
        {
            if (handler == null)
                return false;
            return await handler.SendEntityAction(playerEntityID, (int)entityAction);
        }

        /// <summary>
        /// Use the item currently in the player's hand
        /// </summary>
        /// <returns>TRUE if the item was successfully used</returns>
        public async Task<bool> UseItemOnHandAsync()
        {
            if (handler == null)
                return false;
            return await handler.SendUseItem(0, sequenceId);
        }

        /// <summary>
        /// Use the item currently in the player's left hand
        /// </summary>
        /// <returns>TRUE if the item was successfully used</returns>
        public async Task<bool> UseItemOnOffHandAsync()
        {
            if (handler == null)
                return false;
            return await handler.SendUseItem(1, sequenceId);
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
        public async Task<bool> DoWindowActionAsync(int windowId, int slotId, WindowActionType action)
        {
            if (handler == null)
                return false;

            Item? item = null;
            List<Tuple<short, Item?>> changedSlots = new(); // List<Slot ID, Changed Items>
            lock (inventoryLock)
            {
                if (inventories.TryGetValue(windowId, out Container? container))
                    container.Items.TryGetValue(slotId, out item);

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

                                if (inventory.Items.TryGetValue(slotId, out Item? item1))
                                    changedSlots.Add(new Tuple<short, Item?>((short)slotId, item1));
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
                            if (playerInventory.Items.TryGetValue(-1, out Item? playerItem))
                            {
                                // When item on cursor and clicking slot 0, nothing will happen
                                if (slotId == 0) break;

                                // Check target slot have item?
                                if (inventory.Items.TryGetValue(slotId, out Item? invItem))
                                {
                                    // Check if both item are the same?
                                    if (invItem.Type == playerItem.Type)
                                    {
                                        // Check item stacking
                                        if (invItem.Count < invItem.Type.StackCount())
                                        {
                                            // Drop 1 item count from cursor
                                            playerItem.Count--;
                                            invItem.Count++;
                                        }
                                    }
                                    else
                                    {
                                        // Swap two items
                                        (invItem, playerItem) = (playerItem, invItem);
                                    }
                                }
                                else
                                {
                                    // Drop 1 item count from cursor
                                    inventory.Items[slotId] = new(playerItem.Type, 1, playerItem.NBT);
                                    playerItem.Count--;
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
                            if (inventory.Items.TryGetValue(slotId, out Item? item2))
                                changedSlots.Add(new Tuple<short, Item?>((short)slotId, item2));
                            else
                                changedSlots.Add(new Tuple<short, Item?>((short)slotId, null));
                            break;
                        case WindowActionType.ShiftClick:
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
                            if (inventory.Items.TryGetValue(slotId, out Item? item3))
                            {
                                item3.Count--;
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
            }
            return await handler!.SendWindowAction(windowId, slotId, action, item, changedSlots, inventories[windowId].StateID);
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
        public async Task<bool> DoCreativeGiveAsync(int slot, ItemType itemType, int count, Dictionary<string, object>? nbt = null)
        {
            if (handler == null)
                return false;
            return await handler.SendCreativeInventoryAction(slot, itemType, count, nbt);
        }

        /// <summary>
        /// Plays animation (Player arm swing)
        /// </summary>
        /// <param name="animation">0 for left arm, 1 for right arm</param>
        /// <returns>TRUE if animation successfully done</returns>
        public async Task<bool> DoAnimationAsync(int animation)
        {
            if (handler == null)
                return false;
            return await handler.SendAnimation(animation, playerEntityID);
        }

        /// <summary>
        /// Close the specified inventory window
        /// </summary>
        /// <param name="windowId">Window ID</param>
        /// <returns>TRUE if the window was successfully closed</returns>
        /// <remarks>Sending close window for inventory 0 can cause server to update our inventory if there are any item in the crafting area</remarks>
        public async Task<bool> CloseInventoryAsync(int windowId)
        {
            if (handler == null)
                return false;

            bool needCloseWindow = false;
            lock (inventoryLock)
            {
                if (inventories.ContainsKey(windowId))
                {
                    if (windowId != 0)
                        inventories.Remove(windowId);
                    needCloseWindow = true;
                }
            }
            if (needCloseWindow)
                return await handler.SendCloseWindow(windowId);
            else
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

            lock (inventoryLock)
            {
                inventories.Clear();
                inventories[0] = new Container(0, ContainerType.PlayerInventory, Translations.cmd_inventory_player_inventory);
            }
            return true;
        }

        /// <summary>
        /// Interact with an entity
        /// </summary>
        /// <param name="EntityID"></param>
        /// <param name="type">Type of interaction (interact, attack...)</param>
        /// <param name="hand">Hand.MainHand or Hand.OffHand</param>
        /// <returns>TRUE if interaction succeeded</returns>
        public async Task<bool> InteractEntityAsync(int entityID, InteractType type, Hand hand = Hand.MainHand)
        {
            if (handler == null)
                return false;

            if (entities.ContainsKey(entityID))
            {
                if (type == InteractType.Interact)
                {
                    return await handler.SendInteractEntity(entityID, (int)type, (int)hand);
                }
                else
                {
                    return await handler.SendInteractEntity(entityID, (int)type);
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
        public async Task<bool> PlaceBlockAsync(Location location, Direction blockFace, Hand hand = Hand.MainHand)
        {
            if (handler == null)
                return false;
            return await handler.SendPlayerBlockPlacement((int)hand, location, blockFace, sequenceId);
        }

        /// <summary>
        /// Attempt to dig a block at the specified location
        /// </summary>
        /// <param name="location">Location of block to dig</param>
        /// <param name="swingArms">Also perform the "arm swing" animation</param>
        /// <param name="lookAtBlock">Also look at the block before digging</param>
        public async Task<bool> DigBlockAsync(Location location, bool swingArms = true, bool lookAtBlock = true)
        {
            if (handler == null)
                return false;

            if (!GetTerrainEnabled())
                return false;

            // TODO select best face from current player location
            Direction blockFace = Direction.Down;

            // Look at block before attempting to break it
            if (lookAtBlock)
                UpdateLocation(GetCurrentLocation(), location);

            // Send dig start and dig end, will need to wait for server response to know dig result
            // See https://wiki.vg/How_to_Write_a_Client#Digging for more details
            return (await handler.SendPlayerDigging(0, location, blockFace, sequenceId))
                && (!swingArms || await DoAnimationAsync((int)Hand.MainHand))
                && await handler.SendPlayerDigging(2, location, blockFace, sequenceId);
        }

        /// <summary>
        /// Change active slot in the player inventory
        /// </summary>
        /// <param name="slot">Slot to activate (0 to 8)</param>
        /// <returns>TRUE if the slot was changed</returns>
        public async Task<bool> ChangeSlotAsync(short slot)
        {
            if (handler == null)
                return false;

            if (slot < 0 || slot > 8)
                return false;

            CurrentSlot = Convert.ToByte(slot);
            return await handler.SendHeldItemChange(slot);
        }

        /// <summary>
        /// Update sign text
        /// </summary>
        /// <param name="location">sign location</param>
        /// <param name="line1">text one</param>
        /// <param name="line2">text two</param>
        /// <param name="line3">text three</param>
        /// <param name="line4">text1 four</param>
        public async Task<bool> UpdateSignAsync(Location location, string line1, string line2, string line3, string line4)
        {
            if (handler == null)
                return false;
            // TODO Open sign editor first https://wiki.vg/Protocol#Open_Sign_Editor
            return await handler.SendUpdateSign(location, line1, line2, line3, line4);
        }

        /// <summary>
        /// Select villager trade
        /// </summary>
        /// <param name="selectedSlot">The slot of the trade, starts at 0.</param>
        public async Task<bool> SelectTradeAsync(int selectedSlot)
        {
            if (handler == null)
                return false;
            return await handler.SelectTrade(selectedSlot);
        }

        /// <summary>
        /// Update command block
        /// </summary>
        /// <param name="location">command block location</param>
        /// <param name="command">command</param>
        /// <param name="mode">command block mode</param>
        /// <param name="flags">command block flags</param>
        public async Task<bool> UpdateCommandBlockAsync(Location location, string command, CommandBlockMode mode, CommandBlockFlags flags)
        {
            if (handler == null)
                return false;
            return await handler.UpdateCommandBlock(location, command, mode, flags);
        }

        /// <summary>
        /// Teleport to player in spectator mode
        /// </summary>
        /// <param name="entity">Player to teleport to</param>
        /// Teleporting to other entityies is NOT implemented yet
        public async Task<bool> SpectateAsync(Entity entity)
        {
            if (entity.Type == EntityType.Player)
            {
                return await SpectateByUuidAsync(entity.UUID);
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
        public async Task<bool> SpectateByUuidAsync(Guid UUID)
        {
            if (handler == null)
                return false;

            if (GetGamemode() == 3)
            {
                return await handler.SendSpectate(UUID);
            }
            else
            {
                return false;
            }
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
            botList ??= chatbots;
            foreach (ChatBot bot in botList)
            {
                try
                {
                    action(bot);
                }
                catch (Exception e)
                {
                    if (e is not ThreadAbortException)
                    {
                        // Retrieve parent method name to determine which event caused the exception
                        System.Diagnostics.StackFrame frame = new(1);
                        System.Reflection.MethodBase method = frame.GetMethod()!;
                        string parentMethodName = method.Name;

                        // Display a meaningful error message to help debugging the ChatBot
                        Log.Error(parentMethodName + ": Got error from " + bot.ToString() + ": " + e.ToString());
                    }
                    else throw; // ThreadAbortException should not be caught here as in can happen when disconnecting from server
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
        public async Task OnNetworkPacketAsync(int packetID, byte[] packetData, bool isLogin, bool isInbound)
        {
            if (networkPacketCaptureEnabled)
            {
                await TriggerEvent(McClientEventType.NetworkPacket,
                    new Tuple<int, byte[], bool, bool>(packetID, packetData, isLogin, isInbound));
                DispatchBotEvent(bot => bot.OnNetworkPacket(packetID, new(packetData), isLogin, isInbound));
            }
        }

        /// <summary>
        /// Called when a server was successfully joined
        /// </summary>
        public async Task OnGameJoinedAsync()
        {
            if (handler == null)
                return;

            string? bandString = Config.Main.Advanced.BrandInfo.ToBrandString();
            if (!string.IsNullOrWhiteSpace(bandString))
                await handler.SendBrandInfo(bandString.Trim());

            if (Config.MCSettings.Enabled)
                await handler.SendClientSettings(
                    Config.MCSettings.Locale,
                    Config.MCSettings.RenderDistance,
                    (byte)Config.MCSettings.Difficulty,
                    (byte)Config.MCSettings.ChatMode,
                    Config.MCSettings.ChatColors,
                    Config.MCSettings.Skin.GetByte(),
                    (byte)Config.MCSettings.MainHand);


            if (inventoryHandlingRequested)
            {
                inventoryHandlingRequested = false;
                inventoryHandlingEnabled = true;
                Log.Info(Translations.extra_inventory_enabled);
            }

            await TriggerEvent(McClientEventType.GameJoin, null);
            DispatchBotEvent(bot => bot.AfterGameJoined());

            await ConsoleIO.InitCommandList(dispatcher);
        }

        /// <summary>
        /// Called when the player respawns, which happens on login, respawn and world change.
        /// </summary>
        public async Task OnRespawnAsync()
        {
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

            await TriggerEvent(McClientEventType.Respawn, null);
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
            return (!ClientIsMoving() || path == null) ? Location.Zero : path.Last();
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
                    // https://minecraft.fandom.com/wiki/Sneaking#Effects - Sneaking  1.31m/s
                    Config.Main.Advanced.MovementSpeed = 2;
                    break;
                case MovementType.Walk:
                    // https://minecraft.fandom.com/wiki/Walking#Usage - Walking 4.317 m/s
                    Config.Main.Advanced.MovementSpeed = 4;
                    break;
                case MovementType.Sprint:
                    // https://minecraft.fandom.com/wiki/Sprinting#Usage - Sprinting 5.612 m/s
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
        public void UpdateLocation(Location location)
        {
            this.location = location;
            locationReceived = true;
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
            UpdateLocation(location);
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
        public async Task OnTextReceivedAsync(ChatMessage message)
        {
            UpdateKeepAlive();

            List<string> links = new();
            string messageText;

            if (message.isSignedChat)
            {
                if (!Config.Signature.ShowIllegalSignedChat && !message.isSystemChat && !(bool)message.isSignatureLegal!)
                    return;
                messageText = ChatParser.ParseSignedChat(message, links);
            }
            else
            {
                if (message.isJson)
                    messageText = ChatParser.ParseText(message.content, links);
                else
                    messageText = message.content;
            }

            Log.Chat(messageText);

            if (Config.Main.Advanced.ShowChatLinks)
                foreach (string link in links)
                    Log.Chat(string.Format(Translations.mcc_link, link));

            await TriggerEvent(McClientEventType.TextReceive,
                new Tuple<string, string>(messageText, message.content));
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
        public async Task OnInventoryOpenAsync(int inventoryID, Container inventory)
        {
            inventories[inventoryID] = inventory;

            if (inventoryID != 0)
            {
                Log.Info(string.Format(Translations.extra_inventory_open, inventoryID, inventory.Title));
                Log.Info(Translations.extra_inventory_interact);

                await TriggerEvent(McClientEventType.InventoryOpen, inventoryID);
                DispatchBotEvent(bot => bot.OnInventoryOpen(inventoryID));
            }
        }

        /// <summary>
        /// When an inventory is close
        /// </summary>
        /// <param name="inventoryID">Inventory ID</param>
        public async Task OnInventoryCloseAsync(int inventoryID)
        {
            lock (inventoryLock)
            {
                if (inventoryID == 0)
                    inventories[0].Items.Clear(); // Don't delete player inventory
                else
                    inventories.Remove(inventoryID);
            }

            if (inventoryID != 0)
            {
                Log.Info(string.Format(Translations.extra_inventory_close, inventoryID));

                await TriggerEvent(McClientEventType.InventoryClose, inventoryID);
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
        public async Task OnWindowPropertiesAsync(byte inventoryID, short propertyId, short propertyValue)
        {
            if (!inventories.TryGetValue(inventoryID, out Container? inventory))
                return;

            inventory.Properties.Remove(propertyId);

            inventory.Properties.Add(propertyId, propertyValue);

            await TriggerEvent(McClientEventType.InventoryProperties,
                new Tuple<int, int, int>(inventoryID, propertyId, propertyValue));
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

                    lastEnchantment = new()
                    {
                        TopEnchantment = topEnchantment,
                        MiddleEnchantment = middleEnchantment,
                        BottomEnchantment = bottomEnchantment,

                        Seed = inventory.Properties[3],

                        TopEnchantmentLevel = topEnchantmentLevel,
                        MiddleEnchantmentLevel = middleEnchantmentLevel,
                        BottomEnchantmentLevel = bottomEnchantmentLevel,

                        TopEnchantmentLevelRequirement = topEnchantmentLevelRequirement,
                        MiddleEnchantmentLevelRequirement = middleEnchantmentLevelRequirement,
                        BottomEnchantmentLevelRequirement = bottomEnchantmentLevelRequirement
                    };


                    await TriggerEvent(McClientEventType.Enchantments, lastEnchantment);

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
        public async Task OnWindowItemsAsync(byte inventoryID, Dictionary<int, Item> itemList, int stateId)
        {
            if (inventories.TryGetValue(inventoryID, out Container? container))
            {
                container.Items = itemList;
                container.StateID = stateId;
                await TriggerEvent(McClientEventType.InventoryUpdate, inventoryID);
                DispatchBotEvent(bot => bot.OnInventoryUpdate(inventoryID));
            }
        }

        /// <summary>
        /// When a slot is set inside window items
        /// </summary>
        /// <param name="inventoryID">Window ID</param>
        /// <param name="slotID">Slot ID</param>
        /// <param name="item">Item (may be null for empty slot)</param>
        public async Task OnSetSlotAsync(byte inventoryID, short slotID, Item? item, int stateId)
        {
            lock (inventoryLock)
            {
                if (inventories.TryGetValue(inventoryID, out Container? container))
                    container.StateID = stateId;

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
                            inventories[inventoryID].Items.Remove(slotID);
                        else
                            inventories[inventoryID].Items[slotID] = item;
                    }
                }
            }
            await TriggerEvent(McClientEventType.InventoryUpdate, inventoryID);
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
        public async Task OnPlayerJoinAsync(PlayerInfo player)
        {
            //Ignore placeholders eg 0000tab# from TabListPlus
            if (!ChatBot.IsValidName(player.Name))
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

            await TriggerEvent(McClientEventType.PlayerJoin, player);
            DispatchBotEvent(bot => bot.OnPlayerJoin(player.Uuid, player.Name));
        }

        /// <summary>
        /// Triggered when a player has left the game
        /// </summary>
        /// <param name="uuid">UUID of the player</param>
        public async Task OnPlayerLeaveAsync(Guid uuid)
        {
            PlayerInfo? playerInfo = null;

            lock (onlinePlayers)
            {
                if (onlinePlayers.TryGetValue(uuid, out playerInfo))
                    onlinePlayers.Remove(uuid);
            }

            await TriggerEvent(McClientEventType.PlayerLeave,
                new Tuple<Guid, PlayerInfo?>(uuid, playerInfo));
            DispatchBotEvent(bot => bot.OnPlayerLeave(uuid, username));
        }

        // <summary>
        /// This method is called when a player has been killed by another entity
        /// </summary>
        /// <param name="playerEntity">Victim's entity</param>
        /// <param name="killerEntity">Killer's entity</param>
        public async Task OnPlayerKilledAsync(int killerEntityId, string chatMessage)
        {
            if (!entities.TryGetValue(killerEntityId, out Entity? killer))
                return;

            await TriggerEvent(McClientEventType.PlayerKilled,
                new Tuple<Entity, string>(killer, chatMessage));
            DispatchBotEvent(bot => bot.OnKilled(killer, chatMessage));
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

            if (registeredBotPluginChannels.TryGetValue(channel, out List<ChatBot>? channelList))
            {
                DispatchBotEvent(bot => bot.OnPluginMessage(channel, data), channelList);
            }
        }

        /// <summary>
        /// Called when an entity spawned
        /// </summary>
        public async Task OnSpawnEntity(Entity entity)
        {
            // The entity should not already exist, but if it does, let's consider the previous one is being destroyed
            if (entities.ContainsKey(entity.ID))
                await OnDestroyEntities(new[] { entity.ID });

            entities.Add(entity.ID, entity);

            await TriggerEvent(McClientEventType.EntitySpawn, entity);
            DispatchBotEvent(bot => bot.OnEntitySpawn(entity));
        }

        /// <summary>
        /// Called when an entity effects
        /// </summary>
        public async Task OnEntityEffect(int entityid, Effect effect)
        {
            if (!entities.TryGetValue(entityid, out Entity? entity))
                return;

            await TriggerEvent(McClientEventType.EntityEffect,
                new Tuple<Entity, Effect>(entity, effect));

            byte flag = (byte)((effect.IsFromBeacon ? 1 : 0) | (effect.ShowParticles ? 2 : 0) | (effect.ShowIcon ? 2 : 0));
            DispatchBotEvent(bot => bot.OnEntityEffect(entity, effect.Type, effect.EffectLevel - 1, effect.DurationInTick, flag));
        }
        
        /// <summary>
        /// Called when a player spawns or enters the client's render distance
        /// </summary>
        public async Task OnSpawnPlayer(int entityID, Guid uuid, Location location, byte yaw, byte pitch)
        {
            Entity playerEntity;
            if (onlinePlayers.TryGetValue(uuid, out PlayerInfo? player))
            {
                playerEntity = new(entityID, EntityType.Player, location, uuid, player.Name, yaw, pitch);
                player.entity = playerEntity;
            }
            else
                playerEntity = new(entityID, EntityType.Player, location, uuid, null, yaw, pitch);
            await OnSpawnEntity(playerEntity);
        }

        /// <summary>
        /// Called on Entity Equipment
        /// </summary>
        /// <param name="entityid"> Entity ID</param>
        /// <param name="slot"> Equipment slot. 0: main hand, 1: off hand, 2-5: armor slot (2: boots, 3: leggings, 4: chestplate, 5: helmet)</param>
        /// <param name="item"> Item)</param>
        public async Task OnEntityEquipment(int entityid, int slot, Item? item)
        {
            if (entities.TryGetValue(entityid, out Entity? entity))
            {
                entity.Equipment.Remove(slot);
                if (item != null)
                    entity.Equipment[slot] = item;

                await TriggerEvent(McClientEventType.EntityEquipment,
                    new Tuple<Entity, int, Item?>(entity, slot, item));
                DispatchBotEvent(bot => bot.OnEntityEquipment(entity, slot, item));
            }
        }

        /// <summary>
        /// Called when the Game Mode has been updated for a player
        /// </summary>
        /// <param name="playername">Player Name</param>
        /// <param name="uuid">Player UUID (Empty for initial gamemode on login)</param>
        /// <param name="gamemode">New Game Mode (0: Survival, 1: Creative, 2: Adventure, 3: Spectator).</param>
        public async Task OnGamemodeUpdate(Guid uuid, int gamemode)
        {
            // Initial gamemode on login
            if (uuid == Guid.Empty)
                this.gamemode = gamemode;

            // Further regular gamemode change events
            if (onlinePlayers.TryGetValue(uuid, out PlayerInfo? playerInfo))
            {
                if (playerInfo.Name == username)
                    this.gamemode = gamemode;

                await TriggerEvent(McClientEventType.GamemodeUpdate, 
                    new Tuple<PlayerInfo, int>(playerInfo, gamemode));
                DispatchBotEvent(bot => bot.OnGamemodeUpdate(playerInfo.Name, uuid, gamemode));
            }
        }

        /// <summary>
        /// Called when entities dead/despawn.
        /// </summary>
        public async Task OnDestroyEntities(int[] Entities)
        {
            foreach (int a in Entities)
            {
                if (entities.TryGetValue(a, out Entity? entity))
                {
                    await TriggerEvent(McClientEventType.EntityDespawn, entity);
                    DispatchBotEvent(bot => bot.OnEntityDespawn(entity));
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
        public async Task OnEntityPosition(int EntityID, Double Dx, Double Dy, Double Dz, bool onGround)
        {
            if (entities.TryGetValue(EntityID, out Entity? entity))
            {
                entity.Location.X += Dx;
                entity.Location.Y += Dy;
                entity.Location.Z += Dz;

                await TriggerEvent(McClientEventType.EntityMove, entity);
                DispatchBotEvent(bot => bot.OnEntityMove(entity));
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
        public async Task OnEntityTeleport(int EntityID, Double X, Double Y, Double Z, bool onGround)
        {
            if (entities.TryGetValue(EntityID, out Entity? entity))
            {
                entity.Location = new Location(X, Y, Z);

                await TriggerEvent(McClientEventType.EntityMove, entity);
                DispatchBotEvent(bot => bot.OnEntityMove(entity));
            }
        }

        /// <summary>
        /// Called when received entity properties from server.
        /// </summary>
        /// <param name="EntityID"></param>
        /// <param name="prop"></param>
        public async Task OnEntityProperties(int EntityID, Dictionary<string, double> prop)
        {
            if (EntityID == playerEntityID)
            {
                await TriggerEvent(McClientEventType.PlayerPropertyReceive, prop);
                DispatchBotEvent(bot => bot.OnPlayerProperty(prop));
            }
        }

        /// <summary>
        /// Called when the status of an entity have been changed
        /// </summary>
        /// <param name="entityID">Entity ID</param>
        /// <param name="status">Status ID</param>
        public async Task OnEntityStatus(int entityID, byte status)
        {
            if (entityID == playerEntityID)
            {
                await TriggerEvent(McClientEventType.PlayerStatusUpdate, status);
                DispatchBotEvent(bot => bot.OnPlayerStatus(status));
            }
        }

        /// <summary>
        /// Called when server sent a Time Update packet.
        /// </summary>
        /// <param name="WorldAge"></param>
        /// <param name="TimeOfDay"></param>
        public async Task OnTimeUpdate(long WorldAge, long TimeOfDay)
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
                    await TriggerEvent(McClientEventType.ServerTpsUpdate, tps);
                    DispatchBotEvent(bot => bot.OnServerTpsUpdate(tps));
                }
            }
            else
            {
                lastAge = WorldAge;
                lastTime = DateTime.Now;
            }

            await TriggerEvent(McClientEventType.TimeUpdate,
                new Tuple<long, long>(WorldAge, TimeOfDay));
            DispatchBotEvent(bot => bot.OnTimeUpdate(WorldAge, TimeOfDay));
        }

        /// <summary>
        /// Called when client player's health changed, e.g. getting attack
        /// </summary>
        /// <param name="health">Player current health</param>
        public async Task OnUpdateHealth(float health, int food)
        {
            playerHealth = health;
            playerFoodSaturation = food;

            await TriggerEvent(McClientEventType.HealthUpdate, 
                new Tuple<float, int>(health, food));
            DispatchBotEvent(bot => bot.OnHealthUpdate(health, food));

            if (health <= 0)
            {
                if (Config.Main.Advanced.AutoRespawn)
                {
                    Log.Info(Translations.mcc_player_dead_respawn);
                    respawnTicks = 20;
                }
                else
                {
                    Log.Info(string.Format(Translations.mcc_player_dead, Config.Main.Advanced.InternalCmdChar.ToLogString()));
                }

                await TriggerEvent(McClientEventType.Death, null);
                DispatchBotEvent(bot => bot.OnDeath());
            }
        }

        /// <summary>
        /// Called when experience updates
        /// </summary>
        /// <param name="Experiencebar">Between 0 and 1</param>
        /// <param name="Level">Level</param>
        /// <param name="TotalExperience">Total Experience</param>
        public async Task OnSetExperience(float Experiencebar, int Level, int TotalExperience)
        {
            playerLevel = Level;
            playerTotalExperience = TotalExperience;

            await TriggerEvent(McClientEventType.ExperienceChange,
                new Tuple<float, int, int>(Experiencebar, Level, TotalExperience));
            DispatchBotEvent(bot => bot.OnSetExperience(Experiencebar, Level, TotalExperience));
        }

        /// <summary>
        /// Called when and explosion occurs on the server
        /// </summary>
        /// <param name="location">Explosion location</param>
        /// <param name="strength">Explosion strength</param>
        /// <param name="affectedBlocks">Amount of affected blocks</param>
        public async Task OnExplosion(Location location, float strength, int affectedBlocks)
        {
            await TriggerEvent(McClientEventType.Explosion,
                new Tuple<Location, float, int>(location, strength, affectedBlocks));
            DispatchBotEvent(bot => bot.OnExplosion(location, strength, affectedBlocks));
        }

        /// <summary>
        /// Called when Latency is updated
        /// </summary>
        /// <param name="uuid">player uuid</param>
        /// <param name="latency">Latency</param>
        public async Task OnLatencyUpdate(Guid uuid, int latency)
        {
            if (onlinePlayers.TryGetValue(uuid, out PlayerInfo? player))
            {
                player.Ping = latency;

                await TriggerEvent(McClientEventType.PlayerLatencyUpdate,
                    new Tuple<PlayerInfo, int>(player, latency));

                string playerName = player.Name;
                DispatchBotEvent(bot => bot.OnLatencyUpdate(playerName, uuid, latency));
                if (player.entity != null)
                    DispatchBotEvent(bot => bot.OnLatencyUpdate(player.entity, playerName, uuid, latency));
            }
        }

        /// <summary>
        /// Called when held item change
        /// </summary>
        /// <param name="slot"> item slot</param>
        public async Task OnHeldItemChange(byte slot)
        {
            CurrentSlot = slot;

            await TriggerEvent(McClientEventType.HeldItemChange, slot);
            DispatchBotEvent(bot => bot.OnHeldItemChange(slot));
        }

        /// <summary>
        /// Called when an update of the map is sent by the server, take a look at https://wiki.vg/Protocol#Map_Data for more info on the fields
        /// Map format and colors: https://minecraft.fandom.com/wiki/Map_item_format
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
        public async Task OnMapData(MapData mapData)
        {
            await TriggerEvent(McClientEventType.MapDataReceive, mapData);
            DispatchBotEvent(bot => bot.OnMapData(mapData.MapId,
                                                  mapData.Scale,
                                                  mapData.TrackingPosition,
                                                  mapData.Locked,
                                                  mapData.Icons,
                                                  mapData.ColumnsUpdated,
                                                  mapData.RowsUpdated,
                                                  mapData.MapCoulmnX,
                                                  mapData.MapRowZ,
                                                  mapData.Colors));
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
        public async Task OnTitle(TitlePacket title)
        {
            await TriggerEvent(McClientEventType.TitleReceive, title);
            DispatchBotEvent(bot => bot.OnTitle(title.Action,
                                                title.TitleText,
                                                title.SubtitleText,
                                                title.ActionbarText,
                                                title.FadeIn,
                                                title.Stay,
                                                title.FadeOut,
                                                title.JsonText));
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
        /// Called when the health of an entity changed
        /// </summary>
        /// <param name="entityID">Entity ID</param>
        /// <param name="health">The health of the entity</param>
        public void OnEntityHealth(int entityID, float health)
        {
            if (entities.TryGetValue(entityID, out Entity? entity))
            {
                entity.Health = health;
                DispatchBotEvent(bot => bot.OnEntityHealth(entity, health));
            }
        }

        /// <summary>
        /// Called when the metadata of an entity changed
        /// </summary>
        /// <param name="entityID">Entity ID</param>
        /// <param name="metadata">The metadata of the entity</param>
        public void OnEntityMetadata(int entityID, Dictionary<int, object?> metadata)
        {
            if (entities.TryGetValue(entityID, out Entity? entity))
            {
                entity.Metadata = metadata;
                if (entity.Type.ContainsItem() && metadata.TryGetValue(7, out object? itemObj) && itemObj != null && itemObj.GetType() == typeof(Item))
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
            if (entities.TryGetValue(entityId, out Entity? entity))
            {
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
            if (entities.TryGetValue(entityID, out Entity? entity))
            {
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

        /// <summary>
        /// Send a click container button packet to the server.
        /// Used for Enchanting table, Lectern, stone cutter and loom
        /// </summary>
        /// <param name="windowId">Id of the window being clicked</param>
        /// <param name="buttonId">Id of the clicked button</param>
        /// <returns>True if packet was successfully sent</returns>

        public async Task<bool> ClickContainerButton(int windowId, int buttonId)
        {
            return await handler!.ClickContainerButton(windowId, buttonId);
        }

        #endregion
    }
}
