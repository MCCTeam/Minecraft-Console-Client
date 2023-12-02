using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MinecraftClient.CommandHandler;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;
using MinecraftClient.Scripting;
using Newtonsoft.Json;
using Tomlet.Attributes;

namespace MinecraftClient.ChatBots;

internal class SessionEventArgs : EventArgs
{
    public string SessionId { get; }

    public SessionEventArgs(string sessionId)
    {
        SessionId = sessionId;
    }
}

internal class MessageReceivedEventArgs : EventArgs
{
    public string SessionId { get; }
    public string Message { get; }

    public MessageReceivedEventArgs(string sessionId, string message)
    {
        SessionId = sessionId;
        Message = message;
    }
}

internal class WebSocketSession
{
    public string SessionId { get; set; }
    public WebSocket WebSocket { get; set; }

    public WebSocketSession(string sessionId, WebSocket webSocket)
    {
        SessionId = sessionId;
        WebSocket = webSocket;
    }
}

internal class WebSocketServer
{
    public readonly ConcurrentDictionary<string, WebSocketSession> Sessions;
    public event EventHandler<SessionEventArgs>? NewSession;
    public event EventHandler<SessionEventArgs>? SessionDropped;
    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    private HttpListener? listener;

    public WebSocketServer()
    {
        Sessions = new ConcurrentDictionary<string, WebSocketSession>();
    }

    public async Task Start(string ipAddress, int port)
    {
        listener = new HttpListener();
        listener.Prefixes.Add($"http://{ipAddress}:{port}/");
        listener.Start();

        while (listener.IsListening)
        {
            var context = await listener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
            {
                var sessionGuid = Guid.NewGuid().ToString();
                var webSocketContext = await context.AcceptWebSocketAsync(null);
                var webSocket = webSocketContext.WebSocket;
                var webSocketSession = new WebSocketSession(sessionGuid, webSocket);

                NewSession?.Invoke(this, new SessionEventArgs(sessionGuid));
                Sessions.TryAdd(sessionGuid, webSocketSession);
                _ = ProcessWebSocketSession(webSocketSession);
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }
    }

    public async Task Stop()
    {
        foreach (var session in Sessions)
        {
            await session.Value.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down",
                CancellationToken.None);
        }

        Sessions.Clear();
        listener?.Stop();
    }

    private async Task ProcessWebSocketSession(WebSocketSession webSocketSession)
    {
        var buffer = new byte[1024];

        try
        {
            while (webSocketSession.WebSocket.State == WebSocketState.Open)
            {
                var receiveResult =
                    await webSocketSession.WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer),
                        CancellationToken.None);

                if (receiveResult.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                    MessageReceived?.Invoke(this, new MessageReceivedEventArgs(webSocketSession.SessionId, message));
                }
                else if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    await webSocketSession.WebSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Connection closed by the client",
                        CancellationToken.None);
                    break;
                }
            }
        }
        finally
        {
            Sessions.TryRemove(webSocketSession.SessionId, out _);
            SessionDropped?.Invoke(this, new SessionEventArgs(webSocketSession.SessionId));
        }
    }

    public bool RenameSession(string oldSessionId, string newSessionId)
    {
        if (!Sessions.ContainsKey(oldSessionId) || Sessions.ContainsKey(newSessionId))
            return false;

        if (!Sessions.TryRemove(oldSessionId, out var webSocketSession))
            return false;

        webSocketSession.SessionId = newSessionId;

        if (Sessions.TryAdd(newSessionId, webSocketSession))
            return true;

        webSocketSession.SessionId = oldSessionId;

        if (!Sessions.TryAdd(oldSessionId, webSocketSession))
            throw new Exception("Failed to add back the old session after failed rename");

        return false;
    }

    public async Task SendToSession(string sessionId, string message)
    {
        try
        {
            if (Sessions.TryGetValue(sessionId, out var webSocketSession))
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await webSocketSession.WebSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
            }
        }
        catch (WebSocketException ex)
        {
            if (ex.InnerException is SocketException { SocketErrorCode: SocketError.ConnectionReset })
            {
                if (Sessions.ContainsKey(sessionId))
                    Sessions.Remove(sessionId, out _);
            }
        }
    }
}

internal class WsChatBotCommand
{
    [JsonProperty("command")] public string Command { get; set; } = "";

    [JsonProperty("requestId")] public string RequestId { get; set; } = "";

    [JsonProperty("parameters")] public object[]? Parameters { get; set; }
}

internal class WsCommandResponder
{
    private WebSocketBot _bot;
    private string _sessionId;
    private string _command;
    private string _requestId;

    public WsCommandResponder(WebSocketBot bot, string sessionId, string command, string requestId)
    {
        _bot = bot;
        _sessionId = sessionId;
        _command = command;
        _requestId = requestId;
    }

    private void SendCommandResponse(bool success, string result, bool overrideAuth = false)
    {
        _bot.SendCommandResponse(_sessionId, success, _requestId, _command, result, overrideAuth);
    }

    public void SendErrorResponse(string error, bool overrideAuth = false)
    {
        SendCommandResponse(false, error, overrideAuth);
    }

    public void SendSuccessResponse(string result, bool overrideAuth = false)
    {
        SendCommandResponse(true, result, overrideAuth);
    }

    public void SendSuccessResponse(bool overrideAuth = false)
    {
        SendSuccessResponse(JsonConvert.SerializeObject(true), overrideAuth);
    }

    public string Quote(string text)
    {
        return $"\"{text}\"";
    }
}

internal class NbtData
{
    public NBT? nbt { get; set; }
}

