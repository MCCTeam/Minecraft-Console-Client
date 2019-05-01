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
            HashSet<int> knownStates = new HashSet<int>();
            Dictionary<string, HashSet<int>> blocks = new Dictionary<string, HashSet<int>>();

            Json.JSONData palette = Json.ParseJson(File.ReadAllText(blocksJsonFile));
            foreach (KeyValuePair<string, Json.JSONData> item in palette.Properties)
            {
                //minecraft:item_name => ItemName
                string blockType = String.Concat(
                    item.Key.Replace("minecraft:", "")
                    .Split('_')
                    .Select(word => char.ToUpper(word[0]) + word.Substring(1))
                );

                if (blocks.ContainsKey(blockType))
                    throw new InvalidDataException("Duplicate block type " + blockType + "!?");
                blocks[blockType] = new HashSet<int>();

                foreach (Json.JSONData state in item.Value.Properties["states"].DataArray)
                {
                    int id = int.Parse(state.Properties["id"].StringValue);

                    if (knownStates.Contains(id))
                        throw new InvalidDataException("Duplicate state id " + id + "!?");

                    knownStates.Add(id);
                    blocks[blockType].Add(id);
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
                "        private static Dictionary<int, Material> materials = new Dictionary<int, Material>();",
                "",
                "        static PaletteXXX()",
                "        {",
            });

            foreach (KeyValuePair<string, HashSet<int>> blockType in blocks)
            {
                if (blockType.Value.Count > 0)
                {
                    List<int> idList = blockType.Value.ToList();
                    string materialName = blockType.Key;
                    materials.Add(materialName);

                    if (idList.Count > 1)
                    {
                        idList.Sort();
                        Queue<int> idQueue = new Queue<int>(idList);

                        while (idQueue.Count > 0)
                        {
                            int startValue = idQueue.Dequeue();
                            int endValue = startValue;
                            while (idQueue.Count > 0 && idQueue.Peek() == endValue + 1)
                                endValue = idQueue.Dequeue();
                            if (endValue > startValue)
                            {
                                outFile.Add("            for (int i = " + startValue + "; i <= " + endValue + "; i++)");
                                outFile.Add("                materials[i] = Material." + materialName + ";");
                            }
                            else outFile.Add("            materials[" + startValue + "] = Material." + materialName + ";");
                        }
                    }
                    else outFile.Add("            materials[" + idList[0] + "] = Material." + materialName + ";");
                }
                else throw new InvalidDataException("No state id  for block type " + blockType.Key + "!?");
            }

            outFile.AddRange(new[] {
                "        }",
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
                outFile.AddRange(new[] {
                    "namespace MinecraftClient.Mapping",
                    "{",
                    "    public enum Material",
                    "    {"
                });
                foreach (string material in materials)
                    outFile.Add("        " + material + ",");
                outFile.AddRange(new[] {
                    "    }",
                    "}"
                });
                File.WriteAllLines(outputEnum, outFile);
            }
        }
    }
}
