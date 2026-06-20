namespace MinecraftClient.Mapping
{
    public record MapIcon
    {
        public MapIconType Type { get; set; }
        public byte X { get; set; }
        public byte Z { get; set; }
        public byte Direction { get; set; }
        public string? DisplayName { get; set; } = null;
    }
}
