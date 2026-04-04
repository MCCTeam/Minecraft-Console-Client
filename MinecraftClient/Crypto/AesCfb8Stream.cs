using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MinecraftClient.Crypto.AesHandler;

namespace MinecraftClient.Crypto
{
    public class AesCfb8Stream : Stream
    {
        public const int blockSize = 16;

        private readonly IAesHandler aesHandler;

        private bool inStreamEnded = false;

        private readonly byte[] ReadStreamIV = new byte[16];
        private readonly byte[] WriteStreamIV = new byte[16];

        public Stream BaseStream { get; set; }

        public AesCfb8Stream(Stream stream, byte[] key)
        {
            BaseStream = stream;
            aesHandler = AesHandlerFactory.Create(key);

            Array.Copy(key, ReadStreamIV, 16);
            Array.Copy(key, WriteStreamIV, 16);
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void Flush()
        {
            BaseStream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return BaseStream.FlushAsync(cancellationToken);
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override int ReadByte()
        {
            if (inStreamEnded)
                return -1;

            int inputBuf = BaseStream.ReadByte();
            if (inputBuf == -1)
            {
                inStreamEnded = true;
                return -1;
            }

            Span<byte> blockOutput = stackalloc byte[blockSize];
            aesHandler.EncryptEcb(ReadStreamIV, blockOutput);

            // Shift left
            Array.Copy(ReadStreamIV, 1, ReadStreamIV, 0, blockSize - 1);
            ReadStreamIV[blockSize - 1] = (byte)inputBuf;

            return (byte)(blockOutput[0] ^ inputBuf);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EncryptBlock(ReadOnlySpan<byte> blockInput, Span<byte> blockOutput)
        {
            aesHandler.EncryptEcb(blockInput, blockOutput);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public override int Read(byte[] buffer, int outOffset, int required)
        {
            if (inStreamEnded)
                return 0;

            Span<byte> blockOutput = stackalloc byte[blockSize];
            byte[] inputBuf = ArrayPool<byte>.Shared.Rent(blockSize + required);

            try
            {
                Array.Copy(ReadStreamIV, inputBuf, blockSize);

                for (int readed = 0, curRead; readed < required; readed += curRead)
                {
                    curRead = BaseStream.Read(inputBuf, blockSize + readed, required - readed);
                    if (curRead == 0)
                    {
                        inStreamEnded = true;
                        Array.Copy(inputBuf, readed, ReadStreamIV, 0, blockSize);
                        return readed;
                    }

                    int processEnd = readed + curRead;
                    for (int idx = readed; idx < processEnd; idx++)
                    {
                        ReadOnlySpan<byte> blockInput = new(inputBuf, idx, blockSize);
                        EncryptBlock(blockInput, blockOutput);
                        buffer[outOffset + idx] = (byte)(blockOutput[0] ^ inputBuf[idx + blockSize]);
                    }
                }

                Array.Copy(inputBuf, required, ReadStreamIV, 0, blockSize);

                return required;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(inputBuf);
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void WriteByte(byte b)
        {
            Span<byte> blockOutput = stackalloc byte[blockSize];

            EncryptBlock(WriteStreamIV, blockOutput);

            byte outputBuf = (byte)(blockOutput[0] ^ b);

            BaseStream.WriteByte(outputBuf);

            // Shift left
            Array.Copy(WriteStreamIV, 1, WriteStreamIV, 0, blockSize - 1);
            WriteStreamIV[blockSize - 1] = outputBuf;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public override void Write(byte[] input, int offset, int required)
        {
            byte[] outputBuf = ArrayPool<byte>.Shared.Rent(blockSize + required);

            try
            {
                Array.Copy(WriteStreamIV, outputBuf, blockSize);

                Span<byte> blockOutput = stackalloc byte[blockSize];
                for (int written = 0; written < required; ++written)
                {
                    ReadOnlySpan<byte> blockInput = new(outputBuf, written, blockSize);
                    EncryptBlock(blockInput, blockOutput);
                    outputBuf[blockSize + written] = (byte)(blockOutput[0] ^ input[offset + written]);
                }

                BaseStream.Write(outputBuf, blockSize, required);
                Array.Copy(outputBuf, required, WriteStreamIV, 0, blockSize);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(outputBuf);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (inStreamEnded || buffer.Length == 0)
                return 0;

            byte[] inputBuf = ArrayPool<byte>.Shared.Rent(blockSize + buffer.Length);

            try
            {
                Array.Copy(ReadStreamIV, inputBuf, blockSize);

                for (int readed = 0; readed < buffer.Length;)
                {
                    int curRead = await BaseStream.ReadAsync(inputBuf.AsMemory(blockSize + readed, buffer.Length - readed), cancellationToken);
                    if (curRead == 0)
                    {
                        inStreamEnded = true;
                        Array.Copy(inputBuf, readed, ReadStreamIV, 0, blockSize);
                        return readed;
                    }

                    int processEnd = readed + curRead;
                    DecryptToOutputBuffer(inputBuf, buffer, readed, processEnd);
                    readed = processEnd;
                }

                Array.Copy(inputBuf, buffer.Length, ReadStreamIV, 0, blockSize);
                return buffer.Length;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(inputBuf);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (buffer.Length == 0)
                return;

            byte[] outputBuf = ArrayPool<byte>.Shared.Rent(blockSize + buffer.Length);

            try
            {
                Array.Copy(WriteStreamIV, outputBuf, blockSize);
                EncryptToOutputBuffer(buffer, outputBuf);

                await BaseStream.WriteAsync(outputBuf.AsMemory(blockSize, buffer.Length), cancellationToken);
                Array.Copy(outputBuf, buffer.Length, WriteStreamIV, 0, blockSize);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(outputBuf);
            }
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                aesHandler.Dispose();
            }

            base.Dispose(disposing);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void DecryptToOutputBuffer(byte[] inputBuf, Memory<byte> output, int start, int end)
        {
            Span<byte> blockOutput = stackalloc byte[blockSize];
            for (int idx = start; idx < end; idx++)
            {
                ReadOnlySpan<byte> blockInput = new(inputBuf, idx, blockSize);
                EncryptBlock(blockInput, blockOutput);
                output.Span[idx] = (byte)(blockOutput[0] ^ inputBuf[idx + blockSize]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void EncryptToOutputBuffer(ReadOnlyMemory<byte> input, byte[] outputBuf)
        {
            Span<byte> blockOutput = stackalloc byte[blockSize];
            for (int written = 0; written < input.Length; ++written)
            {
                ReadOnlySpan<byte> blockInput = new(outputBuf, written, blockSize);
                EncryptBlock(blockInput, blockOutput);
                outputBuf[blockSize + written] = (byte)(blockOutput[0] ^ input.Span[written]);
            }
        }
    }
}
