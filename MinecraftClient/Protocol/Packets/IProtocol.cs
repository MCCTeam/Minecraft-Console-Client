using System.Collections.Generic;
using MinecraftClient.Protocol.Packets.Outbound;

namespace MinecraftClient.Protocol.Packets
{
    internal interface IProtocol
    {
        bool SendPacketOut(OutboundTypes type, IEnumerable<byte> packetData, IOutboundRequest data);
        int Dimension();
    }
}