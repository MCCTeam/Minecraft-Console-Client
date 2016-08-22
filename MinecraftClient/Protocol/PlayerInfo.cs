using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol
{
    /// <summary>
    /// Information about a player (player tab list item)
    /// </summary>
    public struct PlayerInfo : IComparable<PlayerInfo>
    {
        private Guid _uuid;
        private string _name;
        private string _displayName;

        public Guid UUID { get { return _uuid; } }
        public string Name { get { return _name; } }
        public string DisplayName { get { return _displayName; } }

        /// <summary>
        /// Create a new PlayerInfo structure
        /// </summary>
        /// <param name="uuid">Player Id</param>
        /// <param name="name">Player Name</param>
        public PlayerInfo(Guid uuid, string name)
        {
            _uuid = uuid;
            _name = name;
            _displayName = name;
        }

        /// <summary>
        /// Create a new PlayerInfo structure
        /// </summary>
        /// <param name="uuid">Player Id</param>
        /// <param name="name">Player Name</param>
        /// <param name="displayName">Player Display Name</param>
        public PlayerInfo(Guid uuid, string name, string displayName)
            : this(uuid, name)
        {
            _displayName = displayName;
        }

        /// <summary>
        /// String representation of the player
        /// </summary>
        /// <returns>Player display name</returns>
        public override string ToString()
        {
            return DisplayName;
        }

        /// <summary>
        /// Compare a player to another player
        /// </summary>
        /// <param name="obj">Other player</param>
        /// <returns>TRUE if same player Id</returns>
        public override bool Equals(object obj)
        {
            if (obj is PlayerInfo)
                return UUID.Equals(((PlayerInfo)obj).UUID);
            return base.Equals(obj);
        }

        /// <summary>
        /// Basic hash function for player, from Player Id
        /// </summary>
        /// <returns>Interger hash</returns>
        /// <remarks>Required when overriding Equals()</remarks>
        public override int GetHashCode()
        {
            return UUID.GetHashCode();
        }

        /// <summary>
        /// Allows sorting players by name
        /// </summary>
        /// <param name="obj">Other player to compare to</param>
        /// <returns>Comparition with the player's name</returns>
        int IComparable<PlayerInfo>.CompareTo(PlayerInfo obj)
        {
            return Name.CompareTo(obj.Name);
        }
    }
}
