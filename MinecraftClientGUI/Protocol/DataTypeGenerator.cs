using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace MinecraftClient.Protocol
{
    /// <summary>
    /// Generic generator for MCC Enumerations such as ItemType or EntityType, mapping protocol IDs to actual enumeration fields such as 1 => Stone for inventories.
    /// Works by processing Minecraft registries.json exported from minecraft_server.jar
    /// </summary>
    /// <remarks>java -cp minecraft_server.jar net.minecraft.data.Main --reports</remarks>
    public static class DataTypeGenerator
    {
        /// <summary>
        /// Read Minecraft registry from Json and build a dictionary
        /// </summary>
        /// <param name="registriesJsonFile">Path to registries.json generated from Minecraft server Jar</param>
        /// <param name="jsonRegistryName">Name of registry we want to process, e.g. minecraft:item</param>
        /// <returns></returns>
        private static Dictionary<int, string> LoadRegistry(string registriesJsonFile, string jsonRegistryName)
        {
            Json.JSONData rawJson = Json.ParseJson(File.ReadAllText(registriesJsonFile));
            Json.JSONData rawRegistry = rawJson.Properties[jsonRegistryName].Properties["entries"];
            Dictionary<int, string> registry = new Dictionary<int, string>();

            foreach (KeyValuePair<string, Json.JSONData> entry in rawRegistry.Properties)
            {
                int entryId = int.Parse(entry.Value.Properties["protocol_id"].StringValue);

                //minecraft:item_name => ItemName
                string entryName = String.Concat(
                    entry.Key.Replace("minecraft:", "")
                    .Split('_')
                    .Select(word => char.ToUpper(word[0]) + word.Substring(1))
                );

                if (registry.ContainsKey(entryId))
                    throw new InvalidDataException("Duplicate entry ID " + entryId + "!?");

                registry.Add(entryId, entryName);
            }

            return registry;
        }

        /// <summary>
        /// Generate MCC Enum from a Minecraft registry without Palette (static enum that does not change between versions)
        /// </summary>
        /// <param name="registriesJsonFile">Path to registries.json generated from Minecraft server Jar</param>
        /// <param name="jsonRegistryName">Name of registry we want to process, e.g. minecraft:item</param>
        /// <param name="outputEnum">Output enum name, e.g. ItemType (output file will be ItemType.cs)</param>
        /// <param name="enumNamespace">Output enum namespace, e.g. MinecraftClient.Inventory</param>
        public static void GenerateEnum(string registriesJsonFile, string jsonRegistryName, string outputEnum, string enumNamespace)
        {
            List<string> outputEnumLines = new List<string>();

            outputEnumLines.AddRange(new[] {
                "namespace " + enumNamespace,
                "{",
                "    public enum " + outputEnum,
                "    {"
            });

            Dictionary<int, string> registry = LoadRegistry(registriesJsonFile, jsonRegistryName);
            foreach (KeyValuePair<int, string> entry in registry)
                outputEnumLines.Add("        " + entry.Value + " = " + entry.Key + ',');

            outputEnumLines.AddRange(new[] {
                "    }",
                "}"
            });

            string outputEnumPath = Path.Combine(Path.GetDirectoryName(registriesJsonFile), outputEnum + "XXX.cs");
            File.WriteAllLines(outputEnumPath, outputEnumLines);
        }

        /// <summary>
        /// Generate MCC Enum from a Minecraft registry with Palette (dynamic enum that changes between versions)
        /// </summary>
        /// <param name="registriesJsonFile">Path to registries.json generated from Minecraft server Jar</param>
        /// <param name="jsonRegistryName">Name of registry we want to process, e.g. minecraft:item</param>
        /// <param name="outputEnum">Output enum name, e.g. ItemType (output file will be ItemType.cs)</param>
        /// <param name="enumNamespace">Enum namespace, e.g. MinecraftClient.Mapping</param>
        /// <param name="outputPalette">Output palette name, e.g. ItemPalette (output file will be ItemPalette.cs and ItemPaletteXXX.cs)</param>
        /// <param name="paletteNamespace">Palette namespace, e.g. MinecraftClient.EntityPalettes</param>
        public static void GenerateEnumWithPalette(string registriesJsonFile, string jsonRegistryName, string outputEnum, string enumNamespace, string outputPalette, string paletteNamespace)
        {
            List<string> outputEnumLines = new List<string>();
            List<string> outputPaletteLines = new List<string>();

            outputEnumLines.AddRange(new[] {
                "namespace " + enumNamespace,
                "{",
                "    public enum " + outputEnum,
                "    {"
            });

            outputPaletteLines.AddRange(new[] {
                "using System;",
                "using System.Collections.Generic;",
                "",
                "namespace " + paletteNamespace,
                "{",
                "    public class " + outputPalette + "XXX : " + outputPalette,
                "    {",
                "        private static Dictionary<int, " + outputEnum + "> mappings = new Dictionary<int, " + outputEnum + ">();",
                "",
                "        static " + outputPalette + "XXX()",
                "        {",
            });

            Dictionary<int, string> registry = LoadRegistry(registriesJsonFile, jsonRegistryName);

            foreach (KeyValuePair<int, string> entry in registry)
            {
                outputEnumLines.Add("        " + entry.Value + ',');
                outputPaletteLines.Add("            mappings[" + entry.Key + "] = " + outputEnum + "." + entry.Value + ";");
            }

            outputEnumLines.AddRange(new[] {
                "    }",
                "}"
            });

            outputPaletteLines.AddRange(new[] {
                "        }",
                "",
                "        protected override Dictionary<int, " + outputEnum + "> GetDict()",
                "        {",
                "            return mappings;",
                "        }",
                "    }",
                "}"
            });

            string outputEnumPath = Path.Combine(Path.GetDirectoryName(registriesJsonFile), outputEnum + "XXX.cs");
            string outputPalettePath = Path.Combine(Path.GetDirectoryName(registriesJsonFile), outputPalette + "XXX.cs");

            File.WriteAllLines(outputEnumPath, outputEnumLines);
            File.WriteAllLines(outputPalettePath, outputPaletteLines);
        }
    }
}
