using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MinecraftClient.Crypto;

namespace MinecraftClient.Protocol.PacketPipeline
{
    /// <summary>
    /// Wrapper for handling unencrypted & encrypted socket
    /// </summary>
    class SocketWrapper
    {
        private TcpClient tcpClient;

        private AesStream? AesStream;

        private PacketStream? packetStream = null;

        private Stream ReadStream, WriteStream;

        private bool Encrypted = false;

        public int CompressionThreshold { get; set; } = 0;


        private SemaphoreSlim SendSemaphore = new(1, 1);

        private Task LastSendTask = Task.CompletedTask;

        /// <summary>
        /// Initialize a new SocketWrapper
        /// </summary>
        /// <param name="client">TcpClient connected to the server</param>
        public SocketWrapper(TcpClient client)
        {
            tcpClient = client;
            ReadStream = WriteStream = client.GetStream();
        }

        /// <summary>
        /// Check if the socket is still connected
        /// </summary>
        /// <returns>TRUE if still connected</returns>
        /// <remarks>Silently dropped connection can only be detected by attempting to read/write data</remarks>
        public bool IsConnected()
        {
            return tcpClient.Client != null && tcpClient.Connected;
        }

        /// <summary>
        /// Check if the socket has data available to read
        /// </summary>
        /// <returns>TRUE if data is available to read</returns>
        public bool HasDataAvailable()
        {
            return tcpClient.Client.Available > 0;
        }

        /// <summary>
        /// Switch network reading/writing to an encrypted stream
        /// </summary>
        /// <param name="secretKey">AES secret key</param>
        public void SwitchToEncrypted(byte[] secretKey)
        {
            if (Encrypted)
                throw new InvalidOperationException("Stream is already encrypted!?");
            Encrypted = true;
            ReadStream = WriteStream = AesStream = new AesStream(tcpClient.Client, secretKey);
        }

        /// <summary>
        /// Send raw data to the server.
        /// </summary>
        /// <param name="buffer">data to send</param>
        public async Task SendAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await SendSemaphore.WaitAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;
            await LastSendTask;
            LastSendTask = WriteStream.WriteAsync(buffer, cancellationToken).AsTask();
            SendSemaphore.Release();
        }

        public async Task<Tuple<int, PacketStream>> GetNextPacket(bool handleCompress, CancellationToken cancellationToken = default)
        {
            if (packetStream != null)
            {
                await packetStream.DisposeAsync();
                packetStream = null;
            }

            int readed = 0;
            (int packetSize, _) = await ReceiveVarIntRaw(ReadStream, cancellationToken);

            int packetID;
            if (handleCompress && CompressionThreshold > 0)
            {
                (int sizeUncompressed, readed) = await ReceiveVarIntRaw(ReadStream, cancellationToken);
                if (sizeUncompressed != 0)
                {
                    ZlibBaseStream zlibBaseStream = new(AesStream ?? ReadStream, packetSize: packetSize - readed);
                    ZLibStream zlibStream = new(zlibBaseStream, CompressionMode.Decompress, leaveOpen: false);

                    if (AesStream == null || AesStream.HwAccelerateEnable)
                    {
                        zlibBaseStream.BufferSize = 64;
                        (packetID, readed) = await ReceiveVarIntRaw(zlibStream, cancellationToken);
                        zlibBaseStream.BufferSize = 1024;
                    }
                    else
                    {
                        zlibBaseStream.BufferSize = 16;
                        (packetID, readed) = await ReceiveVarIntRaw(zlibStream, cancellationToken);
                        zlibBaseStream.BufferSize = 256;
                    }

                    // ConsoleIO.WriteLine("packetID = " + packetID + ", readed = " + zlibBaseStream.packetReaded + ", size = " + packetSize + " -> " + sizeUncompressed);

                    packetStream = new(zlibStream, sizeUncompressed - readed, cancellationToken);

                    return new(packetID, packetStream);
                }
            }

            (packetID, int readed2) = await ReceiveVarIntRaw(ReadStream, cancellationToken);

            packetStream = new(AesStream ?? ReadStream, packetSize - readed - readed2, cancellationToken);

            return new(packetID, packetStream);
        }

        private async Task<Tuple<int, int>> ReceiveVarIntRaw(Stream stream, CancellationToken cancellationToken = default)
        {
            int i = 0;
            int j = 0;
            byte[] b = new byte[1];
            while (true)
            {
                await stream.ReadAsync(b, cancellationToken);
                i |= (b[0] & 0x7F) << j++ * 7;
                if (j > 5) throw new OverflowException("VarInt too big");
                if ((b[0] & 0x80) != 128) break;
            }
            return new(i, j);
        }

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        public void Disconnect()
        {
            try
            {
                tcpClient.Close();
            }
            catch (SocketException) { }
            catch (IOException) { }
            catch (NullReferenceException) { }
            catch (ObjectDisposedException) { }
        }
    }
}
