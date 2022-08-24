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
        private string _password;

        public static event EventHandler<string>? OnNewSession;
        public static event EventHandler<string>? OnSessionClose;
        public static event EventHandler<string>? OnMessageRecived;

        public WsServer(int port, string password)
        {
            _password = password;

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
                WsServer.OnMessageRecived!.Invoke(null, websocketEvent.Data);
            }

            public void SendToClient(string text)
            {
                Send(text);
            }
        }
    }

    class WebSocketBot : ChatBot
    {
        // Videti: https://chronoxor.github.io/NetCoreServer/

        private WsServer? _server;
        private Dictionary<string, WsServer.WsBehavior>? _sessions;

        public WebSocketBot(int port, string password)
        {
            try
            {
                _server = new WsServer(port, password);
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
                LogToConsole("New session connected: " + id);
                _sessions[id] = (WsServer.WsBehavior)sender!;
            };

            WsServer.OnSessionClose += (sender, id) =>
            {
                LogToConsole("Session witn an id \"" + id + "\" has disconnected!");
                _sessions.Remove(id);
            };

            WsServer.OnMessageRecived += (sender, message) =>
            {
                LogToConsole("Got a message: " + message);

                //string result = "";
                //PerformInternalCommand(message, ref result);
                //SendEvent("OnCommandRespone", "{\"response\": \"" + result + "\"}");
            };
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

            SendEvent("OnEntity:Animation", json.ToString());
        }

        public override void GetText(string text)
        {
            string message = "";
            string username = "";

            if (IsPrivateMessage(text, ref message, ref username))
                SendEvent("OnChat:Private", "{\"sender\": \"" + username + "\", \"message\": \"" + message + "\", \"rawText\": \"" + text + "\"}");
            else if (IsChatMessage(text, ref message, ref username))
                SendEvent("OnChat:Public", "{\"sender\": \"" + username + "\", \"message\": \"" + message + "\", \"rawText\": \"" + text + "\"}");
            else if (IsTeleportRequest(text, ref username))
                SendEvent("OnTeleportRequest", "{\"sender\": \"" + username + "\", \"rawText\": \"" + text + "\"}");
        }

        public override void GetText(string text, string? json)
        {
            SendEvent("OnChat:RawJson", json!);
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
            SendEvent("OnPlayer:Property", JsonConvert.SerializeObject(prop));
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
            SendEvent("OnEntity:Move", EntityToJson(entity));
        }
        public override void OnEntitySpawn(Entity entity)
        {
            SendEvent("OnEntity:Spawn", EntityToJson(entity));
        }

        public override void OnEntityDespawn(Entity entity)
        {
            SendEvent("OnEntity:Despawn", EntityToJson(entity));
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

            SendEvent("OnEntity:Equipment", json.ToString());
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

            SendEvent("OnEntity:Effect", json.ToString());
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
            SendEvent("OnInventory:Update", "{\"inventoryId\": " + inventoryId + "}");
        }

        public override void OnInventoryOpen(int inventoryId)
        {
            SendEvent("OnInventory:Open", "{\"inventoryId\": " + inventoryId + "}");
        }

        public override void OnInventoryClose(int inventoryId)
        {
            SendEvent("OnInventory:Close", "{\"inventoryId\": " + inventoryId + "}");
        }

        public override void OnPlayerJoin(Guid uuid, string name)
        {
            StringBuilder json = new StringBuilder();

            json.Append("{");
            json.Append("\"uuid\": \"" + uuid + "\",");
            json.Append("\"name\": \"" + name + "\"");
            json.Append("}");

            SendEvent("OnPlayer:Join", json.ToString());
        }

        public override void OnPlayerLeave(Guid uuid, string? name)
        {
            StringBuilder json = new StringBuilder();

            json.Append("{");
            json.Append("\"uuid\": \"" + uuid + "\",");
            json.Append("\"name\": \"" + (name == null ? "null" : name) + "\"");
            json.Append("}");

            SendEvent("OnPlayer:Leave", json.ToString());
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

            SendEvent("OnEntity:Health", json.ToString());
        }

        public override void OnEntityMetadata(Entity entity, Dictionary<int, object> metadata)
        {
            StringBuilder json = new StringBuilder();

            json.Append("{");
            json.Append("\"entity\": " + EntityToJson(entity) + ",");
            json.Append("\"metadata\": " + JsonConvert.SerializeObject(metadata));
            json.Append("}");

            SendEvent("OnEntity:Metadata", json.ToString());
        }

        public override void OnPlayerStatus(byte statusId)
        {
            SendEvent("OnPlayer:Status", "{\"statusId\": " + statusId + "}");
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
        private void SendEvent(string type, string data)
        {
            foreach (KeyValuePair<string, WsServer.WsBehavior> pair in _sessions!)
            {
                var instance = pair.Value;

                if (instance != null)
                    instance.SendToClient("{\"event\": \"" + type + "\", \"data\": " + (data.IsNullOrEmpty() ? "null" : "\"" + Json.EscapeString(data) + "\"") + "}");
            }
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
