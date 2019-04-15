using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Crypto;
using MinecraftClient.Mapping;

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
        /// <param name="message">Reason</param>
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
    }
}
