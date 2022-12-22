using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MinecraftClient.Crypto;
using static ConsoleInteractive.ConsoleReader;

namespace MinecraftClient.Protocol.PacketPipeline
{
    internal class ZlibBaseStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public int BufferSize { get; set; } = 16;
        public int packetSize = 0, packetReaded = 0;

        private Stream baseStream;
        private AesStream? aesStream;

        public ZlibBaseStream(Stream baseStream, int packetSize)
        {
            packetReaded = 0;
            this.packetSize = packetSize;
            this.baseStream = baseStream;
            aesStream = null;
        }

        public ZlibBaseStream(AesStream aesStream, int packetSize)
        {
            packetReaded = 0;
            this.packetSize = packetSize;
            baseStream = this.aesStream = aesStream;
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (packetReaded == packetSize)
                return 0;
            int readed = baseStream.Read(buffer, offset, Math.Min(BufferSize, Math.Min(count, packetSize - packetReaded)));
            packetReaded += readed;
            return readed;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int readLen = Math.Min(BufferSize, Math.Min(buffer.Length, packetSize - packetReaded));
            if (packetReaded + readLen > packetSize)
                throw new OverflowException("Reach the end of the packet!");
            await baseStream.ReadExactlyAsync(buffer[..readLen], cancellationToken);
            packetReaded += readLen;
            return readLen;
        }

        public new async ValueTask ReadExactlyAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (packetReaded + buffer.Length > packetSize)
                throw new OverflowException("Reach the end of the packet!");
            await baseStream.ReadExactlyAsync(buffer, cancellationToken);
            packetReaded += buffer.Length;
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

        public async Task Skip(int length)
        {
            if (aesStream != null)
            {
                int skipRaw = length - AesStream.BlockSize;
                for (int readed = 0, curRead; readed < skipRaw; readed += curRead)
                    curRead = await aesStream.ReadRawAsync(PacketStream.DropBuf[..Math.Min(PacketStream.DropBufSize, skipRaw - readed)]);
                await aesStream.ReadAsync(PacketStream.DropBuf[..Math.Min(length, AesStream.BlockSize)]);
            }
            else
            {
                for (int readed = 0, curRead; readed < length; readed += curRead)
                    curRead = await baseStream.ReadAsync(PacketStream.DropBuf[..Math.Min(PacketStream.DropBufSize, length - readed)]);
            }
            packetReaded += length;
        }

        public override async ValueTask DisposeAsync()
        {
            if (packetSize - packetReaded > 0)
            {
                // ConsoleIO.WriteLine("Zlib  readed " + packetReaded + ", last " + (packetSize - packetReaded));
                await Skip(packetSize - packetReaded);
            }
        }
    }
}
