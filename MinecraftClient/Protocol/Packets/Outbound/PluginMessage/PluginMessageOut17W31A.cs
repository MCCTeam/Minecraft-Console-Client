using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.PluginMessage
{
    internal class PluginMessageOut17W31A : PluginMessageOut17W13A
    {
        protected override int MinVersion => PacketUtils.MC17w31aVersion;
        protected override int PacketId => 0x09;
    }
}