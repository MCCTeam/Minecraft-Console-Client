using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.ClientSettings
{
    internal class ClientSettingsOut17W31A : ClientSettingsOut17W13A
    {
        protected override int MinVersion => PacketUtils.MC17w31aVersion;
        protected override int PacketId => 0x04;
    }
}