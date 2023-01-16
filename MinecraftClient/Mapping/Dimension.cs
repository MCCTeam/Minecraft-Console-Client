using System;
using System.Collections.Generic;

namespace MinecraftClient.Mapping
{

    /// <summary>
    /// The dimension type, available after 1.16.2
    /// </summary>
    public class Dimension
    {
        /// <summary>
        /// The name of the dimension type (for example, "minecraft:overworld").
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Whether piglins shake and transform to zombified piglins.
        /// </summary>
        public readonly bool piglinSafe = false;

        /// <summary>
        /// Possibly the light level(s) at which monsters can spawn.
        /// </summary>
        public readonly int monsterSpawnMinLightLevel = 0;
        public readonly int monsterSpawnMaxLightLevel = 7;
        public readonly int monsterSpawnBlockLightLimit = 0;

        /// <summary>
        /// When false, compasses spin randomly. When true, nether portals can spawn zombified piglins.
        /// </summary>
        public readonly bool natural = true;

        /// <summary>
        /// How much light the dimension has.
        /// </summary>
        public readonly float ambientLight = 0.0f;


        /// <summary>
        /// If set, the time of the day is the specified value.
        /// Value: -1: not set
        /// Value: [0, 24000]: time of the day
        /// </summary>
        public readonly long fixedTime = -1;

        /// <summary>
        /// A resource location defining what block tag to use for infiniburn.
        /// Value above 1.18.2: "#" or minecraft resource "#minecraft:...".
        /// Value below 1.18.1: "" or minecraft resource "minecraft:...".
        /// </summary>
        public readonly string infiniburn = "#minecraft:infiniburn_overworld";

        /// <summary>
        /// Whether players can charge and use respawn anchors.
        /// </summary>
        public readonly bool respawnAnchorWorks = false;

        /// <summary>
        /// Whether the dimension has skylight access or not.
        /// </summary>
        public readonly bool hasSkylight = true;

        /// <summary>
        /// Whether players can use a bed to sleep.
        /// </summary>
        public readonly bool bedWorks = true;

        /// <summary>
        /// unknown
        /// Values: "minecraft:overworld", "minecraft:the_nether", "minecraft:the_end" or something else.
        /// </summary>
        public readonly string effects = "minecraft:overworld";

        /// <summary>
        /// Whether players with the Bad Omen effect can cause a raid.
        /// </summary>
        public readonly bool hasRaids = true;

        /// <summary>
        /// The minimum Y level.
        /// </summary>
        public readonly int minY = 0;

        /// <summary>
        /// The maximum Y level.
        /// </summary>
        public readonly int maxY = 256;

        /// <summary>
        /// The maximum height.
        /// </summary>
        public readonly int height = 256;

        /// <summary>
        /// The maximum height to which chorus fruits and nether portals can bring players within this dimension.
        /// </summary>
        public readonly int logicalHeight = 256;

        /// <summary>
        /// The multiplier applied to coordinates when traveling to the dimension.
        /// </summary>
        public readonly double coordinateScale = 1.0;

        /// <summary>
        /// Whether the dimensions behaves like the nether (water evaporates and sponges dry) or not. Also causes lava to spread thinner.
        /// </summary>
        public readonly bool ultrawarm = false;

        /// <summary>
        /// Whether the dimension has a bedrock ceiling or not. When true, causes lava to spread faster.
        /// </summary>
        public readonly bool hasCeiling = false;

        /// <summary>
        /// Default value used in version below 1.17
        /// </summary>
        public Dimension()
        {
            Name = "minecraft:overworld";
        }

        /// <summary>
        /// Create from the "Dimension Codec" NBT Tag Compound
        /// </summary>
        /// <param name="name">Dimension name</param>
        /// <param name="nbt">The dimension type (NBT Tag Compound)</param>
        public Dimension(string name, Dictionary<string, object> nbt)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));

            if (nbt == null)
                throw new ArgumentNullException(nameof(nbt));

            if (nbt.ContainsKey("piglin_safe"))
                piglinSafe = Convert.ToBoolean(nbt["piglin_safe"]);
            if (nbt.ContainsKey("monster_spawn_light_level"))
            {
                try
                {
                    var monsterSpawnLightLevelObj = nbt["monster_spawn_light_level"];
                    try
                    {
                        monsterSpawnMinLightLevel = monsterSpawnMaxLightLevel = Convert.ToInt32(monsterSpawnLightLevelObj);
                    }
                    catch (Exception)
                    {
                        var inclusive = (Dictionary<string, object>)(((Dictionary<string, object>)monsterSpawnLightLevelObj)["value"]);
                        monsterSpawnMinLightLevel = Convert.ToInt32(inclusive["min_inclusive"]);
                        monsterSpawnMaxLightLevel = Convert.ToInt32(inclusive["max_inclusive"]);
                    }

                }
                catch (KeyNotFoundException) { }
            }
            if (nbt.ContainsKey("monster_spawn_block_light_limit"))
                monsterSpawnBlockLightLimit = Convert.ToInt32(nbt["monster_spawn_block_light_limit"]);
            if (nbt.ContainsKey("natural"))
                natural = Convert.ToBoolean(nbt["natural"]);
            if (nbt.ContainsKey("ambient_light"))
                ambientLight = (float)Convert.ToDouble(nbt["ambient_light"]);
            if (nbt.ContainsKey("fixed_time"))
                fixedTime = Convert.ToInt64(nbt["fixed_time"]);
            if (nbt.ContainsKey("infiniburn"))
                infiniburn = Convert.ToString(nbt["infiniburn"]) ?? string.Empty;
            if (nbt.ContainsKey("respawn_anchor_works"))
                respawnAnchorWorks = Convert.ToBoolean(nbt["respawn_anchor_works"]);
            if (nbt.ContainsKey("has_skylight"))
                hasSkylight = Convert.ToBoolean(nbt["has_skylight"]);
            if (nbt.ContainsKey("bed_works"))
                bedWorks = Convert.ToBoolean(nbt["bed_works"]);
            if (nbt.ContainsKey("effects"))
                effects = Convert.ToString(nbt["effects"]) ?? string.Empty;
            if (nbt.ContainsKey("has_raids"))
                hasRaids = Convert.ToBoolean(nbt["has_raids"]);
            if (nbt.ContainsKey("min_y"))
                minY = Convert.ToInt32(nbt["min_y"]);
            if (nbt.ContainsKey("height"))
                height = Convert.ToInt32(nbt["height"]);
            if (nbt.ContainsKey("min_y") && nbt.ContainsKey("height"))
                maxY = minY + height;
            if (nbt.ContainsKey("logical_height") && nbt["logical_height"].GetType() != typeof(byte))
                logicalHeight = Convert.ToInt32(nbt["logical_height"]);
            if (nbt.ContainsKey("coordinate_scale"))
                coordinateScale = Convert.ToDouble(nbt["coordinate_scale"]);
            if (nbt.ContainsKey("ultrawarm"))
                ultrawarm = Convert.ToBoolean(nbt["ultrawarm"]);
            if (nbt.ContainsKey("has_ceiling"))
                hasCeiling = Convert.ToBoolean(nbt["has_ceiling"]);
        }
    }
}
