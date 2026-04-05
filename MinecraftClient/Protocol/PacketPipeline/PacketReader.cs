using System;
using System.Buffers.Binary;

namespace MinecraftClient.Protocol.PacketPipeline;

public sealed class PacketReader
{
    private readonly byte[] buffer;
    private int position;

    public PacketReader(byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        this.buffer = buffer;
    }

    public int Position => position;
    public int RemainingLength => buffer.Length - position;
    public ReadOnlySpan<byte> RemainingSpan => buffer.AsSpan(position);
    public ReadOnlySpan<byte> FullSpan => buffer;

    public byte[] GetRawData() => buffer;

    public byte ReadByte()
    {
        EnsureAvailable(1);
        return buffer[position++];
    }

    public byte[] ReadData(int length)
    {
        if (length == 0)
            return [];

        EnsureAvailable(length);
        byte[] data = GC.AllocateUninitializedArray<byte>(length);
        Buffer.BlockCopy(buffer, position, data, 0, length);
        position += length;
        return data;
    }

    public void ReadData(Span<byte> destination)
    {
        if (destination.Length == 0)
            return;

        EnsureAvailable(destination.Length);
        buffer.AsSpan(position, destination.Length).CopyTo(destination);
        position += destination.Length;
    }

    public void ReadDataReverse(Span<byte> destination)
    {
        if (destination.Length == 0)
            return;

        EnsureAvailable(destination.Length);
        for (int i = destination.Length - 1; i >= 0; --i)
            destination[i] = buffer[position++];
    }

    public void Skip(int length)
    {
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length));

        EnsureAvailable(length);
        position += length;
    }

    public ushort ReadUInt16BigEndian()
    {
        EnsureAvailable(sizeof(ushort));
        ushort value = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(position, sizeof(ushort)));
        position += sizeof(ushort);
        return value;
    }

    public short ReadInt16BigEndian()
    {
        EnsureAvailable(sizeof(short));
        short value = BinaryPrimitives.ReadInt16BigEndian(buffer.AsSpan(position, sizeof(short)));
        position += sizeof(short);
        return value;
    }

    public int ReadInt32BigEndian()
    {
        EnsureAvailable(sizeof(int));
        int value = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(position, sizeof(int)));
        position += sizeof(int);
        return value;
    }

    public long ReadInt64BigEndian()
    {
        EnsureAvailable(sizeof(long));
        long value = BinaryPrimitives.ReadInt64BigEndian(buffer.AsSpan(position, sizeof(long)));
        position += sizeof(long);
        return value;
    }

    public byte[] CopyRemaining()
    {
        return ReadOnlySpanToArray(RemainingSpan);
    }

    private void EnsureAvailable(int length)
    {
        if (RemainingLength < length)
            throw new OverflowException("Reached the end of the packet.");
    }

    private static byte[] ReadOnlySpanToArray(ReadOnlySpan<byte> span)
    {
        if (span.IsEmpty)
            return [];

        byte[] copy = GC.AllocateUninitializedArray<byte>(span.Length);
        span.CopyTo(copy);
        return copy;
    }
}
