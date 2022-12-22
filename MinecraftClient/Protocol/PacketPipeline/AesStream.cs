using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MinecraftClient.Crypto;
using MinecraftClient.Crypto.AesHandler;
using static ConsoleInteractive.ConsoleReader;

namespace MinecraftClient.Protocol.PacketPipeline
{
    public class AesStream : Stream
    {
        public const int BlockSize = 16;
        private const int BufferSize = 1024;

        public Socket Client;
        public bool HwAccelerateEnable { init; get; }
        private bool inStreamEnded = false;

        private readonly IAesHandler Aes;

        private int InputBufPos = 0, OutputBufPos = 0;
        private readonly Memory<byte> InputBuf, OutputBuf;
        private readonly Memory<byte> AesBufRead, AesBufSend;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public AesStream(Socket socket, byte[] key)
        {
            Client = socket;

            InputBuf = new byte[BufferSize + BlockSize];
            OutputBuf = new byte[BufferSize + BlockSize];

            AesBufRead = new byte[BlockSize];
            AesBufSend = new byte[BlockSize];

            if (FasterAesX86.IsSupported())
            {
                HwAccelerateEnable = true;
                Aes = new FasterAesX86(key);
            }
            else if (false && FasterAesArm.IsSupported()) // Further testing required
            {
                HwAccelerateEnable = true;
                Aes = new FasterAesArm(key);
            }
            else
            {
                HwAccelerateEnable = false;
                Aes = new BasicAes(key);
            }

            key.CopyTo(InputBuf.Slice(0, BlockSize));
            key.CopyTo(OutputBuf.Slice(0, BlockSize));
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var task = ReadAsync(buffer.AsMemory(offset, count)).AsTask();
            task.Wait();
            return task.Result;
        }

        public override int ReadByte()
        {
            if (inStreamEnded)
                return -1;

            var task = Client.ReceiveAsync(InputBuf.Slice(InputBufPos + BlockSize, 1)).AsTask();
            task.Wait();
            if (task.Result == 0)
            {
                inStreamEnded = true;
                return -1;
            }

            Aes.EncryptEcb(InputBuf.Slice(InputBufPos, BlockSize).Span, AesBufRead.Span);
            byte result = (byte)(AesBufRead.Span[0] ^ InputBuf.Span[InputBufPos + BlockSize]);

            InputBufPos++;
            if (InputBufPos == BufferSize)
            {
                InputBuf.Slice(BufferSize, BlockSize).CopyTo(InputBuf[..BlockSize]);
                InputBufPos = 0;
            }

            return result;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (inStreamEnded)
                return 0;

            int readLimit = Math.Min(buffer.Length, BufferSize - InputBufPos);
            int curRead = await Client.ReceiveAsync(InputBuf.Slice(InputBufPos + BlockSize, readLimit), cancellationToken);

            if (curRead == 0 || cancellationToken.IsCancellationRequested)
            {
                if (curRead == 0)
                    inStreamEnded = true;
                return curRead;
            }

            for (int idx = 0; idx < curRead; idx++)
            {
                Aes.EncryptEcb(InputBuf.Slice(InputBufPos + idx, BlockSize).Span, AesBufRead.Span);
                buffer.Span[idx] = (byte)(AesBufRead.Span[0] ^ InputBuf.Span[InputBufPos + BlockSize + idx]);
            }

            InputBufPos += curRead;
            if (InputBufPos == BufferSize)
            {
                InputBuf.Slice(BufferSize, BlockSize).CopyTo(InputBuf[..BlockSize]);
                InputBufPos = 0;
            }

            return curRead;
        }

        public new async ValueTask ReadExactlyAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (inStreamEnded)
                return;

            for (int readed = 0, curRead; readed < buffer.Length; readed += curRead)
            {
                int readLimit = Math.Min(buffer.Length - readed, BufferSize - InputBufPos);
                curRead = await Client.ReceiveAsync(InputBuf.Slice(InputBufPos + BlockSize, readLimit), cancellationToken);

                if (curRead == 0 || cancellationToken.IsCancellationRequested)
                {
                    if (curRead == 0)
                        inStreamEnded = true;
                    return;
                }

                for (int idx = 0; idx < curRead; idx++)
                {
                    Aes.EncryptEcb(InputBuf.Slice(InputBufPos + idx, BlockSize).Span, AesBufRead.Span);
                    buffer.Span[readed + idx] = (byte)(AesBufRead.Span[0] ^ InputBuf.Span[InputBufPos + BlockSize + idx]);
                }

                InputBufPos += curRead;
                if (InputBufPos == BufferSize)
                {
                    InputBuf.Slice(BufferSize, BlockSize).CopyTo(InputBuf.Slice(0, BlockSize));
                    InputBufPos = 0;
                }
            }
        }

        public async ValueTask<int> ReadRawAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return await Client.ReceiveAsync(buffer, cancellationToken);
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
            WriteAsync(buffer.AsMemory(offset, count)).AsTask().Wait();
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int outputStartPos = OutputBufPos;
            for (int wirtten = 0; wirtten < buffer.Length; ++wirtten)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                Aes.EncryptEcb(OutputBuf.Slice(OutputBufPos, BlockSize).Span, AesBufSend.Span);
                OutputBuf.Span[OutputBufPos + BlockSize] = (byte)(AesBufSend.Span[0] ^ buffer.Span[wirtten]);

                if (++OutputBufPos == BufferSize)
                {
                    await Client.SendAsync(OutputBuf.Slice(outputStartPos + BlockSize, BufferSize - outputStartPos), cancellationToken);
                    OutputBuf.Slice(BufferSize, BlockSize).CopyTo(OutputBuf.Slice(0, BlockSize));
                    OutputBufPos = outputStartPos = 0;
                }
            }

            if (OutputBufPos > outputStartPos)
                await Client.SendAsync(OutputBuf.Slice(outputStartPos + BlockSize, OutputBufPos - outputStartPos), cancellationToken);

            return;
        }
    }
}
