using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.ClientStatus
{
    internal class ClientStatusOut17W13A : ClientStatusOut19
    {
        protected override int MinVersion => PacketUtils.MC17w13aVersion;
        protected override int PacketId => 0x04;
    }
}