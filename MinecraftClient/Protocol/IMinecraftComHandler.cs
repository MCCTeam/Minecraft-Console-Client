using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Mapping;
using MinecraftClient.Inventory;

namespace MinecraftClient.Protocol
{
    /// <summary>
    /// Interface for the MinecraftCom Handler.
    /// It defines some callbacks that the MinecraftCom handler must have.
    /// It allows the protocol handler to abstract from the other parts of the program.
    /// </summary>

    public interface IMinecraftComHandler
    {
        /* The MinecraftCom Handler must
         * provide these getters */

        int GetServerPort();
        string GetServerHost();
        string GetUsername();
        string GetUserUUID();
        string GetSessionID();
        string[] GetOnlinePlayers();
        Dictionary<string, string> GetOnlinePlayersWithUUID();
        Location GetCurrentLocation();
        World GetWorld();
        bool GetTerrainEnabled();
        bool SetTerrainEnabled(bool enabled);
        bool GetInventoryEnabled();
        bool SetInventoryEnabled(bool enabled);
        bool GetEntityHandlingEnabled();
        bool SetEntityHandlingEnabled(bool enabled);
        bool GetNetworkPacketCaptureEnabled();
        void SetNetworkPacketCaptureEnabled(bool enabled);
        int GetProtocolVersion();
        Container GetInventory(int inventoryID);
        ILogger GetLogger();

        /// <summary>
        /// Called when a network packet received or sent
        /// </summary>
        /// <remarks>
        /// Only called if <see cref="McClient.networkPacketEventEnabled"/> is set to True
        /// </remarks>
        /// <param name="packetID">Packet ID</param>
        /// <param name="packetData">A copy of Packet Data</param>
        /// <param name="isLogin">The packet is login phase or playing phase</param>
        /// <param name="isInbound">The packet is received from server or sent by client</param>
        void OnNetworkPacket(int packetID, List<byte> packetData, bool isLogin, bool isInbound);

        /// <summary>
        /// Called when a server was successfully joined
        /// </summary>
        void OnGameJoined();

        /// <summary>
        /// This method is called when the protocol handler receives a chat message
        /// </summary>
        /// <param name="text">Text received from the server</param>
        /// <param name="isJson">TRUE if the text is JSON-Encoded</param>
        void OnTextReceived(string text, bool isJson);
        
        /// <summary>
        /// This method is called when the protocol handler receives a title
        /// </summary>
        void OnTitle(int action, string titletext, string subtitletext, string actionbartext, int fadein, int stay, int fadeout, string json);
        
        /// <summary>
        /// Called when receiving a connection keep-alive from the server
        /// </summary>
        void OnServerKeepAlive();

        /// <summary>
        /// Called when an inventory is opened
        /// </summary>
        void OnInventoryOpen(int inventoryID, Container inventory);

        /// <summary>
        /// Called when an inventory is closed
        /// </summary>
        void OnInventoryClose(int inventoryID);

        /// <summary>
        /// Called when the player respawns, which happens on login, respawn and world change.
        /// </summary>
        void OnRespawn();

        /// <summary>
        /// This method is called when a new player joins the game
        /// </summary>
        /// <param name="uuid">UUID of the player</param>
        /// <param name="name">Name of the player</param>
        void OnPlayerJoin(Guid uuid, string name);

        /// <summary>
        /// This method is called when a player has left the game
        /// </summary>
        /// <param name="uuid">UUID of the player</param>
        void OnPlayerLeave(Guid uuid);

        /// <summary>
        /// Called when the server sets the new location for the player
        /// </summary>
        /// <param name="location">New location of the player</param>
        /// <param name="yaw">New yaw</param>
        /// <param name="pitch">New pitch</param>
        void UpdateLocation(Location location, float yaw, float pitch);

        /// <summary>
        /// This method is called when the connection has been lost
        /// </summary>
        void OnConnectionLost(ChatBot.DisconnectReason reason, string message);

        /// <summary>
        /// Called ~10 times per second (10 ticks per second)
        /// Useful for updating bots in other parts of the program
        /// </summary>
        void OnUpdate();

        /// <summary>
        /// Registers the given plugin channel for the given bot.
        /// </summary>
        /// <param name="channel">The channel to register.</param>
        /// <param name="bot">The bot to register the channel for.</param>
        void RegisterPluginChannel(string channel, ChatBot bot);

