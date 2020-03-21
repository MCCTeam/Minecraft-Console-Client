using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Abstract incoming packet numbering
    /// </summary>
    enum PacketIncomingType
    {
        // modified by reinforce
        SpawnEntity,
        SpawnLivingEntity,
        KeepAlive,
        JoinGame,
        ChatMessage,
        Respawn,
        PlayerPositionAndLook,
        ChunkData,
        MultiBlockChange,
        BlockChange,
        MapChunkBulk,
        UnloadChunk,
        PlayerListUpdate,
        TabCompleteResult,
        PluginMessage,
        KickPacket,
        NetworkCompressionTreshold,
        ResourcePackSend,
        CloseWindow,
        OpenWindow,
        WindowItems,
        SetSlot,
        UnknownPacket,
        DestroyEntities,
        SetCooldown,
        EntityPosition,
        EntityPositionAndRotation,
        EntityProperties,
        TimeUpdate,
        EntityTeleport
    }
}
