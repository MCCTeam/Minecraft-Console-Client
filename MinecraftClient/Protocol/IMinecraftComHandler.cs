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
        /// Called when a non-living entity has spawned
        /// </summary>
        /// <param name="EntityID">Entity ID</param>
        /// <param name="EntityType">Entity Type ID</param>
        /// <param name="UUID">Entity UUID</param>
        /// <param name="location">Entity location</param>
        void OnSpawnEntity(int EntityID, int EntityType, Guid UUID, Location location);

        /// <summary>
        /// Called when a living entity has spawned
        /// </summary>
        /// <param name="EntityID">Entity ID</param>
        /// <param name="EntityType">Entity Type ID</param>
        /// <param name="UUID">Entity UUID</param>
        /// <param name="location">Entity location</param>
        void OnSpawnLivingEntity(int EntityID, int EntityType, Guid UUID, Location location);

        /// <summary>
        /// Called when a player has spawned
        /// </summary>
        /// <param name="EntityID">Entity ID</param>
        /// <param name="UUID">Entity UUID</param>
        /// <param name="location">Entity location</param>
        /// <param name="Yaw">Player head yaw</param>
        /// <param name="Pitch">Player head pitch</param>
        void OnSpawnPlayer(int EntityID, Guid UUID, Location location, byte Yaw, byte Pitch);

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
        void OnEntityPosition(int EntityID, Double Dx, Double Dy, Double Dz,bool onGround);

        /// <summary>
        /// Called when an entity moved to fixed coordinates
        /// </summary>
        /// <param name="EntityID">Entity ID</param>
        /// <param name="Dx">X</param>
        /// <param name="Dy">Y</param>
        /// <param name="Dz">Z</param>
        /// <param name="onGround">TRUE if on ground</param>
        void OnEntityTeleport(int EntityID, Double X, Double Y, Double Z, bool onGround);

        /// <summary>
        /// Called when additional properties have been received for an entity
        /// </summary>
        /// <param name="EntityID">Entity ID</param>
        /// <param name="prop">Dictionary of properties</param>
        void OnEntityProperties(int EntityID, Dictionary<string, Double> prop);

        /// <summary>
        /// Called when the world age has been updated
        /// </summary>
        /// <param name="WorldAge">World age</param>
        /// <param name="TimeOfDay">Time of Day</param>
        void OnTimeUpdate(long WorldAge, long TimeOfDay);

        /// <summary>
        /// Called when inventory items have been received
        /// </summary>
        /// <param name="inventoryID">Inventory ID</param>
        /// <param name="itemList">Item list</param>
        void OnWindowItems(byte inventoryID, Dictionary<int, MinecraftClient.Inventory.Item> itemList);

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
        /// Called when client need to change slot.
        /// </summary>
        /// <remarks>Used for setting player slot after joining game</remarks>
        /// <param name="slot"></param>
        void OnHeldItemChange(byte slot);

        /// <summary>
        /// Called when the Player entity ID has been received from the server
        /// </summary>
        /// <param name="EntityID">Player entity ID</param>
        void SetPlayerEntityID(int EntityID);
    }
}
