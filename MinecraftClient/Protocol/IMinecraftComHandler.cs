﻿using System;
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
        /* The MinecraftCom Hanler must
         * provide these getters */

        int GetServerPort();
        string GetServerHost();
        string GetUsername();
        string GetUserUUID();
        string GetSessionID();
        string[] GetOnlinePlayers();
        Location GetCurrentLocation();
        World GetWorld();

        /// <summary>
        /// Called when a server was successfully joined
        /// </summary>

        void OnGameJoined();

        /// <summary>
        /// This method is called when the protocol handler receives a chat message
        /// </summary>

        void OnTextReceived(string text);

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

        void UpdateLocation(Location location);

        /// <summary>
        /// This method is called when the connection has been lost
        /// </summary>

        void OnConnectionLost(ChatBot.DisconnectReason reason, string message);

        /// <summary>
        /// Called ~10 times per second (10 ticks per second)
        /// Useful for updating bots in other parts of the program
        /// </summary>

        void OnUpdate();
    }
}