internal class NBT
{
    public Dictionary<string, object>? nbt { get; set; }
}

internal class NbtDictionaryConverter : JsonConverter<Dictionary<string, object>>
{
    public override void WriteJson(JsonWriter writer, Dictionary<string, object>? value, JsonSerializer serializer)
        => throw new NotImplementedException();

    public override Dictionary<string, object>? ReadJson(JsonReader reader, Type objectType,
        Dictionary<string, object>? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var keyValuePairs = serializer.Deserialize<IEnumerable<KeyValuePair<string, object>>>(reader);
        return new(keyValuePairs!);
    }
}

public class WebSocketBot : ChatBot
{
    private string? _ip;
    private int _port;
    private string? _password;
    private WebSocketServer? _server;
    private List<string> _authenticatedSessions;
    private List<(string, string)> _waitingEvents;

    public static Configs Config = new();

    [TomlDoNotInlineObject]
    public class Configs
    {
        [NonSerialized] private const string BotName = "Websocket";

        public bool Enabled = false;

        [TomlInlineComment("$ChatBot.WebSocketBot.Ip$")]
        public string? Ip = "127.0.0.1";

        [TomlInlineComment("$ChatBot.WebSocketBot.Port$")]
        public int Port = 8043;

        [TomlInlineComment("$ChatBot.WebSocketBot.Password$")]
        public string? Password = Guid.NewGuid().ToString().Replace("-", "").Trim().ToLower();

        [TomlInlineComment("$ChatBot.WebSocketBot.DebugMode$")]
        public bool DebugMode = false;

        [TomlInlineComment("$ChatBot.WebSocketBot.AllowIpAlias$")]
        public bool AllowIpAlias = false;
    }

    public WebSocketBot()
    {
        _password = Config.Password;
        _authenticatedSessions = new();
        _waitingEvents = new();

        var match = Regex.Match(Config.Ip!, @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}");

        // If AllowIpAlias is set to true in the config, then always ignore this check
        if (!match.Success & !Config.AllowIpAlias!)
        {
            LogToConsole(Translations.bot_WebSocketBot_failed_to_start_ip);
            return;
        }

        if (Config.Port > 65535)
        {
            LogToConsole(string.Format(Translations.bot_WebSocketBot_failed_to_start_port, _port.ToString()));
            return;
        }

        _ip = Config.Ip;
        _port = Config.Port;
    }

    public override void Initialize()
    {
        Task.Run(() =>
        {
            _authenticatedSessions.Clear();

            if (_server != null)
            {
                SendEvent("OnWsRestarting", "");
                _server.Stop(); // If you await, this will freeze the task and the websocket won't work
                _server = null;
            }

            try
            {
                LogToConsole(Translations.bot_WebSocketBot_starting);
                _server = new();
                _server.Start(_ip!, _port); // If you await, this will freeze the task and the websocket won't work

                LogToConsole(string.Format(Translations.bot_WebSocketBot_started, _ip, _port.ToString()));

                foreach (var (eventName, data) in _waitingEvents)
                    SendEvent(eventName, data);
            }
            catch (Exception e)
            {
                LogToConsole(string.Format(Translations.bot_WebSocketBot_failed_to_start_custom, e));
                return;
            }

            _server.NewSession += (_, session) =>
                LogToConsole(string.Format(Translations.bot_WebSocketBot_new_session, session.SessionId));
            _server.SessionDropped += (_, session) =>
                LogToConsole(string.Format(Translations.bot_WebSocketBot_session_disconnected, session.SessionId));

            _server.MessageReceived += (_, messageObject) =>
            {
                if (!ProcessWebsocketCommand(messageObject.SessionId, _password!, messageObject.Message))
                    return;

                var command = messageObject.Message;
                command = command.StartsWith('/') ? command[1..] : $"send {command}";

                CmdResult response = new();
                PerformInternalCommand(command, ref response);
                SendSessionEvent(messageObject.SessionId, "OnMccCommandResponse", $"{{\"response\": \"{response}\"}}");
            };
        });
    }

