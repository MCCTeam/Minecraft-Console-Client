//MCCScript 1.0
//using System.Collections.Concurrent
//using System.Collections.Generic
//using System.IO
//using System.Linq
//using System.Net
//using System.Net.Sockets
//using System.Net.WebSockets
//using System.Text
//using System.Text.Json
//using System.Text.Json.Serialization
//using System.Text.RegularExpressions
//using System.Threading
//using System.Threading.Tasks
//using MinecraftClient.CommandHandler
//using MinecraftClient.Inventory
//using MinecraftClient.Mapping
//using MinecraftClient.Scripting
//using MinecraftClient

// IMPORTANT: Change the password below before use!
MCC.LoadBot(new WebSocketBot("127.0.0.1", 8043, "CHANGE_THIS_PASSWORD"));

//MCCScript Extensions

public class WebSocketSession
{
    public string SessionId { get; set; }
    public WebSocket WebSocket { get; }
    public bool IsAuthenticated { get; set; }

    public WebSocketSession(string sessionId, WebSocket webSocket)
    {
        SessionId = sessionId;
        WebSocket = webSocket;
        IsAuthenticated = false;
    }
}

public class WebSocketServer
{
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private readonly ConcurrentDictionary<string, WebSocketSession> _sessions = new();

    public event Action<string, WebSocketSession>? NewSession;
    public event Action<string>? SessionDropped;
    public event Action<string, string>? MessageReceived;

    public IReadOnlyDictionary<string, WebSocketSession> Sessions => _sessions;

    public async Task Start(string ip, int port)
    {
        _cts = new CancellationTokenSource();
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://{ip}:{port}/");
        _listener.Start();

        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var context = await _listener.GetContextAsync().ConfigureAwait(false);

                if (context.Request.IsWebSocketRequest)
                    _ = Task.Run(() => ProcessWebSocketSession(context, _cts.Token));
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
            catch (ObjectDisposedException) { break; }
            catch (HttpListenerException) { break; }
            catch { /* ignore transient errors */ }
        }
    }

    private async Task ProcessWebSocketSession(HttpListenerContext context, CancellationToken ct)
    {
        WebSocketContext wsContext;
        try
        {
            wsContext = await context.AcceptWebSocketAsync(null).ConfigureAwait(false);
        }
        catch { return; }

        var ws = wsContext.WebSocket;
        var sessionId = Guid.NewGuid().ToString("D");
        var session = new WebSocketSession(sessionId, ws);

        _sessions.TryAdd(sessionId, session);
        NewSession?.Invoke(sessionId, session);

        var buffer = new byte[4096];
        var messageBuffer = new List<byte>();

        try
        {
            while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct).ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                messageBuffer.AddRange(new ArraySegment<byte>(buffer, 0, result.Count));

                if (result.EndOfMessage)
                {
                    var message = Encoding.UTF8.GetString(messageBuffer.ToArray());
                    messageBuffer.Clear();
                    MessageReceived?.Invoke(sessionId, message);
                }
            }
        }
        catch { /* connection dropped */ }
        finally
        {
            _sessions.TryRemove(sessionId, out _);
            SessionDropped?.Invoke(sessionId);

            if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
            {
                try
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Session ended", CancellationToken.None)
                        .ConfigureAwait(false);
                }
                catch { /* best effort */ }
            }

            ws.Dispose();
        }
    }

    public bool RenameSession(string oldId, string newId)
    {
        if (!_sessions.TryRemove(oldId, out var session))
            return false;

        if (_sessions.ContainsKey(newId))
        {
            _sessions.TryAdd(oldId, session);
            return false;
        }

        session.SessionId = newId;
        _sessions.TryAdd(newId, session);
        return true;
    }

    public async Task SendToSession(string sessionId, string message)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return;

        if (session.WebSocket.State != WebSocketState.Open)
            return;

        var bytes = Encoding.UTF8.GetBytes(message);

        try
        {
            await session.WebSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None).ConfigureAwait(false);
        }
        catch { /* send failed, session will be cleaned up */ }
    }

    public void Stop()
    {
        _cts?.Cancel();

        foreach (var kvp in _sessions)
        {
            try
            {
                var ws = kvp.Value.WebSocket;
                if (ws.State == WebSocketState.Open)
                    ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server stopping", CancellationToken.None)
                        .GetAwaiter().GetResult();
                ws.Dispose();
            }
            catch { /* best effort */ }
        }

        _sessions.Clear();

        try { _listener?.Stop(); } catch { }
        try { _listener?.Close(); } catch { }
    }
}

