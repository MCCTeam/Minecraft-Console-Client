using System;
using System.Collections.Generic;
using MinecraftClient.Inventory;
using MinecraftClient.Protocol.Message;

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
        public string? Name;

        /// <summary>
        /// CustomName of the entity.
        /// </summary>
        public string? CustomNameJson;

        /// <summary>
        /// IsCustomNameVisible of the entity.
        /// </summary>
        public bool IsCustomNameVisible;

        /// <summary>
        /// CustomName of the entity.
        /// </summary>
        public string? CustomName;

        /// <summary>
        /// Latency of the entity if it is a player.
        /// </summary>
        public int Latency;

        /// <summary>
        /// Entity type
        /// </summary>
        public EntityType Type;

        /// <summary>
        /// Entity location in the Minecraft world
        /// </summary>
        public Location Location;

        /// <summary>
        /// Entity head yaw
        /// </summary>
        /// <remarks>Untested</remarks>
        public float Yaw = 0;

        /// <summary>
        /// Entity head pitch
        /// </summary>
        /// <remarks>Untested</remarks>
        public float Pitch = 0;

        /// <summary>
        /// Used in Item Frame, Falling Block and Fishing Float.
        /// See https://wiki.vg/Object_Data for details.
        /// </summary>
        /// <remarks>Untested</remarks>
        public int ObjectData = -1;

        /// <summary>
        /// Health of the entity
        /// </summary>
        public float Health;

        /// <summary>
        /// Item of the entity if ItemFrame or Item
        /// </summary>
        public Item Item;

        /// <summary>
        /// Entity pose in the Minecraft world
        /// </summary>
        public EntityPose Pose;

        /// <summary>
        /// Entity metadata
        /// </summary>
        public Dictionary<int, object?>? Metadata;

        /// <summary>
        /// Entity equipment
        /// </summary>
        public Dictionary<int, Item> Equipment;

        /// <summary>
        /// Create a new entity based on Entity ID, Entity Type and location
        /// </summary>
        /// <param name="ID">Entity ID</param>
        /// <param name="type">Entity Type Enum</param>
        /// <param name="location">Entity location</param>
        public Entity(int ID, EntityType type, Location location)
        {
            this.ID = ID;
            Type = type;
            Location = location;
            Health = 1.0f;
            Equipment = new Dictionary<int, Item>();
            Item = new Item(ItemType.Air, 0, null);
        }

        /// <summary>
        /// Create a new entity based on Entity ID, Entity Type and location
        /// </summary>
        /// <param name="ID">Entity ID</param>
        /// <param name="type">Entity Type Enum</param>
        /// <param name="location">Entity location</param>
        public Entity(int ID, EntityType type, Location location, byte yaw, byte pitch, int objectData)
        {
            this.ID = ID;
            Type = type;
            Location = location;
            Health = 1.0f;
            Equipment = new Dictionary<int, Item>();
            Item = new Item(ItemType.Air, 0, null);
            Yaw = yaw * (1F / 256) * 360; // to angle in 360 degree
            Pitch = pitch * (1F / 256) * 360;
            ObjectData = objectData;
        }

        /// <summary>
        /// Create a new entity based on Entity ID, Entity Type, location, name and UUID
        /// </summary>
        /// <param name="ID">Entity ID</param>
        /// <param name="type">Entity Type Enum</param>
        /// <param name="location">Entity location</param>
        /// <param name="uuid">Player uuid</param>
        /// <param name="name">Player name</param>
        public Entity(int ID, EntityType type, Location location, Guid uuid, string? name, byte yaw, byte pitch)
        {
            this.ID = ID;
            Type = type;
            Location = location;
            UUID = uuid;
            Name = name;
            Health = 1.0f;
            Equipment = new Dictionary<int, Item>();
            Item = new Item(ItemType.Air, 0, null);
            Yaw = yaw * (1F / 256) * 360; // to angle in 360 degree
            Pitch = pitch * (1F / 256) * 360;
        }

        public static string GetTypeString(EntityType type)
        {
            string typeStr = type.ToString();
            string? trans = ChatParser.TranslateString("entity.minecraft." + typeStr.ToUnderscoreCase());
            return string.IsNullOrEmpty(trans) ? typeStr : trans;
        }

        public string GetTypeString()
        {
            return GetTypeString(Type);
        }
    }
}
