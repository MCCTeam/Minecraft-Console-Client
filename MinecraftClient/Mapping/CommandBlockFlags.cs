namespace MinecraftClient.Mapping
{
    /// <summary>
    /// Represents a unit movement in the world
    /// </summary>
    /// <see href="https://minecraft.wiki/w/Coordinates"/>
    public enum CommandBlockFlags
    {
        TrackOutput = 0x01,
        IsConditional = 0x02,
        Automatic = 0x04,
    }
}