    private bool ProcessWebsocketCommand(string sessionId, string password, string message)
    {
        message = message.Trim();

        if (string.IsNullOrEmpty(message))
            return false;

        if (message.StartsWith('{'))
        {
            try
            {
                if (Config.DebugMode)
                    LogDebugToConsole($"\n\n\tGot command\n\n\t{message}\n\n");

                var cmd = JsonConvert.DeserializeObject<WsChatBotCommand>(message)!;
                var responder = new WsCommandResponder(this, sessionId, cmd.Command, cmd.RequestId);

                // Allow session name changing without authenticating for easier identification
                if (cmd.Command.Equals("ChangeSessionId", StringComparison.OrdinalIgnoreCase))
                {
                    if (cmd.Parameters is not { Length: 1 })
                    {
                        responder.SendErrorResponse(
                            responder.Quote("Invalid number of parameters, expected 1 (newSessionid)!"), true);
                        return false;
                    }

                    var newId = (cmd.Parameters[0] as string)!;

                    switch (newId.Length)
                    {
                        case 0:
                            responder.SendErrorResponse(responder.Quote("Please provide a valid session ID!"),
                                true);
                            return false;
                        case > 32:
                            responder.SendErrorResponse(
                                responder.Quote("The session ID can't be longer than 32 characters!"), true);
                            return false;
                    }

                    if (!_server!.RenameSession(sessionId, newId))
                    {
                        responder.SendErrorResponse(
                            responder.Quote("Failed to change the session id to: '" + newId + "'"),
                            true);
                        LogToConsole(string.Format(Translations.bot_WebSocketBot_session_id_failed_to_change, sessionId,
                            newId));
                        return false;
                    }

                    // If the session is authenticated, remove the old session id and add the new one
                    if (_authenticatedSessions.Contains(sessionId))
                    {
                        _authenticatedSessions.Remove(sessionId);
                        _authenticatedSessions.Add(newId);
                    }

                    // Update the responder to the new session id
                    responder = new WsCommandResponder(this, newId, cmd.Command, cmd.RequestId);

                    responder.SendSuccessResponse(
                        responder.Quote("The session ID was successfully changed to: '" + newId + "'"), true);
                    LogToConsole(string.Format(Translations.bot_WebSocketBot_session_id_changed, sessionId, newId));
                    return false;
                }

                // Authentication and session commands
                if (password.Length != 0)
                {
                    if (!_authenticatedSessions.Contains(sessionId))
                    {
                        // Special case for authentication
                        if (cmd.Command.Equals("Authenticate", StringComparison.OrdinalIgnoreCase))
                        {
                            if (cmd.Parameters is not { Length: 1 })
                            {
                                responder.SendErrorResponse(
                                    responder.Quote("Invalid number of parameters, expected 1 (password)!"), true);
                                return false;
                            }

                            var pass = (cmd.Parameters[0] as string)!;

                            if (pass.Length == 0)
                            {
                                responder.SendErrorResponse(
                                    responder.Quote(
                                        "Please provide a valid password! (Example: 'Authenticate password123')"),
                                    true);
                                return false;
                            }

                            if (!pass.Equals(password))
                            {
                                responder.SendErrorResponse(responder.Quote("Incorrect password provided!"), true);
                                return false;
                            }

                            _authenticatedSessions.Add(sessionId);
                            responder.SendSuccessResponse(responder.Quote("Successfully authenticated!"), true);
                            LogToConsole(string.Format(Translations.bot_WebSocketBot_session_authenticated, sessionId));
                            return false;
                        }

                        responder.SendErrorResponse(
                            responder.Quote("You must authenticate in order to send and receive data!"), true);
                        return false;
                    }
                }
                else
                {
                    if (!_authenticatedSessions.Contains(sessionId))
                    {
                        responder.SendSuccessResponse(responder.Quote("Successfully authenticated!"));
                        LogToConsole(string.Format(Translations.bot_WebSocketBot_session_authenticated, sessionId));
                        _authenticatedSessions.Add(sessionId);
                        return false;
                    }
                }

                // Process other commands
                switch (cmd.Command)
                {
                    case "LogToConsole":
                        if (cmd.Parameters == null || cmd.Parameters.Length > 1 || cmd.Parameters.Length < 1)
                        {
                            responder.SendErrorResponse(
                                responder.Quote("Invalid number of parameters, expecting a single parameter!"));
                            return false;
                        }

                        LogToConsole((cmd.Parameters[0] as string)!);
                        responder.SendSuccessResponse();
                        break;

                    case "LogDebugToConsole":
                        if (cmd.Parameters == null || cmd.Parameters.Length > 1 || cmd.Parameters.Length < 1)
                        {
                            responder.SendErrorResponse(
                                responder.Quote("Invalid number of parameters, expecting a single parameter!"));
                            return false;
                        }

                        LogDebugToConsole((cmd.Parameters[0] as string)!);
                        responder.SendSuccessResponse();
                        break;

                    case "LogToConsoleTranslated":
                        if (cmd.Parameters == null || cmd.Parameters.Length > 1 || cmd.Parameters.Length < 1)
                        {
                            responder.SendErrorResponse(
                                responder.Quote("Invalid number of parameters, expecting a single parameter!"));
                            return false;
                        }

                        LogToConsoleTranslated((cmd.Parameters[0] as string)!);
                        responder.SendSuccessResponse();
                        break;

                    case "LogDebugToConsoleTranslated":
                        if (cmd.Parameters!.Length > 1 || cmd.Parameters.Length < 1)
                        {
                            responder.SendErrorResponse(
                                responder.Quote("Invalid number of parameters, expecting a single parameter!"));
                            return false;
                        }

                        LogDebugToConsoleTranslated((cmd.Parameters[0] as string)!);
                        responder.SendSuccessResponse();
                        break;

                    case "ReconnectToTheServer":
                        if (cmd.Parameters is not { Length: 2 })
                        {
                            responder.SendErrorResponse(responder.Quote(
                                "Invalid number of parameters, expecting 2 parameters (extraAttempts, delaySeconds)!"));
                            return false;
                        }

                        ReconnectToTheServer(Convert.ToInt32(cmd.Parameters[0]), Convert.ToInt32(cmd.Parameters[1]));
                        responder.SendSuccessResponse();
                        break;

                    case "DisconnectAndExit":
                        responder.SendSuccessResponse();
                        DisconnectAndExit();
                        break;

                    case "SendPrivateMessage":
                        if (cmd.Parameters is not { Length: 2 })
                        {
                            responder.SendErrorResponse(responder.Quote(
                                "Invalid number of parameters, expecting 2 parameters (player, message)!"));
                            return false;
                        }

                        SendPrivateMessage((cmd.Parameters[0] as string)!, (cmd.Parameters[1] as string)!);
                        responder.SendSuccessResponse();
                        break;

                    case "RunScript":
                        if (cmd.Parameters is not { Length: 1 })
                        {
                            responder.SendErrorResponse(
                                responder.Quote("Invalid number of parameters, expecting 1 parameter (filename)!"));
                            return false;
                        }

                        RunScript((cmd.Parameters[0] as string)!);
                        responder.SendSuccessResponse();
                        break;

                    case "GetTerrainEnabled":
                        responder.SendSuccessResponse(GetTerrainEnabled().ToString().ToLower());
                        break;

                    case "SetTerrainEnabled":
                        if (cmd.Parameters is not { Length: 1 })
                        {
                            responder.SendErrorResponse(
                                responder.Quote("Invalid number of parameters, expecting 1 parameter (enabled)!"));
                            return false;
                        }

                        SetTerrainEnabled((bool)cmd.Parameters[0]);
                        responder.SendSuccessResponse();
                        break;

                    case "GetEntityHandlingEnabled":
                        responder.SendSuccessResponse(GetEntityHandlingEnabled().ToString().ToLower());
                        break;

                    case "Sneak":
                        if (cmd.Parameters is not { Length: 1 })
                        {
                            responder.SendErrorResponse(
                                responder.Quote("Invalid number of parameters, expecting 1 parameter (on)!"));
                            return false;
                        }

                        Sneak((bool)cmd.Parameters[0]);
                        responder.SendSuccessResponse();
                        break;

                    case "SendEntityAction":
                        if (cmd.Parameters is not { Length: 1 })
                        {
                            responder.SendErrorResponse(
                                responder.Quote("Invalid number of parameters, expecting 1 parameter (actionType)!"));
                            return false;
                        }

                        SendEntityAction(((Protocol.EntityActionType)(Convert.ToInt32(cmd.Parameters[0]))));
                        responder.SendSuccessResponse();
                        break;

                    case "DigBlock":
                        if (cmd.Parameters == null || cmd.Parameters.Length == 0 || cmd.Parameters.Length < 3 ||
                            cmd.Parameters.Length > 5)
                        {
                            responder.SendErrorResponse(responder.Quote(
                                "Invalid number of parameters, expecting 1 or 3 parameter(s) (location, swingArms?, lookAtBlock?)!"));
                            return false;
                        }

                        var location = new Location(Convert.ToInt32(cmd.Parameters[0]),
                            Convert.ToInt32(cmd.Parameters[1]), Convert.ToInt32(cmd.Parameters[2]));

                        if (location.DistanceSquared(GetCurrentLocation().EyesLocation()) > 25)
                        {
                            responder.SendErrorResponse(
                                responder.Quote("The block you're trying to dig is too far away!"));
                            return false;
                        }

                        if (GetWorld().GetBlock(location).Type == Material.Air)
                        {
                            responder.SendErrorResponse(responder.Quote("The block you're trying to dig is is air!"));
                            return false;
                        }

                        var result = cmd.Parameters.Length switch
                        {
                            3 => DigBlock(location),
                            4 => DigBlock(location, (bool)cmd.Parameters[3]),
                            5 => DigBlock(location, (bool)cmd.Parameters[3], (bool)cmd.Parameters[4]),
                            _ => false
                        };

                        responder.SendSuccessResponse(JsonConvert.SerializeObject(result));
                        break;

                    case "SetSlot":
                        if (cmd.Parameters is not { Length: 1 })
                        {
                            responder.SendErrorResponse(
                                responder.Quote("Invalid number of parameters, expecting 1 parameter (slotNumber)!"));
                            return false;
                        }

                        SetSlot(Convert.ToInt32(cmd.Parameters[0]));
                        responder.SendSuccessResponse();
                        break;

                    case "GetWorld":
                        responder.SendSuccessResponse(JsonConvert.SerializeObject(GetWorld()));
                        break;

                    case "GetEntities":
                        responder.SendSuccessResponse(JsonConvert.SerializeObject(GetEntities()));
                        break;

                    case "GetPlayersLatency":
                        responder.SendSuccessResponse(JsonConvert.SerializeObject(GetPlayersLatency()));
                        break;

                    case "GetCurrentLocation":
                        responder.SendSuccessResponse(JsonConvert.SerializeObject(GetCurrentLocation()));
                        break;

                    case "MoveToLocation":
                        if (cmd.Parameters == null || cmd.Parameters.Length == 0 || cmd.Parameters.Length < 3 ||
                            cmd.Parameters.Length > 8)
                        {
                            responder.SendErrorResponse(responder.Quote(
                                "Invalid number of parameters, expecting 1 or 7 parameter(s) (x, y, z, allowUnsafe?, allowDirectTeleport?, maxOffset?, minoffset?, timeout?)!"));
                            return false;
                        }

                        var allowUnsafe = false;
                        var allowDirectTeleport = false;
                        var maxOffset = 0;
                        var minOffset = 0;
                        TimeSpan? timeout = null;

                        if (cmd.Parameters.Length >= 4)
                            allowUnsafe = (bool)cmd.Parameters[3];

                        if (cmd.Parameters.Length >= 5)
                            allowDirectTeleport = (bool)cmd.Parameters[4];

                        if (cmd.Parameters.Length >= 6)
                            maxOffset = Convert.ToInt32(cmd.Parameters[5]);

                        if (cmd.Parameters.Length >= 7)
                            minOffset = Convert.ToInt32(cmd.Parameters[6]);

                        if (cmd.Parameters.Length == 8)
                            timeout = TimeSpan.FromSeconds(Convert.ToInt32(cmd.Parameters[7]));

                        var canMove = MoveToLocation(
                            new Location(Convert.ToInt32(cmd.Parameters[0]),
                                Convert.ToInt32(cmd.Parameters[1]),
                                Convert.ToInt32(cmd.Parameters[2])),
                            allowUnsafe,
                            allowDirectTeleport,
                            maxOffset,
                            minOffset,
                            timeout);

                        responder.SendSuccessResponse(JsonConvert.SerializeObject(canMove));
                        break;

                    case "ClientIsMoving":
                        responder.SendSuccessResponse(JsonConvert.SerializeObject(ClientIsMoving()));
                        break;

                    case "LookAtLocation":
                        if (cmd.Parameters == null || cmd.Parameters.Length == 0 || cmd.Parameters.Length < 3 ||
                            cmd.Parameters.Length > 3)
                        {
                            responder.SendErrorResponse(
                                responder.Quote("Invalid number of parameters, expecting 3 parameter(s) (x, y, z)!"));
                            return false;
                        }

                        LookAtLocation(new Location(Convert.ToInt32(cmd.Parameters[0]),
                            Convert.ToInt32(cmd.Parameters[1]), Convert.ToInt32(cmd.Parameters[2])));
                        responder.SendSuccessResponse();
                        break;

                    case "GetTimestamp":
                        responder.SendSuccessResponse(JsonConvert.SerializeObject(GetTimestamp()));
                        break;

                    case "GetServerPort":
                        responder.SendSuccessResponse(JsonConvert.SerializeObject(GetServerPort()));
                        break;

                    case "GetServerHost":
                        responder.SendSuccessResponse(JsonConvert.SerializeObject(GetServerHost()));
                        break;

                    case "GetUsername":
                        responder.SendSuccessResponse(JsonConvert.SerializeObject(GetUsername()));
                        break;

                    case "GetGamemode":
                        responder.SendSuccessResponse(JsonConvert.SerializeObject(GameModeString(GetGamemode())));
                        break;

                    case "GetYaw":
                        responder.SendSuccessResponse(JsonConvert.SerializeObject(GetYaw()));
                        break;

                    case "GetPitch":
                        responder.SendSuccessResponse(JsonConvert.SerializeObject(GetPitch()));
                        break;

                    case "GetUserUUID":
                        responder.SendSuccessResponse(JsonConvert.SerializeObject(GetUserUUID()));
                        break;

                    case "GetOnlinePlayers":
                        responder.SendSuccessResponse(JsonConvert.SerializeObject(GetOnlinePlayers()));
                        break;

                    case "GetOnlinePlayersWithUUID":
                        responder.SendSuccessResponse(JsonConvert.SerializeObject(GetOnlinePlayersWithUUID()));
                        break;

                    case "GetServerTPS":
                        responder.SendSuccessResponse(JsonConvert.SerializeObject(GetServerTPS()));
                        break;

                    case "InteractEntity":
                        if (cmd.Parameters == null || cmd.Parameters.Length == 0 || cmd.Parameters.Length < 2 ||
                            cmd.Parameters.Length > 3)
                        {
                            responder.SendErrorResponse(responder.Quote(
                                "Invalid number of parameters, expecting at least 2 and at most 3 parameter(s) (entityId, interactionType, hand?)!"));
                            return false;
                        }

                        var interactionType = (InteractType)Convert.ToInt32(cmd.Parameters[1]);
                        var interactionHand = Hand.MainHand;

                        if (cmd.Parameters.Length == 3)
                            interactionHand = (Hand)Convert.ToInt32(cmd.Parameters[2]);

                        responder.SendSuccessResponse(JsonConvert.SerializeObject(
                            InteractEntity(Convert.ToInt32(cmd.Parameters[0]), interactionType, interactionHand)));
                        break;

                    case "CreativeGive":
                        if (cmd.Parameters == null || cmd.Parameters.Length == 0 || cmd.Parameters.Length < 3 ||
                            cmd.Parameters.Length > 4)
                        {
                            responder.SendErrorResponse(responder.Quote(
                                "Invalid number of parameters, expecting at least 3 and at most 4 parameter(s) (slotId, itemType, count, nbt?)!"));
                            return false;
                        }

                        NBT? nbt = null;

                        if (cmd.Parameters.Length == 4)
                            nbt = JsonConvert.DeserializeObject<NBT>(cmd.Parameters[3].ToString()!,
                                new NbtDictionaryConverter())!;

                        responder.SendSuccessResponse(
                            JsonConvert.SerializeObject(CreativeGive(
                                Convert.ToInt32(cmd.Parameters[0]),
                                (ItemType)Convert.ToInt32(cmd.Parameters[1]),
                                Convert.ToInt32(cmd.Parameters[2]),
                                nbt == null ? new Dictionary<string, object>() : nbt!.nbt!)
                            ));

                        break;

                    case "CreativeDelete":
                        if (cmd.Parameters is not { Length: 1 })
                        {
                            responder.SendErrorResponse(
                                responder.Quote("Invalid number of parameters, expecting at 1 parameter (slotId)!"));
                            return false;
                        }

                        responder.SendSuccessResponse(
                            JsonConvert.SerializeObject(CreativeDelete(Convert.ToInt32(cmd.Parameters[0]))));
                        break;

                    case "SendAnimation":
                        var hand = Hand.MainHand;

                        if (cmd.Parameters is { Length: 1 })
                            hand = (Hand)Convert.ToInt32(cmd.Parameters[0]);

                        responder.SendSuccessResponse(JsonConvert.SerializeObject(SendAnimation(hand)));
                        break;

                    case "SendPlaceBlock":
                        if (cmd.Parameters == null || cmd.Parameters.Length == 0 || cmd.Parameters.Length < 4 ||
                            cmd.Parameters.Length > 4)
                        {
                            responder.SendErrorResponse(responder.Quote(
                                "Invalid number of parameters, expecting at least 4 and at most 5 parameters (x, y, z, blockFace, hand?)!"));
                            return false;
                        }

                        var blockLocation = new Location(Convert.ToInt32(cmd.Parameters[0]),
                            Convert.ToInt32(cmd.Parameters[1]), Convert.ToInt32(cmd.Parameters[2]));
                        var blockFacingDirection = (Direction)Convert.ToInt32(cmd.Parameters[3]);
                        var handToUse = Hand.MainHand;

                        if (cmd.Parameters.Length == 4)
                            handToUse = (Hand)Convert.ToInt32(cmd.Parameters[4]);

                        responder.SendSuccessResponse(
                            JsonConvert.SerializeObject(SendPlaceBlock(blockLocation, blockFacingDirection,
                                handToUse)));
                        break;

                    case "UseItemInHand":
                        responder.SendSuccessResponse(JsonConvert.SerializeObject(UseItemInHand()));
                        break;

                    case "GetInventoryEnabled":
                        responder.SendSuccessResponse(JsonConvert.SerializeObject(GetInventoryEnabled()));
                        break;

                    case "GetPlayerInventory":
                        responder.SendSuccessResponse(JsonConvert.SerializeObject(GetPlayerInventory()));
                        break;

                    case "GetInventories":
                        responder.SendSuccessResponse(JsonConvert.SerializeObject(GetInventories()));
                        break;

                    case "WindowAction":
                        if (cmd.Parameters == null || cmd.Parameters.Length == 0 || cmd.Parameters.Length != 3)
                        {
                            responder.SendErrorResponse(responder.Quote(
                                "Invalid number of parameters, expecting 3 parameters (inventoryId, slotId, windowActionType)!"));
                            return false;
                        }

                        responder.SendSuccessResponse(
                            JsonConvert.SerializeObject(WindowAction(
                                Convert.ToInt32(cmd.Parameters[0]),
                                Convert.ToInt32(cmd.Parameters[1]),
                                (WindowActionType)Convert.ToInt32(cmd.Parameters[2])
                            )));
                        break;

                    case "ChangeSlot":
                        if (cmd.Parameters is not { Length: 1 })
                        {
                            responder.SendErrorResponse(
                                responder.Quote("Invalid number of parameters, expecting 1 parameter (slotId)!"));
                            return false;
                        }

                        responder.SendSuccessResponse(
                            JsonConvert.SerializeObject(ChangeSlot((short)Convert.ToInt32(cmd.Parameters[0]))));
                        break;

                    case "GetCurrentSlot":
                        responder.SendSuccessResponse(JsonConvert.SerializeObject(GetCurrentSlot()));
                        break;

                    case "ClearInventories":
                        responder.SendSuccessResponse(JsonConvert.SerializeObject(ClearInventories()));
                        break;

                    case "UpdateSign":
                        if (cmd.Parameters is not { Length: 7 })
                        {
                            responder.SendErrorResponse(responder.Quote(
                                "Invalid number of parameters, expecting 1 parameter (x, y, z, line1, line2, line3, line4)!"));
                            return false;
                        }

                        var signLocation = new Location(Convert.ToInt32(cmd.Parameters[0]),
                            Convert.ToInt32(cmd.Parameters[1]), Convert.ToInt32(cmd.Parameters[2]));

                        responder.SendSuccessResponse(
                            JsonConvert.SerializeObject(UpdateSign(signLocation,
                                (string)cmd.Parameters[3],
                                (string)cmd.Parameters[4],
                                (string)cmd.Parameters[5],
                                (string)cmd.Parameters[6]
                            )));
                        break;

                    case "SelectTrade":
                        if (cmd.Parameters is not { Length: 1 })
                        {
                            responder.SendErrorResponse(
                                responder.Quote("Invalid number of parameters, expecting 1 parameter (selectedSlot)!"));
                            return false;
                        }

                        responder.SendSuccessResponse(
                            JsonConvert.SerializeObject(SelectTrade(Convert.ToInt32(cmd.Parameters[0]))));
                        break;

                    case "UpdateCommandBlock":
                        if (cmd.Parameters is not { Length: 6 })
                        {
                            responder.SendErrorResponse(responder.Quote(
                                "Invalid number of parameters, expecting 1 parameter (x, y, z, command, commandBlockMode, commandBlockFlags)!"));
                            return false;
                        }

                        var commandBlockLocation = new Location(Convert.ToInt32(cmd.Parameters[0]),
                            Convert.ToInt32(cmd.Parameters[1]), Convert.ToInt32(cmd.Parameters[2]));

                        responder.SendSuccessResponse(
                            UpdateCommandBlock(commandBlockLocation,
                                (string)cmd.Parameters[3],
                                (CommandBlockMode)Convert.ToInt32(cmd.Parameters[4]),
                                (CommandBlockFlags)Convert.ToInt32(cmd.Parameters[5])
                            ).ToString().ToLower());
                        break;

                    case "CloseInventory":
                        if (cmd.Parameters is not { Length: 1 })
                        {
                            responder.SendErrorResponse(
                                responder.Quote("Invalid number of parameters, expecting 1 parameter (inventoryId)!"));
                            return false;
                        }

                        responder.SendSuccessResponse(CloseInventory(Convert.ToInt32(cmd.Parameters[0])).ToString()
                            .ToLower());
                        break;

                    case "GetMaxChatMessageLength":
                        responder.SendSuccessResponse(JsonConvert.SerializeObject(GetMaxChatMessageLength()));
                        break;

                    case "Respawn":
                        responder.SendSuccessResponse(JsonConvert.SerializeObject(Respawn()));
                        break;

                    case "GetProtocolVersion":
                        responder.SendSuccessResponse(JsonConvert.SerializeObject(GetProtocolVersion()));
                        break;

                    default:
                        responder.SendErrorResponse(
                            responder.Quote($"Unknown command {cmd.Command} received!"));
                        break;
                }
            }
            catch (Exception e)
            {
                LogDebugToConsole(e.Message);
                SendSessionEvent(sessionId, "OnWsCommandResponse",
                    "{\"success\": false, \"message\": \"An error occured, possible reasons: mail-formed json, type conversion, internal error\", \"stackTrace\": \"" +
                    Json.EscapeString(e.ToString()) + "\"}", true);
                return false;
            }

            return false;
        }

        if (password.Length != 0)
        {
            if (!_authenticatedSessions.Contains(sessionId))
            {
                SendSessionEvent(sessionId, "OnWsCommandResponse",
                    "{\"error\": true, \"message\": \"You must authenticate in order to send and receive data!\"}",
                    true);
                return false;
            }
        }
        else
        {
            if (!_authenticatedSessions.Contains(sessionId))
            {
                SendSessionEvent(sessionId, "OnWsCommandResponse",
                    "{\"success\": true, \"message\": \"Successfully authenticated!\"}", true);
                LogToConsole(string.Format(Translations.bot_WebSocketBot_session_authenticated, sessionId));
                _authenticatedSessions.Add(sessionId);
            }
        }

        return true;
    }