        /// <summary>
        /// Unregisters the given plugin channel for the given bot.
        /// </summary>
        /// <param name="channel">The channel to unregister.</param>
        /// <param name="bot">The bot to unregister the channel for.</param>
        void UnregisterPluginChannel(string channel, ChatBot bot);

        /// <summary>
        /// Sends a plugin channel packet to the server.
        /// See http://wiki.vg/Plugin_channel for more information about plugin channels.
        /// </summary>
        /// <param name="channel">The channel to send the packet on.</param>
        /// <param name="data">The payload for the packet.</param>
        /// <param name="sendEvenIfNotRegistered">Whether the packet should be sent even if the server or the client hasn't registered it yet.</param>
        /// <returns>Whether the packet was sent: true if it was sent, false if there was a connection error or it wasn't registered.</returns>
        bool SendPluginChannelMessage(string channel, byte[] data, bool sendEvenIfNotRegistered = false);

        /// <summary>
        /// Called when a plugin channel message was sent from the server.
        /// </summary>
        /// <param name="channel">The channel the message was sent on</param>
        /// <param name="data">The data from the channel</param>
        void OnPluginChannelMessage(string channel, byte[] data);

        /// <summary>
        /// Called when an entity has spawned
        /// </summary>
        /// <param name="entity">Spawned entity</param>
        void OnSpawnEntity(Entity entity);
        
        /// <summary>
        /// Called when an entity has spawned
        /// </summary>
        /// <param name="entityid">Entity id</param>
        /// <param name="slot">Equipment slot. 0: main hand, 1: off hand, 2–5: armor slot (2: boots, 3: leggings, 4: chestplate, 5: helmet)/param>
        /// <param name="item">Item/param>
        void OnEntityEquipment(int entityid, int slot, Item item);
        
        /// <summary>
        /// Called when a player spawns or enters the client's render distance
        /// </summary>
        /// <param name="entityID">Entity ID</param>
        /// <param name="uuid">Entity UUID</param>
        /// <param name="location">Entity location</param>
        /// <param name="yaw">Player head yaw</param>
        /// <param name="pitch">Player head pitch</param>
        void OnSpawnPlayer(int entityID, Guid uuid, Location location, byte yaw, byte pitch);

        /// <summary>
        /// Called when entities have despawned
        /// </summary>
        /// <param name="EntityID">List of Entity ID that have despawned</param>
        void OnDestroyEntities(int[] EntityID);

        /// <summary>
        /// Called when an entity moved by coordinate offset
        /// </summary>
        /// <param name="EntityID">Entity ID</param>
        /// <param name="Dx">X offset</param>
        /// <param name="Dy">Y offset</param>
        /// <param name="Dz">Z offset</param>
        /// <param name="onGround">TRUE if on ground</param>
        void OnEntityPosition(int entityID, Double dx, Double dy, Double dz, bool onGround);

        /// <summary>
        /// Called when an entity moved to fixed coordinates
        /// </summary>
        /// <param name="EntityID">Entity ID</param>
        /// <param name="Dx">X</param>
        /// <param name="Dy">Y</param>
        /// <param name="Dz">Z</param>
        /// <param name="onGround">TRUE if on ground</param>
        void OnEntityTeleport(int entityID, Double x, Double y, Double z, bool onGround);

        /// <summary>
        /// Called when additional properties have been received for an entity
        /// </summary>
        /// <param name="EntityID">Entity ID</param>
        /// <param name="prop">Dictionary of properties</param>
        void OnEntityProperties(int entityID, Dictionary<string, Double> prop);

        /// <summary>
        /// Called when the world age has been updated
        /// </summary>
        /// <param name="WorldAge">World age</param>
        /// <param name="TimeOfDay">Time of Day</param>
        void OnTimeUpdate(long worldAge, long timeOfDay);

        /// <summary>
        /// Called when inventory items have been received
        /// </summary>
        /// <param name="inventoryID">Inventory ID</param>
        /// <param name="itemList">Item list</param>
        void OnWindowItems(byte inventoryID, Dictionary<int, Item> itemList);

        /// <summary>
        /// Called when a single slot has been updated inside an inventory
        /// </summary>
        /// <param name="inventoryID">Window ID</param>
        /// <param name="slotID">Slot ID</param>
        /// <param name="item">Item (may be null for empty slot)</param>
        void OnSetSlot(byte inventoryID, short slotID, Item item);

        /// <summary>
        /// Called when player health or hunger changed.
        /// </summary>
        /// <param name="health"></param>
        /// <param name="food"></param>
        void OnUpdateHealth(float health, int food);

