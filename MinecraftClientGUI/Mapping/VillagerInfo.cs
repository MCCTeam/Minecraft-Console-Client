namespace MinecraftClient
{
    /// <summary>
    /// Information of a villager
    /// </summary>
    public class VillagerInfo
    {
        public int Level { get; set; }
        public int Experience { get; set; }
        public bool IsRegularVillager { get; set; }
        public bool CanRestock { get; set; }
    }
}
