using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.ChatMessage
{
    internal class ChatMessageOut : OutboundGamePacket
    {
        protected override int MinVersion => 0;
        protected override int PacketId => 0x01;
        protected override OutboundTypes PackageType => OutboundTypes.ChatMessage;

        public override IEnumerable<byte> TransformData(IEnumerable<byte> packetData, IOutboundRequest data)
        {
            return PacketUtils.getString(((ChatMessageRequest) data).Message);
        }
    }
}