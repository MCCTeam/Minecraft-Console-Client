using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.PlayerPositionAndLook
{
    internal class PlayerPositionAndLookOut : OutboundGamePacket
    {
        protected override int MinVersion => 0;
        protected override int PacketId => 0x06;
        protected override OutboundTypes PackageType => OutboundTypes.PlayerPositionAndLook;

        public override IEnumerable<byte> TransformData(IEnumerable<byte> packetData, IOutboundRequest data)
        {
            return PacketUtils.concatBytes(PacketUtils.getDouble(((PlayerPositionAndLookRequest) data).Location.X),
                PacketUtils.getDouble(((PlayerPositionAndLookRequest) data).Location.Y),
                GetExtraY((PlayerPositionAndLookRequest) data),
                PacketUtils.getDouble(((PlayerPositionAndLookRequest) data).Location.Z),
                GetYawPitch((PlayerPositionAndLookRequest) data),
                new[] {((PlayerPositionAndLookRequest) data).IsOnGround ? (byte) 1 : (byte) 0});
        }

        protected virtual byte[] GetYawPitch(PlayerPositionAndLookRequest data)
        {
            return PacketUtils.concatBytes(PacketUtils.getFloat(data.Yaw),
                PacketUtils.getFloat(data.Pitch));
        }

        protected virtual byte[] GetExtraY(PlayerPositionAndLookRequest data)
        {
            return PacketUtils.getDouble(data.Location.Y + 1.62);
        }
    }
}