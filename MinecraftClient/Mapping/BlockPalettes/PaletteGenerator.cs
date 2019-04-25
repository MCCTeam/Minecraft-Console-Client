using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace MinecraftClient.Mapping.BlockPalettes
{
    /// <summary>
    /// Generator for MCC Palette mappings
    /// </summary>
    public static class PaletteGenerator
    {
        /// <summary>
        /// Generate mapping from Minecraft blocks.jsom
        /// </summary>
        /// <param name="blocksJsonFile">path to blocks.json</param>
        /// <param name="outputClass">output path for blocks.cs</param>
        /// <param name="outputEnum">output path for material.cs</param>
        /// <remarks>java -cp minecraft_server.jar net.minecraft.data.Main --reports</remarks>
        /// <returns>state => block name mappings</returns>
        public static void JsonToClass(string blocksJsonFile, string outputClass, string outputEnum = null)
        {
            Dictionary<int, string> blocks = new Dictionary<int, string>();

            Json.JSONData palette = Json.ParseJson(File.ReadAllText(blocksJsonFile));
            foreach (KeyValuePair<string, Json.JSONData> item in palette.Properties)
            {
                string blockType = item.Key;
                foreach (Json.JSONData state in item.Value.Properties["states"].DataArray)
                {
                    int id = int.Parse(state.Properties["id"].StringValue);
                    blocks[id] = blockType;
                }
            }

            HashSet<string> materials = new HashSet<string>();
            List<string> outFile = new List<string>();
            outFile.AddRange(new[] {
                "using System;",
                "using System.Collections.Generic;",
                "",
                "namespace MinecraftClient.Mapping.BlockPalettes",
                "{",
                "    public class PaletteXXX : PaletteMapping",
                "    {",
                "        private static Dictionary<int, Material> materials = new Dictionary<int, Material>()",
                "        {",
            });

            foreach (KeyValuePair<int, string> item in blocks)
            {
                //minecraft:item_name => ItemName
                string name = String.Concat(
                    item.Value.Replace("minecraft:", "")
                    .Split('_')
                    .Select(word => char.ToUpper(word[0]) + word.Substring(1))
                );
                outFile.Add("            { " + item.Key + ", Material." + name + " },");
                materials.Add(name);
            }

            outFile.AddRange(new[] {
                "        };",
                "",
                "        protected override Dictionary<int, Material> GetDict()",
                "        {",
                "            return materials;",
                "        }",
                "    }",
                "}"
            });

            File.WriteAllLines(outputClass, outFile);

            if (outputEnum != null)
            {
                outFile = new List<string>();
                outFile.Add("    public enum Material");
                outFile.Add("    {");
                foreach (string material in materials)
                    outFile.Add("        " + material + ",");
                outFile.Add("    }");
                File.WriteAllLines(outputEnum, outFile);
            }
        }
    }
}
