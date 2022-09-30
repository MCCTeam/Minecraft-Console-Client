using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace MinecraftClient.Crypto
{
    public class AesCfb8Stream : Stream
    {
        public static readonly int blockSize = 16;

        private readonly Aes? Aes = null;
        private readonly FastAes? FastAes = null;

        private bool inStreamEnded = false;

        private readonly byte[] ReadStreamIV = new byte[16];
        private readonly byte[] WriteStreamIV = new byte[16];

        public Stream BaseStream { get; set; }

        public AesCfb8Stream(Stream stream, byte[] key)
        {
            BaseStream = stream;

            if (FastAes.IsSupported())
                FastAes = new FastAes(key);
            else
            {
                Aes = Aes.Create();
                Aes.BlockSize = 128;
                Aes.KeySize = 128;
                Aes.Key = key;
                Aes.Mode = CipherMode.ECB;
                Aes.Padding = PaddingMode.None;
            }

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
            if (FastAes != null)
                FastAes.EncryptEcb(ReadStreamIV, blockOutput);
            else
                Aes!.EncryptEcb(ReadStreamIV, blockOutput, PaddingMode.None);

            // Shift left
            Array.Copy(ReadStreamIV, 1, ReadStreamIV, 0, blockSize - 1);
            ReadStreamIV[blockSize - 1] = (byte)inputBuf;

            return (byte)(blockOutput[0] ^ inputBuf);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public override int Read(byte[] buffer, int outOffset, int required)
        {
            if (inStreamEnded)
                return 0;

            Span<byte> blockOutput = FastAes != null ? stackalloc byte[blockSize] : null;

            byte[] inputBuf = new byte[blockSize + required];
            Array.Copy(ReadStreamIV, inputBuf, blockSize);

            for (int readed = 0, curRead; readed < required; readed += curRead)
            {
                curRead = BaseStream.Read(inputBuf, blockSize + readed, required - readed);
                if (curRead == 0)
                {
                    inStreamEnded = true;
                    return readed;
                }

                int processEnd = readed + curRead;
                if (FastAes != null)
                {
                    for (int idx = readed; idx < processEnd; idx++)
                    {
                        ReadOnlySpan<byte> blockInput = new(inputBuf, idx, blockSize);
                        FastAes.EncryptEcb(blockInput, blockOutput);
                        buffer[outOffset + idx] = (byte)(blockOutput[0] ^ inputBuf[idx + blockSize]);
                    }
                }
                else
                {
                    OrderablePartitioner<Tuple<int, int>> rangePartitioner = curRead <= 256 ?
                        Partitioner.Create(readed, processEnd, 32) : Partitioner.Create(readed, processEnd);
                    Parallel.ForEach(rangePartitioner, (range, loopState) =>
                    {
                        Span<byte> blockOutput = stackalloc byte[blockSize];
                        for (int idx = range.Item1; idx < range.Item2; idx++)
                        {
                            ReadOnlySpan<byte> blockInput = new(inputBuf, idx, blockSize);
                            Aes!.EncryptEcb(blockInput, blockOutput, PaddingMode.None);
                            buffer[outOffset + idx] = (byte)(blockOutput[0] ^ inputBuf[idx + blockSize]);
                        }
                    });
                }
            }

            Array.Copy(inputBuf, required, ReadStreamIV, 0, blockSize);

            return required;
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

            if (FastAes != null)
                FastAes.EncryptEcb(WriteStreamIV, blockOutput);
            else
                Aes!.EncryptEcb(WriteStreamIV, blockOutput, PaddingMode.None);

            byte outputBuf = (byte)(blockOutput[0] ^ b);

            BaseStream.WriteByte(outputBuf);

            // Shift left
            Array.Copy(WriteStreamIV, 1, WriteStreamIV, 0, blockSize - 1);
            WriteStreamIV[blockSize - 1] = outputBuf;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public override void Write(byte[] input, int offset, int required)
        {
            byte[] outputBuf = new byte[blockSize + required];
            Array.Copy(WriteStreamIV, outputBuf, blockSize);

            Span<byte> blockOutput = stackalloc byte[blockSize];
            for (int wirtten = 0; wirtten < required; ++wirtten)
            {
                ReadOnlySpan<byte> blockInput = new(outputBuf, wirtten, blockSize);
                if (FastAes != null)
                    FastAes.EncryptEcb(blockInput, blockOutput);
                else
                    Aes!.EncryptEcb(blockInput, blockOutput, PaddingMode.None);
                outputBuf[blockSize + wirtten] = (byte)(blockOutput[0] ^ input[offset + wirtten]);
            }
            BaseStream.WriteAsync(outputBuf, blockSize, required);

            Array.Copy(outputBuf, required, WriteStreamIV, 0, blockSize);
        }
    }
}