    public override void OnUnload()
    {
        if (_server != null)
        {
            SendEvent("OnWsConnectionClose", "");
            _server.Stop();
            _server = null;
        }

        _authenticatedSessions.Clear();
    }

    // ==========================================================================================
    // Bot Events
    // ==========================================================================================
    public override void AfterGameJoined()
    {
        // Workaround to wait until the WebSocket server has been started
        // This would fire before the WS server is started, this causing a null exception.
        _waitingEvents.Add(("OnGameJoined", ""));
    }

    public override void OnBlockBreakAnimation(Entity entity, Location location, byte stage)
    {
        SendEvent("OnBlockBreakAnimation", new { entity, location, stage });
    }

    public override void OnEntityAnimation(Entity entity, byte animation)
    {
        SendEvent("OnEntityAnimation", new { entity, animation });
    }

    public override void GetText(string text)
    {
        text = GetVerbatim(text).Trim();

        var message = "";
        var username = "";

        if (IsPrivateMessage(text, ref message, ref username))
            SendEvent("OnChatPrivate", new { sender = username, message, rawText = text });
        else if (IsChatMessage(text, ref message, ref username))
            SendEvent("OnChatPublic", new { username, message, rawText = text });
        else if (IsTeleportRequest(text, ref username))
            SendEvent("OnTeleportRequest", new { sender = username, rawText = text });
    }

