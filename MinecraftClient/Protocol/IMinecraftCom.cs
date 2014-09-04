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

    public interface IMinecraftCom : IDisposable, IAutoComplete, IPaddingProvider
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
    }
}
