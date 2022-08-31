﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace MinecraftClient.ChatBots
{
    internal class WsServer
    {
        private WebSocketServer _server;

        public static event EventHandler<string>? OnNewSession;
        public static event EventHandler<string>? OnSessionClose;
        public static event EventHandler<string>? OnMessageRecived;

        public WsServer(int port)
        {
            _server = new WebSocketServer(port);
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
        public object[] Parameters { get; set; }
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
            SendSuccessResponse("", overrideAuth);
        }

        public string Quote(string text)
        {
            return "\"" + text + "\"";
        }
    }

    class WebSocketBot : ChatBot
    {
        private WsServer? _server;
        private Dictionary<string, WsServer.WsBehavior>? _sessions;

        public WebSocketBot(int port, string password)
        {
            if (port > 65535)
            {
                LogToConsole("§cFailed to start a server! The port number provided is out of the range, it must be 65535 or bellow it!");
                return;
            }

            try
            {
                _server = new WsServer(port);
                _sessions = new Dictionary<string, WsServer.WsBehavior>();

                LogToConsole("§bServer started on port: §a" + port);
            }
            catch (Exception e)
            {
                LogToConsole("§cFailed to start a server!");
                LogToConsole(e);
                return;
            }

            WsServer.OnNewSession += (sender, id) =>
            {
                LogToConsole("§bNew session connected: §a" + id);
                _sessions[id] = (WsServer.WsBehavior)sender!;
            };

            WsServer.OnSessionClose += (sender, id) =>
            {
                LogToConsole("§bSession with an id §a\"" + id + "\"§b has disconnected!");
                _sessions.Remove(id);
            };

            WsServer.OnMessageRecived += (sender, message) =>
            {
                var session = (WsServer.WsBehavior)sender!;

                if (!ProcessWebsocketCommand(session, password, message))
                    return;

                LogDebugToConsole("Got a message: " + message);

                string result = "";
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
                                LogToConsole("§bSession with an id §a\"" + oldId + "\"§b has been renamed to: §a\"" + newId + "\"§b!");
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
                                LogToConsole("§bSession with an id §a\"" + session.GetSessionId() + "\"§b has been succesfully authenticated!\"!");
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
                            LogToConsole("§bSession with an id §a\"" + session.GetSessionId() + "\"§b has been succesfully authenticated!\"!");
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
                            if (cmd.Parameters.Length > 1 || cmd.Parameters.Length < 1)
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
                                responder.SendErrorResponse(responder.Quote("Invalid number of parameters, expecting 1 or 3 parameter(s) (location, swingArms, lookAtBlock)!"));
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

                            responder.SendSuccessResponse(resullt.ToString().ToLower());
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
                    }
                }
                catch (Exception e)
                {
                    LogToConsole(e.Message);
                    SendSessionEvent(session, "OnWsCommandResponse", "{\"success\": false, \"message\": \"Invalid command format, probably malformed JSON!\"}", true);
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
                        LogToConsole("§bSession with an id §a\"" + session.GetSessionId() + "\"§b has been succesfully authenticated!\"!");
                        session.SetAuthenticated(true);
                        return true;
                    }
                }
            }

            return true;
        }

        public override void OnUnload()
        {
            _server!.Stop();
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
            StringBuilder json = new StringBuilder();

            json.Append("{");
            json.Append("\"entity\": " + EntityToJson(entity) + ",");
            json.Append("\"location\": " + LocationToJson(location) + ",");
            json.Append("\"stage\": " + stage);
            json.Append("}");

            SendEvent("OnBlockBreakAnimation", json.ToString());
        }

        public override void OnEntityAnimation(Entity entity, byte animation)
        {
            StringBuilder json = new StringBuilder();

            json.Append("{");
            json.Append("\"entity\": " + EntityToJson(entity) + ",");
            json.Append("\"animation\": " + animation);
            json.Append("}");

            SendEvent("OnEntityAnimation", json.ToString());
        }

        public override void GetText(string text)
        {
            string message = "";
            string username = "";

            if (IsPrivateMessage(text, ref message, ref username))
                SendEvent("OnChatPrivate", "{\"sender\": \"" + username + "\", \"message\": \"" + message + "\", \"rawText\": \"" + text + "\"}");
            else if (IsChatMessage(text, ref message, ref username))
                SendEvent("OnChatPublic", "{\"sender\": \"" + username + "\", \"message\": \"" + message + "\", \"rawText\": \"" + text + "\"}");
            else if (IsTeleportRequest(text, ref username))
                SendEvent("OnTeleportRequest", "{\"sender\": \"" + username + "\", \"rawText\": \"" + text + "\"}");
        }

        public override void GetText(string text, string? json)
        {
            SendEvent("OnChatRaw", json!);
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

            SendEvent("OnDisconnect", "{\"reason\": \"" + reasonString + "\", \"message\": \"" + message + "\"}");
            return false;
        }

        public override void OnPlayerProperty(Dictionary<string, double> prop)
        {
            SendEvent("OnPlayerProperty", JsonConvert.SerializeObject(prop));
        }

        public override void OnServerTpsUpdate(Double tps)
        {
            SendEvent("OnServerTpsUpdate", "{\"tps\": " + Math.Round(tps) + "}");
        }

        public override void OnTimeUpdate(long WorldAge, long TimeOfDay)
        {
            SendEvent("OnTimeUpdate", "{\"worldAge\": " + WorldAge + ", \"timeOfDay\": " + TimeOfDay + "}");
        }

        public override void OnEntityMove(Entity entity)
        {
            SendEvent("OnEntityMove", EntityToJson(entity));
        }

        public override void OnInternalCommand(string commandName, string commandParams, string Result)
        {
            StringBuilder json = new StringBuilder();

            json.Append("{");
            json.Append("\"command\": \"" + commandName + "\",");
            json.Append("\"parameters\": \"" + commandParams + "\",");
            json.Append("\"result\": \"" + Result.Replace("\"", "'") + "\"");
            json.Append("}");

            SendEvent("OnInternalCommand", json.ToString());
        }

        public override void OnEntitySpawn(Entity entity)
        {
            SendEvent("OnEntitySpawn", EntityToJson(entity));
        }

        public override void OnEntityDespawn(Entity entity)
        {
            SendEvent("OnEntityDespawn", EntityToJson(entity));
        }

        public override void OnHeldItemChange(byte slot)
        {
            SendEvent("OnHeldItemChange", "{\"itemSlot\": " + slot + "}");
        }

        public override void OnHealthUpdate(float health, int food)
        {
            SendEvent("OnHealthUpdate", "{\"health\": " + health + ", \"food\": " + food + "}");
        }

        public override void OnExplosion(Location explode, float strength, int recordcount)
        {
            StringBuilder json = new StringBuilder();

            json.Append("{");
            json.Append("\"location\": " + LocationToJson(explode) + ",");
            json.Append("\"strength\": " + strength + ",");
            json.Append("\"recordCount\": " + recordcount);
            json.Append("}");

            SendEvent("OnExplosion", json.ToString());
        }

        public override void OnSetExperience(float Experiencebar, int Level, int TotalExperience)
        {
            StringBuilder json = new StringBuilder();

            json.Append("{");
            json.Append("\"experienceBar\": " + Math.Floor(Experiencebar) + ",");
            json.Append("\"level\": " + Level + ",");
            json.Append("\"totalExperience\": " + TotalExperience);
            json.Append("}");

            SendEvent("OnSetExperience", json.ToString());
        }

        public override void OnGamemodeUpdate(string playername, Guid uuid, int gamemode)
        {
            StringBuilder json = new StringBuilder();

            json.Append("{");
            json.Append("\"playerName\": \"" + playername + "\",");
            json.Append("\"uuid\": \"" + uuid + "\",");
            json.Append("\"gameMode\": \"" + GameModeString(gamemode) + "\"");
            json.Append("}");

            SendEvent("OnGamemodeUpdate", json.ToString());
        }

        public override void OnLatencyUpdate(string playername, Guid uuid, int latency)
        {
            StringBuilder json = new StringBuilder();

            json.Append("{");
            json.Append("\"playerName\": \"" + playername + "\",");
            json.Append("\"uuid\": \"" + uuid + "\",");
            json.Append("\"latency\": " + latency);
            json.Append("}");

            SendEvent("OnLatencyUpdate", json.ToString());
        }

        public override void OnMapData(int mapid, byte scale, bool trackingposition, bool locked, int iconcount)
        {
            StringBuilder json = new StringBuilder();

            json.Append("{");
            json.Append("\"mapId\": " + mapid + ",");
            json.Append("\"trackingPosition\": " + trackingposition + ",");
            json.Append("\"locked\": " + locked + ",");
            json.Append("\"iconCount\": " + iconcount);
            json.Append("}");

            SendEvent("OnMapData", json.ToString());
        }

        public override void OnTradeList(int windowID, List<VillagerTrade> trades, VillagerInfo villagerInfo)
        {
            StringBuilder json = new StringBuilder();

            json.Append("{");
            json.Append("\"windowId\": " + windowID + ",");
            json.Append("\"trades\": " + JsonConvert.SerializeObject(trades) + ",");
            json.Append("\"villagerInfo\": " + JsonConvert.SerializeObject(villagerInfo) + "");
            json.Append("}");

            SendEvent("OnTradeList", json.ToString());
        }

        public override void OnTitle(int action, string titletext, string subtitletext, string actionbartext, int fadein, int stay, int fadeout, string json_)
        {
            StringBuilder json = new StringBuilder();

            json.Append("{");
            json.Append("\"action\": " + action + ",");
            json.Append("\"titleText\": \"" + titletext + "\",");
            json.Append("\"subtitleText\": \"" + subtitletext + "\",");
            json.Append("\"actionBarText\": \"" + actionbartext + "\",");
            json.Append("\"fadeIn\": " + fadein + ",");
            json.Append("\"stay\": " + stay + ",");
            json.Append("\"rawJson\": " + Json.EscapeString(json_));
            json.Append("}");

            SendEvent("OnTitle", json.ToString());
        }

        public override void OnEntityEquipment(Entity entity, int slot, Item item)
        {
            StringBuilder json = new StringBuilder();

            json.Append("{");
            json.Append("\"entity\": " + EntityToJson(entity) + ",");
            json.Append("\"slot\": " + slot + ",");
            json.Append("\"item\": " + ItemToJson(item));
            json.Append("}");

            SendEvent("OnEntityEquipment", json.ToString());
        }

        public override void OnEntityEffect(Entity entity, Effects effect, int amplifier, int duration, byte flags)
        {
            StringBuilder json = new StringBuilder();

            json.Append("{");
            json.Append("\"entity\": " + EntityToJson(entity) + ",");
            json.Append("\"effect\": \"" + effect.ToString() + "\",");
            json.Append("\"amplifier\": " + amplifier + ",");
            json.Append("\"duration\": " + duration + ",");
            json.Append("\"flags\": " + flags);
            json.Append("}");

            SendEvent("OnEntityEffect", json.ToString());
        }

        public override void OnScoreboardObjective(string objectivename, byte mode, string objectivevalue, int type, string json_)
        {
            StringBuilder json = new StringBuilder();

            json.Append("{");
            json.Append("\"objectiveName\": \"" + objectivename + "\",");
            json.Append("\"mode\": " + mode + ",");
            json.Append("\"objectiveValue\": \"" + objectivevalue + "\",");
            json.Append("\"type\": " + type + ",");
            json.Append("\"rawJson\": " + json_);
            json.Append("}");

            SendEvent("OnScoreboardObjective", json.ToString());
        }

        public override void OnUpdateScore(string entityname, int action, string objectivename, int value)
        {
            StringBuilder json = new StringBuilder();

            json.Append("{");
            json.Append("\"entityName\": \"" + entityname + "\",");
            json.Append("\"action\": " + action + ",");
            json.Append("\"objectiveName\": \"" + objectivename + "\",");
            json.Append("\"type\": " + value);
            json.Append("}");

            SendEvent("OnUpdateScore", json.ToString());
        }

        public override void OnInventoryUpdate(int inventoryId)
        {
            SendEvent("OnInventoryUpdate", "{\"inventoryId\": " + inventoryId + "}");
        }

        public override void OnInventoryOpen(int inventoryId)
        {
            SendEvent("OnInventoryOpen", "{\"inventoryId\": " + inventoryId + "}");
        }

        public override void OnInventoryClose(int inventoryId)
        {
            SendEvent("OnInventoryClose", "{\"inventoryId\": " + inventoryId + "}");
        }

        public override void OnPlayerJoin(Guid uuid, string name)
        {
            StringBuilder json = new StringBuilder();

            json.Append("{");
            json.Append("\"uuid\": \"" + uuid + "\",");
            json.Append("\"name\": \"" + name + "\"");
            json.Append("}");

            SendEvent("OnPlayerJoin", json.ToString());
        }

        public override void OnPlayerLeave(Guid uuid, string? name)
        {
            StringBuilder json = new StringBuilder();

            json.Append("{");
            json.Append("\"uuid\": \"" + uuid + "\",");
            json.Append("\"name\": \"" + (name == null ? "null" : name) + "\"");
            json.Append("}");

            SendEvent("OnPlayerLeave", json.ToString());
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
            StringBuilder json = new StringBuilder();

            json.Append("{");
            json.Append("\"entity\": " + EntityToJson(entity) + ",");
            json.Append("\"health\": " + health.ToString("0.00"));
            json.Append("}");

            //SendEvent("OnEntityHealth", json.ToString());
        }

        public override void OnEntityMetadata(Entity entity, Dictionary<int, object> metadata)
        {
            StringBuilder json = new StringBuilder();

            json.Append("{");
            json.Append("\"entity\": " + EntityToJson(entity) + ",");
            json.Append("\"metadata\": " + JsonConvert.SerializeObject(metadata));
            json.Append("}");

            SendEvent("OnEntityMetadata", json.ToString());
        }

        public override void OnPlayerStatus(byte statusId)
        {
            SendEvent("OnPlayerStatus", "{\"statusId\": " + statusId + "}");
        }

        public override void OnNetworkPacket(int packetID, List<byte> packetData, bool isLogin, bool isInbound)
        {
            StringBuilder json = new StringBuilder();

            json.Append("{");
            json.Append("\"packetId\": " + packetID + ",");
            json.Append("\"isLogin\": " + isLogin + ",");
            json.Append("\"isInbound\": " + isInbound + ",");
            json.Append("\"packetData\": " + JsonConvert.SerializeObject(packetData));
            json.Append("}");

            SendEvent("OnNetworkPacket", json.ToString());
        }

        // ==========================================================================================
        // Helper methods
        // ==========================================================================================
        private void SendEvent(string type, string data, bool overrideAuth = false)
        {
            foreach (KeyValuePair<string, WsServer.WsBehavior> pair in _sessions!)
                SendSessionEvent(pair.Value, type, data, overrideAuth);
        }

        private void SendSessionEvent(WsServer.WsBehavior session, string type, string data, bool overrideAuth = false)
        {
            if (session != null && (overrideAuth || session.IsAuthenticated()))
                session.SendToClient("{\"event\": \"" + type + "\", \"data\": " + (data.IsNullOrEmpty() ? "null" : "\"" + Json.EscapeString(data) + "\"") + "}");
        }

        public void SendCommandResponse(WsServer.WsBehavior session, bool success, string requestId, string command, string result, bool overrideAuth = false)
        {
            SendSessionEvent(session, "OnWsCommandResponse", "{\"success\": " + success.ToString().ToLower() + ", \"requestId\": \"" + requestId + "\", \"command\": \"" + command + "\", \"result\": " + (string.IsNullOrEmpty(result) ? "null" : result) + "}", overrideAuth);
        }

        private string EntityToJson(Entity entity)
        {
            return JsonConvert.SerializeObject(entity);
        }

        private string ItemToJson(Item item)
        {
            return JsonConvert.SerializeObject(item);
        }

        private string LocationToJson(Location location)
        {
            return JsonConvert.SerializeObject(location);
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
