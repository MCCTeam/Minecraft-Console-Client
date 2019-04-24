using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.TeleportConfirm
{
    internal class TeleportConfirmOut : OutboundGamePacket
    {
        protected override int MinVersion => PacketUtils.MC19Version;
        protected override int PacketId => 0x00;
        protected override OutboundTypes PackageType => OutboundTypes.TeleportConfirm;

        public override IEnumerable<byte> TransformData(IEnumerable<byte> packetData, IOutboundRequest data)
        {
            return packetData;
        }
    }
}