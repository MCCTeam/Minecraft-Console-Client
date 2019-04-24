using MinecraftClient.Protocol.Handlers;

namespace MinecraftClient.Protocol.Packets.Outbound.ClientStatus
{
    internal class ClientStatusOut17W45A : ClientStatusOut17W31A
    {
        protected override int MinVersion => PacketUtils.MC17w45aVersion;
        protected override int PacketId => 0x02;
    }
}