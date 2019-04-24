using System.Collections.Generic;

namespace MinecraftClient.Protocol.Packets.Outbound
{
    internal abstract class OutboundGamePacket : GamePacketHandler, IOutboundGamePacket
    {
        // ReSharper disable once MemberCanBeProtected.Global
        // ReSharper disable once EmptyConstructor
        // ReSharper disable once PublicConstructorInAbstractClass
        public OutboundGamePacket()
        {
        }

        protected abstract OutboundTypes PackageType { get; }

        public OutboundTypes Type()
        {
            return PackageType;
        }

        public abstract IEnumerable<byte> TransformData(IEnumerable<byte> packetData, IOutboundRequest data);

        public override int PacketIntType()
        {
            return (int) PackageType;
        }
    }
}