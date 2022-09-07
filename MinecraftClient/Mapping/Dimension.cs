using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            this.Name = "minecraft:overworld";
        }

        /// <summary>
        /// Create from the "Dimension Codec" NBT Tag Compound
        /// </summary>
        /// <param name="name">Dimension name</param>
        /// <param name="nbt">The dimension type (NBT Tag Compound)</param>
        public Dimension(string name, Dictionary<string, object> nbt)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (nbt == null)
                throw new ArgumentNullException("nbt Data");

            this.Name = name;

            if (nbt.ContainsKey("piglin_safe"))
                this.piglinSafe = 1 == (byte)nbt["piglin_safe"];
            if (nbt.ContainsKey("monster_spawn_light_level"))
            {
                try
                {
                    var monsterSpawnLightLevelObj = nbt["monster_spawn_light_level"];
                    if (monsterSpawnLightLevelObj.GetType() == typeof(int))
                        this.monsterSpawnMinLightLevel = this.monsterSpawnMaxLightLevel = (int)monsterSpawnLightLevelObj;
                    else
                    {
                        var inclusive = (Dictionary<string, object>)(((Dictionary<string, object>)monsterSpawnLightLevelObj)["value"]);
                        this.monsterSpawnMinLightLevel = (int)inclusive["min_inclusive"];
                        this.monsterSpawnMaxLightLevel = (int)inclusive["max_inclusive"];
                    }

                }
                catch (KeyNotFoundException) { }
            }
            if (nbt.ContainsKey("monster_spawn_block_light_limit"))
                this.monsterSpawnBlockLightLimit = (int)nbt["monster_spawn_block_light_limit"];
            if (nbt.ContainsKey("natural"))
                this.natural = 1 == (byte)nbt["natural"];
            if (nbt.ContainsKey("ambient_light"))
                this.ambientLight = (float)nbt["ambient_light"];
            if (nbt.ContainsKey("fixed_time"))
                this.fixedTime = (long)nbt["fixed_time"];
            if (nbt.ContainsKey("infiniburn"))
                this.infiniburn = (string)nbt["infiniburn"];
            if (nbt.ContainsKey("respawn_anchor_works"))
                this.respawnAnchorWorks = 1 == (byte)nbt["respawn_anchor_works"];
            if (nbt.ContainsKey("has_skylight"))
                this.hasSkylight = 1 == (byte)nbt["has_skylight"];
            if (nbt.ContainsKey("bed_works"))
                this.bedWorks = 1 == (byte)nbt["bed_works"];
            if (nbt.ContainsKey("effects"))
                this.effects = (string)nbt["effects"];
            if (nbt.ContainsKey("has_raids"))
                this.hasRaids = 1 == (byte)nbt["has_raids"];
            if (nbt.ContainsKey("min_y"))
                this.minY = (int)nbt["min_y"];
            if (nbt.ContainsKey("height"))
                this.height = (int)nbt["height"];
            if (nbt.ContainsKey("min_y") && nbt.ContainsKey("height"))
                this.maxY = this.minY + this.height;
            if (nbt.ContainsKey("logical_height") && nbt["logical_height"].GetType() != typeof(byte))
                this.logicalHeight = (int)nbt["logical_height"];
            if (nbt.ContainsKey("coordinate_scale"))
            {
                var coordinateScaleObj = nbt["coordinate_scale"];
                if (coordinateScaleObj.GetType() == typeof(float))
                    this.coordinateScale = (float)coordinateScaleObj;
                else
                    this.coordinateScale = (double)coordinateScaleObj;
            }
            if (nbt.ContainsKey("ultrawarm"))
                this.ultrawarm = 1 == (byte)nbt["ultrawarm"];
            if (nbt.ContainsKey("has_ceiling"))
                this.hasCeiling = 1 == (byte)nbt["has_ceiling"];
        }

    }
}
