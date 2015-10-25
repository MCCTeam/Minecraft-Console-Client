using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Crypto;

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
        /// Tell the server what client is being used to connect to the server
        /// </summary>
        /// <param name="brandInfo">Client string describing the client</param>
        /// <returns>True if brand info was successfully sent</returns>

        bool SendBrandInfo(string brandInfo);

        /// <summary>
        /// Send a plugin channel packet to the server.
        ///
        /// http://dinnerbone.com/blog/2012/01/13/minecraft-plugin-channels-messaging/
        /// </summary>
        /// <param name="channel">Channel to send packet on</param>
        /// <param name="data">packet Data</param>
        /// <returns>True if message was successfully sent</returns>

        bool SendPluginChannelPacket(string channel, byte[] data);
    }
}
