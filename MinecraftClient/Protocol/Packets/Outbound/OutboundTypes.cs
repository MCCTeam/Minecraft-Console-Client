namespace MinecraftClient.Protocol.Packets.Outbound
{
    public enum OutboundTypes
    {
        KeepAlive = 0,
        ResourcePackStatus = 1,
        ChatMessage = 2,
        ClientStatus = 3,
        ClientSettings = 4,
        PluginMessage = 5,
        TabComplete = 6,
        PlayerPosition = 7,
        PlayerPositionAndLook = 8,
        TeleportConfirm = 9
    }
}