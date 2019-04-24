using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers;
using MinecraftClient.Protocol.Packets.Outbound;

namespace MinecraftClient.Protocol.Packets.Inbound.PlayerPositionAndLook
{
    internal class PlayerPositionAndLookHandler19 : PlayerPositionAndLookHandler18
    {
        protected override int MinVersion => PacketUtils.MC19Version;
        protected override int PacketId => 0x2E;

        protected override void ConfirmTeleport(IProtocol protocol, List<byte> packetData)
        {
            var teleportId = PacketUtils.readNextVarInt(packetData);
            protocol.SendPacketOut(OutboundTypes.TeleportConfirm, PacketUtils.getVarInt(teleportId), null);
        }
    }
}