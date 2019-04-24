using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.ClientSettings
{
    internal class ClientSettingsOut114Pre5 : ClientSettingsOut113Pre7
    {
        protected override int MinVersion => PacketUtils.MC114pre5Version;
        protected override int PacketId => 0x05;
    }
}