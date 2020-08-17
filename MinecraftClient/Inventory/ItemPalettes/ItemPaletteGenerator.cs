using MinecraftClient.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Inventory.ItemPalettes
{
    public static class ItemPaletteGenerator
    {
        /// <summary>
        /// Place below line to Program.cs
        /// Inventory.ItemPalettes.ItemPaletteGenerator.GenerateItemType(@"your\path\to\registries.json");
        /// See https://wiki.vg/Data_Generators for getting those .json
        /// </summary>
        /// <param name="registriesJsonFile"></param>
        public static void GenerateItemType(string registriesJsonFile)
        {
            DataTypeGenerator.GenerateEnumWithPalette(
                registriesJsonFile,
                "minecraft:item", 
                "ItemType", 
                "MinecraftClient.Inventory",
                "ItemPalette",
                "MinecraftClient.Inventory.ItemPalettes");
        }
    }
}
