using System.Collections.Generic;
using MinecraftClient.Commands;

namespace MinecraftClient.Protocol.Packets.Outbound
{
    internal interface IOutboundGamePacket : IGamePacketHandler
    {
        OutboundTypes Type();
        IEnumerable<byte> TransformData(IEnumerable<byte> packetData, IOutboundRequest data);
    }
}