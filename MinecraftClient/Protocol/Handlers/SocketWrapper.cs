using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MinecraftClient.Crypto;
using MinecraftClient.Protocol.PacketPipeline;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Wrapper for handling unencrypted & encrypted socket
    /// </summary>
    public class SocketWrapper
    {
        private readonly TcpClient client;
        private readonly Stream networkStream;
        private readonly SemaphoreSlim sendSemaphore = new(1, 1);
        private readonly byte[] singleByteBuffer = new byte[1];
        private AesCfb8Stream? encryptedStream;
        private Stream readStream;
        private Stream writeStream;
        private bool encrypted = false;

        /// <summary>
        /// Initialize a new SocketWrapper
        /// </summary>
        /// <param name="client">TcpClient connected to the server</param>
        public SocketWrapper(TcpClient client)
        {
            this.client = client;
            networkStream = client.GetStream();
            readStream = writeStream = networkStream;
        }

        /// <summary>
        /// Check if the socket is still connected
        /// </summary>
        /// <returns>TRUE if still connected</returns>
        /// <remarks>Silently dropped connection can only be detected by attempting to read/write data</remarks>
        public bool IsConnected()
        {
            return client.Client is not null && client.Connected;
        }

        /// <summary>
        /// Check if the socket has data available to read
        /// </summary>
        /// <returns>TRUE if data is available to read</returns>
        public bool HasDataAvailable()
        {
            return client.Client.Available > 0;
        }

        /// <summary>
        /// Switch network reading/writing to an encrypted stream
        /// </summary>
        /// <param name="secretKey">AES secret key</param>
        public void SwitchToEncrypted(byte[] secretKey)
        {
            if (encrypted)
                throw new InvalidOperationException("Stream is already encrypted!?");
            encryptedStream = new AesCfb8Stream(networkStream, secretKey);
            readStream = writeStream = encryptedStream;
            encrypted = true;
        }

        public byte ReadByteRAW()
        {
            readStream.ReadExactly(singleByteBuffer);
            return singleByteBuffer[0];
        }

        public async ValueTask<byte> ReadByteRAWAsync(CancellationToken cancellationToken)
        {
            await readStream.ReadExactlyAsync(singleByteBuffer.AsMemory(0, 1), cancellationToken);
            return singleByteBuffer[0];
        }

        /// <summary>
        /// Read some data from the server.
        /// </summary>
        /// <param name="length">Amount of bytes to read</param>
        /// <returns>The data read from the network as an array</returns>
        public byte[] ReadDataRAW(int length)
        {
            if (length > 0)
            {
                byte[] cache = GC.AllocateUninitializedArray<byte>(length);
                readStream.ReadExactly(cache);
                return cache;
            }
            return Array.Empty<byte>();
        }

        public async Task<byte[]> ReadDataRAWAsync(int length, CancellationToken cancellationToken)
        {
            if (length > 0)
            {
                byte[] cache = GC.AllocateUninitializedArray<byte>(length);
                await readStream.ReadExactlyAsync(cache.AsMemory(0, length), cancellationToken);
                return cache;
            }

            return Array.Empty<byte>();
        }

        internal IncomingPacket GetNextPacket(int compressionThreshold, DataTypes dataTypes)
        {
            int packetLength = ReadNextVarIntRaw();
            using PacketReadStream packetStream = new(readStream, packetLength);
            byte[] payload = ReadPacketPayload(packetStream, compressionThreshold);
            var packetData = new PacketReader(payload);
            int packetId = dataTypes.ReadNextVarInt(packetData);
            return new(packetId, packetData.CopyRemaining());
        }

        internal async Task<IncomingPacket> GetNextPacketAsync(int compressionThreshold, DataTypes dataTypes, CancellationToken cancellationToken)
        {
            int packetLength = await ReadNextVarIntRawAsync(cancellationToken);
            await using PacketReadStream packetStream = new(readStream, packetLength);
            byte[] payload = await ReadPacketPayloadAsync(packetStream, compressionThreshold, cancellationToken);
            var packetData = new PacketReader(payload);
            int packetId = dataTypes.ReadNextVarInt(packetData);
            return new(packetId, packetData.CopyRemaining());
        }

        /// <summary>
        /// Send raw data to the server.
        /// </summary>
        /// <param name="buffer">data to send</param>
        public void SendDataRAW(byte[] buffer)
        {
            if (!IsConnected())
                throw new SocketException((int)SocketError.NotConnected);

            sendSemaphore.Wait();
            try
            {
                writeStream.Write(buffer, 0, buffer.Length);
                writeStream.Flush();
            }
            finally
            {
                sendSemaphore.Release();
            }
        }

        public async Task SendDataRAWAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            if (!IsConnected())
                throw new SocketException((int)SocketError.NotConnected);

            await sendSemaphore.WaitAsync(cancellationToken);
            try
            {
                await writeStream.WriteAsync(buffer, cancellationToken);
                await writeStream.FlushAsync(cancellationToken);
            }
            finally
            {
                sendSemaphore.Release();
            }
        }

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        public void Disconnect()
        {
            try
            {
                encryptedStream?.Dispose();
                client.Close();
            }
            catch (SocketException) { }
            catch (System.IO.IOException) { }
            catch (NullReferenceException) { }
            catch (ObjectDisposedException) { }
        }

        private int ReadNextVarIntRaw()
        {
            int value = 0;
            int position = 0;

            while (true)
            {
                byte current = ReadByteRAW();
                value |= (current & 0x7F) << position++ * 7;
                if (position > 5)
                    throw new OverflowException("VarInt too big");
                if ((current & 0x80) != 0x80)
                    return value;
            }
        }

        private async Task<int> ReadNextVarIntRawAsync(CancellationToken cancellationToken)
        {
            int value = 0;
            int position = 0;

            while (true)
            {
                byte current = await ReadByteRAWAsync(cancellationToken);
                value |= (current & 0x7F) << position++ * 7;
                if (position > 5)
                    throw new OverflowException("VarInt too big");
                if ((current & 0x80) != 0x80)
                    return value;
            }
        }

        private static byte[] ReadPacketPayload(PacketReadStream packetStream, int compressionThreshold)
        {
            if (compressionThreshold >= 0)
            {
                int uncompressedLength = ReadNextVarIntRaw(packetStream);
                if (uncompressedLength > 0)
                {
                    using ZLibStream zlibStream = new(packetStream, CompressionMode.Decompress, leaveOpen: true);
                    byte[] payload = GC.AllocateUninitializedArray<byte>(uncompressedLength);
                    zlibStream.ReadExactly(payload);
                    return payload;
                }
            }

            return packetStream.ReadRemaining();
        }

        private static async Task<byte[]> ReadPacketPayloadAsync(PacketReadStream packetStream, int compressionThreshold, CancellationToken cancellationToken)
        {
            if (compressionThreshold >= 0)
            {
                int uncompressedLength = await ReadNextVarIntRawAsync(packetStream, cancellationToken);
                if (uncompressedLength > 0)
                {
                    await using ZLibStream zlibStream = new(packetStream, CompressionMode.Decompress, leaveOpen: true);
                    byte[] payload = GC.AllocateUninitializedArray<byte>(uncompressedLength);
                    await zlibStream.ReadExactlyAsync(payload.AsMemory(0, uncompressedLength), cancellationToken);
                    return payload;
                }
            }

            return await packetStream.ReadRemainingAsync(cancellationToken);
        }

        private static int ReadNextVarIntRaw(Stream stream)
        {
            int value = 0;
            int position = 0;

            while (true)
            {
                int current = stream.ReadByte();
                if (current < 0)
                    throw new IOException("Connection closed.");

                value |= (current & 0x7F) << position++ * 7;
                if (position > 5)
                    throw new OverflowException("VarInt too big");
                if ((current & 0x80) != 0x80)
                    return value;
            }
        }

        private static async Task<int> ReadNextVarIntRawAsync(Stream stream, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[1];
            int value = 0;
            int position = 0;

            while (true)
            {
                await stream.ReadExactlyAsync(buffer.AsMemory(0, 1), cancellationToken);
                byte current = buffer[0];

                value |= (current & 0x7F) << position++ * 7;
                if (position > 5)
                    throw new OverflowException("VarInt too big");
                if ((current & 0x80) != 0x80)
                    return value;
            }
        }
    }
}
