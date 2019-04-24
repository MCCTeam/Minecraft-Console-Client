using System.Collections.Generic;

namespace MinecraftClient.Protocol.Packets.Inbound
{
    internal interface IInboundGamePacketHandler: IGamePacketHandler
    {
        InboundTypes Type();
        IInboundData Handle(IProtocol protocol, IMinecraftComHandler handler, List<byte> packetData);
    }
}