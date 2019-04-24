using System.Collections.Generic;

namespace MinecraftClient.Protocol.Packets.Outbound.KeepAlive
{
    internal class KeepAliveOut : OutboundGamePacket
    {
        protected override int MinVersion => 0;
        protected override int PacketId => 0x00;
        protected override OutboundTypes PackageType => OutboundTypes.KeepAlive;
        public override IEnumerable<byte> TransformData(IEnumerable<byte> packetData, IOutboundRequest data)
        {
            return packetData;
        }
    }
}