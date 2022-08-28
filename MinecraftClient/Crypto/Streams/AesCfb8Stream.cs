using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace MinecraftClient.Crypto.Streams
{
    internal class AesCfb8Stream : Stream, IAesStream
    {
        public static readonly int blockSize = 16;

        private readonly Aes? Aes = null;

        private readonly AesContext? FastAes = null;

        public System.IO.Stream BaseStream { get; set; }

        private bool inStreamEnded = false;

        private byte[] ReadStreamIV = new byte[16];
        private byte[] WriteStreamIV = new byte[16];

        public AesCfb8Stream(System.IO.Stream stream, byte[] key)
        {
            BaseStream = stream;

            if (System.Runtime.Intrinsics.X86.Sse2.IsSupported && System.Runtime.Intrinsics.X86.Aes.IsSupported)
                FastAes = new AesContext(key);
            else
                Aes = GenerateAES(key);

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
            byte[] temp = new byte[1];
            Read(temp, 0, 1);
            return temp[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public override int Read(byte[] buffer, int outOffset, int required)
        {
            if (this.inStreamEnded)
                return 0;

            byte[] inputBuf = new byte[blockSize + required];
            Array.Copy(ReadStreamIV, inputBuf, blockSize);

            for (int readed = 0, curRead; readed < required; readed += curRead)
            {
                curRead = BaseStream.Read(inputBuf, blockSize + readed, required - readed);
                if (curRead == 0)
                {
                    this.inStreamEnded = true;
                    return readed;
                }

                OrderablePartitioner<Tuple<int, int>> rangePartitioner = (curRead <= 256) ?
                    Partitioner.Create(readed, readed + curRead, 32) : Partitioner.Create(readed, readed + curRead);
                Parallel.ForEach(rangePartitioner, (range, loopState) =>
                {
                    Span<byte> blockOutput = stackalloc byte[blockSize];
                    for (int idx = range.Item1; idx < range.Item2; idx++)
                    {
                        ReadOnlySpan<byte> blockInput = new(inputBuf, idx, blockSize);
                        if (FastAes != null)
                            FastAes.EncryptEcb(blockInput, blockOutput);
                        else
                            Aes!.EncryptEcb(blockInput, blockOutput, PaddingMode.None);
                        buffer[outOffset + idx] = (byte)(blockOutput[0] ^ inputBuf[idx + blockSize]);
                    }
                });
            }

            Array.Copy(inputBuf, required, ReadStreamIV, 0, blockSize);

            return required;
        }

        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void WriteByte(byte b)
        {
            Write(new byte[] { b }, 0, 1);
        }

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

        private static Aes GenerateAES(byte[] key)
        {
            Aes aes = Aes.Create();
            aes.BlockSize = 128;
            aes.KeySize = 128;
            aes.Key = key;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;
            return aes;
        }
    }
}
