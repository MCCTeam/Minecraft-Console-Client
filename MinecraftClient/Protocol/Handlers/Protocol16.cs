using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using MinecraftClient.Crypto;
using MinecraftClient.Proxy;
using System.Security.Cryptography;
using MinecraftClient.Mapping;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Implementation for Minecraft 1.4.X, 1.5.X, 1.6.X Protocols
    /// </summary>

    class Protocol16Handler : IMinecraftCom
    {
        IMinecraftComHandler handler;
        private bool autocomplete_received = false;
        private string autocomplete_result = "";
        private bool encrypted = false;
        private int protocolversion;
        private Thread netRead;
        Crypto.IAesStream s;
        TcpClient c;

        public Protocol16Handler(TcpClient Client, int ProtocolVersion, IMinecraftComHandler Handler)
        {
            ConsoleIO.SetAutoCompleteEngine(this);
            if (protocolversion >= 72)
                ChatParser.InitTranslations();
            this.c = Client;
            this.protocolversion = ProtocolVersion;
            this.handler = Handler;

            if (Handler.GetTerrainEnabled())
            {
                ConsoleIO.WriteLineFormatted("§8Terrain & Movements currently not handled for that MC version.");
                Handler.SetTerrainEnabled(false);
            }
        }

        private Protocol16Handler(TcpClient Client)
        {
            this.c = Client;
        }

        private void Updater()
        {
            try
            {
                do
                {
                    Thread.Sleep(100);
                }
                while (Update());
            }
            catch (System.IO.IOException) { }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }

            handler.OnConnectionLost(ChatBot.DisconnectReason.ConnectionLost, "");
        }

        private bool Update()
        {
            handler.OnUpdate();
            bool connection_ok = true;
            while (c.Client.Available > 0 && connection_ok)
            {
                byte id = readNextByte();
                connection_ok = processPacket(id);
            }
            return connection_ok;
        }

        private bool processPacket(byte id)
        {
            int nbr = 0;
            switch (id)
            {
                case 0x00: byte[] keepalive = new byte[5] { 0, 0, 0, 0, 0 };
                    Receive(keepalive, 1, 4, SocketFlags.None);
                    Send(keepalive); break;
                case 0x01: readData(4); readNextString(); readData(5); break;
                case 0x02: readData(1); readNextString(); readNextString(); readData(4); break;
                case 0x03:
                    string message = readNextString();
                    handler.OnTextReceived(message, protocolversion >= 72); break;
                case 0x04: readData(16); break;
                case 0x05: readData(6); readNextItemSlot(); break;
                case 0x06: readData(12); break;
                case 0x07: readData(9); break;
                case 0x08: if (protocolversion >= 72) { readData(10); } else readData(8); break;
                case 0x09: readData(8); readNextString(); break;
                case 0x0A: readData(1); break;
                case 0x0B: readData(33); break;
                case 0x0C: readData(9); break;
                case 0x0D: readData(41); break;
                case 0x0E: readData(11); break;
                case 0x0F: readData(10); readNextItemSlot(); readData(3); break;
                case 0x10: readData(2); break;
                case 0x11: readData(14); break;
                case 0x12: readData(5); break;
                case 0x13: if (protocolversion >= 72) { readData(9); } else readData(5); break;
                case 0x14: readData(4); readNextString(); readData(16); readNextEntityMetaData(); break;
                case 0x16: readData(8); break;
                case 0x17: readData(19); readNextObjectData(); break;
                case 0x18: readData(26); readNextEntityMetaData(); break;
                case 0x19: readData(4); readNextString(); readData(16); break;
                case 0x1A: readData(18); break;
                case 0x1B: if (protocolversion >= 72) { readData(10); } break;
                case 0x1C: readData(10); break;
                case 0x1D: nbr = (int)readNextByte(); readData(nbr * 4); break;
                case 0x1E: readData(4); break;
                case 0x1F: readData(7); break;
                case 0x20: readData(6); break;
                case 0x21: readData(9); break;
                case 0x22: readData(18); break;
                case 0x23: readData(5); break;
                case 0x26: readData(5); break;
                case 0x27: if (protocolversion >= 72) { readData(9); } else readData(8); break;
                case 0x28: readData(4); readNextEntityMetaData(); break;
                case 0x29: readData(8); break;
                case 0x2A: readData(5); break;
                case 0x2B: readData(8); break;
                case 0x2C: if (protocolversion >= 72) { readNextEntityProperties(protocolversion); } break;
                case 0x33: readData(13); nbr = readNextInt(); readData(nbr); break;
                case 0x34: readData(10); nbr = readNextInt(); readData(nbr); break;
                case 0x35: readData(12); break;
                case 0x36: readData(14); break;
                case 0x37: readData(17); break;
                case 0x38: readNextChunkBulkData(); break;
                case 0x3C: readData(28); nbr = readNextInt(); readData(3 * nbr); readData(12); break;
                case 0x3D: readData(18); break;
                case 0x3E: readNextString(); readData(17); break;
                case 0x3F: if (protocolversion > 51) { readNextString(); readData(32); } break;
                case 0x46: readData(2); break;
                case 0x47: readData(17); break;
                case 0x64: readNextWindowData(protocolversion); break;
                case 0x65: readData(1); break;
                case 0x66: readData(7); readNextItemSlot(); break;
                case 0x67: readData(3); readNextItemSlot(); break;
                case 0x68: readData(1); for (nbr = readNextShort(); nbr > 0; nbr--) { readNextItemSlot(); } break;
                case 0x69: readData(5); break;
                case 0x6A: readData(4); break;
                case 0x6B: readData(2); readNextItemSlot(); break;
                case 0x6C: readData(2); break;
                case 0x82: readData(10); readNextString(); readNextString(); readNextString(); readNextString(); break;
                case 0x83: readData(4); nbr = readNextShort(); readData(nbr); break;
                case 0x84: readData(11); nbr = readNextShort(); if (nbr > 0) { readData(nbr); } break;
                case 0x85: if (protocolversion >= 74) { readData(13); } break;
                case 0xC8:
                    if (readNextInt() == 2022) { ConsoleIO.WriteLineFormatted("§MCC: You are dead. Type /reco to respawn & reconnect."); }
                    if (protocolversion >= 72) { readData(4); } else readData(1);
                    break;
                case 0xC9:
                    string name = readNextString(); bool online = readNextByte() != 0x00; readData(2);
                    Guid FakeUUID = new Guid(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(name)).Take(16).ToArray());
                    if (online) { handler.OnPlayerJoin(FakeUUID, name); } else { handler.OnPlayerLeave(FakeUUID); }
                    break;
                case 0xCA: if (protocolversion >= 72) { readData(9); } else readData(3); break;
                case 0xCB: autocomplete_result = readNextString(); autocomplete_received = true; break;
                case 0xCC: readNextString(); readData(4); break;
                case 0xCD: readData(1); break;
                case 0xCE: if (protocolversion > 51) { readNextString(); readNextString(); readData(1); } break;
                case 0xCF: if (protocolversion > 51) { readNextString(); readData(1); readNextString(); } readData(4); break;
                case 0xD0: if (protocolversion > 51) { readData(1); readNextString(); } break;
                case 0xD1: if (protocolversion > 51) { readNextTeamData(); } break;
                case 0xFA: string channel = readNextString();
                    byte[] payload = readNextByteArray();
                    handler.OnPluginChannelMessage(channel, payload);
                    break;
                case 0xFF: string reason = readNextString();
                    handler.OnConnectionLost(ChatBot.DisconnectReason.InGameKick, reason); break;
                default: return false; //unknown packet!
            }
            return true; //packet has been successfully skipped
        }

        private void StartUpdating()
        {
            netRead = new Thread(new ThreadStart(Updater));
            netRead.Name = "ProtocolPacketHandler";
            netRead.Start();
        }

        public void Dispose()
        {
            try
            {
                if (netRead != null)
                {
                    netRead.Abort();
                    c.Close();
                }
            }
            catch { }
        }

        private void readData(int offset)
        {
            if (offset > 0)
            {
                try
                {
                    byte[] cache = new byte[offset];
                    Receive(cache, 0, offset, SocketFlags.None);
                }
                catch (OutOfMemoryException) { }
            }
        }

        private string readNextString()
        {
            ushort length = (ushort)readNextShort();
            if (length > 0)
            {
                byte[] cache = new byte[length * 2];
                Receive(cache, 0, length * 2, SocketFlags.None);
                string result = Encoding.BigEndianUnicode.GetString(cache);
                return result;
            }
            else return "";
        }

        private byte[] readNextByteArray()
        {
            short len = readNextShort();
            byte[] data = new byte[len];
            Receive(data, 0, len, SocketFlags.None);
            return data;
        }

        private short readNextShort()
        {
            byte[] tmp = new byte[2];
            Receive(tmp, 0, 2, SocketFlags.None);
            Array.Reverse(tmp);
            return BitConverter.ToInt16(tmp, 0);
        }

        private int readNextInt()
        {
            byte[] tmp = new byte[4];
            Receive(tmp, 0, 4, SocketFlags.None);
            Array.Reverse(tmp);
            return BitConverter.ToInt32(tmp, 0);
        }

        private byte readNextByte()
        {
            byte[] result = new byte[1];
            Receive(result, 0, 1, SocketFlags.None);
            return result[0];
        }

        private void readNextItemSlot()
        {
            short itemid = readNextShort();
            //If slot not empty (item ID != -1)
            if (itemid != -1)
            {
                readData(1); //Item count
                readData(2); //Item damage
                short length = readNextShort();
                //If length of optional NBT data > 0, read it
                if (length > 0) { readData(length); }
            }
        }

        private void readNextEntityMetaData()
        {
            do
            {
                byte[] id = new byte[1];
                Receive(id, 0, 1, SocketFlags.None);
                if (id[0] == 0x7F) { break; }
                int index = id[0] & 0x1F;
                int type = id[0] >> 5;
                switch (type)
                {
                    case 0: readData(1); break;        //Byte
                    case 1: readData(2); break;        //Short
                    case 2: readData(4); break;        //Int
                    case 3: readData(4); break;        //Float
                    case 4: readNextString(); break;   //String
                    case 5: readNextItemSlot(); break; //Slot
                    case 6: readData(12); break;       //Vector (3 Int)
                }
            } while (true);
        }

        private void readNextObjectData()
        {
            int id = readNextInt();
            if (id != 0) { readData(6); }
        }

        private void readNextTeamData()
        {
            readNextString(); //Internal Name
            byte mode = readNextByte();

            if (mode == 0 || mode == 2)
            {
                readNextString(); //Display Name
                readNextString(); //Prefix
                readNextString(); //Suffix
                readData(1); //Friendly Fire
            }

            if (mode == 0 || mode == 3 || mode == 4)
            {
                short count = readNextShort();
                for (int i = 0; i < count; i++)
                {
                    readNextString(); //Players
                }
            }
        }

        private void readNextEntityProperties(int protocolversion)
        {
            if (protocolversion >= 72)
            {
                if (protocolversion >= 74)
                {
                    //Minecraft 1.6.2
                    readNextInt(); //Entity ID
                    int count = readNextInt();
                    for (int i = 0; i < count; i++)
                    {
                        readNextString(); //Property name
                        readData(8); //Property value (Double)
                        short othercount = readNextShort();
                        readData(25 * othercount);
                    }
                }
                else
                {
                    //Minecraft 1.6.0 / 1.6.1
                    readNextInt(); //Entity ID
                    int count = readNextInt();
                    for (int i = 0; i < count; i++)
                    {
                        readNextString(); //Property name
                        readData(8); //Property value (Double)
                    }
                }
            }
        }

        private void readNextWindowData(int protocolversion)
        {
            readData(1);
            byte windowtype = readNextByte();
            readNextString();
            readData(1);
            if (protocolversion > 51)
            {
                readData(1);
                if (protocolversion >= 72 && windowtype == 0xb)
                {
                    readNextInt();
                }
            }
        }

        private void readNextChunkBulkData()
        {
            short chunkcount = readNextShort();
            int datalen = readNextInt();
            readData(1);
            readData(datalen);
            readData(12 * (chunkcount));
        }

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

        private void Send(byte[] buffer)
        {
            if (encrypted)
            {
                s.Write(buffer, 0, buffer.Length);
            }
            else c.Client.Send(buffer);
        }

        private bool Handshake(string uuid, string username, string sessionID, string host, int port)
        {
            //array
            byte[] data = new byte[10 + (username.Length + host.Length) * 2];

            //packet id
            data[0] = (byte)2;

            //Protocol Version
            data[1] = (byte)protocolversion;

            //short len
            byte[] sh = BitConverter.GetBytes((short)username.Length);
            Array.Reverse(sh);
            sh.CopyTo(data, 2);

            //username
            byte[] bname = Encoding.BigEndianUnicode.GetBytes(username);
            bname.CopyTo(data, 4);

            //short len
            sh = BitConverter.GetBytes((short)host.Length);
            Array.Reverse(sh);
            sh.CopyTo(data, 4 + (username.Length * 2));

            //host
            byte[] bhost = Encoding.BigEndianUnicode.GetBytes(host);
            bhost.CopyTo(data, 6 + (username.Length * 2));

            //port
            sh = BitConverter.GetBytes(port);
            Array.Reverse(sh);
            sh.CopyTo(data, 6 + (username.Length * 2) + (host.Length * 2));

            Send(data);

            byte[] pid = new byte[1];
            Receive(pid, 0, 1, SocketFlags.None);
            while (pid[0] == 0xFA) //Skip some early plugin messages
            {
                processPacket(pid[0]);
                Receive(pid, 0, 1, SocketFlags.None);
            }
            if (pid[0] == 0xFD)
            {
                string serverID = readNextString();
                byte[] PublicServerkey = readNextByteArray();
                byte[] token = readNextByteArray();

                if (serverID == "-")
                    ConsoleIO.WriteLineFormatted("§8Server is in offline mode.");
                else if (Settings.DebugMessages)
                    ConsoleIO.WriteLineFormatted("§8Handshake successful. (Server ID: " + serverID + ')');

                return StartEncryption(uuid, username, sessionID, token, serverID, PublicServerkey);
            }
            else return false;
        }

        private bool StartEncryption(string uuid, string username, string sessionID, byte[] token, string serverIDhash, byte[] serverKey)
        {
            System.Security.Cryptography.RSACryptoServiceProvider RSAService = CryptoHandler.DecodeRSAPublicKey(serverKey);
            byte[] secretKey = CryptoHandler.GenerateAESPrivateKey();

            if (Settings.DebugMessages)
                ConsoleIO.WriteLineFormatted("§8Crypto keys & hash generated.");

            if (serverIDhash != "-")
            {
                Console.WriteLine("Checking Session...");
                if (!ProtocolHandler.SessionCheck(uuid, sessionID, CryptoHandler.getServerHash(serverIDhash, serverKey, secretKey)))
                {
                    return false;
                }
            }

            //Encrypt the data
            byte[] key_enc = RSAService.Encrypt(secretKey, false);
            byte[] token_enc = RSAService.Encrypt(token, false);
            byte[] keylen = BitConverter.GetBytes((short)key_enc.Length);
            byte[] tokenlen = BitConverter.GetBytes((short)token_enc.Length);

            Array.Reverse(keylen);
            Array.Reverse(tokenlen);

            //Building the packet
            byte[] data = new byte[5 + (short)key_enc.Length + (short)token_enc.Length];
            data[0] = 0xFC;
            keylen.CopyTo(data, 1);
            key_enc.CopyTo(data, 3);
            tokenlen.CopyTo(data, 3 + (short)key_enc.Length);
            token_enc.CopyTo(data, 5 + (short)key_enc.Length);

            //Send it back
            Send(data);

            //Getting the next packet
            byte[] pid = new byte[1];
            Receive(pid, 0, 1, SocketFlags.None);
            if (pid[0] == 0xFC)
            {
                readData(4);
                s = CryptoHandler.getAesStream(c.GetStream(), secretKey);
                encrypted = true;
                return true;
            }
            else return false;
        }

        public bool Login()
        {
            if (Handshake(handler.GetUserUUID(), handler.GetUsername(), handler.GetSessionID(), handler.GetServerHost(), handler.GetServerPort()))
            {
                Send(new byte[] { 0xCD, 0 });
                try
                {
                    byte[] pid = new byte[1];
                    try
                    {
                        if (c.Connected)
                        {
                            Receive(pid, 0, 1, SocketFlags.None);
                            while (pid[0] >= 0xC0 && pid[0] != 0xFF) //Skip some early packets or plugin messages
                            {
                                processPacket(pid[0]);
                                Receive(pid, 0, 1, SocketFlags.None);
                            }
                            if (pid[0] == (byte)1)
                            {
                                readData(4); readNextString(); readData(5);
                                StartUpdating();
                                return true; //The Server accepted the request
                            }
                            else if (pid[0] == (byte)0xFF)
                            {
                                string reason = readNextString();
                                handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected, reason);
                                return false;
                            }
                        }
                    }
                    catch
                    {
                        //Connection failed
                        return false;
                    }
                }
                catch
                {
                    handler.OnConnectionLost(ChatBot.DisconnectReason.ConnectionLost, "");
                    return false;
                }
                return false; //Login was unsuccessful (received a kick...)
            }
            else return false;
        }

        public void Disconnect()
        {
            const string message = "disconnect.quitting";

            try
            {
                byte[] reason = new byte[3 + (message.Length * 2)];
                reason[0] = (byte)0xff;

                byte[] msglen;
                msglen = BitConverter.GetBytes((short)message.Length);
                Array.Reverse(msglen);
                msglen.CopyTo(reason, 1);

                if (message.Length > 0)
                {
                    byte[] msg;
                    msg = Encoding.BigEndianUnicode.GetBytes(message);
                    msg.CopyTo(reason, 3);
                }

                Send(reason);
            }
            catch (SocketException) { }
            catch (System.IO.IOException) { }
        }

        public int GetMaxChatMessageLength()
        {
            return 100;
        }

        public bool SendChatMessage(string message)
        {
            if (String.IsNullOrEmpty(message))
                return true;

            try
            {
                byte[] chat = new byte[3 + (message.Length * 2)];
                chat[0] = (byte)3;

                byte[] msglen;
                msglen = BitConverter.GetBytes((short)message.Length);
                Array.Reverse(msglen);
                msglen.CopyTo(chat, 1);

                byte[] msg;
                msg = Encoding.BigEndianUnicode.GetBytes(message);
                msg.CopyTo(chat, 3);

                Send(chat);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
        }

        public bool SendRespawnPacket()
        {
            try
            {
                Send(new byte[] { 0xCD, 1 });
                return true;
            }
            catch (SocketException) { return false; }
        }

        public bool SendBrandInfo(string brandInfo)
        {
            return false; //Only supported since MC 1.7
        }

        public bool SendClientSettings(string language, byte viewDistance, byte difficulty, byte chatMode, bool chatColors, byte skinParts, byte mainHand)
        {
            return false; //Currently not implemented
        }

        public bool SendLocationUpdate(Location location, bool onGround, float? yaw, float? pitch)
        {
            return false; //Currently not implemented
        }

        /// <summary>
        /// Send a plugin channel packet to the server.
        /// </summary>
        /// <param name="channel">Channel to send packet on</param>
        /// <param name="data">packet Data</param>

        public bool SendPluginChannelPacket(string channel, byte[] data)
        {
            try {
                byte[] channelLength = BitConverter.GetBytes((short)channel.Length);
                Array.Reverse(channelLength);

                byte[] channelData = Encoding.BigEndianUnicode.GetBytes(channel);

                byte[] dataLength = BitConverter.GetBytes((short)data.Length);
                Array.Reverse(dataLength);

                Send(concatBytes(new byte[] { 0xFA }, channelLength, channelData, dataLength, data));

                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
        }

        IEnumerable<string> IAutoComplete.AutoComplete(string BehindCursor)
        {
            if (String.IsNullOrEmpty(BehindCursor))
                return new string[] { };

            byte[] autocomplete = new byte[3 + (BehindCursor.Length * 2)];
            autocomplete[0] = 0xCB;
            byte[] msglen = BitConverter.GetBytes((short)BehindCursor.Length);
            Array.Reverse(msglen); msglen.CopyTo(autocomplete, 1);
            byte[] msg = Encoding.BigEndianUnicode.GetBytes(BehindCursor);
            msg.CopyTo(autocomplete, 3);

            autocomplete_received = false;
            autocomplete_result = BehindCursor;
            Send(autocomplete);

            int wait_left = 50; //do not wait more than 5 seconds (50 * 100 ms)
            while (wait_left > 0 && !autocomplete_received) { System.Threading.Thread.Sleep(100); wait_left--; }
            if (!String.IsNullOrEmpty(autocomplete_result) && autocomplete_received)
                ConsoleIO.WriteLineFormatted("§8" + autocomplete_result.Replace((char)0x00, ' '), false);
            return autocomplete_result.Split((char)0x00);
        }

        private static byte[] concatBytes(params byte[][] bytes)
        {
            List<byte> result = new List<byte>();
            foreach (byte[] array in bytes)
                result.AddRange(array);
            return result.ToArray();
        }

        public static bool doPing(string host, int port, ref int protocolversion)
        {
            try
            {
                string version = "";
                TcpClient tcp = ProxyHandler.newTcpClient(host, port);
                tcp.ReceiveTimeout = 5000; //MC 1.7.2+ SpigotMC servers won't respond, so we need a reasonable timeout.
                byte[] ping = new byte[2] { 0xfe, 0x01 };
                tcp.Client.Send(ping, SocketFlags.None);

                tcp.Client.Receive(ping, 0, 1, SocketFlags.None);
                if (ping[0] == 0xff)
                {
                    Protocol16Handler ComTmp = new Protocol16Handler(tcp);
                    string result = ComTmp.readNextString();
                    if (result.Length > 2 && result[0] == '§' && result[1] == '1')
                    {
                        string[] tmp = result.Split((char)0x00);
                        protocolversion = (byte)Int16.Parse(tmp[1]);
                        version = tmp[2];

                        if (protocolversion == 127) //MC 1.7.2+
                            return false;
                    }
                    else
                    {
                        protocolversion = (byte)39;
                        version = "B1.8.1 - 1.3.2";
                    }
                    ConsoleIO.WriteLineFormatted("§8Server version : MC " + version + " (protocol v" + protocolversion + ").");
                    return true;
                }
                else return false;
            }
            catch { return false; }
        }
    }
}
