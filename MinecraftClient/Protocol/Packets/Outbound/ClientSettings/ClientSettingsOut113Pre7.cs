using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.ClientSettings
{
    internal class ClientSettingsOut113Pre7 : ClientSettingsOut17W45A
    {
        protected override int MinVersion => PacketUtils.MC113pre7Version;
        protected override int PacketId => 0x04;
    }
}