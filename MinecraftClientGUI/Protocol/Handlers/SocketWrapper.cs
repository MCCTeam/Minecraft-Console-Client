using System;
using System.Net.Sockets;
using MinecraftClient.Crypto;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Wrapper for handling unencrypted & encrypted socket
    /// </summary>
    class SocketWrapper
    {
        TcpClient c;
        IAesStream s;
        bool encrypted = false;

        /// <summary>
        /// Initialize a new SocketWrapper
        /// </summary>
        /// <param name="client">TcpClient connected to the server</param>
        public SocketWrapper(TcpClient client)
        {
            this.c = client;
        }

        /// <summary>
        /// Check if the socket is still connected
        /// </summary>
        /// <returns>TRUE if still connected</returns>
        /// <remarks>Silently dropped connection can only be detected by attempting to read/write data</remarks>
        public bool IsConnected()
        {
            return c.Client != null && c.Connected;
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
            this.s = CryptoHandler.getAesStream(c.GetStream(), secretKey);
            this.encrypted = true;
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
                {
                    read += s.Read(buffer, start + read, offset - read);
                }
                else read += c.Client.Receive(buffer, start + read, offset - read, f);
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
            return new byte[] { };
        }

        /// <summary>
        /// Send raw data to the server.
        /// </summary>
        /// <param name="buffer">data to send</param>
        public void SendDataRAW(byte[] buffer)
        {
            if (encrypted)
            {
                s.Write(buffer, 0, buffer.Length);
            }
            else c.Client.Send(buffer);
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
