using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Contains packet ID mappings for Protocol18
    /// </summary>
    class Protocol18PacketTypes
    {
        /// <summary>
        /// Get abstract numbering of the specified packet ID
        /// </summary>
        /// <remarks>
        /// When adding a new packet, see https://wiki.vg/Protocol_version_numbers
        /// For each switch below, see the corresponding page (e.g. MC 1.7, then 1.9) and add the ID
        /// By the way, also look for packet layout changes across versions and handle them in Protocol18.cs
        /// Please add entries in the same order as they are displayed in PacketIncomingType.cs
        /// </remarks>
        /// <param name="packetID">Packet ID</param>
        /// <param name="protocol">Protocol version</param>
        /// <returns>Abstract numbering</returns>
        public static PacketIncomingType GetPacketIncomingType(int packetID, int protocol)
        {
            if (protocol <= Protocol18Handler.MC18Version) // MC 1.7 and 1.8
            {
                switch (packetID)
                {
                    case 0x00: return PacketIncomingType.KeepAlive;
                    case 0x01: return PacketIncomingType.JoinGame;
                    case 0x02: return PacketIncomingType.ChatMessage;
                    case 0x07: return PacketIncomingType.Respawn;
                    case 0x08: return PacketIncomingType.PlayerPositionAndLook;
                    case 0x21: return PacketIncomingType.ChunkData;
                    case 0x22: return PacketIncomingType.MultiBlockChange;
                    case 0x23: return PacketIncomingType.BlockChange;
                    case 0x26: return PacketIncomingType.MapChunkBulk;
                    // UnloadChunk does not exist prior to 1.9
                    case 0x38: return PacketIncomingType.PlayerListUpdate;
                    case 0x3A: return PacketIncomingType.TabCompleteResult;
                    case 0x3F: return PacketIncomingType.PluginMessage;
                    case 0x40: return PacketIncomingType.KickPacket;
                    case 0x46: return PacketIncomingType.NetworkCompressionTreshold;
                    case 0x48: return PacketIncomingType.ResourcePackSend;
                    case 0x2E: return PacketIncomingType.CloseWindow;
                    case 0x2D: return PacketIncomingType.OpenWindow;
                    case 0x30: return PacketIncomingType.WindowItems;
                    case 0x2F: return PacketIncomingType.SetSlot;
                    case 0x0E: return PacketIncomingType.SpawnEntity;
                    case 0x0F: return PacketIncomingType.SpawnLivingEntity;
                    case 0x05: return PacketIncomingType.SpawnPlayer;
                    case 0x13: return PacketIncomingType.DestroyEntities;
                    // SetCooldown does not exist prior to 1.9
                    case 0x15: return PacketIncomingType.EntityPosition;
                    case 0x17: return PacketIncomingType.EntityPositionAndRotation;
                    case 0x20: return PacketIncomingType.EntityProperties;
                    case 0x18: return PacketIncomingType.EntityTeleport;
                    case 0x12: return PacketIncomingType.EntityVelocity;
                    case 0x04: return PacketIncomingType.EntityEquipment;
                    case 0x1E: return PacketIncomingType.EntityEffect;
                    case 0x03: return PacketIncomingType.TimeUpdate;
                    case 0x06: return PacketIncomingType.UpdateHealth;
                    case 0x1F: return PacketIncomingType.SetExperience;
                    case 0x09: return PacketIncomingType.HeldItemChange;
                    case 0x27: return PacketIncomingType.Explosion;
                    case 0x34: return PacketIncomingType.MapData;
                    case 0x45: return PacketIncomingType.Title;
                    case 0x3B: return PacketIncomingType.ScoreboardObjective;
                    case 0x3C: return PacketIncomingType.UpdateScore;
                }
            }
            else if (protocol <= Protocol18Handler.MC1112Version) // MC 1.9, 1.10 and 1.11
            {
                switch (packetID)
                {
                    case 0x1F: return PacketIncomingType.KeepAlive;
                    case 0x23: return PacketIncomingType.JoinGame;
                    case 0x0F: return PacketIncomingType.ChatMessage;
                    case 0x33: return PacketIncomingType.Respawn;
                    case 0x2E: return PacketIncomingType.PlayerPositionAndLook;
                    case 0x20: return PacketIncomingType.ChunkData;
                    case 0x10: return PacketIncomingType.MultiBlockChange;
                    case 0x0B: return PacketIncomingType.BlockChange;
                    // MapChunkBulk has been removed in 1.9
                    case 0x1D: return PacketIncomingType.UnloadChunk;
                    case 0x2D: return PacketIncomingType.PlayerListUpdate;
                    case 0x0E: return PacketIncomingType.TabCompleteResult;
                    case 0x18: return PacketIncomingType.PluginMessage;
                    case 0x1A: return PacketIncomingType.KickPacket;
                    // NetworkCompressionTreshold has been removed in 1.9
                    case 0x32: return PacketIncomingType.ResourcePackSend;
                    case 0x12: return PacketIncomingType.CloseWindow;
                    case 0x13: return PacketIncomingType.OpenWindow;
                    case 0x14: return PacketIncomingType.WindowItems;
                    case 0x16: return PacketIncomingType.SetSlot;
                    case 0x00: return PacketIncomingType.SpawnEntity;
                    case 0x03: return PacketIncomingType.SpawnLivingEntity;
                    case 0x05: return PacketIncomingType.SpawnPlayer;
                    case 0x30: return PacketIncomingType.DestroyEntities;
                    case 0x17: return PacketIncomingType.SetCooldown;
                    case 0x25: return PacketIncomingType.EntityPosition;
                    case 0x26: return PacketIncomingType.EntityPositionAndRotation;
                    case 0x4A: return PacketIncomingType.EntityProperties;
                    case 0x49: return PacketIncomingType.EntityTeleport;
                    case 0x3B: return PacketIncomingType.EntityVelocity;
                    case 0x3C: return PacketIncomingType.EntityEquipment;
                    case 0x4B: return PacketIncomingType.EntityEffect;
                    case 0x44: return PacketIncomingType.TimeUpdate;
                    case 0x3E: return PacketIncomingType.UpdateHealth;
                    case 0x3D: return PacketIncomingType.SetExperience;
                    case 0x37: return PacketIncomingType.HeldItemChange;
                    case 0x1C: return PacketIncomingType.Explosion;
                    case 0x24: return PacketIncomingType.MapData;
                    case 0x45: return PacketIncomingType.Title;
                    case 0x3F: return PacketIncomingType.ScoreboardObjective;
                    case 0x42: return PacketIncomingType.UpdateScore;
                }
            }
            else if (protocol <= Protocol18Handler.MC112Version) // MC 1.12.0
            {
                switch (packetID)
                {
                    case 0x1F: return PacketIncomingType.KeepAlive;
                    case 0x23: return PacketIncomingType.JoinGame;
                    case 0x0F: return PacketIncomingType.ChatMessage;
                    case 0x34: return PacketIncomingType.Respawn;
                    case 0x2E: return PacketIncomingType.PlayerPositionAndLook;
                    case 0x20: return PacketIncomingType.ChunkData;
                    case 0x10: return PacketIncomingType.MultiBlockChange;
                    case 0x0B: return PacketIncomingType.BlockChange;
                    // MapChunkBulk does not exist since 1.9
                    case 0x1D: return PacketIncomingType.UnloadChunk;
                    case 0x2D: return PacketIncomingType.PlayerListUpdate;
                    case 0x0E: return PacketIncomingType.TabCompleteResult;
                    case 0x18: return PacketIncomingType.PluginMessage;
                    case 0x1A: return PacketIncomingType.KickPacket;
                    // NetworkCompressionTreshold does not exist since 1.9
                    case 0x33: return PacketIncomingType.ResourcePackSend;
                    case 0x12: return PacketIncomingType.CloseWindow;
                    case 0x13: return PacketIncomingType.OpenWindow;
                    case 0x14: return PacketIncomingType.WindowItems;
                    case 0x16: return PacketIncomingType.SetSlot;
                    case 0x00: return PacketIncomingType.SpawnEntity;
                    case 0x03: return PacketIncomingType.SpawnLivingEntity;
                    case 0x05: return PacketIncomingType.SpawnPlayer;
                    case 0x31: return PacketIncomingType.DestroyEntities;
                    case 0x17: return PacketIncomingType.SetCooldown;
                    case 0x26: return PacketIncomingType.EntityPosition;
                    case 0x27: return PacketIncomingType.EntityPositionAndRotation;
                    case 0x4D: return PacketIncomingType.EntityProperties;
                    case 0x4B: return PacketIncomingType.EntityTeleport;
                    case 0x3D: return PacketIncomingType.EntityVelocity;
                    case 0x3E: return PacketIncomingType.EntityEquipment;
                    case 0x4E: return PacketIncomingType.EntityEffect;
                    case 0x46: return PacketIncomingType.TimeUpdate;
                    case 0x40: return PacketIncomingType.UpdateHealth;
                    case 0x3F: return PacketIncomingType.SetExperience;
                    case 0x39: return PacketIncomingType.HeldItemChange;
                    case 0x1C: return PacketIncomingType.Explosion;
                    case 0x24: return PacketIncomingType.MapData;
                    case 0x47: return PacketIncomingType.Title;
                    case 0x41: return PacketIncomingType.ScoreboardObjective;
                    case 0x44: return PacketIncomingType.UpdateScore;
                }
            }
            else if (protocol <= Protocol18Handler.MC1122Version) // MC 1.12.2
            {
                switch (packetID)
                {
                    case 0x1F: return PacketIncomingType.KeepAlive;
                    case 0x23: return PacketIncomingType.JoinGame;
                    case 0x0F: return PacketIncomingType.ChatMessage;
                    case 0x35: return PacketIncomingType.Respawn;
                    case 0x2F: return PacketIncomingType.PlayerPositionAndLook;
                    case 0x20: return PacketIncomingType.ChunkData;
                    case 0x10: return PacketIncomingType.MultiBlockChange;
                    case 0x0B: return PacketIncomingType.BlockChange;
                    // MapChunkBulk does not exist since 1.9
                    case 0x1D: return PacketIncomingType.UnloadChunk;
                    case 0x2E: return PacketIncomingType.PlayerListUpdate;
                    case 0x0E: return PacketIncomingType.TabCompleteResult;
                    case 0x18: return PacketIncomingType.PluginMessage;
                    case 0x1A: return PacketIncomingType.KickPacket;
                    // NetworkCompressionTreshold does not exist since 1.9
                    case 0x34: return PacketIncomingType.ResourcePackSend;
                    case 0x12: return PacketIncomingType.CloseWindow;
                    case 0x13: return PacketIncomingType.OpenWindow;
                    case 0x14: return PacketIncomingType.WindowItems;
                    case 0x16: return PacketIncomingType.SetSlot;
                    case 0x00: return PacketIncomingType.SpawnEntity;
                    case 0x03: return PacketIncomingType.SpawnLivingEntity;
                    case 0x05: return PacketIncomingType.SpawnPlayer;
                    case 0x32: return PacketIncomingType.DestroyEntities;
                    case 0x17: return PacketIncomingType.SetCooldown;
                    case 0x26: return PacketIncomingType.EntityPosition;
                    case 0x27: return PacketIncomingType.EntityPositionAndRotation;
                    case 0x4E: return PacketIncomingType.EntityProperties;
                    case 0x4C: return PacketIncomingType.EntityTeleport;
                    case 0x3E: return PacketIncomingType.EntityVelocity;
                    case 0x3F: return PacketIncomingType.EntityEquipment;
                    case 0x4F: return PacketIncomingType.EntityEffect;
                    case 0x47: return PacketIncomingType.TimeUpdate;
                    case 0x41: return PacketIncomingType.UpdateHealth;
                    case 0x40: return PacketIncomingType.SetExperience;
                    case 0x3A: return PacketIncomingType.HeldItemChange;
                    case 0x1C: return PacketIncomingType.Explosion;
                    case 0x25: return PacketIncomingType.MapData;
                    case 0x48: return PacketIncomingType.Title;
                    case 0x42: return PacketIncomingType.ScoreboardObjective;
                    case 0x45: return PacketIncomingType.UpdateScore;
                }
            }
            else if (protocol < Protocol18Handler.MC114Version) // MC 1.13 to 1.13.2
            {
                switch (packetID)
                {
                    case 0x21: return PacketIncomingType.KeepAlive;
                    case 0x25: return PacketIncomingType.JoinGame;
                    case 0x0E: return PacketIncomingType.ChatMessage;
                    case 0x38: return PacketIncomingType.Respawn;
                    case 0x32: return PacketIncomingType.PlayerPositionAndLook;
                    case 0x22: return PacketIncomingType.ChunkData;
                    case 0x0F: return PacketIncomingType.MultiBlockChange;
                    case 0x0B: return PacketIncomingType.BlockChange;
                    // MapChunkBulk does not exist since 1.9
                    case 0x1F: return PacketIncomingType.UnloadChunk;
                    case 0x30: return PacketIncomingType.PlayerListUpdate;
                    case 0x10: return PacketIncomingType.TabCompleteResult;
                    case 0x19: return PacketIncomingType.PluginMessage;
                    case 0x1B: return PacketIncomingType.KickPacket;
                    // NetworkCompressionTreshold does not exist since 1.9
                    case 0x37: return PacketIncomingType.ResourcePackSend;
                    case 0x13: return PacketIncomingType.CloseWindow;
                    case 0x14: return PacketIncomingType.OpenWindow;
                    case 0x15: return PacketIncomingType.WindowItems;
                    case 0x17: return PacketIncomingType.SetSlot;
                    case 0x00: return PacketIncomingType.SpawnEntity;
                    case 0x03: return PacketIncomingType.SpawnLivingEntity;
                    case 0x05: return PacketIncomingType.SpawnPlayer;
                    case 0x35: return PacketIncomingType.DestroyEntities;
                    case 0x18: return PacketIncomingType.SetCooldown;
                    case 0x28: return PacketIncomingType.EntityPosition;
                    case 0x29: return PacketIncomingType.EntityPositionAndRotation;
                    case 0x52: return PacketIncomingType.EntityProperties;
                    case 0x50: return PacketIncomingType.EntityTeleport;
                    case 0x41: return PacketIncomingType.EntityVelocity;
                    case 0x42: return PacketIncomingType.EntityEquipment;
                    case 0x53: return PacketIncomingType.EntityEffect;
                    case 0x4A: return PacketIncomingType.TimeUpdate;
                    case 0x44: return PacketIncomingType.UpdateHealth;
                    case 0x43: return PacketIncomingType.SetExperience;
                    case 0x3D: return PacketIncomingType.HeldItemChange;
                    case 0x1E: return PacketIncomingType.Explosion;
                    case 0x26: return PacketIncomingType.MapData;
                    case 0x4B: return PacketIncomingType.Title;
                    case 0x45: return PacketIncomingType.ScoreboardObjective;
                    case 0x48: return PacketIncomingType.UpdateScore;
                }
            }
            else if (protocol < Protocol18Handler.MC115Version) // MC 1.14 to 1.14.4
            {
                switch (packetID)
                {
                    case 0x20: return PacketIncomingType.KeepAlive;
                    case 0x25: return PacketIncomingType.JoinGame;
                    case 0x0E: return PacketIncomingType.ChatMessage;
                    case 0x3A: return PacketIncomingType.Respawn;
                    case 0x35: return PacketIncomingType.PlayerPositionAndLook;
                    case 0x21: return PacketIncomingType.ChunkData;
                    case 0x0F: return PacketIncomingType.MultiBlockChange;
                    case 0x0B: return PacketIncomingType.BlockChange;
                    // MapChunkBulk does not exist since 1.9
                    case 0x1D: return PacketIncomingType.UnloadChunk;
                    case 0x33: return PacketIncomingType.PlayerListUpdate;
                    case 0x10: return PacketIncomingType.TabCompleteResult;
                    case 0x18: return PacketIncomingType.PluginMessage;
                    case 0x1A: return PacketIncomingType.KickPacket;
                    // NetworkCompressionTreshold does not exist since 1.9
                    case 0x39: return PacketIncomingType.ResourcePackSend;
                    case 0x13: return PacketIncomingType.CloseWindow;
                    case 0x2E: return PacketIncomingType.OpenWindow;
                    case 0x14: return PacketIncomingType.WindowItems;
                    case 0x16: return PacketIncomingType.SetSlot;
                    case 0x00: return PacketIncomingType.SpawnEntity;
                    case 0x03: return PacketIncomingType.SpawnLivingEntity;
                    case 0x05: return PacketIncomingType.SpawnPlayer;
                    case 0x37: return PacketIncomingType.DestroyEntities;
                    case 0x17: return PacketIncomingType.SetCooldown;
                    case 0x28: return PacketIncomingType.EntityPosition;
                    case 0x29: return PacketIncomingType.EntityPositionAndRotation;
                    case 0x58: return PacketIncomingType.EntityProperties;
                    case 0x56: return PacketIncomingType.EntityTeleport;
                    case 0x41: return PacketIncomingType.EntityVelocity;
                    case 0x46: return PacketIncomingType.EntityEquipment;
                    case 0x59: return PacketIncomingType.EntityEffect;
                    case 0x4E: return PacketIncomingType.TimeUpdate;
                    case 0x48: return PacketIncomingType.UpdateHealth;
                    case 0x47: return PacketIncomingType.SetExperience;
                    case 0x3F: return PacketIncomingType.HeldItemChange;
                    case 0x1C: return PacketIncomingType.Explosion;
                    case 0x26: return PacketIncomingType.MapData;
                    case 0x4F: return PacketIncomingType.Title;
                    case 0x49: return PacketIncomingType.ScoreboardObjective;
                    case 0x4C: return PacketIncomingType.UpdateScore;
                }
            }
            else if (protocol <= Protocol18Handler.MC1152Version) // MC 1.15 to 1.15.2
            {
                switch (packetID)
                {
                    case 0x21: return PacketIncomingType.KeepAlive;
                    case 0x26: return PacketIncomingType.JoinGame;
                    case 0x0F: return PacketIncomingType.ChatMessage;
                    case 0x3B: return PacketIncomingType.Respawn;
                    case 0x36: return PacketIncomingType.PlayerPositionAndLook;
                    case 0x22: return PacketIncomingType.ChunkData;
                    case 0x10: return PacketIncomingType.MultiBlockChange;
                    case 0x0C: return PacketIncomingType.BlockChange;
                    // MapChunkBulk does not exist since 1.9
                    case 0x1E: return PacketIncomingType.UnloadChunk;
                    case 0x34: return PacketIncomingType.PlayerListUpdate;
                    case 0x11: return PacketIncomingType.TabCompleteResult;
                    case 0x19: return PacketIncomingType.PluginMessage;
                    case 0x1B: return PacketIncomingType.KickPacket;
                    // NetworkCompressionTreshold does not exist since 1.9
                    case 0x3A: return PacketIncomingType.ResourcePackSend;
                    case 0x14: return PacketIncomingType.CloseWindow;
                    case 0x2F: return PacketIncomingType.OpenWindow;
                    case 0x15: return PacketIncomingType.WindowItems;
                    case 0x17: return PacketIncomingType.SetSlot;
                    case 0x00: return PacketIncomingType.SpawnEntity;
                    case 0x03: return PacketIncomingType.SpawnLivingEntity;
                    case 0x05: return PacketIncomingType.SpawnPlayer;
                    case 0x38: return PacketIncomingType.DestroyEntities;
                    case 0x18: return PacketIncomingType.SetCooldown;
                    case 0x29: return PacketIncomingType.EntityPosition;
                    case 0x2A: return PacketIncomingType.EntityPositionAndRotation;
                    case 0x59: return PacketIncomingType.EntityProperties;
                    case 0x57: return PacketIncomingType.EntityTeleport;
                    case 0x46: return PacketIncomingType.EntityVelocity;
                    case 0x47: return PacketIncomingType.EntityEquipment;
                    case 0x5A: return PacketIncomingType.EntityEffect;
                    case 0x4F: return PacketIncomingType.TimeUpdate;
                    case 0x49: return PacketIncomingType.UpdateHealth;
                    case 0x48: return PacketIncomingType.SetExperience;
                    case 0x40: return PacketIncomingType.HeldItemChange;
                    case 0x1D: return PacketIncomingType.Explosion;
                    case 0x27: return PacketIncomingType.MapData;
                    case 0x50: return PacketIncomingType.Title;
                    case 0x4A: return PacketIncomingType.ScoreboardObjective;
                    case 0x4D: return PacketIncomingType.UpdateScore;

                }
            } else {
                switch (packetID)
                {
                    case 0x20: return PacketIncomingType.KeepAlive;
                    case 0x25: return PacketIncomingType.JoinGame;
                    case 0x0E: return PacketIncomingType.ChatMessage;
                    case 0x3A: return PacketIncomingType.Respawn;
                    case 0x35: return PacketIncomingType.PlayerPositionAndLook;
                    case 0x21: return PacketIncomingType.ChunkData;
                    case 0x0F: return PacketIncomingType.MultiBlockChange;
                    case 0x0B: return PacketIncomingType.BlockChange;
                    // MapChunkBulk does not exist since 1.9
                    case 0x1D: return PacketIncomingType.UnloadChunk;
                    case 0x33: return PacketIncomingType.PlayerListUpdate;
                    case 0x10: return PacketIncomingType.TabCompleteResult;
                    case 0x18: return PacketIncomingType.PluginMessage;
                    case 0x1A: return PacketIncomingType.KickPacket;
                    // NetworkCompressionTreshold does not exist since 1.9
                    case 0x39: return PacketIncomingType.ResourcePackSend;
                    case 0x13: return PacketIncomingType.CloseWindow;
                    case 0x2E: return PacketIncomingType.OpenWindow;
                    case 0x14: return PacketIncomingType.WindowItems;
                    case 0x16: return PacketIncomingType.SetSlot;
                    case 0x00: return PacketIncomingType.SpawnEntity;
                    case 0x02: return PacketIncomingType.SpawnLivingEntity;
                    case 0x04: return PacketIncomingType.SpawnPlayer;
                    case 0x37: return PacketIncomingType.DestroyEntities;
                    case 0x17: return PacketIncomingType.SetCooldown;
                    case 0x28: return PacketIncomingType.EntityPosition;
                    case 0x29: return PacketIncomingType.EntityPositionAndRotation;
                    case 0x58: return PacketIncomingType.EntityProperties;
                    case 0x56: return PacketIncomingType.EntityTeleport;
                    case 0x46: return PacketIncomingType.EntityVelocity;
                    case 0x47: return PacketIncomingType.EntityEquipment;
                    case 0x59: return PacketIncomingType.EntityEffect;
                    case 0x4E: return PacketIncomingType.TimeUpdate;
                    case 0x49: return PacketIncomingType.UpdateHealth;
                    case 0x48: return PacketIncomingType.SetExperience;
                    case 0x3F: return PacketIncomingType.HeldItemChange;
                    case 0x1C: return PacketIncomingType.Explosion;
                    case 0x26: return PacketIncomingType.MapData;
                    case 0x4F: return PacketIncomingType.Title;
                    case 0x4A: return PacketIncomingType.ScoreboardObjective;
                    case 0x4D: return PacketIncomingType.UpdateScore;
                }
            }

            return PacketIncomingType.UnknownPacket;
        }

        /// <summary>
        /// Get packet ID of the specified outgoing packet
        /// </summary>
        /// <remarks>
        /// When adding a new packet, see https://wiki.vg/Protocol_version_numbers
        /// For each switch below, see the corresponding page (e.g. MC 1.7, then 1.9) and add the ID
        /// By the way, also look for packet layout changes across versions and handle them in Protocol18.cs
        /// Please add entries in the same order as they are displayed in PacketOutgoingType.cs
        /// </remarks>
        /// <param name="packet">Abstract packet numbering</param>
        /// <param name="protocol">Protocol version</param>
        /// <returns>Packet ID</returns>
        public static int GetPacketOutgoingID(PacketOutgoingType packet, int protocol)
        {
            if (protocol <= Protocol18Handler.MC18Version) // MC 1.7 and 1.8
            {
                switch (packet)
                {
                    case PacketOutgoingType.KeepAlive: return 0x00;
                    case PacketOutgoingType.ResourcePackStatus: return 0x19;
                    case PacketOutgoingType.ChatMessage: return 0x01;
                    case PacketOutgoingType.ClientStatus: return 0x16;
                    case PacketOutgoingType.ClientSettings: return 0x15;
                    case PacketOutgoingType.PluginMessage: return 0x17;
                    case PacketOutgoingType.TabComplete: return 0x14;
                    case PacketOutgoingType.EntityAction: return 0x0B;
                    case PacketOutgoingType.PlayerPosition: return 0x04;
                    case PacketOutgoingType.PlayerPositionAndLook: return 0x06;
                    case PacketOutgoingType.TeleportConfirm: throw new InvalidOperationException("Teleport confirm is not supported in protocol " + protocol);
                    case PacketOutgoingType.HeldItemChange: return 0x17;
                    case PacketOutgoingType.InteractEntity: return 0x02;
                    case PacketOutgoingType.UseItem: throw new InvalidOperationException("Use item is not supported in protocol " + protocol);
                    case PacketOutgoingType.ClickWindow: return 0x0E;
                    case PacketOutgoingType.CloseWindow: return 0x0D;
                    case PacketOutgoingType.PlayerBlockPlacement: return 0x08;
                    case PacketOutgoingType.CreativeInventoryAction: return 0x10;
                    case PacketOutgoingType.Animation: return 0x0A;
                    case PacketOutgoingType.PlayerDigging: return 0x07;
                    case PacketOutgoingType.UpdateSign: return 0x12;
                    case PacketOutgoingType.UpdateCommandBlock: return 0x20;
                }
            }
            else if (protocol <= Protocol18Handler.MC1112Version) // MC 1.9, 1,10 and 1.11
            {
                switch (packet)
                {
                    case PacketOutgoingType.KeepAlive: return 0x0B;
                    case PacketOutgoingType.ResourcePackStatus: return 0x16;
                    case PacketOutgoingType.ChatMessage: return 0x02;
                    case PacketOutgoingType.ClientStatus: return 0x03;
                    case PacketOutgoingType.ClientSettings: return 0x04;
                    case PacketOutgoingType.PluginMessage: return 0x09;
                    case PacketOutgoingType.TabComplete: return 0x01;
                    case PacketOutgoingType.EntityAction: return 0x14;
                    case PacketOutgoingType.PlayerPosition: return 0x0C;
                    case PacketOutgoingType.PlayerPositionAndLook: return 0x0D;
                    case PacketOutgoingType.TeleportConfirm: return 0x00;
                    case PacketOutgoingType.HeldItemChange: return 0x17;
                    case PacketOutgoingType.InteractEntity: return 0x0A;
                    case PacketOutgoingType.UseItem: return 0x1D;
                    case PacketOutgoingType.ClickWindow: return 0x07;
                    case PacketOutgoingType.CloseWindow: return 0x08;
                    case PacketOutgoingType.PlayerBlockPlacement: return 0x1C;
                    case PacketOutgoingType.CreativeInventoryAction: return 0x18;
                    case PacketOutgoingType.Animation: return 0x1A;
                    case PacketOutgoingType.PlayerDigging: return 0x13;
                    case PacketOutgoingType.UpdateSign: return 0x19;
                    case PacketOutgoingType.UpdateCommandBlock: return 0x20;
                }
            }
            else if (protocol <= Protocol18Handler.MC112Version) // MC 1.12
            {
                switch (packet)
                {
                    case PacketOutgoingType.KeepAlive: return 0x0C;
                    case PacketOutgoingType.ResourcePackStatus: return 0x18;
                    case PacketOutgoingType.ChatMessage: return 0x03;
                    case PacketOutgoingType.ClientStatus: return 0x04;
                    case PacketOutgoingType.ClientSettings: return 0x05;
                    case PacketOutgoingType.PluginMessage: return 0x0A;
                    case PacketOutgoingType.TabComplete: return 0x02;
                    case PacketOutgoingType.EntityAction: return 0x15;
                    case PacketOutgoingType.PlayerPosition: return 0x0E;
                    case PacketOutgoingType.PlayerPositionAndLook: return 0x0F;
                    case PacketOutgoingType.TeleportConfirm: return 0x00;
                    case PacketOutgoingType.HeldItemChange: return 0x1A;
                    case PacketOutgoingType.InteractEntity: return 0x0B;
                    case PacketOutgoingType.UseItem: return 0x20;
                    case PacketOutgoingType.ClickWindow: return 0x07;
                    case PacketOutgoingType.CloseWindow: return 0x08;
                    case PacketOutgoingType.PlayerBlockPlacement: return 0x1F;
                    case PacketOutgoingType.CreativeInventoryAction: return 0x1B;
                    case PacketOutgoingType.Animation: return 0x1D;
                    case PacketOutgoingType.PlayerDigging: return 0x14;
                    case PacketOutgoingType.UpdateSign: return 0x1C;
                    case PacketOutgoingType.UpdateCommandBlock: return 0x20;
                }
            }
            else if (protocol <= Protocol18Handler.MC1122Version) // 1.12.2
            {
                switch (packet)
                {
                    case PacketOutgoingType.KeepAlive: return 0x0B;
                    case PacketOutgoingType.ResourcePackStatus: return 0x18;
                    case PacketOutgoingType.ChatMessage: return 0x02;
                    case PacketOutgoingType.ClientStatus: return 0x03;
                    case PacketOutgoingType.ClientSettings: return 0x04;
                    case PacketOutgoingType.PluginMessage: return 0x09;
                    case PacketOutgoingType.TabComplete: return 0x01;
                    case PacketOutgoingType.EntityAction: return 0x15;
                    case PacketOutgoingType.PlayerPosition: return 0x0D;
                    case PacketOutgoingType.PlayerPositionAndLook: return 0x0E;
                    case PacketOutgoingType.TeleportConfirm: return 0x00;
                    case PacketOutgoingType.HeldItemChange: return 0x1F;
                    case PacketOutgoingType.InteractEntity: return 0x0A;
                    case PacketOutgoingType.UseItem: return 0x20;
                    case PacketOutgoingType.ClickWindow: return 0x07;
                    case PacketOutgoingType.CloseWindow: return 0x08;
                    case PacketOutgoingType.PlayerBlockPlacement: return 0x1F;
                    case PacketOutgoingType.CreativeInventoryAction: return 0x1B;
                    case PacketOutgoingType.Animation: return 0x1D;
                    case PacketOutgoingType.PlayerDigging: return 0x14;
                    case PacketOutgoingType.UpdateSign: return 0x1C;
                    case PacketOutgoingType.UpdateCommandBlock: return 0x20;
                }
            }
            else if (protocol < Protocol18Handler.MC114Version) // MC 1.13 to 1.13.2
            {
                switch (packet)
                {
                    case PacketOutgoingType.KeepAlive: return 0x0E;
                    case PacketOutgoingType.ResourcePackStatus: return 0x1D;
                    case PacketOutgoingType.ChatMessage: return 0x02;
                    case PacketOutgoingType.ClientStatus: return 0x03;
                    case PacketOutgoingType.ClientSettings: return 0x04;
                    case PacketOutgoingType.PluginMessage: return 0x0A;
                    case PacketOutgoingType.TabComplete: return 0x05;
                    case PacketOutgoingType.EntityAction: return 0x19;
                    case PacketOutgoingType.PlayerPosition: return 0x10;
                    case PacketOutgoingType.PlayerPositionAndLook: return 0x11;
                    case PacketOutgoingType.TeleportConfirm: return 0x00;
                    case PacketOutgoingType.HeldItemChange: return 0x21;
                    case PacketOutgoingType.InteractEntity: return 0x0D;
                    case PacketOutgoingType.UseItem: return 0x2A;
                    case PacketOutgoingType.ClickWindow: return 0x08;
                    case PacketOutgoingType.CloseWindow: return 0x09;
                    case PacketOutgoingType.PlayerBlockPlacement: return 0x29;
                    case PacketOutgoingType.CreativeInventoryAction: return 0x24;
                    case PacketOutgoingType.Animation: return 0x27;
                    case PacketOutgoingType.PlayerDigging: return 0x18;
                    case PacketOutgoingType.UpdateSign: return 0x26;
                    case PacketOutgoingType.UpdateCommandBlock: return 0x22;
                }
            }
            else if (protocol <= Protocol18Handler.MC1152Version) //MC 1.14 to 1.15.2
            {
                switch (packet)
                {
                    case PacketOutgoingType.KeepAlive: return 0x0F;
                    case PacketOutgoingType.ResourcePackStatus: return 0x1F;
                    case PacketOutgoingType.ChatMessage: return 0x03;
                    case PacketOutgoingType.ClientStatus: return 0x04;
                    case PacketOutgoingType.ClientSettings: return 0x05;
                    case PacketOutgoingType.PluginMessage: return 0x0B;
                    case PacketOutgoingType.TabComplete: return 0x06;
                    case PacketOutgoingType.EntityAction: return 0x1B;
                    case PacketOutgoingType.PlayerPosition: return 0x11;
                    case PacketOutgoingType.PlayerPositionAndLook: return 0x12;
                    case PacketOutgoingType.TeleportConfirm: return 0x00;
                    case PacketOutgoingType.HeldItemChange: return 0x23;
                    case PacketOutgoingType.InteractEntity: return 0x0E;
                    case PacketOutgoingType.UseItem: return 0x2D;
                    case PacketOutgoingType.ClickWindow: return 0x09;
                    case PacketOutgoingType.CloseWindow: return 0x0A;
                    case PacketOutgoingType.PlayerBlockPlacement: return 0x2C;
                    case PacketOutgoingType.CreativeInventoryAction: return 0x26;
                    case PacketOutgoingType.Animation: return 0x2A;
                    case PacketOutgoingType.PlayerDigging: return 0x1A;
                    case PacketOutgoingType.UpdateSign: return 0x29;
                    case PacketOutgoingType.UpdateCommandBlock: return 0x24;
                }
            }
            else
            {
                switch (packet)
                {
                    case PacketOutgoingType.KeepAlive: return 0x10;
                    case PacketOutgoingType.ResourcePackStatus: return 0x20;
                    case PacketOutgoingType.ChatMessage: return 0x03;
                    case PacketOutgoingType.ClientStatus: return 0x04;
                    case PacketOutgoingType.ClientSettings: return 0x05;
                    case PacketOutgoingType.PluginMessage: return 0x0B;
                    case PacketOutgoingType.TabComplete: return 0x06;
                    case PacketOutgoingType.EntityAction: return 0x1C;
                    case PacketOutgoingType.PlayerPosition: return 0x12;
                    case PacketOutgoingType.PlayerPositionAndLook: return 0x13;
                    case PacketOutgoingType.TeleportConfirm: return 0x00;
                    case PacketOutgoingType.HeldItemChange: return 0x24;
                    case PacketOutgoingType.InteractEntity: return 0x0E;
                    case PacketOutgoingType.UseItem: return 0x2E;
                    case PacketOutgoingType.ClickWindow: return 0x09;
                    case PacketOutgoingType.CloseWindow: return 0x0A;
                    case PacketOutgoingType.PlayerBlockPlacement: return 0x2D;
                    case PacketOutgoingType.CreativeInventoryAction: return 0x27;
                    case PacketOutgoingType.Animation: return 0x2B;
                    case PacketOutgoingType.PlayerDigging: return 0x1B;
                    case PacketOutgoingType.UpdateSign: return 0x2A;
                    case PacketOutgoingType.UpdateCommandBlock: return 0x25;
                }
            }

            throw new System.ComponentModel.InvalidEnumArgumentException("Unknown PacketOutgoingType (protocol=" + protocol + ")", (int)packet, typeof(PacketOutgoingType));
        }
    }
}
