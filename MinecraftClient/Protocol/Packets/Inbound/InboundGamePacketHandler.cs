using System.Collections.Generic;

namespace MinecraftClient.Protocol.Packets.Inbound
{
    internal abstract class InboundGamePacketHandler : GamePacketHandler, IInboundGamePacketHandler
    {
        // ReSharper disable once MemberCanBeProtected.Global
        // ReSharper disable once EmptyConstructor
        // ReSharper disable once PublicConstructorInAbstractClass
        public InboundGamePacketHandler()
        {
        }
        
        protected abstract InboundTypes PackageType { get; }

        InboundTypes IInboundGamePacketHandler.Type()
        {
            return PackageType;
        }

        public abstract IInboundData Handle(IProtocol protocol, IMinecraftComHandler handler, List<byte> packetData);

        public override int PacketIntType()
        {
            return (int) PackageType;
        }
    }
}