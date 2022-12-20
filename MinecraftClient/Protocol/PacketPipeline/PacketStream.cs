using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static ConsoleInteractive.ConsoleReader;

namespace MinecraftClient.Protocol.PacketPipeline
{
    internal class PacketStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        readonly CancellationToken CancelToken;

        private readonly Stream baseStream;
        private readonly AesStream? aesStream;
        private ZLibStream? zlibStream;

        private int packetSize, packetReaded;

        internal const int DropBufSize = 1024;
        internal static readonly Memory<byte> DropBuf = new byte[DropBufSize];

        private static readonly byte[] SingleByteBuf = new byte[1];

        public PacketStream(ZLibStream zlibStream, int packetSize, CancellationToken cancellationToken = default)
        {
            CancelToken = cancellationToken;

            this.aesStream = null;
            this.zlibStream = zlibStream;
            this.baseStream = zlibStream;

            this.packetReaded = 0;
            this.packetSize = packetSize;
        }

        public PacketStream(AesStream aesStream, int packetSize, CancellationToken cancellationToken = default)
        {
            CancelToken = cancellationToken;

            this.aesStream = aesStream;
            this.zlibStream = null;
            this.baseStream = aesStream;

            this.packetReaded = 0;
            this.packetSize = packetSize;
        }

        public PacketStream(Stream baseStream, int packetSize, CancellationToken cancellationToken = default)
        {
            CancelToken = cancellationToken;

            this.aesStream = null;
            this.zlibStream = null;
            this.baseStream = baseStream;

            this.packetReaded = 0;
            this.packetSize = packetSize;
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public new byte ReadByte()
        {
            ++packetReaded;
            if (packetReaded > packetSize)
                throw new OverflowException("Reach the end of the packet!");
            baseStream.Read(SingleByteBuf, 0, 1);
            return SingleByteBuf[0];
        }

        public async Task<byte> ReadByteAsync()
        {
            ++packetReaded;
            if (packetReaded > packetSize)
                throw new OverflowException("Reach the end of the packet!");
            await baseStream.ReadExactlyAsync(SingleByteBuf, CancelToken);
            return SingleByteBuf[0];
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (packetReaded + buffer.Length > packetSize)
                throw new OverflowException("Reach the end of the packet!");
            int readed = baseStream.Read(buffer, offset, count);
            packetReaded += readed;
            return readed;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (packetReaded + buffer.Length > packetSize)
                throw new OverflowException("Reach the end of the packet!");
            int readed = await baseStream.ReadAsync(buffer, CancelToken);
            packetReaded += readed;
            return readed;
        }

        public new async ValueTask ReadExactlyAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (packetReaded + buffer.Length > packetSize)
                throw new OverflowException("Reach the end of the packet!");
            await baseStream.ReadExactlyAsync(buffer, CancelToken);
            packetReaded += buffer.Length;
        }

        public async Task<byte[]> ReadFullPacket()
        {
            byte[] buffer = new byte[packetSize - packetReaded];
            await ReadExactlyAsync(buffer);
            packetReaded = packetSize;
            return buffer;
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
            if (zlibStream != null)
            {
                for (int readed = 0, curRead; readed < length; readed += curRead)
                    curRead = await zlibStream.ReadAsync(DropBuf[..Math.Min(DropBufSize, length - readed)]);
            }
            else if (aesStream != null)
            {
                int skipRaw = length - AesStream.BlockSize;
                for (int readed = 0, curRead; readed < skipRaw; readed += curRead)
                    curRead = await aesStream.ReadRawAsync(DropBuf[..Math.Min(DropBufSize, skipRaw - readed)]);
                await aesStream.ReadAsync(DropBuf[..Math.Min(length, AesStream.BlockSize)]);
            }
            else
            {
                for (int readed = 0, curRead; readed < length; readed += curRead)
                    curRead = await baseStream.ReadAsync(DropBuf[..Math.Min(DropBufSize, length - readed)]);
            }
            packetReaded += length;
        }

        public override async ValueTask DisposeAsync()
        {
            if (CancelToken.IsCancellationRequested)
                return;

            if (zlibStream != null)
            {
                await zlibStream.DisposeAsync();
                zlibStream = null;
                packetReaded = packetSize;
            }
            else
            {
                if (packetSize - packetReaded > 0)
                {
                    // ConsoleIO.WriteLine("Plain readed " + packetReaded + ", last " + (packetSize - packetReaded));
                    await Skip(packetSize - packetReaded);
                }
            }
        }
    }
}
