using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.PluginMessage
{
    internal class PluginMessageOut113Pre7 : PluginMessageOut17W45A
    {
        protected override int MinVersion => PacketUtils.MC113pre7Version;
        protected override int PacketId => 0x0A;
    }
}