using System;
using System.Collections.Generic;
using System.Linq;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.PluginMessage
{
    internal class PluginMessageOut : OutboundGamePacket
    {
        protected override int MinVersion => 0;
        protected override int PacketId => 0x17;
        protected override OutboundTypes PackageType => OutboundTypes.PluginMessage;

        public override IEnumerable<byte> TransformData(IEnumerable<byte> packetData, IOutboundRequest data)
        {
            var dataA = packetData.ToArray();
            byte[] length = BitConverter.GetBytes((short) dataA.Length);
            Array.Reverse(length);

            return PacketUtils.concatBytes(PacketUtils.getString(((PluginMessageRequest) data).Channel), length, dataA);
        }
    }

    internal class PluginMessageOut18 : PluginMessageOut
    {
        protected override int MinVersion => PacketUtils.MC18Version;

        public override IEnumerable<byte> TransformData(IEnumerable<byte> packetData, IOutboundRequest data)
        {
            return PacketUtils.concatBytes(PacketUtils.getString(((PluginMessageRequest) data).Channel),
                packetData.ToArray());
        }
    }
}