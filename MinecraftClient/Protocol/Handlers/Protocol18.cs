using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using MinecraftClient.Crypto;
using MinecraftClient.Proxy;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Implementation for Minecraft 1.8.X Protocol
    /// </summary>

    class Protocol18Handler : IMinecraftCom
    {
        IMinecraftComHandler handler;
        private int compression_treshold = 0;
        private bool autocomplete_received = false;
        private string autocomplete_result = "";
        private bool login_phase = true;
        private bool encrypted = false;
        private int protocolversion;
        private Thread netRead;
        Crypto.IAesStream s;
        TcpClient c;

        public Protocol18Handler(TcpClient Client, int ProtocolVersion, IMinecraftComHandler Handler)
        {
            ConsoleIO.SetAutoCompleteEngine(this);
            ChatParser.InitTranslations();
            this.c = Client;
            this.protocolversion = ProtocolVersion;
            this.handler = Handler;
        }

        private Protocol18Handler(TcpClient Client)
        {
            this.c = Client;
        }

        /// <summary>
        /// Separate thread. Network reading loop.
        /// </summary>

        private void Updater()
        {
            int keep_alive_interval = 100;
            int keep_alive_timer = 100;
            try
            {
                do
                {
                    Thread.Sleep(100);
                    keep_alive_timer--;
                    if (keep_alive_timer <= 0)
                    {
                        SendRAW(getPaddingPacket());
                        keep_alive_timer = keep_alive_interval;
                    }
                }
                while (Update());
            }
            catch (System.IO.IOException) { }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }

            handler.OnConnectionLost(ChatBot.DisconnectReason.ConnectionLost, "");
        }

        /// <summary>
        /// Read data from the network. Should be called on a separate thread.
        /// </summary>
        /// <returns></returns>

        private bool Update()
        {
            handler.OnUpdate();
            if (c.Client == null || !c.Connected) { return false; }
            try
            {
                while (c.Client.Available > 0)
                {
                    int packetID = 0;
                    byte[] packetData = new byte[] { };
                    readNextPacket(ref packetID, ref packetData);
                    handlePacket(packetID, packetData);
                }
            }
            catch (SocketException) { return false; }
            return true;
        }

        /// <summary>
        /// Read the next packet from the network
        /// </summary>
        /// <param name="packetID">will contain packet ID</param>
        /// <param name="packetData">will contain raw packet Data</param>

        private void readNextPacket(ref int packetID, ref byte[] packetData)
        {
            int size = readNextVarIntRAW(); //Packet size
            packetData = readDataRAW(size); //Packet contents

            if (compression_treshold > 0) //Handle packet decompression
            {
                int size_uncompressed = readNextVarInt(ref packetData);
                if (size_uncompressed != 0) // != 0 means compressed, let's decompress
                    packetData = ZlibUtils.Decompress(packetData, size_uncompressed);
            }

            packetID = readNextVarInt(ref packetData); //Packet ID
        }

        /// <summary>
        /// Handle the given packet
        /// </summary>
        /// <param name="packetID">Packet ID</param>
        /// <param name="packetData">Packet contents</param>
        /// <returns>TRUE if the packet was processed, FALSE if ignored or unknown</returns>

        private bool handlePacket(int packetID, byte[] packetData)
        {
            if (login_phase)
            {
                switch (packetID) //Packet IDs are different while logging in
                {
                    case 0x03:
                        compression_treshold = readNextVarInt(ref packetData);
                        break;
                    default:
                        return false; //Ignored packet
                }
            }
            else //Regular in-game packets
            {
                switch (packetID)
                {
                    case 0x00:
                        SendPacket(0x00, getVarInt(readNextVarInt(ref packetData)));
                        break;
                    case 0x02:
						handler.OnTextReceived(ChatParser.ParseText(readNextString(ref packetData)));
                        break;
					case 0x0C: //Entity Look and Relative Move
						//ConsoleIO.WriteLineFormatted("§8 0x0C entity:" + readNextVarInt(ref packetData) + " has come in to sight");
						break;
					case 0x38: // update player list
						int action = readNextVarInt (ref packetData);
						int numActions = readNextVarInt (ref packetData);
						string uuid = readNextUUID (ref packetData);
						switch (action) {
							case 0x00:
								string name = readNextString (ref packetData);
								handler.addPlayer (uuid, name);
								break;
							case 0x04:
								handler.removePlayer (uuid);
								break;
							default:
								//do nothing
								break;
						}
						break;
                    case 0x3A:
                        int autocomplete_count = readNextVarInt(ref packetData);
                        string tab_list = "";
                        for (int i = 0; i < autocomplete_count; i++)
                        {
                            autocomplete_result = readNextString(ref packetData);
                            if (autocomplete_result != "")
                                tab_list = tab_list + autocomplete_result + " ";
                        }
                        autocomplete_received = true;
                        tab_list = tab_list.Trim();
                        if (tab_list.Length > 0)
                            ConsoleIO.WriteLineFormatted("§8" + tab_list, false);
                        break;
                    case 0x40:
                        handler.OnConnectionLost(ChatBot.DisconnectReason.InGameKick, ChatParser.ParseText(readNextString(ref packetData)));
                        return false;
                    case 0x46:
                        compression_treshold = readNextVarInt(ref packetData);
                        break;
                    default:
                        return false; //Ignored packet
                }
            }
            return true; //Packet processed
        }

        /// <summary>
        /// Start the updating thread. Should be called after login success.
        /// </summary>

        private void StartUpdating()
        {
            netRead = new Thread(new ThreadStart(Updater));
            netRead.Name = "ProtocolPacketHandler";
            netRead.Start();
        }

        /// <summary>
        /// Disconnect from the server, cancel network reading.
        /// </summary>

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

        /// <summary>
        /// Read some data directly from the network
        /// </summary>
        /// <param name="offset">Amount of bytes to read</param>
        /// <returns>The data read from the network as an array</returns>
        
        private byte[] readDataRAW(int offset)
        {
            if (offset > 0)
            {
                try
                {
                    byte[] cache = new byte[offset];
                    Receive(cache, 0, offset, SocketFlags.None);
                    return cache;
                }
                catch (OutOfMemoryException) { }
            }
            return new byte[] { };
        }
        
        /// <summary>
        /// Read some data from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="offset">Amount of bytes to read</param>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The data read from the cache as an array</returns>

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

        /// <summary>
        /// Read a string from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The string</returns>

        private string readNextString(ref byte[] cache)
        {
            int length = readNextVarInt(ref cache);
            if (length > 0)
            {
                return Encoding.UTF8.GetString(readData(length, ref cache));
            }
            else return "";
        }

		/// <summary>
		/// Read a uuid from a cache of bytes and remove it from the cache
		/// </summary>
		/// <param name="cache">Cache of bytes to read from</param>
		/// <returns>The uuid as a string</returns>

		private string readNextUUID(ref byte[] cache)
		{
			return BitConverter.ToString(readData (16, ref cache)).Replace ("-", string.Empty).ToLower();
		}

        /// <summary>
        /// Read a byte array from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The byte array</returns>

        private byte[] readNextByteArray(ref byte[] cache)
        {
            int len = readNextVarInt(ref cache);
            return readData(len, ref cache);
        }

        /// <summary>
        /// Read an integer from the network
        /// </summary>
        /// <returns>The integer</returns>

        private int readNextVarIntRAW()
        {
            int i = 0;
            int j = 0;
            int k = 0;
            byte[] tmp = new byte[1];
            while (true)
            {
                Receive(tmp, 0, 1, SocketFlags.None);
                k = tmp[0];
                i |= (k & 0x7F) << j++ * 7;
                if (j > 5) throw new OverflowException("VarInt too big");
                if ((k & 0x80) != 128) break;
            }
            return i;
        }
        
        /// <summary>
        /// Read an integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The integer</returns>

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

        /// <summary>
        /// Build an integer for sending over the network
        /// </summary>
        /// <param name="paramInt">Integer to encode</param>
        /// <returns>Byte array for this integer</returns>

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

        /// <summary>
        /// Easily append several byte arrays
        /// </summary>
        /// <param name="bytes">Bytes to append</param>
        /// <returns>Array containing all the data</returns>

        private static byte[] concatBytes(params byte[][] bytes)
        {
            List<byte> result = new List<byte>();
            foreach (byte[] array in bytes)
                result.AddRange(array);
            return result.ToArray();
        }

        /// <summary>
        /// C-like atoi function for parsing an int from string
        /// </summary>
        /// <param name="str">String to parse</param>
        /// <returns>Int parsed</returns>

        private static int atoi(string str)
        {
            return int.Parse(new string(str.Trim().TakeWhile(char.IsDigit).ToArray()));
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
        /// Send a packet to the server, compression and encryption will be handled automatically
        /// </summary>
        /// <param name="packetID">packet ID</param>
        /// <param name="packetData">packet Data</param>

        private void SendPacket(int packetID, byte[] packetData)
        {
            //The inner packet
            byte[] the_packet = concatBytes(getVarInt(packetID), packetData);

            if (compression_treshold > 0) //Compression enabled?
            {
                if (the_packet.Length > compression_treshold) //Packet long enough for compressing?
                {
                    byte[] uncompressed_length = getVarInt(the_packet.Length);
                    byte[] compressed_packet = ZlibUtils.Compress(the_packet);
                    byte[] compressed_packet_length = getVarInt(compressed_packet.Length);
                    the_packet = concatBytes(compressed_packet_length, compressed_packet);
                }
                else
                {
                    byte[] uncompressed_length = getVarInt(0); //Not compressed (short packet)
                    the_packet = concatBytes(uncompressed_length, the_packet);
                }
            }

            SendRAW(concatBytes(getVarInt(the_packet.Length), the_packet)); 
        }

        /// <summary>
        /// Send raw data to the server. Encryption will be handled automatically.
        /// </summary>
        /// <param name="buffer">data to send</param>

        private void SendRAW(byte[] buffer)
        {
            if (encrypted)
            {
                s.Write(buffer, 0, buffer.Length);
            }
            else c.Client.Send(buffer);
        }

        /// <summary>
        /// Do the Minecraft login.
        /// </summary>
        /// <returns>True if login successful</returns>

        public bool Login()
        {
            byte[] protocol_version = getVarInt(protocolversion);
            byte[] server_adress_val = Encoding.UTF8.GetBytes(handler.getServerHost());
            byte[] server_adress_len = getVarInt(server_adress_val.Length);
            byte[] server_port = BitConverter.GetBytes((ushort)handler.getServerPort()); Array.Reverse(server_port);
            byte[] next_state = getVarInt(2);
            byte[] handshake_packet = concatBytes( protocol_version, server_adress_len, server_adress_val, server_port, next_state);

            SendPacket(0x00, handshake_packet);

            byte[] username_val = Encoding.UTF8.GetBytes(handler.getUsername());
            byte[] username_len = getVarInt(username_val.Length);
            byte[] login_packet = concatBytes(username_len, username_val);

            SendPacket(0x00, login_packet);

            int packetID = -1;
            byte[] packetData = new byte[] { };
            while (true)
            {
                readNextPacket(ref packetID, ref packetData);
                if (packetID == 0x00) //Login rejected
                {
                    handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected, ChatParser.ParseText(readNextString(ref packetData)));
                    return false;
                }
                else if (packetID == 0x01) //Encryption request
                {
                    string serverID = readNextString(ref packetData);
                    byte[] Serverkey = readNextByteArray(ref packetData);
                    byte[] token = readNextByteArray(ref packetData);
                    return StartEncryption(handler.getUserUUID(), handler.getSessionID(), token, serverID, Serverkey);
                }
                else if (packetID == 0x02) //Login successful
                {
                    ConsoleIO.WriteLineFormatted("§8Server is in offline mode.");
                    login_phase = false;
                    StartUpdating();
                    return true; //No need to check session or start encryption
                }
                else handlePacket(packetID, packetData);
            }
        }

        /// <summary>
        /// Start network encryption. Automatically called by Login() if the server requests encryption.
        /// </summary>
        /// <returns>True if encryption was successful</returns>

        private bool StartEncryption(string uuid, string sessionID, byte[] token, string serverIDhash, byte[] serverKey)
        {
            System.Security.Cryptography.RSACryptoServiceProvider RSAService = CryptoHandler.DecodeRSAPublicKey(serverKey);
            byte[] secretKey = CryptoHandler.GenerateAESPrivateKey();

            ConsoleIO.WriteLineFormatted("§8Crypto keys & hash generated.");

            if (serverIDhash != "-")
            {
                Console.WriteLine("Checking Session...");
                if (!ProtocolHandler.SessionCheck(uuid, sessionID, CryptoHandler.getServerHash(serverIDhash, serverKey, secretKey)))
                {
                    handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected, "Failed to check session.");
                    return false;
                }
            }

            //Encrypt the data
            byte[] key_enc = RSAService.Encrypt(secretKey, false);
            byte[] token_enc = RSAService.Encrypt(token, false);
            byte[] key_len = getVarInt(key_enc.Length);
            byte[] token_len = getVarInt(token_enc.Length);

            //Encryption Response packet
            SendPacket(0x01, concatBytes(key_len, key_enc, token_len, token_enc));

            //Start client-side encryption
            s = CryptoHandler.getAesStream(c.GetStream(), secretKey, this);
            encrypted = true;

            //Process the next packet
            int packetID = -1;
            byte[] packetData = new byte[] { };
            while (true)
            {
                readNextPacket(ref packetID, ref packetData);
                if (packetID == 0x00) //Login rejected
                {
                    handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected, ChatParser.ParseText(readNextString(ref packetData)));
                    return false;
                }
                else if (packetID == 0x02) //Login successful
                {
                    login_phase = false;
                    StartUpdating();
                    return true;
                }
                else handlePacket(packetID, packetData);
            }
        }

        /// <summary>
        /// Useless padding packet for solving Mono issue.
        /// </summary>
        /// <returns>The padding packet</returns>

        public byte[] getPaddingPacket()
        {
            //Will generate a 15-bytes long padding packet
            byte[] compression = compression_treshold >= 0 ? getVarInt(0) : new byte[] { };
            byte[] id = getVarInt(0x17); //Plugin Message
            byte[] channel_name = Encoding.UTF8.GetBytes("MCC|Pad");
            byte[] channel_name_len = getVarInt(channel_name.Length);
            byte[] data = compression_treshold >= 0 ? new byte[] { 0x00, 0x00, 0x00 } : new byte[] { 0x00, 0x00, 0x00, 0x00 };
            byte[] data_len = getVarInt(data.Length);
            byte[] packet_data = concatBytes(compression, id, channel_name_len, channel_name, data_len, data);
            byte[] packet_length = getVarInt(packet_data.Length);
            return concatBytes(packet_length, packet_data);
        }

        /// <summary>
        /// Send a chat message to the server
        /// </summary>
        /// <param name="message">Message</param>
        /// <returns>True if properly sent</returns>

        public bool SendChatMessage(string message)
        {
            if (String.IsNullOrEmpty(message))
                return true;
            try
            {
                byte[] message_val = Encoding.UTF8.GetBytes(message);
                byte[] message_len = getVarInt(message_val.Length);
                byte[] message_packet = concatBytes(message_len, message_val);
                SendPacket(0x01, message_packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
        }

        /// <summary>
        /// Send a respawn packet to the server
        /// </summary>
        /// <param name="message">Message</param>
        /// <returns>True if properly sent</returns>

        public bool SendRespawnPacket()
        {
            try
            {
                SendPacket(0x16, new byte[] { 0 });
                return true;
            }
            catch (SocketException) { return false; }
        }

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        /// <param name="message">Optional disconnect reason</param>

        public void Disconnect()
        {
            try
            {
                byte[] message_val = Encoding.UTF8.GetBytes("\"disconnect.quitting\"");
                byte[] message_len = getVarInt(message_val.Length);
                byte[] disconnect_packet = concatBytes(message_len, message_val);
                SendPacket(0x40, disconnect_packet);
            }
            catch (SocketException) { }
            catch (System.IO.IOException) { }
            catch (NullReferenceException) { }
            catch (ObjectDisposedException) { }
        }

        /// <summary>
        /// Autocomplete text while typing username or command
        /// </summary>
        /// <param name="BehindCursor">Text behind cursor</param>
        /// <returns>Completed text</returns>

        public string AutoComplete(string BehindCursor)
        {
            if (String.IsNullOrEmpty(BehindCursor))
                return "";

            byte[] tocomplete_val = Encoding.UTF8.GetBytes(BehindCursor);
            byte[] tocomplete_len = getVarInt(tocomplete_val.Length);
            byte[] has_position = new byte[] { 0x00 }; //false, no position sent
            byte[] tabcomplete_packet = concatBytes(tocomplete_len, tocomplete_val, has_position);

            autocomplete_received = false;
            autocomplete_result = BehindCursor;
            SendPacket(0x14, tabcomplete_packet);

            int wait_left = 50; //do not wait more than 5 seconds (50 * 100 ms)
            while (wait_left > 0 && !autocomplete_received) { System.Threading.Thread.Sleep(100); wait_left--; }
            return autocomplete_result;
        }
    }
}
