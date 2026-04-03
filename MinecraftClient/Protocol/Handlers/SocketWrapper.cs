using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MinecraftClient.Crypto;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Wrapper for handling unencrypted & encrypted socket
    /// </summary>
    public class SocketWrapper
    {
        readonly TcpClient c;
        AesCfb8Stream? s;
        bool encrypted = false;

        /// <summary>
        /// Initialize a new SocketWrapper
        /// </summary>
        /// <param name="client">TcpClient connected to the server</param>
        public SocketWrapper(TcpClient client)
        {
            c = client;
        }

        /// <summary>
        /// Check if the socket is still connected
        /// </summary>
        /// <returns>TRUE if still connected</returns>
        /// <remarks>Silently dropped connection can only be detected by attempting to read/write data</remarks>
        public bool IsConnected()
        {
            return c.Client is not null && c.Connected;
        }

        /// <summary>
        /// Check if the socket has data available to read
        /// </summary>
        /// <returns>TRUE if data is available to read</returns>
        public bool HasDataAvailable()
        {
            return c.Client.Available > 0;
        }

        /// <summary>
        /// Switch network reading/writing to an encrypted stream
        /// </summary>
        /// <param name="secretKey">AES secret key</param>
        public void SwitchToEncrypted(byte[] secretKey)
        {
            if (encrypted)
                throw new InvalidOperationException("Stream is already encrypted!?");
            s = new AesCfb8Stream(c.GetStream(), secretKey);
            encrypted = true;
        }

        /// <summary>
        /// Network reading method. Read bytes from the socket or encrypted socket.
        /// </summary>
        private void Receive(byte[] buffer, int start, int offset, SocketFlags f)
        {
            int read = 0;
            while (read < offset)
            {
                if (encrypted)
                    read += s!.Read(buffer, start + read, offset - read);
                else
                    read += c.Client.Receive(buffer, start + read, offset - read, f);
            }
        }

        private async Task ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            int read = 0;
            while (read < buffer.Length)
            {
                int currentRead = encrypted
                    ? await s!.ReadAsync(buffer[read..], cancellationToken)
                    : await c.GetStream().ReadAsync(buffer[read..], cancellationToken);

                if (currentRead == 0)
                    throw new IOException("Connection closed.");

                read += currentRead;
            }
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
                byte[] cache = new byte[length];
                Receive(cache, 0, length, SocketFlags.None);
                return cache;
            }
            return Array.Empty<byte>();
        }

        public async Task<byte[]> ReadDataRAWAsync(int length, CancellationToken cancellationToken)
        {
            if (length > 0)
            {
                byte[] cache = new byte[length];
                await ReceiveAsync(cache, cancellationToken);
                return cache;
            }

            return Array.Empty<byte>();
        }

        /// <summary>
        /// Send raw data to the server.
        /// </summary>
        /// <param name="buffer">data to send</param>
        public void SendDataRAW(byte[] buffer)
        {
            if (!IsConnected())
                throw new SocketException((int)SocketError.NotConnected);

            if (encrypted)
                s!.Write(buffer, 0, buffer.Length);
            else
                c.Client.Send(buffer);
        }

        public async Task SendDataRAWAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            if (!IsConnected())
                throw new SocketException((int)SocketError.NotConnected);

            if (encrypted)
                await s!.WriteAsync(buffer, cancellationToken);
            else
                await c.GetStream().WriteAsync(buffer, cancellationToken);
        }

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        public void Disconnect()
        {
            try
            {
                c.Close();
            }
            catch (SocketException) { }
            catch (System.IO.IOException) { }
            catch (NullReferenceException) { }
            catch (ObjectDisposedException) { }
        }
    }
}