    public override void GetText(string text, string? json)
    {
        SendEvent("OnChatRaw", new { text, json });
    }

    public override bool OnDisconnect(DisconnectReason reason, string message)
    {
        var reasonString = reason switch
        {
            DisconnectReason.ConnectionLost => "Connection Lost",
            DisconnectReason.UserLogout => "User Logout",
            DisconnectReason.InGameKick => "In-Game Kick",
            DisconnectReason.LoginRejected => "Login Rejected",
            _ => "Unknown"
        };

        SendEvent("OnDisconnect", new { reason = reasonString, message });
        return false;
    }

    public override void OnPlayerProperty(Dictionary<string, double> prop)
    {
        SendEvent("OnPlayerProperty", prop);
    }

    public override void OnServerTpsUpdate(double tps)
    {
        SendEvent("OnServerTpsUpdate", new { tps });
    }

    public override void OnTimeUpdate(long worldAge, long timeOfDay)
    {
        SendEvent("OnTimeUpdate", new { worldAge, timeOfDay });
    }

    public override void OnEntityMove(Entity entity)
    {
        SendEvent("OnEntityMove", entity);
    }

    public override void OnInternalCommand(string commandName, string commandParams, CmdResult result)
    {
        SendEvent("OnInternalCommand",
            new { command = commandName, parameters = commandParams, result = result.ToString().Replace("\"", "'") });
    }

