using System.Collections.Generic;

namespace MinecraftClient.Protocol.Packets.Outbound.ClientStatus
{
    internal class ClientStatusOut : OutboundGamePacket
    {
        protected override int MinVersion => 0;
        protected override int PacketId => 0x16;
        protected override OutboundTypes PackageType => OutboundTypes.ClientStatus;

        public override IEnumerable<byte> TransformData(IEnumerable<byte> packetData, IOutboundRequest data)
        {
            return packetData;
        }
    }
}