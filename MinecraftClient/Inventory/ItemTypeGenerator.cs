using MinecraftClient.Protocol;

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
        /// <remarks>java -cp minecraft_server.jar net.minecraft.data.Main --reports</remarks>
        public static void GenerateItemTypes(string registriesJsonFile)
        {
            DataTypeGenerator.GenerateEnum(registriesJsonFile, "minecraft:item", "ItemType", "MinecraftClient.Inventory");
        }
    }
}
