using System;
using System.Collections.Generic;

namespace MinecraftClient.Mapping
{
    /// <summary>
    /// Represents an entity evolving into a Minecraft world
    /// </summary>
    public class Entity
    {
        /// <summary>
        /// ID of the entity on the Minecraft server
        /// </summary>
        public int ID;

        /// <summary>
        /// UUID of the entity if it is a player.
        /// </summary>
        public Guid UUID;

        /// <summary>
        /// Nickname of the entity if it is a player.
        /// </summary>
        public string Name;

        /// <summary>
        /// Entity type
        /// </summary>
        public EntityType Type;

        /// <summary>
        /// Entity location in the Minecraft world
        /// </summary>
        public Location Location;

        /// <summary>
        /// Create a new entity based on Entity ID, Entity Type and location
        /// </summary>
        /// <param name="ID">Entity ID</param>
        /// <param name="type">Entity Type Enum</param>
        /// <param name="location">Entity location</param>
        public Entity(int ID, EntityType type, Location location)
        {
            this.ID = ID;
            this.Type = type;
            this.Location = location;
        }
        /// <summary>
        /// Create a new entity based on Entity ID, Entity Type, location and UUID
        /// </summary>
        /// <param name="ID">Entity ID</param>
        /// <param name="type">Entity Type Enum</param>
        /// <param name="location">Entity location</param>
        /// <param name="uuid">Player uuid</param>
        public Entity(int ID, EntityType type, Location location, Guid uuid, string name)
        {
            this.ID = ID;
            this.Type = type;
            this.Location = location;
            this.UUID = uuid;
            this.Name = name;
        }
    }
}
