using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using MinecraftClient.Crypto;
using MinecraftClient.Proxy;
using System.Security.Cryptography;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Implementation for Minecraft 1.7.X and 1.8.X Protocols
    /// </summary>

    class Protocol18Handler : IMinecraftCom
    {
        private const int MC18Version = 47;
        
        private int compression_treshold = 0;
        private bool autocomplete_received = false;
        private string autocomplete_result = "";
        private bool login_phase = true;
        private bool encrypted = false;
        private int protocolversion;

        IMinecraftComHandler handler;
        Thread netRead;
        IAesStream s;
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
            catch (NullReferenceException) { return false; }
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

            //Handle packet decompression
            if (protocolversion >= MC18Version
                && compression_treshold > 0)
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
                        if (protocolversion >= MC18Version)
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
                    case 0x00: //Keep-Alive
                        SendPacket(0x00, packetData);
                        break;
                    case 0x01: //Join game
                        handler.OnGameJoined();
                        break;
                    case 0x02: //Chat message
                        string message = readNextString(ref packetData);
                        try
                        {
                            //Hide system messages or xp bar messages?
                            byte messageType = readData(1, ref packetData)[0];
                            if ((messageType == 1 && !Settings.DisplaySystemMessages)
                                || (messageType == 2 && !Settings.DisplayXPBarMessages))
                                break;
                        }
                        catch (IndexOutOfRangeException) { /* No message type */ }
                        handler.OnTextReceived(ChatParser.ParseText(message));
                        break;
                    case 0x38: //Player List update
                        if (protocolversion >= MC18Version)
                        {
                            int action = readNextVarInt(ref packetData);
                            int numActions = readNextVarInt(ref packetData);
                            for (int i = 0; i < numActions; i++)
                            {
                                Guid uuid = readNextUUID(ref packetData);
                                switch (action)
                                {
                                    case 0x00: //Player Join
                                        string name = readNextString(ref packetData);
                                        handler.OnPlayerJoin(uuid, name);
                                        break;
                                    case 0x04: //Player Leave
                                        handler.OnPlayerLeave(uuid);
                                        break;
                                    default:
                                        //Unknown player list item type
                                        break;
                                }
                            }
                        }
                        else //MC 1.7.X does not provide UUID in tab-list updates
                        {
                            string name = readNextString(ref packetData);
                            bool online = readNextBool(ref packetData);
                            short ping = readNextShort(ref packetData);
                            Guid FakeUUID = new Guid(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(name)).Take(16).ToArray());
                            if (online)
                                handler.OnPlayerJoin(FakeUUID, name);
                            else handler.OnPlayerLeave(FakeUUID);
                        }
                        break;
                    case 0x3A: //Tab-Complete Result
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
                    case 0x40: //Kick Packet
                        handler.OnConnectionLost(ChatBot.DisconnectReason.InGameKick, ChatParser.ParseText(readNextString(ref packetData)));
                        return false;
                    case 0x46: //Network Compression Treshold Info
                        if (protocolversion >= MC18Version)
                            compression_treshold = readNextVarInt(ref packetData);
                        break;
                    case 0x48: //Resource Pack Send
                        string url = readNextString(ref packetData);
                        string hash = readNextString(ref packetData);
                        //Send back "accepted" and "successfully loaded" responses for plugins making use of resource pack mandatory
                        SendPacket(0x19, concatBytes(getVarInt(hash.Length), Encoding.UTF8.GetBytes(hash), getVarInt(3)));
                        SendPacket(0x19, concatBytes(getVarInt(hash.Length), Encoding.UTF8.GetBytes(hash), getVarInt(0)));
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

        private static byte[] readData(int offset, ref byte[] cache)
        {
            byte[] result = cache.Take(offset).ToArray();
            cache = cache.Skip(offset).ToArray();
            return result;
        }

        /// <summary>
        /// Read a string from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The string</returns>

        private static string readNextString(ref byte[] cache)
        {
            int length = readNextVarInt(ref cache);
            if (length > 0)
            {
                return Encoding.UTF8.GetString(readData(length, ref cache));
            }
            else return "";
        }

        /// <summary>
        /// Read a boolean from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The boolean value</returns>

        private static bool readNextBool(ref byte[] cache)
        {
            return readData(1, ref cache)[0] != 0x00;
        }

        /// <summary>
        /// Read a short integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The short integer value</returns>

        private static short readNextShort(ref byte[] cache)
        {
            byte[] rawValue = readData(2, ref cache);
            Array.Reverse(rawValue); //Endianness
            return BitConverter.ToInt16(rawValue, 0);
        }

        /// <summary>
        /// Read a uuid from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The uuid</returns>

        private static Guid readNextUUID(ref byte[] cache)
        {
            return new Guid(readData(16, ref cache));
        }

        /// <summary>
        /// Read a byte array from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The byte array</returns>

        private byte[] readNextByteArray(ref byte[] cache)
        {
            int len = protocolversion >= MC18Version
                ? readNextVarInt(ref cache)
                : readNextShort(ref cache);
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

        private static int readNextVarInt(ref byte[] cache)
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
        /// Get byte array with length information prepended to it
        /// </summary>
        /// <param name="array">Array to process</param>
        /// <returns>Array ready to send</returns>

        private byte[] getArray(byte[] array)
        {
            if (protocolversion < MC18Version)
            {
                byte[] length = BitConverter.GetBytes((short)array.Length);
                Array.Reverse(length);
                return concatBytes(length, array);
            }
            else return concatBytes(getVarInt(array.Length), array);
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
            byte[] server_adress_val = Encoding.UTF8.GetBytes(handler.GetServerHost() + "\0FML\0");
            byte[] server_adress_len = getVarInt(server_adress_val.Length);
            byte[] server_port = BitConverter.GetBytes((ushort)handler.GetServerPort()); Array.Reverse(server_port);
            byte[] next_state = getVarInt(2);
            byte[] handshake_packet = concatBytes(protocol_version, server_adress_len, server_adress_val, server_port, next_state);

            SendPacket(0x00, handshake_packet);

            byte[] username_val = Encoding.UTF8.GetBytes(handler.GetUsername());
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
                    return StartEncryption(handler.GetUserUUID(), handler.GetSessionID(), token, serverID, Serverkey);
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
            byte[] key_enc = getArray(RSAService.Encrypt(secretKey, false));
            byte[] token_enc = getArray(RSAService.Encrypt(token, false));

            //Encryption Response packet
            SendPacket(0x01, concatBytes(key_enc, token_enc));

            //Start client-side encryption
            s = CryptoHandler.getAesStream(c.GetStream(), secretKey);
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
        /// Tell the server what client is being used to connect to the server
        /// </summary>
        /// <param name="brandInfo">Client string describing the client</param>
        /// <returns>True if brand info was successfully sent</returns>

        public bool SendBrandInfo(string brandInfo)
        {
            if (String.IsNullOrEmpty(brandInfo))
                return false;
            try
            {
                byte[] channel = Encoding.UTF8.GetBytes("MC|Brand");
                byte[] channelLen = getVarInt(channel.Length);
                byte[] brand = Encoding.UTF8.GetBytes(brandInfo);
                byte[] brandLen = getVarInt(brand.Length);
                SendPacket(0x17, concatBytes(channelLen, channel, brandLen, brand));
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
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
            byte[] has_position = new byte[] { 0x00 };
            byte[] tabcomplete_packet = protocolversion >= MC18Version
                ? concatBytes(tocomplete_len, tocomplete_val, has_position)
                : concatBytes(tocomplete_len, tocomplete_val);

            autocomplete_received = false;
            autocomplete_result = BehindCursor;
            SendPacket(0x14, tabcomplete_packet);

            int wait_left = 50; //do not wait more than 5 seconds (50 * 100 ms)
            while (wait_left > 0 && !autocomplete_received) { System.Threading.Thread.Sleep(100); wait_left--; }
            return autocomplete_result;
        }

        /// <summary>
        /// Ping a Minecraft server to get information about the server
        /// </summary>
        /// <returns>True if ping was successful</returns>

        public static bool doPing(string host, int port, ref int protocolversion)
        {
            string version = "";
            TcpClient tcp = ProxyHandler.newTcpClient(host, port);
            tcp.ReceiveBufferSize = 1024 * 1024;

            byte[] packet_id = getVarInt(0);
            byte[] protocol_version = getVarInt(4);
            byte[] server_adress_val = Encoding.UTF8.GetBytes(host);
            byte[] server_adress_len = getVarInt(server_adress_val.Length);
            byte[] server_port = BitConverter.GetBytes((ushort)port); Array.Reverse(server_port);
            byte[] next_state = getVarInt(1);
            byte[] packet = concatBytes(packet_id, protocol_version, server_adress_len, server_adress_val, server_port, next_state);
            byte[] tosend = concatBytes(getVarInt(packet.Length), packet);

            tcp.Client.Send(tosend, SocketFlags.None);

            byte[] status_request = getVarInt(0);
            byte[] request_packet = concatBytes(getVarInt(status_request.Length), status_request);

            tcp.Client.Send(request_packet, SocketFlags.None);

            Protocol18Handler ComTmp = new Protocol18Handler(tcp);
            int packetLength = ComTmp.readNextVarIntRAW();
            if (packetLength > 0) //Read Response length
            {
                byte[] packetData = ComTmp.readDataRAW(packetLength);
                if (readNextVarInt(ref packetData) == 0x00) //Read Packet ID
                {
                    string result = readNextString(ref packetData); //Get the Json data

                    if (!String.IsNullOrEmpty(result) && result.StartsWith("{") && result.EndsWith("}"))
                    {
                        Json.JSONData jsonData = Json.ParseJson(result);
                        if (jsonData.Type == Json.JSONData.DataType.Object && jsonData.Properties.ContainsKey("version"))
                        {
                            jsonData = jsonData.Properties["version"];

                            //Retrieve display name of the Minecraft version
                            if (jsonData.Properties.ContainsKey("name"))
                                version = jsonData.Properties["name"].StringValue;

                            //Retrieve protocol version number for handling this server
                            if (jsonData.Properties.ContainsKey("protocol"))
                                protocolversion = atoi(jsonData.Properties["protocol"].StringValue);

                            //Automatic fix for BungeeCord 1.8 reporting itself as 1.7...
                            if (protocolversion < 47 && version.Split(' ', '/').Contains("1.8"))
                                protocolversion = ProtocolHandler.MCVer2ProtocolVersion("1.8.0");

                            ConsoleIO.WriteLineFormatted("§8Server version : " + version + " (protocol v" + protocolversion + ").");
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
