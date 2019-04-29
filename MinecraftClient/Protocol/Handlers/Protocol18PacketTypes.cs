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
                    //UnloadChunk does not exists prior to 1.9
                    case 0x38: return PacketIncomingType.PlayerListUpdate;
                    case 0x3A: return PacketIncomingType.TabCompleteResult;
                    case 0x3F: return PacketIncomingType.PluginMessage;
                    case 0x40: return PacketIncomingType.KickPacket;
                    case 0x46: return PacketIncomingType.NetworkCompressionTreshold;
                    case 0x48: return PacketIncomingType.ResourcePackSend;
                    default: return PacketIncomingType.UnknownPacket;
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
                    //MapChunkBulk removed in 1.9
                    case 0x1D: return PacketIncomingType.UnloadChunk;
                    case 0x2D: return PacketIncomingType.PlayerListUpdate;
                    case 0x0E: return PacketIncomingType.TabCompleteResult;
                    case 0x18: return PacketIncomingType.PluginMessage;
                    case 0x1A: return PacketIncomingType.KickPacket;
                    //NetworkCompressionTreshold removed in 1.9
                    case 0x32: return PacketIncomingType.ResourcePackSend;
                    default: return PacketIncomingType.UnknownPacket;
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
                    case 0x1D: return PacketIncomingType.UnloadChunk;
                    case 0x2D: return PacketIncomingType.PlayerListUpdate;
                    case 0x0E: return PacketIncomingType.TabCompleteResult;
                    case 0x18: return PacketIncomingType.PluginMessage;
                    case 0x1A: return PacketIncomingType.KickPacket;
                    case 0x33: return PacketIncomingType.ResourcePackSend;
                    default: return PacketIncomingType.UnknownPacket;
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
                    case 0x1D: return PacketIncomingType.UnloadChunk;
                    case 0x2E: return PacketIncomingType.PlayerListUpdate;
                    case 0x0E: return PacketIncomingType.TabCompleteResult;
                    case 0x18: return PacketIncomingType.PluginMessage;
                    case 0x1A: return PacketIncomingType.KickPacket;
                    case 0x34: return PacketIncomingType.ResourcePackSend;
                    default: return PacketIncomingType.UnknownPacket;
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
                    case 0x1F: return PacketIncomingType.UnloadChunk;
                    case 0x30: return PacketIncomingType.PlayerListUpdate;
                    case 0x10: return PacketIncomingType.TabCompleteResult;
                    case 0x19: return PacketIncomingType.PluginMessage;
                    case 0x1B: return PacketIncomingType.KickPacket;
                    case 0x37: return PacketIncomingType.ResourcePackSend;
                    default: return PacketIncomingType.UnknownPacket;
                }
            }
            else // MC 1.14
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
                    case 0x1D: return PacketIncomingType.UnloadChunk;
                    case 0x33: return PacketIncomingType.PlayerListUpdate;
                    case 0x10: return PacketIncomingType.TabCompleteResult;
                    case 0x18: return PacketIncomingType.PluginMessage;
                    case 0x1A: return PacketIncomingType.KickPacket;
                    case 0x39: return PacketIncomingType.ResourcePackSend;
                    default: return PacketIncomingType.UnknownPacket;
                }
            }
        }

        /// <summary>
        /// Get packet ID of the specified outgoing packet
        /// </summary>
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
                    case PacketOutgoingType.PlayerPosition: return 0x04;
                    case PacketOutgoingType.PlayerPositionAndLook: return 0x06;
                    case PacketOutgoingType.TeleportConfirm: throw new InvalidOperationException("Teleport confirm is not supported in protocol " + protocol);
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
                    case PacketOutgoingType.PlayerPosition: return 0x0C;
                    case PacketOutgoingType.PlayerPositionAndLook: return 0x0D;
                    case PacketOutgoingType.TeleportConfirm: return 0x00;
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
                    case PacketOutgoingType.PlayerPosition: return 0x0E;
                    case PacketOutgoingType.PlayerPositionAndLook: return 0x0F;
                    case PacketOutgoingType.TeleportConfirm: return 0x00;
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
                    case PacketOutgoingType.PlayerPosition: return 0x0D;
                    case PacketOutgoingType.PlayerPositionAndLook: return 0x0E;
                    case PacketOutgoingType.TeleportConfirm: return 0x00;
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
                    case PacketOutgoingType.PlayerPosition: return 0x10;
                    case PacketOutgoingType.PlayerPositionAndLook: return 0x11;
                    case PacketOutgoingType.TeleportConfirm: return 0x00;
                }
            }
            else // MC 1.14
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
                    case PacketOutgoingType.PlayerPosition: return 0x11;
                    case PacketOutgoingType.PlayerPositionAndLook: return 0x12;
                    case PacketOutgoingType.TeleportConfirm: return 0x00;
                }
            }

            throw new System.ComponentModel.InvalidEnumArgumentException("Unknown PacketOutgoingType (protocol=" + protocol + ")", (int)packet, typeof(PacketOutgoingType));
        }
    }
}
