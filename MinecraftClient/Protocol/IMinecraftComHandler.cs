using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Mapping;

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
    }
}
