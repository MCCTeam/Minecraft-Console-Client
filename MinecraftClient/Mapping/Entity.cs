using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        /// Entity type determined by Minecraft Console Client
        /// </summary>
        public EntityType Type;

        /// <summary>
        /// Entity type ID (more precise than Type, but may change between Minecraft versions)
        /// </summary>
        public int TypeID;

        /// <summary>
        /// Entity location in the Minecraft world
        /// </summary>
        public Location Location;

        /// <summary>
        /// Create a new entity based on Entity ID and location
        /// </summary>
        /// <param name="ID">Entity ID</param>
        /// <param name="location">Entity location</param>
        public Entity(int ID, Location location)
        {
            this.ID = ID;
            this.Location = location;
        }

        /// <summary>
        /// Create a new entity based on Entity ID, Entity Type and location
        /// </summary>
        /// <param name="ID">Entity ID</param>
        /// <param name="TypeID">Entity Type ID</param>
        /// <param name="location">Entity location</param>
        public Entity(int ID, int TypeID, Location location)
        {
            this.ID = ID;
            this.TypeID = TypeID;
            this.Location = location;
        }

        /// <summary>
        /// Create a new entity based on Entity ID, Entity Type and location
        /// </summary>
        /// <param name="ID">Entity ID</param>
        /// <param name="TypeID">Entity Type ID</param>
        /// <param name="type">Entity Type Enum</param>
        /// <param name="location">Entity location</param>
        public Entity(int ID, int TypeID, EntityType type, Location location)
        {
            this.ID = ID;
            this.TypeID = TypeID;
            this.Type = type;
            this.Location = location;
        }

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
        /// Return TRUE if the Entity is an hostile mob
        /// </summary>
        /// <remarks>New mobs added in newer Minecraft versions might be absent from the list</remarks>
        /// <returns>TRUE if hostile</returns>
        public bool IsHostile()
        {
            switch (TypeID)
            {
                case 5: return true;  // Blaze;
                case 12: return true; // Creeper
                case 16: return true; // Drowned
                case 23: return true; // Evoker
                case 29: return true; // Ghast
                case 31: return true; // Guardian
                case 33: return true; // Husk
                case 41: return true; // Magma Cube
                case 57: return true; // Zombie Pigman
                case 63: return true; // Shulker
                case 65: return true; // Silverfish
                case 66: return true; // Skeleton
                case 68: return true; // Slime
                case 75: return true; // Stray
                case 84: return true; // Vex
                case 87: return true; // Vindicator
                case 88: return true; // Pillager
                case 90: return true; // Witch
                case 92: return true; // Wither Skeleton
                case 95: return true; // Zombie
                case 97: return true; // Zombie Villager
                case 98: return true; // Phantom
                case 99: return true; // Ravager
                default: return false;
            }
        }
    }
}