    public override void OnEntitySpawn(Entity entity)
    {
        SendEvent("OnEntitySpawn", entity);
    }

    public override void OnEntityDespawn(Entity entity)
    {
        SendEvent("OnEntityDespawn", entity);
    }

    public override void OnHeldItemChange(byte slot)
    {
        SendEvent("OnHeldItemChange", new { itemSlot = slot });
    }

    public override void OnHealthUpdate(float health, int food)
    {
        SendEvent("OnHealthUpdate", new { health, food });
    }

    public override void OnExplosion(Location explode, float strength, int recordCount)
    {
        SendEvent("OnExplosion", new { location = explode, strength, recordCount });
    }

    public override void OnSetExperience(float experienceBar, int level, int totalExperience)
    {
        SendEvent("OnSetExperience",
            new { experienceBar, level, totalExperience });
    }

    public override void OnGamemodeUpdate(string playerName, Guid uuid, int gameMode)
    {
        SendEvent("OnGamemodeUpdate", new { playerName, uuid, gameMode = GameModeString(gameMode) });
    }

    public override void OnLatencyUpdate(string playerName, Guid uuid, int latency)
    {
        SendEvent("OnLatencyUpdate", new { playerName, uuid, latency });
    }

    public override void OnMapData(int mapId, byte scale, bool trackingPosition, bool locked, List<MapIcon> icons,
        byte columnsUpdated, byte rowsUpdated, byte mapColumnX, byte mapRowZ, byte[]? colors)
    {
        SendEvent("OnMapData",
            new
            {
                mapId, scale, trackingPosition, locked, icons, columnsUpdated, rowsUpdated, mapColumnX, mapRowZ,
                colors
            });
    }

