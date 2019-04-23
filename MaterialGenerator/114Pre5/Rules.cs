using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace MaterialGenerator._114Pre5
{
    public class Rules114Pre5 : IRules
    {
        public Dictionary<Material, List<string>> Rules()
        {
            return new Dictionary<Material, List<string>>
            {
                {
                    Material.Air, new List<string> {"air", "void_air", "cave_air"}
                },
                {
                    Material.Solid, new List<string>
                    {
                        "stone", "*granite", "*diorite", "*andesite",
                        "*dirt", "podzol", "*cobblestone", "*planks", "*sand", "gravel",
                        "*_log", "*_wood", "*_leaves", "*sponge", "*glass", "*sandstone",
                        "*piston*", "fern", "*bush", "*_wool", "*_block", "bricks", "tnt",
                        "bookshelf", "obsidian", "spawner", "*_ice", "clay", "jukebox",
                        "*_pumpkin", "netherrack", "glowstone", "jack_o_lantern", "*_bricks",
                        "infested_*", "melon", "end_stone", "redstone_lamp", "*anvil", "*_pillar",
                        "*terracotta*", "*concrete*"
                    }
                },
                {
                    Material.Walkable, new List<string>
                    {
                        "*sapling", "*rail", "cobweb", "dandelion", "poppy", "blue_orchid",
                        "allium", "azure_bluet", "*_tulip", "oxeye_daisy", "cornflower",
                        "lily_of_the_valley", "*_mushroom", "*_torch", "*_stairs", "redstone_wire",
                        "wheat", "*_pressure_plate", "snow", "sugar_cane",
                        "*_portal", "*_stem", "vine", "lily_pad", "nether_wart", "potted_*",
                        "daylight_detector", "hopper", "*slab", "*carpet", "*grass", "*coral_fan", "*coral_wall_fan"
                    }
                },
                {
                    Material.NonWalkable, new List<string>
                    {
                        "*_sign", "*_door", "ladder", "*_fence", "*glass_pane", "iron_bars", "*_gate",
                        "*_skull", "*_head", "*_banner", "*_wall"
                    }
                },
                {
                    Material.Undestroyable, new List<string>
                    {
                        "bedrock", "end_portal_frame"
                    }
                },
                {
                    Material.Water, new List<string> {"water"}
                },
                {
                    Material.Lava, new List<string> {"lava"}
                },
                {
                    Material.Ore, new List<string> {"*_ore"}
                },
                {
                    Material.HasInterface, new List<string>
                    {
                        "dispenser", "chest", "crafting_table",
                        "furnace", "enchanting_table", "brewing_stand",
                        "dropper", "*shulker_box", "loom", "barrel", "smoker", "blast_furnace",
                        "cartography_table", "fletching_table", "grindstone", "lectern",
                        "smithing_table", "stonecutter"
                    }
                },
                {
                    Material.CanUse, new List<string>
                    {
                        "lever", "*_button", "cake", "repeater", "*_trapdoor", "cauldron", "comparator", "bell",
                        "composter"
                    }
                },
                {
                    Material.Bed, new List<string> {"*_bed"}
                },
                {
                    Material.CanHarm, new List<string>
                    {
                        "wither_rose", "fire", "cactus", "campfire",
                    }
                }
            };
        }

        public Dictionary<string, List<int>> GetFromFile(string data)
        {
            var rawData = JsonConvert.DeserializeObject<Dictionary<string, DataObject>>(data);

            var res = new Dictionary<string, List<int>>();
            foreach (var o in rawData)
            {
                res.Add(o.Key.Substring("minecraft:".Length), o.Value.States.Select(x => x.Id).ToList());
            }

            return res;
        }
    }
}