public class WebSocketBot : ChatBot
{
    private readonly string _ip;
    private readonly int _port;
    private readonly string _password;
    private readonly bool _debugMode;

    private WebSocketServer? _server;
    private JsonSerializerOptions _jsonOptions = null!;
    private readonly List<string> _waitingEvents = new();
    private bool _gameJoined;

    private static readonly Regex Ipv4Regex = new(
        @"^((25[0-5]|(2[0-4]|1\d|[1-9]|)\d)\.){3}(25[0-5]|(2[0-4]|1\d|[1-9]|)\d)$",
        RegexOptions.Compiled);

    public WebSocketBot(string ip, int port, string password, bool debugMode = false)
    {
        if (!Ipv4Regex.IsMatch(ip) && ip != "+" && ip != "*")
            throw new ArgumentException($"Invalid IP address: {ip}");

        if (port is < 1 or > 65535)
            throw new ArgumentException($"Invalid port: {port}. Must be between 1 and 65535.");

        _ip = ip;
        _port = port;
        _password = password;
        _debugMode = debugMode;
    }

    public override void Initialize()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            IncludeFields = true,
            Converters = { new JsonStringEnumConverter() }
        };

        _server = new WebSocketServer();
        _server.NewSession += OnNewSession;
        _server.SessionDropped += OnSessionDropped;
        _server.MessageReceived += OnMessageReceived;

        _ = Task.Run(async () =>
        {
            try
            {
                await _server.Start(_ip, _port);
            }
            catch (Exception ex)
            {
                LogToConsole($"[WebSocketBot] Server failed to start: {ex.Message}");
            }
        });

        LogToConsole($"[WebSocketBot] Starting on {_ip}:{_port}");
    }

    public override void AfterGameJoined()
    {
        _gameJoined = true;
        _waitingEvents.Add("OnGameJoined");
    }

    public override void Update()
    {
        if (_waitingEvents.Count > 0)
        {
            var events = _waitingEvents.ToList();
            _waitingEvents.Clear();

            foreach (var evt in events)
                BroadcastEvent(evt, "N/A");
        }
    }

    public override void OnUnload()
    {
        BroadcastEvent("OnWsConnectionClose", "N/A");
        _server?.Stop();
    }

    // --- Event Overrides ---

    public override void GetText(string text)
    {
        text = GetVerbatim(text);
        string message = "", username = "";

        if (IsPrivateMessage(text, ref message, ref username))
            BroadcastEvent("OnChatPrivate", SerializeData(new { sender = username, message, rawText = text }));
        else if (IsChatMessage(text, ref message, ref username))
            BroadcastEvent("OnChatPublic", SerializeData(new { sender = username, message, rawText = text }));

        string tpSender = "";
        if (IsTeleportRequest(text, ref tpSender))
            BroadcastEvent("OnTeleportRequest", SerializeData(new { sender = tpSender, rawText = text }));
    }

    public override void GetText(string text, string? json)
    {
        BroadcastEvent("OnChatRaw", SerializeData(new { text, json }));
    }

    public override bool OnDisconnect(DisconnectReason reason, string message)
    {
        BroadcastEvent("OnDisconnect", SerializeData(new { reason = reason.ToString(), message }));
        return false;
    }

    public override void OnBlockBreakAnimation(Entity entity, Location location, byte stage)
    {
        BroadcastEvent("OnBlockBreakAnimation", SerializeData(new { entity, location, stage }));
    }

    public override void OnEntityAnimation(Entity entity, byte animation)
    {
        BroadcastEvent("OnEntityAnimation", SerializeData(new { entity, animation }));
    }

    public override void OnPlayerProperty(Dictionary<string, double> prop)
    {
        BroadcastEvent("OnPlayerProperty", SerializeData(prop));
    }

    public override void OnServerTpsUpdate(double tps)
    {
        BroadcastEvent("OnServerTpsUpdate", SerializeData(new { tps }));
    }

    public override void OnTimeUpdate(long worldAge, long timeOfDay)
    {
        BroadcastEvent("OnTimeUpdate", SerializeData(new { worldAge, timeOfDay }));
    }

    public override void OnEntityMove(Entity entity)
    {
        BroadcastEvent("OnEntityMove", SerializeData(entity));
    }

    public override void OnInternalCommand(string commandName, string commandParams, CmdResult result)
    {
        BroadcastEvent("OnInternalCommand", SerializeData(new
        {
            commandName,
            commandParams,
            result = new { status = result.status.ToString(), result = result.result }
        }));
    }

    public override void OnEntitySpawn(Entity entity)
    {
        BroadcastEvent("OnEntitySpawn", SerializeData(entity));
    }

    public override void OnEntityDespawn(Entity entity)
    {
        BroadcastEvent("OnEntityDespawn", SerializeData(entity));
    }

    public override void OnHeldItemChange(byte slot)
    {
        BroadcastEvent("OnHeldItemChange", SerializeData(new { slot }));
    }

    public override void OnHealthUpdate(float health, int food)
    {
        BroadcastEvent("OnHealthUpdate", SerializeData(new { health, food }));
    }

    public override void OnExplosion(Location explode, float strength, int recordcount)
    {
        BroadcastEvent("OnExplosion", SerializeData(new { location = explode, strength, recordcount }));
    }

    public override void OnSetExperience(float experienceBar, int level, int totalExperience)
    {
        BroadcastEvent("OnSetExperience", SerializeData(new { experienceBar, level, totalExperience }));
    }

    public override void OnGamemodeUpdate(string playerName, Guid uuid, int gamemode)
    {
        BroadcastEvent("OnGamemodeUpdate", SerializeData(new { playerName, uuid, gamemode }));
    }

    public override void OnLatencyUpdate(string playerName, Guid uuid, int latency)
    {
        BroadcastEvent("OnLatencyUpdate", SerializeData(new { playerName, uuid, latency }));
    }

    public override void OnMapData(int mapId, byte scale, bool trackingPosition, bool locked,
        List<MapIcon> icons, byte columnsUpdated, byte rowsUpdated, byte mapColumnX,
        byte mapRowZ, byte[]? colors)
    {
        BroadcastEvent("OnMapData", SerializeData(new
        {
            mapId, scale, trackingPosition, locked, icons,
            columnsUpdated, rowsUpdated, mapColumnX, mapRowZ,
            colors = colors != null ? Convert.ToBase64String(colors) : null
        }));
    }

    public override void OnTradeList(int windowId, List<VillagerTrade> trades, VillagerInfo villagerInfo)
    {
        BroadcastEvent("OnTradeList", SerializeData(new { windowId, trades, villagerInfo }));
    }

    public override void OnTitle(int action, string titleText, string subtitleText,
        string actionBarText, int fadeIn, int stay, int fadeOut, string json)
    {
        BroadcastEvent("OnTitle", SerializeData(new
        {
            action, titleText, subtitleText, actionBarText,
            fadeIn, stay, fadeOut, json
        }));
    }

    public override void OnEntityEquipment(Entity entity, int slot, Item? item)
    {
        BroadcastEvent("OnEntityEquipment", SerializeData(new { entity, slot, item }));
    }

    public override void OnEntityEffect(Entity entity, Effects effect, int amplifier, int duration, byte flags)
    {
        BroadcastEvent("OnEntityEffect", SerializeData(new
        {
            entity, effect = effect.ToString(), amplifier, duration, flags
        }));
    }

    public override void OnScoreboardObjective(string objectiveName, byte mode,
        string objectiveValue, int type, string json, int numberFormat)
    {
        BroadcastEvent("OnScoreboardObjective", SerializeData(new
        {
            objectiveName, mode, objectiveValue, type, json, numberFormat
        }));
    }

    public override void OnUpdateScore(string entityName, int action, string objectiveName,
        string objectiveDisplayName, int value, int numberFormat)
    {
        BroadcastEvent("OnUpdateScore", SerializeData(new
        {
            entityName, action, objectiveName, objectiveDisplayName, value, numberFormat
        }));
    }

    public override void OnInventoryUpdate(int inventoryId)
    {
        BroadcastEvent("OnInventoryUpdate", SerializeData(new { inventoryId }));
    }

    public override void OnInventoryOpen(int inventoryId)
    {
        BroadcastEvent("OnInventoryOpen", SerializeData(new { inventoryId }));
    }

    public override void OnInventoryClose(int inventoryId)
    {
        BroadcastEvent("OnInventoryClose", SerializeData(new { inventoryId }));
    }

    public override void OnPlayerJoin(Guid uuid, string name)
    {
        BroadcastEvent("OnPlayerJoin", SerializeData(new { uuid, name }));
    }

    public override void OnPlayerLeave(Guid uuid, string? name)
    {
        BroadcastEvent("OnPlayerLeave", SerializeData(new { uuid, name }));
    }

    public override void OnDeath()
    {
        BroadcastEvent("OnDeath", "N/A");
    }

    public override void OnRespawn()
    {
        BroadcastEvent("OnRespawn", "N/A");
    }

    public override void OnEntityHealth(Entity entity, float health)
    {
        BroadcastEvent("OnEntityHealth", SerializeData(new { entity, health }));
    }

    public override void OnEntityMetadata(Entity entity, Dictionary<int, object?> metadata)
    {
        BroadcastEvent("OnEntityMetadata", SerializeData(new { entity, metadata }));
    }

    public override void OnPlayerStatus(byte statusId)
    {
        BroadcastEvent("OnPlayerStatus", SerializeData(new { statusId }));
    }

    public override void OnNetworkPacket(int packetID, List<byte> packetData, bool isLogin, bool isInbound)
    {
        BroadcastEvent("OnNetworkPacket", SerializeData(new
        {
            packetID,
            data = Convert.ToBase64String(packetData.ToArray()),
            isLogin,
            isInbound
        }));
    }

    // --- Serialization helpers ---

    private string SerializeData(object data)
    {
        return JsonSerializer.Serialize(data, _jsonOptions);
    }

    private void BroadcastEvent(string eventName, string data)
    {
        if (_server == null) return;

        var envelope = new Dictionary<string, string> { ["event"] = eventName, ["data"] = data };
        var json = JsonSerializer.Serialize(envelope);

        foreach (var kvp in _server.Sessions)
        {
            if (!kvp.Value.IsAuthenticated) continue;
            _ = _server.SendToSession(kvp.Key, json);
        }
    }

    private void SendSessionEvent(string sessionId, string eventName, string data)
    {
        if (_server == null) return;

        var envelope = new Dictionary<string, string> { ["event"] = eventName, ["data"] = data };
        var json = JsonSerializer.Serialize(envelope);

        _ = _server.SendToSession(sessionId, json);
    }

    // --- Session event handlers ---

    private void OnNewSession(string sessionId, WebSocketSession session)
    {
        if (_debugMode)
            LogToConsole($"[WebSocketBot] New session: {sessionId}");
    }

    private void OnSessionDropped(string sessionId)
    {
        if (_debugMode)
            LogToConsole($"[WebSocketBot] Session dropped: {sessionId}");
    }

    private void OnMessageReceived(string sessionId, string message)
    {
        if (_debugMode)
            LogToConsole($"[WebSocketBot] [{sessionId}] Received: {message}");

        if (_server == null || !_server.Sessions.TryGetValue(sessionId, out var session))
            return;

        try
        {
            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;

            if (root.TryGetProperty("command", out var commandElement))
            {
                var command = commandElement.GetString() ?? "";
                var requestId = root.TryGetProperty("requestId", out var rid) ? rid.GetString() ?? "" : "";

                var parameters = new List<object?>();
                if (root.TryGetProperty("parameters", out var paramsElement) &&
                    paramsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var p in paramsElement.EnumerateArray())
                    {
                        parameters.Add(p.ValueKind switch
                        {
                            JsonValueKind.String => p.GetString(),
                            JsonValueKind.Number => p.TryGetInt64(out var l) ? (object)l : p.GetDouble(),
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            JsonValueKind.Null => null,
                            _ => p.GetRawText()
                        });
                    }
                }

                HandleCommand(sessionId, session, command, requestId, parameters);
                return;
            }
        }
        catch
        {
            // Not valid JSON, treat as plain text
        }

        HandlePlainText(sessionId, session, message);
    }

    private void HandlePlainText(string sessionId, WebSocketSession session, string text)
    {
        if (!session.IsAuthenticated)
        {
            SendSessionEvent(sessionId, "OnWsCommandResponse",
                SerializeData(new { success = false, message = "Not authenticated", requestId = "" }));
            return;
        }

        if (text.StartsWith('/'))
        {
            var cmd = text[1..];
            var result = new CmdResult();
            PerformInternalCommand("send " + cmd, ref result);
            SendSessionEvent(sessionId, "OnMccCommandResponse",
                SerializeData(new { command = cmd, status = result.status.ToString(), result = result.result ?? "" }));
        }
        else
        {
            SendText(text);
        }
    }

    // --- Command processing ---

    private void HandleCommand(string sessionId, WebSocketSession session, string command,
        string requestId, List<object?> parameters)
    {
        // Protocol commands available without auth
        switch (command)
        {
            case "Authenticate":
                HandleAuthenticate(sessionId, session, requestId, parameters);
                return;
            case "ChangeSessionId":
                HandleChangeSessionId(sessionId, session, requestId, parameters);
                return;
        }

        if (!session.IsAuthenticated)
        {
            SendCommandResponse(sessionId, requestId, false, "Not authenticated");
            return;
        }

        try
        {
            switch (command)
            {
                case "LogToConsole":
                    LogToConsole(GetParam<string>(parameters, 0));
                    SendCommandResponse(sessionId, requestId, true);
                    break;

                case "LogDebugToConsole":
                    LogDebugToConsole(GetParam<string>(parameters, 0));
                    SendCommandResponse(sessionId, requestId, true);
                    break;

                case "LogToConsoleTranslated":
                    LogToConsoleTranslated(GetParam<string>(parameters, 0));
                    SendCommandResponse(sessionId, requestId, true);
                    break;

                case "LogDebugToConsoleTranslated":
                    LogDebugToConsoleTranslated(GetParam<string>(parameters, 0));
                    SendCommandResponse(sessionId, requestId, true);
                    break;

                case "ReconnectToTheServer":
                {
                    var extra = parameters.Count > 0 ? Convert.ToInt32(parameters[0]) : 3;
                    var delay = parameters.Count > 1 ? Convert.ToInt32(parameters[1]) : 0;
                    ReconnectToTheServer(extra, delay);
                    SendCommandResponse(sessionId, requestId, true);
                    break;
                }

                case "DisconnectAndExit":
                    SendCommandResponse(sessionId, requestId, true);
                    DisconnectAndExit();
                    break;

                case "SendPrivateMessage":
                    SendPrivateMessage(GetParam<string>(parameters, 0), GetParam<string>(parameters, 1));
                    SendCommandResponse(sessionId, requestId, true);
                    break;

                case "RunScript":
                    RunScript(GetParam<string>(parameters, 0));
                    SendCommandResponse(sessionId, requestId, true);
                    break;

                case "GetTerrainEnabled":
                    SendCommandResponse(sessionId, requestId, true, SerializeData(new { enabled = GetTerrainEnabled() }));
                    break;

                case "SetTerrainEnabled":
                    SetTerrainEnabled(GetParam<bool>(parameters, 0));
                    SendCommandResponse(sessionId, requestId, true);
                    break;

                case "GetEntityHandlingEnabled":
                    SendCommandResponse(sessionId, requestId, true,
                        SerializeData(new { enabled = GetEntityHandlingEnabled() }));
                    break;

                case "Sneak":
                {
                    var on = GetParam<bool>(parameters, 0);
                    var result = Sneak(on);
                    SendCommandResponse(sessionId, requestId, result);
                    break;
                }

                case "SendEntityAction":
                {
                    var actionType = ParseEnum<MinecraftClient.Protocol.EntityActionType>(parameters[0]);
                    var result = SendEntityAction(actionType);
                    SendCommandResponse(sessionId, requestId, result);
                    break;
                }

                case "DigBlock":
                {
                    var x = Convert.ToDouble(parameters[0]);
                    var y = Convert.ToDouble(parameters[1]);
                    var z = Convert.ToDouble(parameters[2]);
                    var direction = parameters.Count > 3 ? ParseEnum<Direction>(parameters[3]) : Direction.Down;
                    var loc = new Location(x, y, z);

                    if (!GetTerrainEnabled())
                    {
                        SendCommandResponse(sessionId, requestId, false, "Terrain not enabled");
                        break;
                    }

                    var current = GetCurrentLocation();
                    if (current.Distance(loc) > 6.0)
                    {
                        SendCommandResponse(sessionId, requestId, false, "Block too far away (max 6 blocks)");
                        break;
                    }

                    var world = GetWorld();
                    var block = world.GetBlock(loc);
                    if (block.Type == Material.Air)
                    {
                        SendCommandResponse(sessionId, requestId, false, "Block is air");
                        break;
                    }

                    var digResult = DigBlock(loc, direction);
                    SendCommandResponse(sessionId, requestId, digResult);
                    break;
                }

                case "SetSlot":
                {
                    var slot = Convert.ToInt32(parameters[0]);
                    SetSlot(slot);
                    SendCommandResponse(sessionId, requestId, true);
                    break;
                }

                case "GetWorld":
                {
                    if (!GetTerrainEnabled())
                    {
                        SendCommandResponse(sessionId, requestId, false, "Terrain not enabled");
                        break;
                    }
                    // Return basic world info rather than full world data
                    SendCommandResponse(sessionId, requestId, true, SerializeData(new { available = true }));
                    break;
                }

                case "GetEntities":
                {
                    if (!GetEntityHandlingEnabled())
                    {
                        SendCommandResponse(sessionId, requestId, false, "Entity handling not enabled");
                        break;
                    }
                    var entities = GetEntities();
                    SendCommandResponse(sessionId, requestId, true, SerializeData(entities));
                    break;
                }

                case "GetPlayersLatency":
                {
                    var latency = GetPlayersLatency();
                    SendCommandResponse(sessionId, requestId, true, SerializeData(latency));
                    break;
                }

                case "GetCurrentLocation":
                    SendCommandResponse(sessionId, requestId, true, SerializeData(GetCurrentLocation()));
                    break;

                case "MoveToLocation":
                {
                    var x = Convert.ToDouble(parameters[0]);
                    var y = Convert.ToDouble(parameters[1]);
                    var z = Convert.ToDouble(parameters[2]);
                    var allowUnsafe = parameters.Count > 3 && GetParam<bool>(parameters, 3);
                    var allowDirectTp = parameters.Count > 4 && GetParam<bool>(parameters, 4);
                    var maxOffset = parameters.Count > 5 ? Convert.ToInt32(parameters[5]) : 0;
                    var minOffset = parameters.Count > 6 ? Convert.ToInt32(parameters[6]) : 0;
                    var result = MoveToLocation(new Location(x, y, z), allowUnsafe, allowDirectTp, maxOffset, minOffset);
                    SendCommandResponse(sessionId, requestId, result);
                    break;
                }

                case "ClientIsMoving":
                    SendCommandResponse(sessionId, requestId, true,
                        SerializeData(new { moving = ClientIsMoving() }));
                    break;

                case "LookAtLocation":
                {
                    var x = Convert.ToDouble(parameters[0]);
                    var y = Convert.ToDouble(parameters[1]);
                    var z = Convert.ToDouble(parameters[2]);
                    LookAtLocation(new Location(x, y, z));
                    SendCommandResponse(sessionId, requestId, true);
                    break;
                }

                case "GetTimestamp":
                    SendCommandResponse(sessionId, requestId, true,
                        SerializeData(new { timestamp = GetTimestamp() }));
                    break;

                case "GetServerPort":
                    SendCommandResponse(sessionId, requestId, true,
                        SerializeData(new { port = GetServerPort() }));
                    break;

                case "GetServerHost":
                    SendCommandResponse(sessionId, requestId, true,
                        SerializeData(new { host = GetServerHost() }));
                    break;

                case "GetUsername":
                    SendCommandResponse(sessionId, requestId, true,
                        SerializeData(new { username = GetUsername() }));
                    break;

                case "GetGamemode":
                    SendCommandResponse(sessionId, requestId, true,
                        SerializeData(new { gamemode = GetGamemode() }));
                    break;

                case "GetYaw":
                    SendCommandResponse(sessionId, requestId, true,
                        SerializeData(new { yaw = GetYaw() }));
                    break;

                case "GetPitch":
                    SendCommandResponse(sessionId, requestId, true,
                        SerializeData(new { pitch = GetPitch() }));
                    break;

                case "GetUserUUID":
                    SendCommandResponse(sessionId, requestId, true,
                        SerializeData(new { uuid = GetUserUUID() }));
                    break;

                case "GetOnlinePlayers":
                    SendCommandResponse(sessionId, requestId, true,
                        SerializeData(GetOnlinePlayers()));
                    break;

                case "GetOnlinePlayersWithUUID":
                    SendCommandResponse(sessionId, requestId, true,
                        SerializeData(GetOnlinePlayersWithUUID()));
                    break;

                case "GetServerTPS":
                    SendCommandResponse(sessionId, requestId, true,
                        SerializeData(new { tps = GetServerTPS() }));
                    break;

                case "InteractEntity":
                {
                    var entityId = Convert.ToInt32(parameters[0]);
                    var interactType = ParseEnum<InteractType>(parameters[1]);
                    var hand = parameters.Count > 2 ? ParseEnum<Hand>(parameters[2]) : Hand.MainHand;
                    var result = InteractEntity(entityId, interactType, hand);
                    SendCommandResponse(sessionId, requestId, result);
                    break;
                }

                case "CreativeGive":
                {
                    var slot = Convert.ToInt32(parameters[0]);
                    var itemType = ParseEnum<ItemType>(parameters[1]);
                    var count = Convert.ToInt32(parameters[2]);
                    var result = CreativeGive(slot, itemType, count);
                    SendCommandResponse(sessionId, requestId, result);
                    break;
                }

                case "CreativeDelete":
                {
                    var slot = Convert.ToInt32(parameters[0]);
                    var result = CreativeDelete(slot);
                    SendCommandResponse(sessionId, requestId, result);
                    break;
                }

                case "SendAnimation":
                {
                    var hand = parameters.Count > 0 ? ParseEnum<Hand>(parameters[0]) : Hand.MainHand;
                    var result = SendAnimation(hand);
                    SendCommandResponse(sessionId, requestId, result);
                    break;
                }

                case "SendPlaceBlock":
                {
                    var x = Convert.ToDouble(parameters[0]);
                    var y = Convert.ToDouble(parameters[1]);
                    var z = Convert.ToDouble(parameters[2]);
                    var direction = ParseEnum<Direction>(parameters[3]);
                    var hand = parameters.Count > 4 ? ParseEnum<Hand>(parameters[4]) : Hand.MainHand;
                    var result = SendPlaceBlock(new Location(x, y, z), direction, hand);
                    SendCommandResponse(sessionId, requestId, result);
                    break;
                }

                case "UseItemInHand":
                {
                    var result = UseItemInHand();
                    SendCommandResponse(sessionId, requestId, result);
                    break;
                }

                case "GetInventoryEnabled":
                    SendCommandResponse(sessionId, requestId, true,
                        SerializeData(new { enabled = GetInventoryEnabled() }));
                    break;

                case "GetPlayerInventory":
                {
                    if (!GetInventoryEnabled())
                    {
                        SendCommandResponse(sessionId, requestId, false, "Inventory not enabled");
                        break;
                    }
                    var inv = GetPlayerInventory();
                    SendCommandResponse(sessionId, requestId, true, SerializeData(inv));
                    break;
                }

                case "GetInventories":
                {
                    if (!GetInventoryEnabled())
                    {
                        SendCommandResponse(sessionId, requestId, false, "Inventory not enabled");
                        break;
                    }
                    var inventories = GetInventories();
                    SendCommandResponse(sessionId, requestId, true, SerializeData(inventories));
                    break;
                }

                case "WindowAction":
                {
                    var inventoryId = Convert.ToInt32(parameters[0]);
                    var slot = Convert.ToInt32(parameters[1]);
                    var actionType = ParseEnum<WindowActionType>(parameters[2]);
                    var result = WindowAction(inventoryId, slot, actionType);
                    SendCommandResponse(sessionId, requestId, result);
                    break;
                }

                case "ChangeSlot":
                {
                    var slot = Convert.ToInt16(parameters[0]);
                    var result = ChangeSlot(slot);
                    SendCommandResponse(sessionId, requestId, result);
                    break;
                }

                case "GetCurrentSlot":
                    SendCommandResponse(sessionId, requestId, true,
                        SerializeData(new { slot = GetCurrentSlot() }));
                    break;

                case "ClearInventories":
                {
                    var result = ClearInventories();
                    SendCommandResponse(sessionId, requestId, result);
                    break;
                }

                case "UpdateSign":
                {
                    var x = Convert.ToDouble(parameters[0]);
                    var y = Convert.ToDouble(parameters[1]);
                    var z = Convert.ToDouble(parameters[2]);
                    var line1 = GetParam<string>(parameters, 3);
                    var line2 = GetParam<string>(parameters, 4);
                    var line3 = GetParam<string>(parameters, 5);
                    var line4 = GetParam<string>(parameters, 6);
                    var result = UpdateSign(new Location(x, y, z), line1, line2, line3, line4);
                    SendCommandResponse(sessionId, requestId, result);
                    break;
                }

                case "SelectTrade":
                {
                    var selectedSlot = Convert.ToInt32(parameters[0]);
                    var result = SelectTrade(selectedSlot);
                    SendCommandResponse(sessionId, requestId, result);
                    break;
                }

                case "UpdateCommandBlock":
                {
                    var x = Convert.ToDouble(parameters[0]);
                    var y = Convert.ToDouble(parameters[1]);
                    var z = Convert.ToDouble(parameters[2]);
                    var cmd = GetParam<string>(parameters, 3);
                    var mode = ParseEnum<CommandBlockMode>(parameters[4]);
                    var flags = ParseEnum<CommandBlockFlags>(parameters[5]);
                    var result = UpdateCommandBlock(new Location(x, y, z), cmd, mode, flags);
                    SendCommandResponse(sessionId, requestId, result);
                    break;
                }

                case "CloseInventory":
                {
                    var inventoryId = Convert.ToInt32(parameters[0]);
                    var result = CloseInventory(inventoryId);
                    SendCommandResponse(sessionId, requestId, result);
                    break;
                }

                case "GetMaxChatMessageLength":
                    SendCommandResponse(sessionId, requestId, true,
                        SerializeData(new { length = GetMaxChatMessageLength() }));
                    break;

                case "Respawn":
                {
                    var result = Respawn();
                    SendCommandResponse(sessionId, requestId, result);
                    break;
                }

                case "GetProtocolVersion":
                    SendCommandResponse(sessionId, requestId, true,
                        SerializeData(new { protocolVersion = GetProtocolVersion() }));
                    break;

                case "GetItemTypeMappings":
                {
                    var mappings = new Dictionary<string, int>();
                    foreach (ItemType value in Enum.GetValues(typeof(ItemType)))
                        mappings[value.ToString()] = (int)value;
                    SendCommandResponse(sessionId, requestId, true, SerializeData(mappings));
                    break;
                }

                case "GetEntityTypeMappings":
                {
                    var mappings = new Dictionary<string, int>();
                    foreach (EntityType value in Enum.GetValues(typeof(EntityType)))
                        mappings[value.ToString()] = (int)value;
                    SendCommandResponse(sessionId, requestId, true, SerializeData(mappings));
                    break;
                }

                default:
                    SendCommandResponse(sessionId, requestId, false, $"Unknown command: {command}");
                    break;
            }
        }
        catch (Exception ex)
        {
            SendCommandResponse(sessionId, requestId, false, $"Error: {ex.Message}");
        }
    }

    private void HandleAuthenticate(string sessionId, WebSocketSession session, string requestId,
        List<object?> parameters)
    {
        if (parameters.Count == 0)
        {
            SendCommandResponse(sessionId, requestId, false, "Invalid password");
            return;
        }

        var provided = GetParam<string>(parameters, 0);
        var expected = _password;

        // Fixed-time comparison to prevent timing attacks
        var diff = provided.Length ^ expected.Length;
        for (int i = 0; i < expected.Length; i++)
            diff |= expected[i] ^ (i < provided.Length ? provided[i] : 0xFF);

        if (diff != 0)
        {
            SendCommandResponse(sessionId, requestId, false, "Invalid password");
            return;
        }

        session.IsAuthenticated = true;
        SendCommandResponse(sessionId, requestId, true, "Authenticated");
    }

    private void HandleChangeSessionId(string sessionId, WebSocketSession session, string requestId,
        List<object?> parameters)
    {
        if (parameters.Count == 0)
        {
            SendCommandResponse(sessionId, requestId, false, "New session ID required");
            return;
        }

        var newId = GetParam<string>(parameters, 0);
        if (string.IsNullOrWhiteSpace(newId))
        {
            SendCommandResponse(sessionId, requestId, false, "New session ID cannot be empty");
            return;
        }

        if (_server == null)
        {
            SendCommandResponse(sessionId, requestId, false, "Server not initialized");
            return;
        }

        if (_server.RenameSession(sessionId, newId))
            SendCommandResponse(newId, requestId, true, $"Session renamed to {newId}");
        else
            SendCommandResponse(sessionId, requestId, false, $"Failed to rename session to {newId}");
    }

    // --- Response helpers ---

    private void SendCommandResponse(string sessionId, string requestId, bool success, string? message = null)
    {
        var response = new Dictionary<string, object?>
        {
            ["success"] = success,
            ["requestId"] = requestId
        };

        if (message != null)
            response["message"] = message;

        SendSessionEvent(sessionId, "OnWsCommandResponse", SerializeData(response));
    }

    // --- Parsing helpers ---

    private static T ParseEnum<T>(object? param) where T : struct, Enum
    {
        if (param is string s)
        {
            if (Enum.TryParse<T>(s, ignoreCase: true, out var parsed))
                return parsed;
        }

        try
        {
            var numeric = Convert.ToInt32(param);
            return (T)Enum.ToObject(typeof(T), numeric);
        }
        catch
        {
            throw new ArgumentException($"Cannot parse '{param}' as {typeof(T).Name}");
        }
    }

    private static T GetParam<T>(List<object?> parameters, int index)
    {
        if (index >= parameters.Count)
            throw new ArgumentException($"Missing parameter at index {index}");

        var val = parameters[index];

        if (val is T typed)
            return typed;

        if (typeof(T) == typeof(bool) && val != null)
            return (T)(object)Convert.ToBoolean(val);

        if (typeof(T) == typeof(string))
            return (T)(object)(val?.ToString() ?? "");

        return (T)Convert.ChangeType(val!, typeof(T));
    }
}
