using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MinecraftClient.Protocol.PacketPipeline;

internal sealed class PacketReadStream : Stream
{
    private const int DrainBufferSize = 4096;

    private readonly Stream baseStream;
    private readonly byte[] singleByteBuffer = new byte[1];
    private int remainingLength;

    public PacketReadStream(Stream baseStream, int packetLength)
    {
        ArgumentNullException.ThrowIfNull(baseStream);
        if (packetLength < 0)
            throw new ArgumentOutOfRangeException(nameof(packetLength));

        this.baseStream = baseStream;
        remainingLength = packetLength;
    }

    public int RemainingLength => remainingLength;

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (remainingLength == 0)
            return 0;

        int readLength = Math.Min(count, remainingLength);
        int read = baseStream.Read(buffer, offset, readLength);
        remainingLength -= read;
        return read;
    }

    public override int Read(Span<byte> buffer)
    {
        if (remainingLength == 0)
            return 0;

        int readLength = Math.Min(buffer.Length, remainingLength);
        int read = baseStream.Read(buffer[..readLength]);
        remainingLength -= read;
        return read;
    }

    public override int ReadByte()
    {
        if (remainingLength == 0)
            return -1;

        int value = baseStream.ReadByte();
        if (value == -1)
            return -1;

        remainingLength--;
        return value;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (remainingLength == 0)
            return 0;

        int readLength = Math.Min(buffer.Length, remainingLength);
        int read = await baseStream.ReadAsync(buffer[..readLength], cancellationToken);
        remainingLength -= read;
        return read;
    }

    public new async ValueTask ReadExactlyAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (buffer.Length > remainingLength)
            throw new OverflowException("Reached the end of the packet.");

        await baseStream.ReadExactlyAsync(buffer, cancellationToken);
        remainingLength -= buffer.Length;
    }

    public new void ReadExactly(Span<byte> buffer)
    {
        if (buffer.Length > remainingLength)
            throw new OverflowException("Reached the end of the packet.");

        baseStream.ReadExactly(buffer);
        remainingLength -= buffer.Length;
    }

    public byte[] ReadRemaining()
    {
        if (remainingLength == 0)
            return [];

        byte[] buffer = GC.AllocateUninitializedArray<byte>(remainingLength);
        ReadExactly(buffer);
        return buffer;
    }

    public async Task<byte[]> ReadRemainingAsync(CancellationToken cancellationToken = default)
    {
        if (remainingLength == 0)
            return [];

        byte[] buffer = GC.AllocateUninitializedArray<byte>(remainingLength);
        await ReadExactlyAsync(buffer, cancellationToken);
        return buffer;
    }

    public void DrainRemaining()
    {
        if (remainingLength == 0)
            return;

        byte[] buffer = GC.AllocateUninitializedArray<byte>(Math.Min(DrainBufferSize, remainingLength));
        while (remainingLength > 0)
        {
            int read = baseStream.Read(buffer, 0, Math.Min(buffer.Length, remainingLength));
            if (read <= 0)
                throw new EndOfStreamException("Connection closed while draining packet data.");

            remainingLength -= read;
        }
    }

    public async ValueTask DrainRemainingAsync(CancellationToken cancellationToken = default)
    {
        if (remainingLength == 0)
            return;

        byte[] buffer = GC.AllocateUninitializedArray<byte>(Math.Min(DrainBufferSize, remainingLength));
        while (remainingLength > 0)
        {
            int read = await baseStream.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, remainingLength)), cancellationToken);
            if (read <= 0)
                throw new EndOfStreamException("Connection closed while draining packet data.");

            remainingLength -= read;
        }
    }

    public override void Flush()
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && remainingLength > 0)
        {
            try
            {
                DrainRemaining();
            }
            catch (IOException) { }
            catch (ObjectDisposedException) { }
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (remainingLength > 0)
        {
            try
            {
                await DrainRemainingAsync();
            }
            catch (IOException) { }
            catch (ObjectDisposedException) { }
        }

        await base.DisposeAsync();
    }
}
