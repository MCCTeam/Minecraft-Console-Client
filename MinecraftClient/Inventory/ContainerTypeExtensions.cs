using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Inventory
{
    public static class ContainerTypeExtensions
    {
        /// <summary>
        /// Get the slot count of the container
        /// </summary>
        /// <param name="c"></param>
        /// <returns>Slot count of the container</returns>
        public static int SlotCount(this ContainerType c)
        {
            switch (c)
            {
                case ContainerType.PlayerInventory: return 46;
                case ContainerType.Generic_9x3: return 63;
                case ContainerType.Generic_9x6: return 90;
                case ContainerType.Generic_3x3: return 45;
                case ContainerType.Crafting: return 46;
                case ContainerType.BlastFurnace: return 39;
                case ContainerType.Furnace: return 39;
                case ContainerType.Smoker: return 39;
                case ContainerType.Enchantment: return 38;
                case ContainerType.BrewingStand: return 41;
                case ContainerType.Merchant: return 39;
                case ContainerType.Beacon: return 37;
                case ContainerType.Anvil: return 39;
                case ContainerType.Hopper: return 41;
                case ContainerType.ShulkerBox: return 63;
                case ContainerType.Loom: return 40;
                case ContainerType.Stonecutter: return 38;
                case ContainerType.Lectern: return 37;
                case ContainerType.Cartography: return 39;
                case ContainerType.Grindstone: return 39;
                case ContainerType.Unknown: return 0;
                default: return 0;
            }
        }

        /// <summary>
        /// Get an ASCII art representation of the container
        /// </summary>
        /// <param name="c"></param>
        /// <returns>ASCII art representation or NULL if not implemented for this container type</returns>
        public static string GetAsciiArt(this ContainerType c)
        {
            switch (c)
            {
                case ContainerType.PlayerInventory: return DefaultConfigResource.ContainerType_PlayerInventory;
                case ContainerType.Generic_9x3: return DefaultConfigResource.ContainerType_Generic_9x3;
                case ContainerType.Generic_9x6: return DefaultConfigResource.ContainerType_Generic_9x6;
                case ContainerType.Generic_3x3: return DefaultConfigResource.ContainerType_Generic_3x3;
                case ContainerType.Crafting: return DefaultConfigResource.ContainerType_Crafting;
                case ContainerType.BlastFurnace: return null;
                case ContainerType.Furnace: return null;
                case ContainerType.Smoker: return null;
                case ContainerType.Enchantment: return null;
                case ContainerType.BrewingStand: return DefaultConfigResource.ContainerType_BrewingStand;
                case ContainerType.Merchant: return null;
                case ContainerType.Beacon: return null;
                case ContainerType.Anvil: return null;
                case ContainerType.Hopper: return null;
                case ContainerType.ShulkerBox: return null;
                case ContainerType.Loom: return null;
                case ContainerType.Stonecutter: return null;
                case ContainerType.Lectern: return null;
                case ContainerType.Cartography: return null;
                case ContainerType.Grindstone: return null;
                case ContainerType.Unknown: return null;
                default: return null;
            }
        }
    }
}
