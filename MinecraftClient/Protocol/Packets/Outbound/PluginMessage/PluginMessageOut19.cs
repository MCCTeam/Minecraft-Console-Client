using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.PluginMessage
{
    internal class PluginMessageOut19 : PluginMessageOut18
    {
        protected override int MinVersion => PacketUtils.MC19Version;
        protected override int PacketId => 0x09;
    }
}