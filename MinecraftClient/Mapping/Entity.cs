using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Mapping
{
    public class Entity
    {
        public int ID;
        public int TypeID;
        public EntityType Type;
        public string Name;
        public Location Location;
        public Entity(int ID, Location location)
        {
            this.ID = ID;
            this.Location = location;
        }
        public Entity(int ID, int TypeID, Location location)
        {
            this.ID = ID;
            this.TypeID = TypeID;
            this.Name = GetMobName(TypeID);
            this.Location = location;
        }
        public Entity(int ID, int TypeID, EntityType type, Location location)
        {
            this.ID = ID;
            this.TypeID = TypeID;
            this.Type = type;
            this.Name = GetMobName(TypeID);
            this.Location = location;
        }
        public Entity(int ID, EntityType type, Location location)
        {
            this.ID = ID;
            this.Type = type;
            this.Location = location;
        }
        public Entity(int ID, int TypeID, string Name, Location location)
        {
            this.ID = ID;
            this.TypeID = TypeID;
            this.Name = Name;
            this.Location = location;
        }


        /// <summary>
        /// Calculate the distance between two coordinate
        /// </summary>
        /// <param name="l1"></param>
        /// <param name="l2"></param>
        /// <returns></returns>
        public static double CalculateDistance(Location l1, Location l2)
        {
            return Math.Sqrt(Math.Pow(l2.X - l1.X, 2) + Math.Pow(l2.Y - l1.Y, 2) + Math.Pow(l2.Z - l1.Z, 2));
        }
        /// <summary>
        /// Get the mob name by entity type ID.
        /// </summary>
        /// <param name="EntityType"></param>
        /// <returns></returns>
        public static string GetMobName(int EntityType)
        {
            // only mobs in this list will be auto attacked
            switch (EntityType)
            {
                case 5: return "Blaze";
                case 12: return "Creeper";
                case 16: return "Drowned";
                case 23: return "Evoker";
                case 29: return "Ghast";
                case 31: return "Guardian";
                case 33: return "Husk";
                case 41: return "Magma Cube";
                case 57: return "Zombie Pigman";
                case 63: return "Shulker";
                case 65: return "Silverfish";
                case 66: return "Skeleton";
                case 68: return "Slime";
                case 75: return "Stray";
                case 84: return "Vex";
                case 87: return "Vindicator";
                case 88: return "Pillager";
                case 90: return "Witch";
                case 92: return "Wither Skeleton";
                case 95: return "Zombie";
                case 97: return "Zombie Villager";
                case 98: return "Phantom";
                case 99: return "Ravager";
                default: return "";
            }
        }
        public string GetMobName()
        {
            return GetMobName(TypeID);
        }
    }
}
