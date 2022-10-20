﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;
using Newtonsoft.Json;
using Tomlet.Attributes;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace MinecraftClient.ChatBots
{
    public class WsServer
    {
        private WebSocketServer _server;

        public static event EventHandler<string>? OnNewSession;
        public static event EventHandler<string>? OnSessionClose;
        public static event EventHandler<string>? OnMessageRecived;

        public WsServer(string ip, int port)
        {
            _server = new WebSocketServer(IPAddress.Parse(ip), port);
            _server.AddWebSocketService<WsBehavior>("/mcc");
            _server.Start();
        }

        public void Stop()
        {
            _server.Stop();
        }

        public class WsBehavior : WebSocketBehavior
        {
            private string _sessionId { get; set; } = "";
            public bool _authenticated { get; set; } = false;

            public WsBehavior()
            {
                IgnoreExtensions = true;
            }

            protected override void OnOpen()
            {
                _sessionId = Guid.NewGuid().ToString();
                OnNewSession!.Invoke(this, _sessionId);
            }

            protected override void OnClose(CloseEventArgs e)
            {
                OnSessionClose!.Invoke(this, _sessionId);
            }

            protected override void OnMessage(MessageEventArgs websocketEvent)
            {
                WsServer.OnMessageRecived!.Invoke(this, websocketEvent.Data);
            }

            public void SendToClient(string text)
            {
                Send(text);
            }

            public void SetSessionId(string newSessionId)
            {
                _sessionId = newSessionId;
            }

            public string GetSessionId()
            {
                return _sessionId;
            }

            public void SetAuthenticated(bool authenticated)
            {
                _authenticated = authenticated;
            }

            public bool IsAuthenticated()
            {
                return _authenticated;
            }
        }
    }

    internal class WsChatBotCommand
    {
        [JsonProperty("command")]
        public string Command { get; set; } = "";

        [JsonProperty("requestId")]
        public string RequestId { get; set; } = "";

        [JsonProperty("parameters")]
        public object[]? Parameters { get; set; }
    }

    internal class WsCommandResponder
    {
        private WebSocketBot _bot;
        private WsServer.WsBehavior _session;
        private string _command;
        private string _requestId;

        public WsCommandResponder(WebSocketBot bot, WsServer.WsBehavior session, string command, string requestId)
        {
            _bot = bot;
            _session = session;
            _command = command;
            _requestId = requestId;
        }

        private void SendCommandResponse(bool success, string result, bool overrideAuth = false)
        {
            _bot.SendCommandResponse(_session, success, _requestId, _command, result, overrideAuth);
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
            return "\"" + text + "\"";
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

        public override Dictionary<string, object>? ReadJson(JsonReader reader, Type objectType, Dictionary<string, object>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var keyValuePairs = serializer.Deserialize<IEnumerable<KeyValuePair<string, object>>>(reader);
            return new Dictionary<string, object>(keyValuePairs!);
        }
    }

    public class WebSocketBot : ChatBot
    {
        private string? _ip;
        private int _port;
        private string? _password;
        private WsServer? _server;
        private Dictionary<string, WsServer.WsBehavior>? _sessions;

        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "Websocket";

            public bool Enabled = false;

            [TomlInlineComment("$config.ChatBot.WebSocketBot.Ip$")]
            public string? Ip = "127.0.0.1";

            [TomlInlineComment("$config.ChatBot.WebSocketBot.Port$")]
            public int Port = 8043;

            [TomlInlineComment("$config.ChatBot.WebSocketBot.Password$")]
            public string? Password = "wspass12345";
        }

        public WebSocketBot()
        {
            Match match = Regex.Match(Config.Ip!, @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}");

            if (!match.Success)
            {
                LogToConsole(Translations.TryGet("bot.WebSocketBot.failed_to_start.ip"));
                return;
            }

            if (Config.Port > 65535)
            {
                LogToConsole(Translations.TryGet("bot.WebSocketBot.failed_to_start.port"));
                return;
            }

            _ip = Config.Ip;
            _port = Config.Port;
            _password = Config.Password;
        }

        public override void Initialize()
        {
            if (_server != null)
            {
                SendEvent("OnWsRestarting", "");
                _server.Stop();
            }

            try
            {
                LogToConsole(Translations.TryGet("bot.WebSocketBot.starting"));
                _server = new WsServer(_ip!, _port);
                _sessions = new Dictionary<string, WsServer.WsBehavior>();

                LogToConsole(Translations.TryGet("bot.WebSocketBot.started", _ip, _port));
            }
            catch (Exception e)
            {
                LogToConsole(Translations.TryGet("bot.WebSocketBot.failed_to_start.custom", e));
                return;
            }

            WsServer.OnNewSession += (sender, id) =>
            {
                LogToConsole(Translations.TryGet("bot.WebSocketBot.new_session", id));
                _sessions[id] = (WsServer.WsBehavior)sender!;
            };

            WsServer.OnSessionClose += (sender, id) =>
            {
                LogToConsole(Translations.TryGet("bot.WebSocketBot.session_disconnected", id));
                _sessions.Remove(id);
            };

            WsServer.OnMessageRecived += (sender, message) =>
            {
                var session = (WsServer.WsBehavior)sender!;

                if (!ProcessWebsocketCommand(session, _password!, message))
                    return;

                string? result = "";
                PerformInternalCommand(message, ref result);
                SendSessionEvent(session, "OnMccCommandResponse", "{\"response\": \"" + result + "\"}");
            };
        }

        public bool ProcessWebsocketCommand(WsServer.WsBehavior session, string password, string message)
        {
            message = message.Trim();

            if (string.IsNullOrEmpty(message))
                return false;

            if (message.StartsWith('{'))
            {
                try
                {
                    LogDebugToConsole("\n\n\tGot command\n\n\t" + message + "\n\n");

                    WsChatBotCommand cmd = JsonConvert.DeserializeObject<WsChatBotCommand>(message)!;
                    WsCommandResponder responder = new WsCommandResponder(this, session, cmd.Command, cmd.RequestId);

                    // Authentication and session commands
                    if (password.Length != 0)
                    {
                        if (!session.IsAuthenticated())
                        {
                            // Allow session name changing before login for easier identification
                            if (cmd.Command.Equals("ChangeSessionId", StringComparison.OrdinalIgnoreCase))
                            {
                                if (cmd.Parameters == null || cmd.Parameters.Length != 1)
                                {
                                    responder.SendErrorResponse(responder.Quote("Invalid number of parameters, expected 1 (newSessionid)!"), true);
                                    return false;
                                }

                                string newId = (cmd.Parameters[0] as string)!;

                                if (newId.Length == 0)
                                {
                                    responder.SendErrorResponse(responder.Quote("Please provide a valid session ID!"), true);
                                    return false;
                                }

                                if (newId.Length > 32)
                                {
                                    responder.SendErrorResponse(responder.Quote("The session ID can't be longer than 32 characters!"), true);
                                    return false;
                                }

                                string oldId = session.GetSessionId();

                                _sessions!.Remove(oldId);
                                _sessions[newId] = session;
                                session.SetSessionId(newId);

                                responder.SendSuccessResponse(responder.Quote("The session ID was successfully changed to: '" + newId + "'"), true);
                                LogToConsole(Translations.TryGet("bot.WebSocketBot.session_id_changed", oldId, newId));
                                return false;
                            }

                            // Special case for authentication
                            if (cmd.Command.Equals("Authenticate", StringComparison.OrdinalIgnoreCase))
                            {
                                if (cmd.Parameters == null || cmd.Parameters.Length != 1)
                                {
                                    responder.SendErrorResponse(responder.Quote("Invalid number of parameters, expected 1 (password)!"), true);
                                    return false;
                                }

                                string pass = (cmd.Parameters[0] as string)!;

                                if (pass.Length == 0)
                                {
                                    responder.SendErrorResponse(responder.Quote("Please provide a valid password! (Example: 'Authenticate password123')"), true);
                                    return false;
                                }

                                if (!pass.Equals(password))
                                {
                                    responder.SendErrorResponse(responder.Quote("Incorrect password provided!"), true);
                                    return false;
                                }

                                session.SetAuthenticated(true);
                                responder.SendSuccessResponse(responder.Quote("Succesfully authenticated!"), true);
                                LogToConsole(Translations.TryGet("bot.WebSocketBot.session_authenticated", session.GetSessionId()));
                                return false;
                            }

                            responder.SendErrorResponse(responder.Quote("You must authenticate in order to send and recieve data!"), true);
                            return false;
                        }
                    }
                    else
                    {
                        if (!session.IsAuthenticated())
                        {
                            responder.SendSuccessResponse(responder.Quote("Succesfully authenticated!"));
                            LogToConsole(Translations.TryGet("bot.WebSocketBot.session_authenticated", session.GetSessionId()));
                            session.SetAuthenticated(true);
                            return false;
                        }
                    }

                    // Process other commands
                    switch (cmd.Command)
                    {
                        case "LogToConsole":
                            if (cmd.Parameters == null || cmd.Parameters.Length > 1 || cmd.Parameters.Length < 1)
                            {
                                responder.SendErrorResponse(responder.Quote("Invalid number of parameters, expecting a single parameter!"));
                                return false;
                            }

                            LogToConsole((cmd.Parameters[0] as string)!);
                            responder.SendSuccessResponse();
                            break;

                        case "LogDebugToConsole":
                            if (cmd.Parameters == null || cmd.Parameters.Length > 1 || cmd.Parameters.Length < 1)
                            {
                                responder.SendErrorResponse(responder.Quote("Invalid number of parameters, expecting a single parameter!"));
                                return false;
                            }

                            LogDebugToConsole((cmd.Parameters[0] as string)!);
                            responder.SendSuccessResponse();
                            break;

                        case "LogToConsoleTranslated":
                            if (cmd.Parameters == null || cmd.Parameters.Length > 1 || cmd.Parameters.Length < 1)
                            {
                                responder.SendErrorResponse(responder.Quote("Invalid number of parameters, expecting a single parameter!"));
                                return false;
                            }

                            LogToConsoleTranslated((cmd.Parameters[0] as string)!);
                            responder.SendSuccessResponse();
                            break;

                        case "LogDebugToConsoleTranslated":
                            if (cmd.Parameters!.Length > 1 || cmd.Parameters.Length < 1)
                            {
                                responder.SendErrorResponse(responder.Quote("Invalid number of parameters, expecting a single parameter!"));
                                return false;
                            }

                            LogDebugToConsoleTranslated((cmd.Parameters[0] as string)!);
                            responder.SendSuccessResponse();
                            break;

                        case "ReconnectToTheServer":
                            if (cmd.Parameters == null || cmd.Parameters.Length != 2)
                            {
                                responder.SendErrorResponse(responder.Quote("Invalid number of parameters, expecting 2 parameters (extraAttempts, delaySeconds)!"));
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
                            if (cmd.Parameters == null || cmd.Parameters.Length != 2)
                            {
                                responder.SendErrorResponse(responder.Quote("Invalid number of parameters, expecting 2 parameters (player, message)!"));
                                return false;
                            }

                            SendPrivateMessage((cmd.Parameters[0] as string)!, (cmd.Parameters[1] as string)!);
                            responder.SendSuccessResponse();
                            break;

                        case "RunScript":
                            if (cmd.Parameters == null || cmd.Parameters.Length != 1)
                            {
                                responder.SendErrorResponse(responder.Quote("Invalid number of parameters, expecting 1 parameter (filename)!"));
                                return false;
                            }

                            RunScript((cmd.Parameters[0] as string)!);
                            responder.SendSuccessResponse();
                            break;

                        case "GetTerrainEnabled":
                            responder.SendSuccessResponse(GetTerrainEnabled().ToString().ToLower());
                            break;

                        case "SetTerrainEnabled":
                            if (cmd.Parameters == null || cmd.Parameters.Length != 1)
                            {
                                responder.SendErrorResponse(responder.Quote("Invalid number of parameters, expecting 1 parameter (enabled)!"));
                                return false;
                            }

                            SetTerrainEnabled((bool)cmd.Parameters[0]);
                            responder.SendSuccessResponse();
                            break;

                        case "GetEntityHandlingEnabled":
                            responder.SendSuccessResponse(GetEntityHandlingEnabled().ToString().ToLower());
                            break;

                        case "Sneak":
                            if (cmd.Parameters == null || cmd.Parameters.Length != 1)
                            {
                                responder.SendErrorResponse(responder.Quote("Invalid number of parameters, expecting 1 parameter (on)!"));
                                return false;
                            }

                            Sneak((bool)cmd.Parameters[0]);
                            responder.SendSuccessResponse();
                            break;

                        case "SendEntityAction":
                            if (cmd.Parameters == null || cmd.Parameters.Length != 1)
                            {
                                responder.SendErrorResponse(responder.Quote("Invalid number of parameters, expecting 1 parameter (actionType)!"));
                                return false;
                            }

                            SendEntityAction(((Protocol.EntityActionType)(Convert.ToInt32(cmd.Parameters[0]))));
                            responder.SendSuccessResponse();
                            break;

                        case "DigBlock":
                            if (cmd.Parameters == null || cmd.Parameters.Length == 0 || cmd.Parameters.Length < 3 || cmd.Parameters.Length > 5)
                            {
                                responder.SendErrorResponse(responder.Quote("Invalid number of parameters, expecting 1 or 3 parameter(s) (location, swingArms?, lookAtBlock?)!"));
                                return false;
                            }

                            Location location = new Location(Convert.ToInt32(cmd.Parameters[0]), Convert.ToInt32(cmd.Parameters[1]), Convert.ToInt32(cmd.Parameters[2]));

                            if (location.DistanceSquared(GetCurrentLocation().EyesLocation()) > 25)
                            {
                                responder.SendErrorResponse(responder.Quote("The block you're trying to dig is too far away!"));
                                return false;
                            }

                            if (GetWorld().GetBlock(location).Type == Material.Air)
                            {
                                responder.SendErrorResponse(responder.Quote("The block you're trying to dig is is air!"));
                                return false;
                            }

                            bool resullt = false;

                            if (cmd.Parameters.Length == 3)
                                resullt = DigBlock(location);
                            else if (cmd.Parameters.Length == 4)
                                resullt = DigBlock(location, (bool)cmd.Parameters[3]);
                            else if (cmd.Parameters.Length == 5)
                                resullt = DigBlock(location, (bool)cmd.Parameters[3], (bool)cmd.Parameters[4]);

                            responder.SendSuccessResponse(JsonConvert.SerializeObject(resullt));
                            break;

                        case "SetSlot":
                            if (cmd.Parameters == null || cmd.Parameters.Length != 1)
                            {
                                responder.SendErrorResponse(responder.Quote("Invalid number of parameters, expecting 1 parameter (slotNumber)!"));
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
                            if (cmd.Parameters == null || cmd.Parameters.Length == 0 || cmd.Parameters.Length < 3 || cmd.Parameters.Length > 8)
                            {
                                responder.SendErrorResponse(responder.Quote("Invalid number of parameters, expecting 1 or 7 parameter(s) (x, y, z, allowUnsafe?, allowDirectTeleport?, maxOffset?, minoffset?, timeout?)!"));
                                return false;
                            }

                            bool allowUnsafe = false;
                            bool allowDirectTeleport = false;
                            int maxOffset = 0;
                            int minOffset = 0;
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

                            bool canMove = MoveToLocation(
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
                            if (cmd.Parameters == null || cmd.Parameters.Length == 0 || cmd.Parameters.Length < 3 || cmd.Parameters.Length > 3)
                            {
                                responder.SendErrorResponse(responder.Quote("Invalid number of parameters, expecting 3 parameter(s) (x, y, z)!"));
                                return false;
                            }

                            LookAtLocation(new Location(Convert.ToInt32(cmd.Parameters[0]), Convert.ToInt32(cmd.Parameters[1]), Convert.ToInt32(cmd.Parameters[2])));
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
                            if (cmd.Parameters == null || cmd.Parameters.Length == 0 || cmd.Parameters.Length < 2 || cmd.Parameters.Length > 3)
                            {
                                responder.SendErrorResponse(responder.Quote("Invalid number of parameters, expecting at least 2 and at most 3 parameter(s) (entityId, interactionType, hand?)!"));
                                return false;
                            }

                            InteractType interactionType = (InteractType)Convert.ToInt32(cmd.Parameters[1]);

                            Hand interactionHand = Hand.MainHand;

                            if (cmd.Parameters.Length == 3)
                                interactionHand = (Hand)Convert.ToInt32(cmd.Parameters[2]);

                            responder.SendSuccessResponse(JsonConvert.SerializeObject(InteractEntity(Convert.ToInt32(cmd.Parameters[0]), interactionType, interactionHand)));
                            break;

                        case "CreativeGive":
                            if (cmd.Parameters == null || cmd.Parameters.Length == 0 || cmd.Parameters.Length < 3 || cmd.Parameters.Length > 4)
                            {
                                responder.SendErrorResponse(responder.Quote("Invalid number of parameters, expecting at least 3 and at most 4 parameter(s) (slotId, itemType, count, nbt?)!"));
                                return false;
                            }

                            NBT? nbt = null;

                            if (cmd.Parameters.Length == 4)
                                nbt = JsonConvert.DeserializeObject<NBT>(cmd.Parameters[3].ToString()!, new NbtDictionaryConverter())!;

                            responder.SendSuccessResponse(
                                JsonConvert.SerializeObject(CreativeGive(
                                    Convert.ToInt32(cmd.Parameters[0]),
                                    (ItemType)Convert.ToInt32(cmd.Parameters[1]),
                                    Convert.ToInt32(cmd.Parameters[2]),
                                    nbt == null ? new Dictionary<string, object>() : nbt!.nbt!)
                                    ));

                            break;

                        case "CreativeDelete":
                            if (cmd.Parameters == null || cmd.Parameters.Length != 1)
                            {
                                responder.SendErrorResponse(responder.Quote("Invalid number of parameters, expecting at 1 parameter (slotId)!"));
                                return false;
                            }

                            responder.SendSuccessResponse(JsonConvert.SerializeObject(CreativeDelete(Convert.ToInt32(cmd.Parameters[0]))));
                            break;

                        case "SendAnimation":
                            Hand hand = Hand.MainHand;

                            if (cmd.Parameters != null && cmd.Parameters.Length == 1)
                                hand = (Hand)Convert.ToInt32(cmd.Parameters[0]);

                            responder.SendSuccessResponse(JsonConvert.SerializeObject(SendAnimation(hand)));
                            break;

                        case "SendPlaceBlock":
                            if (cmd.Parameters == null || cmd.Parameters.Length == 0 || cmd.Parameters.Length < 4 || cmd.Parameters.Length > 4)
                            {
                                responder.SendErrorResponse(responder.Quote("Invalid number of parameters, expecting at least 4 and at most 5 parameters (x, y, z, blockFace, hand?)!"));
                                return false;
                            }

                            Location blockLocation = new Location(Convert.ToInt32(cmd.Parameters[0]), Convert.ToInt32(cmd.Parameters[1]), Convert.ToInt32(cmd.Parameters[2]));

                            Direction blockFacingDirection = (Direction)Convert.ToInt32(cmd.Parameters[3]);
                            Hand handToUse = Hand.MainHand;

                            if (cmd.Parameters.Length == 4)
                                handToUse = (Hand)Convert.ToInt32(cmd.Parameters[4]);

                            responder.SendSuccessResponse(JsonConvert.SerializeObject(SendPlaceBlock(blockLocation, blockFacingDirection, handToUse)));
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
                                responder.SendErrorResponse(responder.Quote("Invalid number of parameters, expecting 3 parameters (inventoryId, slotId, windowActionType)!"));
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
                            if (cmd.Parameters == null || cmd.Parameters.Length != 1)
                            {
                                responder.SendErrorResponse(responder.Quote("Invalid number of parameters, expecting 1 parameter (slotId)!"));
                                return false;
                            }

                            responder.SendSuccessResponse(JsonConvert.SerializeObject(ChangeSlot((short)Convert.ToInt32(cmd.Parameters[0]))));
                            break;

                        case "GetCurrentSlot":
                            responder.SendSuccessResponse(JsonConvert.SerializeObject(GetCurrentSlot()));
                            break;

                        case "ClearInventories":
                            responder.SendSuccessResponse(JsonConvert.SerializeObject(ClearInventories()));
                            break;

                        case "UpdateSign":
                            if (cmd.Parameters == null || cmd.Parameters.Length != 7)
                            {
                                responder.SendErrorResponse(responder.Quote("Invalid number of parameters, expecting 1 parameter (x, y, z, line1, line2, line3, line4)!"));
                                return false;
                            }

                            Location signLocation = new Location(Convert.ToInt32(cmd.Parameters[0]), Convert.ToInt32(cmd.Parameters[1]), Convert.ToInt32(cmd.Parameters[2]));

                            responder.SendSuccessResponse(
                                JsonConvert.SerializeObject(UpdateSign(signLocation,
                                    (string)cmd.Parameters[3],
                                    (string)cmd.Parameters[4],
                                    (string)cmd.Parameters[5],
                                    (string)cmd.Parameters[6]
                                )));
                            break;

                        case "SelectTrade":
                            if (cmd.Parameters == null || cmd.Parameters.Length != 1)
                            {
                                responder.SendErrorResponse(responder.Quote("Invalid number of parameters, expecting 1 parameter (selectedSlot)!"));
                                return false;
                            }

                            responder.SendSuccessResponse(JsonConvert.SerializeObject(SelectTrade(Convert.ToInt32(cmd.Parameters[0]))));
                            break;

                        case "UpdateCommandBlock":
                            if (cmd.Parameters == null || cmd.Parameters.Length != 6)
                            {
                                responder.SendErrorResponse(responder.Quote("Invalid number of parameters, expecting 1 parameter (x, y, z, command, commandBlockMode, commandBlockFlags)!"));
                                return false;
                            }

                            Location commandBlockLocation = new Location(Convert.ToInt32(cmd.Parameters[0]), Convert.ToInt32(cmd.Parameters[1]), Convert.ToInt32(cmd.Parameters[2]));

                            responder.SendSuccessResponse(
                                UpdateCommandBlock(commandBlockLocation,
                                    (string)cmd.Parameters[3],
                                    (CommandBlockMode)Convert.ToInt32(cmd.Parameters[4]),
                                    (CommandBlockFlags)Convert.ToInt32(cmd.Parameters[5])
                                ).ToString().ToLower());
                            break;

                        case "CloseInventory":
                            if (cmd.Parameters == null || cmd.Parameters.Length != 1)
                            {
                                responder.SendErrorResponse(responder.Quote("Invalid number of parameters, expecting 1 parameter (inventoryId)!"));
                                return false;
                            }

                            responder.SendSuccessResponse(CloseInventory(Convert.ToInt32(cmd.Parameters[0])).ToString().ToLower());
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
                    }
                }
                catch (Exception e)
                {
                    LogDebugToConsole(e.Message);
                    SendSessionEvent(session, "OnWsCommandResponse", "{\"success\": false, \"message\": \"An error occured, possible reasons: mailformed json, type conversion, internal error\", \"stackTrace\": \"" + Json.EscapeString(e.ToString()) + "\"}", true);
                    return false;
                }

                return false;
            }
            else
            {
                if (password.Length != 0)
                {
                    if (!session.IsAuthenticated())
                    {
                        SendSessionEvent(session, "OnWsCommandResponse", "{\"error\": true, \"message\": \"You must authenticate in order to send and recieve data!\"}", true);
                        return false;
                    }
                }
                else
                {
                    if (!session.IsAuthenticated())
                    {
                        SendSessionEvent(session, "OnWsCommandResponse", "{\"success\": true, \"message\": \"Succesfully authenticated!\"}", true);
                        LogToConsole(Translations.TryGet("bot.WebSocketBot.session_authenticated", session.GetSessionId()));
                        session.SetAuthenticated(true);
                        return true;
                    }
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
            }
        }

        // ==========================================================================================
        // Bot Events
        // ==========================================================================================
        public override void AfterGameJoined()
        {
            SendEvent("OnGameJoined", "");
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

            string message = "";
            string username = "";

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
            string reasonString = "Unknown";

            switch (reason)
            {
                case DisconnectReason.ConnectionLost:
                    reasonString = "Connection Lost";
                    break;

                case DisconnectReason.UserLogout:
                    reasonString = "User Logout";
                    break;

                case DisconnectReason.InGameKick:
                    reasonString = "In-Game Kick";
                    break;

                case DisconnectReason.LoginRejected:
                    reasonString = "Login Rejected";
                    break;
            }

            SendEvent("OnDisconnect", new { reason = reasonString, message });
            return false;
        }

        public override void OnPlayerProperty(Dictionary<string, double> prop)
        {
            SendEvent("OnPlayerProperty", prop);
        }

        public override void OnServerTpsUpdate(Double tps)
        {
            SendEvent("OnServerTpsUpdate", new { tps });
        }

        public override void OnTimeUpdate(long WorldAge, long TimeOfDay)
        {
            SendEvent("OnTimeUpdate", new { worldAge = WorldAge, timeOfDay = TimeOfDay });
        }

        public override void OnEntityMove(Entity entity)
        {
            SendEvent("OnEntityMove", entity);
        }

        public override void OnInternalCommand(string commandName, string commandParams, string Result)
        {
            SendEvent("OnInternalCommand", new { command = commandName, parameters = commandParams, result = Result.Replace("\"", "'") });
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

        public override void OnExplosion(Location explode, float strength, int recordcount)
        {
            SendEvent("OnExplosion", new { location = explode, strength, recordCount = recordcount });
        }

        public override void OnSetExperience(float Experiencebar, int Level, int TotalExperience)
        {
            SendEvent("OnSetExperience", new { experienceBar = Experiencebar, level = Level, totalExperience = TotalExperience });
        }

        public override void OnGamemodeUpdate(string playername, Guid uuid, int gamemode)
        {
            SendEvent("OnGamemodeUpdate", new { playerName = playername, uuid, gameMode = GameModeString(gamemode) });
        }

        public override void OnLatencyUpdate(string playername, Guid uuid, int latency)
        {
            SendEvent("OnLatencyUpdate", new { playerName = playername, uuid, latency });
        }

        public override void OnMapData(int mapid, byte scale, bool trackingPosition, bool locked, List<MapIcon> icons, byte columnsUpdated, byte rowsUpdated, byte mapCoulmnX, byte mapRowZ, byte[]? colors)
        {
            SendEvent("OnMapData", new { mapId = mapid, scale, trackingPosition, locked, icons, columnsUpdated, rowsUpdated, mapCoulmnX, mapRowZ, colors });
        }

        public override void OnTradeList(int windowID, List<VillagerTrade> trades, VillagerInfo villagerInfo)
        {
            SendEvent("OnTradeList", new { windowId = windowID, trades, villagerInfo });
        }

        public override void OnTitle(int action, string titletext, string subtitletext, string actionbartext, int fadein, int stay, int fadeout, string json_)
        {
            SendEvent("OnTitle", new { action, titleText = titletext, subtitleText = subtitletext, actionBarText = actionbartext, fadeIn = fadein, stay, rawJson = json_ });
        }

        public override void OnEntityEquipment(Entity entity, int slot, Item? item)
        {
            SendEvent("OnEntityEquipment", new { entity, slot, item });
        }

        public override void OnEntityEffect(Entity entity, Effects effect, int amplifier, int duration, byte flags)
        {
            SendEvent("OnEntityEffect", new { entity, effect, amplifier, duration, flags });
        }

        public override void OnScoreboardObjective(string objectivename, byte mode, string objectivevalue, int type, string json_)
        {
            SendEvent("OnScoreboardObjective", new { objectiveName = objectivename, mode, objectiveValue = objectivevalue, type, rawJson = json_ });
        }

        public override void OnUpdateScore(string entityname, int action, string objectivename, int value)
        {
            SendEvent("OnUpdateScore", new { entityName = entityname, action, objectiveName = objectivename, type = value });
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
            SendEvent("OnPlayerLeave", new { uuid, name = (name == null ? "null" : name) });
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
            foreach (KeyValuePair<string, WsServer.WsBehavior> pair in _sessions!)
                SendSessionEvent(pair.Value, type, JsonConvert.SerializeObject(data), overrideAuth);
        }

        private void SendEvent(string type, string data, bool overrideAuth = false)
        {
            foreach (KeyValuePair<string, WsServer.WsBehavior> pair in _sessions!)
                SendSessionEvent(pair.Value, type, data, overrideAuth);
        }

        private void SendSessionEvent(WsServer.WsBehavior session, string type, string data, bool overrideAuth = false)
        {
            if (session != null && (overrideAuth || session.IsAuthenticated()))
            {
                session.SendToClient("{\"event\": \"" + type + "\", \"data\": " + (data.IsNullOrEmpty() ? "null" : "\"" + Json.EscapeString(data) + "\"") + "}");

                if (!(type.Contains("Entity") || type.Equals("OnTimeUpdate") || type.Equals("OnServerTpsUpdate")))
                    LogDebugToConsole(
                        "\n\n\tSending:\n\n\t{\"event\": \"" + type +
                        "\", \"data\": " + (data.IsNullOrEmpty() ? "null" : "\""
                        + Json.EscapeString(data) + "\"") + "}\n\n");
            }
        }

        public void SendCommandResponse(WsServer.WsBehavior session, bool success, string requestId, string command, string result, bool overrideAuth = false)
        {
            SendSessionEvent(session, "OnWsCommandResponse", "{\"success\": " + success.ToString().ToLower() + ", \"requestId\": \"" + requestId + "\", \"command\": \"" + command + "\", \"result\": " + (string.IsNullOrEmpty(result) ? "null" : result) + "}", overrideAuth);
        }

        public string GameModeString(int gameMode)
        {
            if (gameMode == 0)
                return "survival";

            if (gameMode == 1)
                return "creative";

            if (gameMode == 2)
                return "adventure";

            if (gameMode == 3)
                return "spectator";

            return "unknown";
        }
    }
}
