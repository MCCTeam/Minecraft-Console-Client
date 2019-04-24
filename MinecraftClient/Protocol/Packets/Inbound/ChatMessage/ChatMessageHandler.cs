using System;
using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Inbound.ChatMessage
{
    /// <summary>
    /// Implementation of the Clientbound Chat Message Packet
    /// https://wiki.vg/Protocol#Chat_Message_.28clientbound.29
    /// </summary>
    internal class ChatMessageHandler : InboundGamePacketHandler
    {
        protected override int MinVersion => 0;
        protected override int PacketId => 0x02;
        protected override InboundTypes PackageType => InboundTypes.ChatMessage;

        public override IInboundData Handle(IProtocol protocol, IMinecraftComHandler handler, List<byte> packetData)
        {
            var res = new ChatMessageResult
            {
                Message = PacketUtils.readNextString(packetData)
            };

            try
            {
                res.MessageType = PacketUtils.readNextByte(packetData);
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            if ((res.MessageType != 1 || Settings.DisplaySystemMessages) &&
                (res.MessageType != 2 || Settings.DisplayXPBarMessages))
            {
                handler.OnTextReceived(res.Message, true);
            }

            return res;
        }
    }
}