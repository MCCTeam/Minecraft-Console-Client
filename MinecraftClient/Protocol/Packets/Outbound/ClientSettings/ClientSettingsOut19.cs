using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.ClientSettings
{
    internal class ClientSettingsOut19 : ClientSettingsOut18
    {
        protected override int MinVersion => PacketUtils.MC19Version;
        protected override int PacketId => 0x04;

        public override IEnumerable<byte> TransformData(IEnumerable<byte> packetData, IOutboundRequest data)
        {
            List<byte> fields = new List<byte>();
            fields.AddRange(PacketUtils.getString(((ClientSettingsRequest) data).Language));
            fields.Add(((ClientSettingsRequest) data).ViewDistance);
            fields.AddRange(PacketUtils.getVarInt(((ClientSettingsRequest) data).ChatMode));
            fields.Add(((ClientSettingsRequest) data).ChatColors ? (byte) 1 : (byte) 0);
            fields.Add(((ClientSettingsRequest) data).SkinParts);
            fields.AddRange(PacketUtils.getVarInt(((ClientSettingsRequest) data).MainHand));

            return fields;
        }
    }
}