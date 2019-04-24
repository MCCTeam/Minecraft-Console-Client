using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.Kick
{
    internal class KickHandler : InboundGamePacketHandler
    {
        protected override int MinVersion => 0;
        protected override int PacketId => 0x40;
        protected override InboundTypes PackageType => InboundTypes.KickPacket;

        public override IInboundData Handle(IProtocol protocol, IMinecraftComHandler handler, List<byte> packetData)
        {
            handler.OnConnectionLost(ChatBot.DisconnectReason.InGameKick,
                ChatParser.ParseText(PacketUtils.readNextString(packetData)));

            return null;
        }
    }
}