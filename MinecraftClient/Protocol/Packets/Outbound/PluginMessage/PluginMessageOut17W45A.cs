using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.PluginMessage
{
    internal class PluginMessageOut17W45A : PluginMessageOut17W31A
    {
        protected override int MinVersion => PacketUtils.MC17w45aVersion;
        protected override int PacketId => 0x08;
    }
}