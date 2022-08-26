using System;
using System.Collections.Generic;
using System.Text;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;
using Newtonsoft.Json;
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
                SendEvent("OnMccCommandRespone", "{\"response\": \"" + result + "\"}");
            };
        }

        public bool ProcessWebsocketCommand(WsServer.WsBehavior session, string password, string message)
        {
            message = message.Trim();

            if (password.Length != 0)
            {
                if (!session.IsAuthenticated())
                {
                    if (message.StartsWith("Authenticate", StringComparison.OrdinalIgnoreCase))
                    {
                        string input = message.Replace("Authenticate", "", StringComparison.OrdinalIgnoreCase).Trim();

                        if (input.Length == 0)
                        {
                            SendSessionEvent(session, "OnWsCommandResponse", "{\"error\": true, \"message\": \"Please provide a valid password! (Example: 'Authenticate password123')\"}", true);
                            return false;
                        }

                        if (!input.Equals(password))
                        {
                            SendSessionEvent(session, "OnWsCommandResponse", "{\"error\": true, \"message\": \"Incorrect password provided!\"}", true);
                            return false;
                        }

                        session.SetAuthenticated(true);
                        SendSessionEvent(session, "OnWsCommandResponse", "{\"success\": true, \"message\": \"Succesfully authenticated!\"}", true);
                        LogToConsole("§bSession with an id §a\"" + session.GetSessionId() + "\"§b has been succesfully authenticated!\"!");
                        return false;
                    }

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

            if (message.StartsWith("ChangeSessionId", StringComparison.OrdinalIgnoreCase))
            {
                string newId = message.Replace("ChangeSessionId", "").Trim();

                if (newId.Length == 0)
                {
                    SendSessionEvent(session, "OnWsCommandResponse", "{\"error\": true, \"message\": \"Please provide a valid session ID!\"}", true);
                    return false;
                }

                if (newId.Length > 32)
                {
                    SendSessionEvent(session, "OnWsCommandResponse", "{\"error\": true, \"message\": \"The session ID can't be longer than 32 characters!\"}", true);
                    return false;
                }

                string oldId = session.GetSessionId();

                _sessions!.Remove(oldId);
                _sessions[newId] = session;
                session.SetSessionId(newId);

                SendSessionEvent(session, "OnWsCommandResponse", "{\"success\": true, \"message\": \"The session ID was successfully changed to: '" + newId + "'\"}", true);
                LogToConsole("§bSession with an id §a\"" + oldId + "\"§b has been renamed to: §a\"" + newId + "\"§b!");
                return false;
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
            json.Append("\"health\": " + String.Format("{0:0.00}", health));
            json.Append("}");

            SendEvent("OnEntityHealth", json.ToString());
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
