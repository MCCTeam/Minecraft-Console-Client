using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Crypto;
using MinecraftClient.Mapping;
using MinecraftClient.Inventory;

namespace MinecraftClient.Protocol
{
    /// <summary>
    /// Interface for the Minecraft protocol handler.
    /// A protocol handler is used to communicate with the Minecraft server.
    /// This interface allows to abstract from the underlying minecraft version in other parts of the program.
    /// The protocol handler will take care of parsing and building the appropriate network packets.
    /// </summary>

    public interface IMinecraftCom : IDisposable, IAutoComplete
    {
        /// <summary>
        /// Start the login procedure once connected to the server
        /// </summary>
        /// <returns>True if login was successful</returns>
        bool Login();

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Get max length for chat messages
        /// </summary>
        /// <returns>Max length, in characters</returns>
        int GetMaxChatMessageLength();

        /// <summary>
        /// Send a chat message or command to the server
        /// </summary>
        /// <param name="message">Text to send</param>
        /// <returns>True if successfully sent</returns>
        bool SendChatMessage(string message);

        /// <summary>
        /// Allow to respawn after death
        /// </summary>
        /// <returns>True if packet successfully sent</returns>
        bool SendRespawnPacket();

        /// <summary>
        /// Inform the server of the client being used to connect
        /// </summary>
        /// <param name="brandInfo">Client string describing the client</param>
        /// <returns>True if brand info was successfully sent</returns>
        bool SendBrandInfo(string brandInfo);

        /// <summary>
        /// Inform the server of the client's Minecraft settings
        /// </summary>
        /// <param name="language">Client language eg en_US</param>
        /// <param name="viewDistance">View distance, in chunks</param>
        /// <param name="difficulty">Game difficulty (client-side...)</param>
        /// <param name="chatMode">Chat mode (allows muting yourself)</param>
        /// <param name="chatColors">Show chat colors</param>
        /// <param name="skinParts">Show skin layers</param>
        /// <param name="mainHand">1.9+ main hand</param>
        /// <returns>True if client settings were successfully sent</returns>
        bool SendClientSettings(string language, byte viewDistance, byte difficulty, byte chatMode, bool chatColors, byte skinParts, byte mainHand);

        /// <summary>
        /// Send a location update telling that we moved to that location
        /// </summary>
        /// <param name="location">The new location</param>
        /// <param name="onGround">True if the player is on the ground</param>
        /// <param name="yaw">The new yaw (optional)</param>
        /// <param name="pitch">The new pitch (optional)</param>
        /// <returns>True if packet was successfully sent</returns>
        bool SendLocationUpdate(Location location, bool onGround, float? yaw, float? pitch);

        /// <summary>
        /// Send a plugin channel packet to the server.
        /// </summary>
        /// <see href="http://dinnerbone.com/blog/2012/01/13/minecraft-plugin-channels-messaging/" />
        /// <param name="channel">Channel to send packet on</param>
        /// <param name="data">packet Data</param>
        /// <returns>True if message was successfully sent</returns>
        bool SendPluginChannelPacket(string channel, byte[] data);
        
        /// <summary>
        /// Send Entity Action packet to the server.
        /// </summary>
        /// <param name="entityID">PlayerID</param>
        /// <param name="type">Type of packet to send</param>
        /// <returns>True if packet was successfully sent</returns>
        bool SendEntityAction(int EntityID, int type);
        
        /// <summary>
        /// Send a held item change packet to the server.
        /// </summary>
        /// <param name="slot">New active slot in the inventory hotbar</param>
        /// <returns>True if packet was successfully sent</returns>
        bool SendHeldItemChange(short slot);

        /// <summary>
        /// Send an entity interaction packet to the server.
        /// </summary>
        /// <param name="EntityID">Entity ID to interact with</param>
        /// <param name="type">Type of interaction (0: interact, 1: attack, 2: interact at)</param>
        /// <returns>True if packet was successfully sent</returns>
        bool SendInteractEntity(int EntityID, int type);

        /// <summary>
        /// Send an entity interaction packet to the server.
        /// </summary>
        /// <param name="EntityID">Entity ID to interact with</param>
        /// <param name="type">Type of interaction (0: interact, 1: attack, 2: interact at)</param>
        /// <param name="X">X coordinate for "interact at"</param>
        /// <param name="Y">Y coordinate for "interact at"</param>
        /// <param name="Z">Z coordinate for "interact at"</param>
        /// <param name="hand">Player hand (0: main hand, 1: off hand)</param>
        /// <returns>True if packet was successfully sent</returns>
        bool SendInteractEntity(int EntityID, int type, float X, float Y, float Z, int hand);

        /// <summary>
        /// Send an entity interaction packet to the server.
        /// </summary>
        /// <param name="EntityID">Entity ID to interact with</param>
        /// <param name="type">Type of interaction (0: interact, 1: attack, 2: interact at)</param>
        /// <param name="X">X coordinate for "interact at"</param>
        /// <param name="Y">Y coordinate for "interact at"</param>
        /// <param name="Z">Z coordinate for "interact at"</param>
        /// <returns>True if packet was successfully sent</returns>
        bool SendInteractEntity(int EntityID, int type, float X, float Y, float Z);

        /// <summary>
        /// Send a use item packet to the server
        /// </summary>
        /// <param name="hand">0: main hand, 1: off hand</param>
        /// <returns>True if packet was successfully sent</returns>
        bool SendUseItem(int hand);

        /// <summary>
        /// Send a click window slot packet to the server
        /// </summary>
        /// <param name="windowId">Id of the window being clicked</param>
        /// <param name="slotId">Id of the clicked slot</param>
        /// <param name="buttom">Action to perform</param>
        /// <param name="item">Item in the clicked slot</param>
        /// <returns>True if packet was successfully sent</returns>
        bool SendWindowAction(int windowId, int slotId, WindowActionType action, Item item);

        /// <summary>
        /// Request Creative Mode item creation into regular/survival Player Inventory
        /// </summary>
        /// <remarks>(obviously) requires to be in creative mode</remarks>
        /// <param name="slot">Destination inventory slot</param>
        /// <param name="itemType">Item type</param>
        /// <param name="count">Item count</param>
        /// <returns>TRUE if item given successfully</returns>
        bool SendCreativeInventoryAction(int slot, ItemType itemType, int count);

        /// <summary>
        /// Plays animation
        /// </summary>
        /// <param name="animation">0 for left arm, 1 for right arm</param>
        /// <param name="playerid">Player Entity ID</param>
        /// <returns>TRUE if item given successfully</returns>
        bool SendAnimation(int animation, int playerid);

        /// <summary>
        /// Send a close window packet to the server
        /// </summary>
        /// <param name="windowId">Id of the window being closed</param>
        bool SendCloseWindow(int windowId);

        /// <summary>
        /// Send player block placement packet to the server
        /// </summary>
        /// <param name="hand">0: main hand, 1: off hand</param>
        /// <param name="location">Location to place block at</param>
        /// <param name="face">Block face</param>
        /// <param name="CursorX">Cursor X</param>
        /// <param name="CursorY">Cursor Y</param>
        /// <param name="CursorZ">Cursor Z</param>
        /// <param name="insideBlock">TRUE if inside block</param>
        /// <returns>True if packet was successfully sent</returns>
        bool SendPlayerBlockPlacement(int hand, Location location, int face, float CursorX, float CursorY, float CursorZ, bool insideBlock);
        
        bool SendPlayerDigging(int status, Location location, byte face);
    }
}
