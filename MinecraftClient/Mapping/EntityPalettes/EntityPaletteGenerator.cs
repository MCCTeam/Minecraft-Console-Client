using MinecraftClient.Protocol;

namespace MinecraftClient.Mapping.EntityPalettes
{
    /// <summary>
    /// Generator for MCC ItemType enumeration
    /// </summary>
    public static class EntityPaletteGenerator
    {
        /// <summary>
        /// Generate EntityType.cs from Minecraft registries.json
        /// </summary>
        /// <param name="registriesJsonFile">path to registries.json</param>
        /// <remarks>
        /// java -cp minecraft_server.jar net.minecraft.data.Main --reports</remarks>
        /// For 1.18+: java -DbundlerMainClass=net.minecraft.data.Main -jar minecraft_server.jar --reports
        /// </remarks>
        public static void GenerateEntityTypes(string registriesJsonFile)
        {
            DataTypeGenerator.GenerateEnumWithPalette(registriesJsonFile, "minecraft:entity_type", "EntityType", "MinecraftClient.Mapping", "EntityPalette", "MinecraftClient.Mapping.EntityPalettes");
        }
    }
}
