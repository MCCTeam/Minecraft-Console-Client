using System.Collections.Generic;
using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.PlayerPositionAndLook
{
    /// <summary>
    /// Implementation of the Clientbound Player Position And Look Packet
    /// https://wiki.vg/Protocol#Player_Position_And_Look_.28clientbound.29
    /// </summary>
    internal class PlayerPositionAndLookHandler : InboundGamePacketHandler
    {
        protected override int MinVersion => 0;
        protected override int PacketId => 0x08;
        protected override InboundTypes PackageType => InboundTypes.PlayerPositionAndLook;

        public override IInboundData Handle(IProtocol protocol, IMinecraftComHandler handler, List<byte> packetData)
        {
            var x = PacketUtils.readNextDouble(packetData);
            var y = PacketUtils.readNextDouble(packetData);
            var z = PacketUtils.readNextDouble(packetData);
            var yaw = PacketUtils.readNextFloat(packetData);
            var pitch = PacketUtils.readNextFloat(packetData);
            var locMask = PacketUtils.readNextByte(packetData);

            UpdateLocation(handler, x, y, z, yaw, pitch, locMask);
            ConfirmTeleport(protocol, packetData);
            return null;
        }

        protected virtual void UpdateLocation(IMinecraftComHandler handler, double x, double y, double z, float yaw,
            float pitch, byte locMask)
        {
            handler.UpdateLocation(new Location(x, y, z), yaw, pitch);
        }

        protected virtual void ConfirmTeleport(IProtocol protocol, List<byte> packetData)
        {
        }
    }
}