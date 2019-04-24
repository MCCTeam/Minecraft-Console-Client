using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.ClientSettings
{
    internal class ClientSettingsOut : OutboundGamePacket
    {
        protected override int MinVersion => 0;
        protected override int PacketId => 0x15;
        protected override OutboundTypes PackageType => OutboundTypes.ClientSettings;

        public override IEnumerable<byte> TransformData(IEnumerable<byte> packetData, IOutboundRequest data)
        {
            List<byte> fields = new List<byte>();
            fields.AddRange(PacketUtils.getString(((ClientSettingsRequest) data).Language));
            fields.Add(((ClientSettingsRequest) data).ViewDistance);
            fields.AddRange(new[] {((ClientSettingsRequest) data).ChatMode});
            fields.Add(((ClientSettingsRequest) data).ChatColors ? (byte) 1 : (byte) 0);
            fields.Add(((ClientSettingsRequest) data).Difficulty);
            fields.Add((byte) (((ClientSettingsRequest) data).SkinParts & 0x1)); //show cape

            return fields;
        }
    }
}