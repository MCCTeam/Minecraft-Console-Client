using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.PluginMessage
{
    internal class PluginMessageOut17W13A : PluginMessageOut19
    {
        protected override int MinVersion => PacketUtils.MC17w13aVersion;
        protected override int PacketId => 0x0A;
    }
}