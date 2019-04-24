namespace MinecraftClient.Protocol.Packets.Inbound
{
    internal enum InboundTypes
    {
        KeepAlive = 1,
        JoinGame = 2,
        ChatMessage = 3,
        Respawn = 4,
        PlayerPositionAndLook = 5,
        ChunkData = 6,
        MultiBlockChange = 7,
        BlockChange = 8,
        MapChunkBulk = 9,
        UnloadChunk = 10,
        PlayerListUpdate = 11,
        TabCompleteResult = 12,
        PluginMessage = 13,
        KickPacket = 14,
        NetworkCompressionThreshold = 15,
        ResourcePackSend = 16,
        UnknownPacket = 0
    }
}