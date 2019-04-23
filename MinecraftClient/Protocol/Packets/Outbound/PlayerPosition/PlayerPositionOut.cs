using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.PlayerPosition
{
    internal class PlayerPositionOut : OutboundGamePacket
    {
        protected override int MinVersion => 0;
        protected override int PacketId => 0x04;
        protected override OutboundTypes PackageType => OutboundTypes.PlayerPosition;

        public override IEnumerable<byte> TransformData(IEnumerable<byte> packetData, IOutboundRequest data)
        {
            return PacketUtils.concatBytes(PacketUtils.getDouble(((PlayerPositionRequest) data).Location.X),
                PacketUtils.getDouble(((PlayerPositionRequest) data).Location.Y),
                GetExtraY((PlayerPositionRequest) data),
                PacketUtils.getDouble(((PlayerPositionRequest) data).Location.Z),
                new[] {((PlayerPositionRequest) data).IsOnGround ? (byte) 1 : (byte) 0});
        }

        protected virtual byte[] GetExtraY(PlayerPositionRequest data)
        {
            return PacketUtils.getDouble(data.Location.Y + 1.62);
        }
    }
}