    public override void OnTradeList(int windowId, List<VillagerTrade> trades, VillagerInfo villagerInfo)
    {
        SendEvent("OnTradeList", new { windowId, trades, villagerInfo });
    }

    public override void OnTitle(int action, string titleText, string subtitleText, string actionBarText, int fadein,
        int stay, int fadeout, string json_)
    {
        SendEvent("OnTitle",
            new
            {
                action, titleText, subtitleText, actionBarText,
                fadeIn = fadein, stay, rawJson = json_
            });
    }

    public override void OnEntityEquipment(Entity entity, int slot, Item? item)
    {
        SendEvent("OnEntityEquipment", new { entity, slot, item });
    }

    public override void OnEntityEffect(Entity entity, Effects effect, int amplifier, int duration, byte flags)
    {
        SendEvent("OnEntityEffect", new { entity, effect, amplifier, duration, flags });
    }

    public override void OnScoreboardObjective(string objectiveName, byte mode, string objectiveValue, int type,
        string json_)
    {
        SendEvent("OnScoreboardObjective",
            new { objectiveName, mode, objectiveValue, type, rawJson = json_ });
    }

    public override void OnUpdateScore(string entityName, int action, string objectiveName, int value)
    {
        SendEvent("OnUpdateScore",
            new { entityName, action, objectiveName, type = value });
    }