        /// <summary>
        /// Called when the health of an entity changed
        /// </summary>
        /// <param name="entityID">Entity ID</param>
        /// <param name="health">The health of the entity</param>
        void OnEntityHealth(int entityID, float health);
        
        /// <summary>
        /// Called when entity metadata or metadata changed.
        /// </summary>
        /// <param name="EntityID">Entity ID</param>
        /// <param name="metadata">Entity metadata</param>
        void OnEntityMetadata(int EntityID, Dictionary<int, object> metadata);

        /// <summary>
        /// Called when and explosion occurs on the server
        /// </summary>
        /// <param name="location">Explosion location</param>
        /// <param name="strength">Explosion strength</param>
        /// <param name="affectedBlocks">Amount of affected blocks</param>
        void OnExplosion(Location location, float strength, int affectedBlocks);

        /// <summary>
        /// Called when a player's game mode has changed
        /// </summary>
        /// <param name="uuid">Affected player's UUID</param>
        /// <param name="gamemode">New game mode</param>
        void OnGamemodeUpdate(Guid uuid, int gamemode);
        
        /// <summary>
        /// Called when a player's latency has changed
        /// </summary>
        /// <param name="uuid">Affected player's UUID</param>
        /// <param name="latency">latency</param>
        void OnLatencyUpdate(Guid uuid, int latency);
        
        /// <summary>
        /// Called when Experience bar is updated
        /// </summary>
        /// <param name="Experiencebar">Experience bar level</param>
        /// <param name="Level">Player Level</param>
        /// <param name="TotalExperience">Total experience</param>
        void OnSetExperience(float Experiencebar, int Level, int TotalExperience);

        /// <summary>
        /// Called when client need to change slot.
        /// </summary>
        /// <remarks>Used for setting player slot after joining game</remarks>
        /// <param name="slot"></param>
        void OnHeldItemChange(byte slot);
        
        /// <summary>
        /// Called map data
        /// </summary>
        /// <param name="mapid"></param>
        /// <param name="scale"></param>
        /// <param name="trackingposition"></param>
        /// <param name="locked"></param>
        /// <param name="iconcount"></param>
        void OnMapData(int mapid, byte scale, bool trackingposition, bool locked, int iconcount);
        
        /// <summary>
        /// Called when the Player entity ID has been received from the server
        /// </summary>
        /// <param name="EntityID">Player entity ID</param>
        void OnReceivePlayerEntityID(int EntityID);
        
        /// <summary>
        /// Called when the Entity use effects
        /// </summary>
        /// <param name="entityid">entity ID</param>
        /// <param name="effect">effect id</param>
        /// <param name="amplifier">effect amplifier</param>
        /// <param name="duration">effect duration</param>
        /// <param name="flags">effect flags</param>
        void OnEntityEffect(int entityid, Effects effect, int amplifier, int duration, byte flags);
        
        /// <summary>
        /// Called when coreboardObjective
        /// </summary>
        /// <param name="objectivename">objective name</param>
        /// <param name="mode">0 to create the scoreboard. 1 to remove the scoreboard. 2 to update the display text.</param>
        /// <param name="objectivevalue">Only if mode is 0 or 2. The text to be displayed for the score</param>
        /// <param name="type">Only if mode is 0 or 2. 0 = "integer", 1 = "hearts".</param>
        void OnScoreboardObjective(string objectivename, byte mode, string objectivevalue, int type);
        
        /// <summary>
        /// Called when DisplayScoreboard
        /// </summary>
        /// <param name="entityname">The entity whose score this is. For players, this is their username; for other entities, it is their UUID.</param>
        /// <param name="action">0 to create/update an item. 1 to remove an item.</param>
        /// <param name="objectivename">The name of the objective the score belongs to</param>
        /// <param name="value">he score to be displayed next to the entry. Only sent when Action does not equal 1.</param>
        void OnUpdateScore(string entityname, byte action, string objectivename, int value);

        /// <summary>
        /// Called when tradeList is received from server
        /// </summary>
        /// <param name="windowID">Window ID</param>
        /// <param name="trades">List of trades.</param>
        /// <param name="villagerLevel">The level the villager is.</param>
        /// <param name="experience">The amount of experience the villager has.</param>
        /// <param name="isRegularVillager">True if regular villagers and false if the wandering trader.</param>
        /// <param name="canRestock">If the villager can restock his trades at a workstation, True for regular villagers and false for the wandering trader.</param>
        void OnTradeList(int windowID, List<VillagerTrade> trades, VillagerInfo villagerInfo);
    }
}
