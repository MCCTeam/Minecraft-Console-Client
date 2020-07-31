using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Abstract incoming packet numbering
    /// </summary>
    /// <remarks>
    /// Please add new entries at the bottom of the list (but above UnknownPakcket)
    /// You'll also need to add them to Protocol18PacketTypes for all MC versions since MC 1.7
    /// </remarks>
    enum PacketIncomingType
    {
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
        WindowConfirmation,
        SetSlot,
        SpawnEntity,
        SpawnLivingEntity,
        SpawnPlayer,
        DestroyEntities,
        SetCooldown,
        EntityPosition,
        EntityPositionAndRotation,
        EntityProperties,
        EntityTeleport,
        EntityEquipment,
        EntityVelocity,
        EntityEffect,
        TimeUpdate,
        UpdateHealth,
        SetExperience,
        HeldItemChange,
        Explosion,
        MapData,
        Title,
        ScoreboardObjective,
        UpdateScore,

        /// <summary>
        /// Represents a packet not implemented in MCC.
        /// </summary>
        UnknownPacket,
    }
}
