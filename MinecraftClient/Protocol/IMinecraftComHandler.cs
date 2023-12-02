using System;
using System.Collections.Generic;
using MinecraftClient.Inventory;
using MinecraftClient.Logger;
using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Message;
using MinecraftClient.Protocol.ProfileKey;
using MinecraftClient.Scripting;

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
        Guid GetUserUuid();
        string GetUserUuidStr();
        string GetSessionID();
        string[] GetOnlinePlayers();
        Dictionary<string, string> GetOnlinePlayersWithUUID();
        PlayerInfo? GetPlayerInfo(Guid uuid);
        PlayerKeyPair? GetPlayerKeyPair();
        Location GetCurrentLocation();
        World GetWorld();
        bool GetIsSupportPreviewsChat();
        bool GetTerrainEnabled();
        bool SetTerrainEnabled(bool enabled);
        bool GetInventoryEnabled();
        bool SetInventoryEnabled(bool enabled);
        bool GetEntityHandlingEnabled();
        bool SetEntityHandlingEnabled(bool enabled);
        bool GetNetworkPacketCaptureEnabled();
        void SetNetworkPacketCaptureEnabled(bool enabled);
        int GetProtocolVersion();
        Container? GetInventory(int inventoryID);
        ILogger GetLogger();

        /// <summary>
        /// Invoke a task on the main thread, wait for completion and retrieve return value.
        /// </summary>
        /// <param name="task">Task to run with any type or return value</param>
        /// <returns>Any result returned from task, result type is inferred from the task</returns>
        /// <example>bool result = InvokeOnMainThread(methodThatReturnsAbool);</example>
        /// <example>bool result = InvokeOnMainThread(() => methodThatReturnsAbool(argument));</example>
        /// <example>int result = InvokeOnMainThread(() => { yourCode(); return 42; });</example>
        /// <typeparam name="T">Type of the return value</typeparam>
        T InvokeOnMainThread<T>(Func<T> task);

        /// <summary>
        /// Invoke a task on the main thread and wait for completion
        /// </summary>
        /// <param name="task">Task to run without return value</param>
        /// <example>InvokeOnMainThread(methodThatReturnsNothing);</example>
        /// <example>InvokeOnMainThread(() => methodThatReturnsNothing(argument));</example>
        /// <example>InvokeOnMainThread(() => { yourCode(); });</example>
        void InvokeOnMainThread(Action task);

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
        void OnGameJoined(bool isOnlineMode);

        /// <summary>
        /// Received chat/system message from the server
        /// </summary>
        /// <param name="message">Message received</param>
        public void OnTextReceived(ChatMessage message);

        /// <summary>
        /// Will be called every animations of the hit and place block
        /// </summary>
        /// <param name="entityID">Player ID</param>
        /// <param name="animation">0 = LMB, 1 = RMB (RMB Corrent not work)</param>
        void OnEntityAnimation(int entityID, byte animation);

        /// <summary>
        /// Will be called when a Synchronization sequence is recevied, this sequence need to be sent when breaking or placing blocks
        /// </summary>
        /// <param name="sequenceId">Sequence ID</param>
        void OnBlockChangeAck(int sequenceId);

        /// <summary>
        /// Will be called every player break block in gamemode 0
        /// </summary>
        /// <param name="entityId">Player ID</param>
        /// <param name="location">Block location</param>
        /// <param name="stage">Destroy stage, maximum 255</param>
        void OnBlockBreakAnimation(int entityID, Location location, byte stage);

        /// <summary>
        /// This method is called when the protocol handler receives a title
        /// </summary>
        void OnTitle(int action, string titletext, string subtitletext, string actionbartext, int fadein, int stay, int fadeout, string json);

        /// <summary>
        /// Called when receiving a connection keep-alive from the server
        /// </summary>
        void OnServerKeepAlive();

        /// <summary>
        /// This method is called when the protocol handler receives server data
        /// </summary>
        /// <param name="hasMotd">Indicates if the server has a motd message</param>
        /// <param name="motd">Server MOTD message</param>
        /// <param name="hasIcon">Indicates if the server has a an icon</param>
        /// <param name="iconBase64">Server icon in Base 64 format</param>
        /// <param name="previewsChat">Indicates if the server previews chat</param>
        void OnServerDataRecived(bool hasMotd, string motd, bool hasIcon, string iconBase64, bool previewsChat);

        /// <summary>
        /// This method is called when the protocol handler receives "Set Display Chat Preview" packet
        /// </summary>
        /// <param name="previewsChat">Indicates if the server previews chat</param>
        public void OnChatPreviewSettingUpdate(bool previewsChat);

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
        /// Triggered when a new player joins the game
        /// </summary>
        /// <param name="player">player info</param>
        public void OnPlayerJoin(PlayerInfo player);

        /// <summary>
        /// This method is called when a player has left the game
        /// </summary>
        /// <param name="uuid">UUID of the player</param>
        void OnPlayerLeave(Guid uuid);

        /// <summary>
        /// This method is called when a player has been killed by another entity
        /// </summary>
        /// <param name="killerEntityId">Killer's entity if</param>
        /// <param name="chatMessage">message sent in chat when player is killed</param>
        void OnPlayerKilled(int killerEntityId, string chatMessage);

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
        void OnEntityEquipment(int entityid, int slot, Item? item);

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
        /// Called when an entity moved and rotated
        /// </summary>
        /// <param name="EntityID">Entity ID</param>
        /// <param name="Dx">X offset</param>
        /// <param name="Dy">Y offset</param>
        /// <param name="Dz">Z offset</param>
        /// <param name="yaw">Yaw</param>
        /// <param name="pitch">Pitch</param>
        /// <param name="onGround">TRUE if on ground</param>
        void OnEntityPosition(int entityID, Double dx, Double dy, Double dz, float yaw, float pitch, bool onGround);

        /// <summary>
        /// Called when an entity rotated
        /// </summary>
        /// <param name="EntityID">Entity ID</param>
        /// <param name="yaw">Yaw</param>
        /// <param name="pitch">Pitch</param>
        /// <param name="onGround">TRUE if on ground</param>
        void OnEntityRotation(int entityID, float yaw, float pitch, bool onGround);

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
        /// Called when the status of an entity have been changed
        /// </summary>
        /// <param name="entityID">Entity ID</param>
        /// <param name="status">Status ID</param>
        void OnEntityStatus(int entityID, byte status);

        /// <summary>
        /// Called when the world age has been updated
        /// </summary>
        /// <param name="WorldAge">World age</param>
        /// <param name="TimeOfDay">Time of Day</param>
        void OnTimeUpdate(long worldAge, long timeOfDay);

        /// <summary>
        /// When received window properties from server.
        /// 
        /// </summary>
        /// <param name="inventoryID">Inventory ID</param>
        /// <param name="propertyId">Property ID</param>
        /// <param name="propertyValue">Property Value</param>
        public void OnWindowProperties(byte inventoryID, short propertyId, short propertyValue);

        /// <summary>
        /// Called when inventory items have been received
        /// </summary>
        /// <param name="inventoryID">Inventory ID</param>
        /// <param name="itemList">Item list</param>
        /// <param name="stateId">State ID</param>
        void OnWindowItems(byte inventoryID, Dictionary<int, Item> itemList, int stateId);

        /// <summary>
        /// Called when a single slot has been updated inside an inventory
        /// </summary>
        /// <param name="inventoryID">Window ID</param>
        /// <param name="slotID">Slot ID</param>
        /// <param name="item">Item (may be null for empty slot)</param>
        /// <param name="stateId">State ID</param>
        void OnSetSlot(byte inventoryID, short slotID, Item? item, int stateId);

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
        void OnEntityMetadata(int EntityID, Dictionary<int, object?> metadata);

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
        void OnMapData(int mapid, byte scale, bool trackingPosition, bool locked, List<MapIcon> icons, byte columnsUpdated, byte rowsUpdated, byte mapCoulmnX, byte mapRowZ, byte[]? colors);

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
        /// <param name="hasFactorData">has factor data</param>
        /// <param name="factorCodec">factorCodec</param>
        void OnEntityEffect(int entityid, Effects effect, int amplifier, int duration, byte flags, bool hasFactorData, Dictionary<String, object>? factorCodec);

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
        void OnUpdateScore(string entityname, int action, string objectivename, int value);

        /// <summary>
        /// Called when the client received the Tab Header and Footer
        /// </summary>
        /// <param name="header">Header</param>
        /// <param name="footer">Footer</param>
        void OnTabListHeaderAndFooter(string header, string footer);
        
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

        /// <summary>
        /// This method is called when the protocol handler receives "Login Success" packet
        /// </summary>
        /// <param name="uuid">The player's UUID received from the server</param>
        /// <param name="userName">The player's username received from the server</param>
        /// <param name="playerProperty">Tuple<Name, Value, Signature(empty if there is no signature)></param>
        public void OnLoginSuccess(Guid uuid, string userName, Tuple<string, string, string>[]? playerProperty);

        /// <summary>
        /// Used for a wide variety of game events, from weather to bed use to gamemode to demo messages.
        /// </summary>
        /// <param name="reason">Event type</param>
        /// <param name="value">Depends on Reason</param>
        public void OnGameEvent(byte reason, float value);

        /// <summary>
        /// Called when a block is changed.
        /// </summary>
        /// <param name="location">The location of the block.</param>
        /// <param name="block">The block</param>
        public void OnBlockChange(Location location, Block block);

        /// <summary>
        /// Called when "AutoComplete" completes.
        /// </summary>
        /// <param name="transactionId">The number of this result.</param>
        /// <param name="result">All commands.</param>
        public void OnAutoCompleteDone(int transactionId, string[] result);

        public void SetCanSendMessage(bool canSendMessage);

        /// <summary>
        /// Send a click container button packet to the server.
        /// Used for Enchanting table, Lectern, stone cutter and loom
        /// </summary>
        /// <param name="windowId">Id of the window being clicked</param>
        /// <param name="buttonId">Id of the clicked button</param>
        /// <returns>True if packet was successfully sent</returns>

        bool ClickContainerButton(int windowId, int buttonId);
        
        /// <summary>
        /// Send a rename item packet when the anvil inventory is open and there is an item in the first slot
        /// </summary>
        /// <param name="itemName">New name (max 50 characters)</param>
        /// <returns>True if packet was successfully sent</returns>

        bool SendRenameItem(string itemName);
    }
}
