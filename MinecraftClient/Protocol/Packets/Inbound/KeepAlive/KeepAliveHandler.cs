using System.Collections.Generic;
using MinecraftClient.Protocol.Packets.Outbound;

namespace MinecraftClient.Protocol.Packets.Inbound.KeepAlive
{
    /// <summary>
    /// Implementation of the Clientbound Keep Alive Packet
    /// https://wiki.vg/Protocol#Keep_Alive_.28clientbound.29
    /// </summary>
    internal class KeepAliveHandler : InboundGamePacketHandler
    {
        protected override int MinVersion => 0;

        protected override int PacketId => 0x00;
        protected override InboundTypes PackageType => InboundTypes.KeepAlive;

        public override IInboundData Handle(IProtocol protocol, IMinecraftComHandler handler, List<byte> packetData)
        {
            protocol.SendPacketOut(OutboundTypes.KeepAlive, packetData, null);
            return null;
        }
    }
}