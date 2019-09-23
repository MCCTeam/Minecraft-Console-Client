using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;

namespace MinecraftClient.Protocol.Handlers
{
    class PacketProxy
    {
        private int compression_treshold = 0;
        private bool handshake_phase = true;
        private bool login_phase = true;
        TcpClient client;
        TcpClient server;

        public PacketProxy(TcpClient client, TcpClient server)
        {
            this.client = client;
            this.server = server;
        }

        public void Run()
        {
            Thread t = new Thread(() =>
            {
                try
                {
                    do { Thread.Sleep(100); }
                    while (Update(true));
                }
                catch (System.IO.IOException) { }
                catch (SocketException) { }
                catch (ObjectDisposedException) { }
            });
            t.Name = "UpdaterServer";
            t.Start();

            t = new Thread(() =>
            {
                try
                {
                    do { Thread.Sleep(100); }
                    while (Update(false));
                }
                catch (System.IO.IOException) { }
                catch (SocketException) { }
                catch (ObjectDisposedException) { }
            });
            t.Name = "UpdaterClient";
            t.Start();
        }

        private bool Update(bool server)
        {
            TcpClient c = server ? this.server : this.client;
            if (c.Client == null || !c.Connected) { return false; }
            try
            {
                while (c.Client.Available > 0)
                {
                    int packetID = 0;
                    byte[] packetData = new byte[] { };
                    byte[] packetRawData = new byte[] { };
                    readNextPacket(c, ref packetID, ref packetData, ref packetRawData);
                    handlePacket(packetID, (byte[])packetData.Clone(), server);
                    (server ? this.client : this.server).Client.Send(packetRawData);
                }
            }
            catch (SocketException) { return false; }
            return true;
        }

        private void readNextPacket(TcpClient c, ref int packetID, ref byte[] packetData, ref byte[] packetRawData)
        {
            int size = readNextVarIntRAW(c);
            packetData = readDataRAW(c, size);
            packetRawData = concatBytes(getVarInt(size), packetData);

            if (compression_treshold > 0)
            {
                int size_uncompressed = readNextVarInt(ref packetData);
                if (size_uncompressed != 0)
                    packetData = ZlibUtils.Decompress(packetData, size_uncompressed);
            }

            packetID = readNextVarInt(ref packetData);
        }

        private void handlePacket(int packetID, byte[] packetData, bool server)
        {
            //Console.WriteLine((server ? "[S -> C] 0x" : "[C -> S] 0x") + packetID.ToString("x2"));
            if (login_phase)
            {
                if (server)
                {
                    switch (packetID)
                    {
                        case 0x00:
                            Console.WriteLine("[S -> C] Login rejected");
                            break;
                        case 0x01:
                            Console.WriteLine("[S -> C] Encryption request");
                            Console.WriteLine(@"[WARNING] ENCRYPTION IS NOT SUPPORTED BY PROXY !!");
                            break;
                        case 0x02:
                            login_phase = false;
                            Console.WriteLine("[S -> C] Login successfull");
                            break;
                        case 0x03:
                            compression_treshold = readNextVarInt(ref packetData);
                            Console.WriteLine("[S -> C] Compression Treshold: " + compression_treshold);
                            break;
                    }
                }
                else
                {
                    switch (packetID)
                    {
                        case 0x00:
                            Console.WriteLine("[C -> S] " + (handshake_phase ? "Handshake" : "Login request"));
                            handshake_phase = false;
                            break;
                    }
                }
            }
            else
            {
                if (!server)
                {
                    double x, y, z;
                    bool g;
                    switch (packetID)
                    {
                        //Do debug work here
                        case 0x0C:
                            x = readNextDouble(ref packetData);
                            y = readNextDouble(ref packetData);
                            z = readNextDouble(ref packetData);
                            g = readNextBool(ref packetData);
                            Console.WriteLine("[C -> S] Location: " + x + ", " + y + ", " + z + ", " + g);
                            break;
                        case 0x0D:
                            x = readNextDouble(ref packetData);
                            y = readNextDouble(ref packetData);
                            z = readNextDouble(ref packetData);
                            readNextDouble(ref packetData); //skip 2 floats: look yaw & pitch
                            g = readNextBool(ref packetData);
                            Console.WriteLine("[C -> S] Location: " + x + ", " + y + ", " + z + ", (look)" + ", " + g);
                            break;
                    }
                }
            }
        }

        public void Dispose()
        {
            try
            {
                client.Close();
                server.Close();
            }
            catch { }
        }

        private byte[] readDataRAW(TcpClient c, int offset)
        {
            if (offset > 0)
            {
                try
                {
                    byte[] cache = new byte[offset];
                    Receive(c, cache, 0, offset, SocketFlags.None);
                    return cache;
                }
                catch (OutOfMemoryException) { }
            }
            return new byte[] { };
        }

        private byte[] readData(int offset, ref byte[] cache)
        {
            List<byte> read = new List<byte>();
            List<byte> list = new List<byte>(cache);
            while (offset > 0 && list.Count > 0)
            {
                read.Add(list[0]);
                list.RemoveAt(0);
                offset--;
            }
            cache = list.ToArray();
            return read.ToArray();
        }

        private string readNextString(ref byte[] cache)
        {
            int length = readNextVarInt(ref cache);
            if (length > 0)
            {
                return Encoding.UTF8.GetString(readData(length, ref cache));
            }
            else return "";
        }

        private bool readNextBool(ref byte[] cache)
        {
            byte[] rawValue = readData(1, ref cache);
            return rawValue[0] != 0;
        }

        private double readNextDouble(ref byte[] cache)
        {
            byte[] rawValue = readData(8, ref cache);
            Array.Reverse(rawValue); //Endianness
            return BitConverter.ToDouble(rawValue, 0);
        }

        private int readNextVarIntRAW(TcpClient c)
        {
            int i = 0;
            int j = 0;
            int k = 0;
            byte[] tmp = new byte[1];
            while (true)
            {
                Receive(c, tmp, 0, 1, SocketFlags.None);
                k = tmp[0];
                i |= (k & 0x7F) << j++ * 7;
                if (j > 5) throw new OverflowException("VarInt too big");
                if ((k & 0x80) != 128) break;
            }
            return i;
        }

        private int readNextVarInt(ref byte[] cache)
        {
            int i = 0;
            int j = 0;
            int k = 0;
            byte[] tmp = new byte[1];
            while (true)
            {
                tmp = readData(1, ref cache);
                k = tmp[0];
                i |= (k & 0x7F) << j++ * 7;
                if (j > 5) throw new OverflowException("VarInt too big");
                if ((k & 0x80) != 128) break;
            }
            return i;
        }

        private static byte[] getVarInt(int paramInt)
        {
            List<byte> bytes = new List<byte>();
            while ((paramInt & -128) != 0)
            {
                bytes.Add((byte)(paramInt & 127 | 128));
                paramInt = (int)(((uint)paramInt) >> 7);
            }
            bytes.Add((byte)paramInt);
            return bytes.ToArray();
        }

        private static byte[] concatBytes(params byte[][] bytes)
        {
            List<byte> result = new List<byte>();
            foreach (byte[] array in bytes)
                result.AddRange(array);
            return result.ToArray();
        }

        private void Receive(TcpClient c, byte[] buffer, int start, int offset, SocketFlags f)
        {
            int read = 0;
            while (read < offset)
                read += c.Client.Receive(buffer, start + read, offset - read, f);
        }
    }
}
