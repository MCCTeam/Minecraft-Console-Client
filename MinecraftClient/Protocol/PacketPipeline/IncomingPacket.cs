using System;

namespace MinecraftClient.Protocol.PacketPipeline;

internal readonly record struct IncomingPacket(int PacketId, byte[] Payload)
{
    public PacketReader CreateReader()
    {
        return new PacketReader(Payload);
    }

    public ReadOnlySpan<byte> PayloadSpan => Payload;
}
