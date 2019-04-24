using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.Respawn
{
    internal class RespawnHandler114Pre5 : RespawnHandler113Pre7
    {
        protected override int MinVersion => PacketUtils.MC114pre5Version;
        protected override int PacketId => 0x3A;

        protected override byte ReadDifficulty(List<byte> packetData)
        {
            return 0;
        }
    }
}