    public override void OnInventoryUpdate(int inventoryId)
    {
        SendEvent("OnInventoryUpdate", new { inventoryId });
    }

    public override void OnInventoryOpen(int inventoryId)
    {
        SendEvent("OnInventoryOpen", new { inventoryId });
    }

    public override void OnInventoryClose(int inventoryId)
    {
        SendEvent("OnInventoryClose", new { inventoryId });
    }

    public override void OnPlayerJoin(Guid uuid, string name)
    {
        SendEvent("OnPlayerJoin", new { uuid, name });
    }

    public override void OnPlayerLeave(Guid uuid, string? name)
    {
        SendEvent("OnPlayerLeave", new { uuid, name = name ?? "null" });
    }

    public override void OnDeath()
    {
        SendEvent("OnDeath", "");
    }

    public override void OnRespawn()
    {
        SendEvent("OnRespawn", "");
    }

    public override void OnEntityHealth(Entity entity, float health)
    {
        SendEvent("OnEntityHealth", new { entity, health });
    }

    public override void OnEntityMetadata(Entity entity, Dictionary<int, object?>? metadata)
    {
        SendEvent("OnEntityMetadata", new { entity, metadata });
    }

    public override void OnPlayerStatus(byte statusId)
    {
        SendEvent("OnPlayerStatus", new { statusId });
    }

    public override void OnNetworkPacket(int packetID, List<byte> packetData, bool isLogin, bool isInbound)
    {
        SendEvent("OnNetworkPacket", new { packetId = packetID, isLogin, isInbound, packetData });
    }

    // ==========================================================================================
    // Helper methods
    // ==========================================================================================

    private void SendEvent(string type, object data, bool overrideAuth = false)
    {
        if (_server == null)
            return;

        foreach (var (sessionId, _) in _server!.Sessions)
            SendSessionEvent(sessionId, type, JsonConvert.SerializeObject(data), overrideAuth);
    }

    private void SendEvent(string type, string data, bool overrideAuth = false)
    {
        if (_server == null)
            return;

        foreach (var (sessionId, _) in _server.Sessions)
            SendSessionEvent(sessionId, type, data, overrideAuth);
    }

    private void SendSessionEvent(string sessionId, string type, string data, bool overrideAuth = false)
    {
        if (sessionId.Length > 0 && (overrideAuth || _authenticatedSessions.Contains(sessionId)))
        {
            _server?.SendToSession(sessionId,
                    $"{{\"event\": \"{type}\", \"data\": {(string.IsNullOrEmpty(data) ? "null" : $"\"{Json.EscapeString(data)}\"")}}}")
                .Wait();

            if (!(type.Contains("Entity") || type.Equals("OnTimeUpdate") || type.Equals("OnServerTpsUpdate")) &&
                Config.DebugMode)
                LogDebugToConsole(
                    $"\n\n\tSending:\n\n\t{{\"event\": \"{type}\", \"data\": {(string.IsNullOrEmpty(data)
                        ? "null"
                        : $"\"{Json.EscapeString(data)}\"")}}}\n\n");
        }
    }

    public void SendCommandResponse(string sessionId, bool success, string requestId, string command,
        string result, bool overrideAuth = false)
    {
        SendSessionEvent(sessionId, "OnWsCommandResponse",
            $"{{\"success\": {success.ToString().ToLower()}, \"requestId\": \"{requestId}\", \"command\": \"{command}\", \"result\": {(string.IsNullOrEmpty(result) ? "null" : result)}}}",
            overrideAuth);
    }

    private static string GameModeString(int gameMode)
    {
        return gameMode switch
        {
            0 => "survival",
            1 => "creative",
            2 => "adventure",
            3 => "spectator",
            _ => "unknown"
        };
    }
}