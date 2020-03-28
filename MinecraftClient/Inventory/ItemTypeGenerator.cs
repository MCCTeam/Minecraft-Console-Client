using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace MinecraftClient.Inventory
{
    /// <summary>
    /// Generator for MCC ItemType enumeration
    /// </summary>
    public static class ItemTypeGenerator
    {
        /// <summary>
        /// Generate ItemType.cs from Minecraft registries.json
        /// </summary>
        /// <param name="registriesJsonFile">path to registries.json</param>
        /// <param name="outputEnum">output path for ItemTypes.cs</param>
        /// <remarks>java -cp minecraft_server.jar net.minecraft.data.Main --reports</remarks>
        public static void JsonToClass(string registriesJsonFile, string outputEnum)
        {
            HashSet<int> itemIds = new HashSet<int>();
            Json.JSONData registries = Json.ParseJson(File.ReadAllText(registriesJsonFile));
            Json.JSONData itemRegistry = registries.Properties["minecraft:item"].Properties["entries"];
            List<string> outFile = new List<string>();

            outFile.AddRange(new[] {
                "namespace MinecraftClient.Inventory",
                "{",
                "    public enum ItemType",
                "    {"
            });

            foreach (KeyValuePair<string, Json.JSONData> item in itemRegistry.Properties)
            {
                int itemId = int.Parse(item.Value.Properties["protocol_id"].StringValue);

                //minecraft:item_name => ItemName
                string itemName = String.Concat(
                    item.Key.Replace("minecraft:", "")
                    .Split('_')
                    .Select(word => char.ToUpper(word[0]) + word.Substring(1))
                );

                if (itemIds.Contains(itemId))
                    throw new InvalidDataException("Duplicate item ID " + itemId + "!?");

                outFile.Add("        " + itemName + " = " + itemId + ',');
            }

            outFile.AddRange(new[] {
                "    }",
                "}"
            });

            File.WriteAllLines(outputEnum, outFile);
        }
    }
}
