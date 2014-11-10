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
    /// Implementation for Minecraft 1.7.X Protocol
    /// </summary>

    class Protocol17Handler : IMinecraftCom
    {
        IMinecraftComHandler handler;
        private bool autocomplete_received = false;
        private string autocomplete_result = "";
        private bool encrypted = false;
        private int protocolversion;
        private Thread netRead;
        Crypto.IAesStream s;
        TcpClient c;

        public Protocol17Handler(TcpClient Client, int ProtocolVersion, IMinecraftComHandler Handler)
        {
            ConsoleIO.SetAutoCompleteEngine(this);
            ChatParser.InitTranslations();
            this.c = Client;
            this.protocolversion = ProtocolVersion;
            this.handler = Handler;
        }

        private Protocol17Handler(TcpClient Client)
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
                        Send(getPaddingPacket());
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
        /// Read and data from the network. Should be called on a separate thread.
        /// </summary>
        /// <returns></returns>

        private bool Update()
        {
            handler.OnUpdate();
            if (c.Client == null || !c.Connected) { return false; }
            int id = 0, size = 0;
            try
            {
                while (c.Client.Available > 0)
                {
                    size = readNextVarInt(); //Packet size
                    id = readNextVarInt(); //Packet ID

                    switch (id)
                    {
                        case 0x00:
                            byte[] keepalive = new byte[4] { 0, 0, 0, 0 };
                            Receive(keepalive, 0, 4, SocketFlags.None);
                            byte[] keepalive_packet = concatBytes(getVarInt(0x00), keepalive);
                            byte[] keepalive_tosend = concatBytes(getVarInt(keepalive_packet.Length), keepalive_packet);
                            Send(keepalive_tosend);
                            break;
                        case 0x02:
                            handler.OnTextReceived(ChatParser.ParseText(readNextString()));
                            break;
                        case 0x38:
                            string name = readNextString();
                            bool online = readNextBool();
                            short ping = readNextShort();
                            Guid FakeUUID = new Guid(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(name)).Take(16).ToArray());
                            if (online)
                            {
                                handler.OnPlayerJoin(FakeUUID, name);
                            }
                            else handler.OnPlayerLeave(FakeUUID);
                            break;
                        case 0x3A:
                            int autocomplete_count = readNextVarInt();
                            string tab_list = "";
                            for (int i = 0; i < autocomplete_count; i++)
                            {
                                autocomplete_result = readNextString();
                                if (autocomplete_result != "")
                                    tab_list = tab_list + autocomplete_result + " ";
                            }
                            autocomplete_received = true;
                            tab_list = tab_list.Trim();
                            if (tab_list.Length > 0)
                                ConsoleIO.WriteLineFormatted("§8" + tab_list, false);
                            break;
                        case 0x40:
                            handler.OnConnectionLost(ChatBot.DisconnectReason.InGameKick, ChatParser.ParseText(readNextString()));
                            return false;
                        default:
                            readData(size - getVarInt(id).Length); //Skip packet
                            break;
                    }
                }
            }
            catch (SocketException) { return false; }
            return true;
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
        /// Read some data and discard the result
        /// </summary>
        /// <param name="offset">Amount of bytes to read</param>
        
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

        /// <summary>
        /// Read a string from the network
        /// </summary>
        /// <returns>The string</returns>

        private string readNextString()
        {
            int length = readNextVarInt();
            if (length > 0)
            {
                byte[] cache = new byte[length];
                Receive(cache, 0, length, SocketFlags.None);
                return Encoding.UTF8.GetString(cache);
            }
            else return "";
        }

        /// <summary>
        /// Read a uuid from the network
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The uuid</returns>

        private Guid readNextUUID()
        {
            byte[] cache = new byte[16];
            Receive(cache, 0, 16, SocketFlags.None);
            return new Guid(cache);
        }

        /// <summary>
        /// Read a short from the network
        /// </summary>
        /// <returns></returns>

        private short readNextShort()
        {
            byte[] tmp = new byte[2];
            Receive(tmp, 0, 2, SocketFlags.None);
            Array.Reverse(tmp);
            return BitConverter.ToInt16(tmp, 0);
        }

        /// <summary>
        /// Read a boolean from the network
        /// </summary>
        /// <returns></returns>

        private bool readNextBool()
        {
            byte[] tmp = new byte[1];
            Receive(tmp, 0, 1, SocketFlags.None);
            return tmp[0] != 0x00;
        }

        /// <summary>
        /// Read a byte array from the network
        /// </summary>
        /// <returns>The byte array</returns>

        private byte[] readNextByteArray()
        {
            byte[] tmp = new byte[2];
            Receive(tmp, 0, 2, SocketFlags.None);
            Array.Reverse(tmp);
            short len = BitConverter.ToInt16(tmp, 0);
            byte[] data = new byte[len];
            Receive(data, 0, len, SocketFlags.None);
            return data;
        }

        /// <summary>
        /// Read an integer from the network
        /// </summary>
        /// <returns>The integer</returns>

        private int readNextVarInt()
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
        /// Network sending method. Send bytes using the socket or encrypted socket.
        /// </summary>
        /// <param name="buffer"></param>

        private void Send(byte[] buffer)
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
            byte[] packet_id = getVarInt(0);
            byte[] protocol_version = getVarInt(protocolversion);
            byte[] server_adress_val = Encoding.UTF8.GetBytes(handler.getServerHost());
            byte[] server_adress_len = getVarInt(server_adress_val.Length);
            byte[] server_port = BitConverter.GetBytes((ushort)handler.getServerPort()); Array.Reverse(server_port);
            byte[] next_state = getVarInt(2);
            byte[] handshake_packet = concatBytes(packet_id, protocol_version, server_adress_len, server_adress_val, server_port, next_state);
            byte[] handshake_packet_tosend = concatBytes(getVarInt(handshake_packet.Length), handshake_packet);

            Send(handshake_packet_tosend);

            byte[] username_val = Encoding.UTF8.GetBytes(handler.getUsername());
            byte[] username_len = getVarInt(username_val.Length);
            byte[] login_packet = concatBytes(packet_id, username_len, username_val);
            byte[] login_packet_tosend = concatBytes(getVarInt(login_packet.Length), login_packet);

            Send(login_packet_tosend);

            readNextVarInt(); //Packet size
            int pid = readNextVarInt(); //Packet ID
            if (pid == 0x00) //Login rejected
            {
                handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected, ChatParser.ParseText(readNextString()));
                return false;
            }
            else if (pid == 0x01) //Encryption request
            {
                string serverID = readNextString();
                byte[] Serverkey = readNextByteArray();
                byte[] token = readNextByteArray();
                return StartEncryption(handler.getUserUUID(), handler.getSessionID(), token, serverID, Serverkey);
            }
            else if (pid == 0x02) //Login successful
            {
                ConsoleIO.WriteLineFormatted("§8Server is in offline mode.");
                StartUpdating();
                return true; //No need to check session or start encryption
            }
            else return false;
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
            byte[] key_len = BitConverter.GetBytes((short)key_enc.Length); Array.Reverse(key_len);
            byte[] token_len = BitConverter.GetBytes((short)token_enc.Length); Array.Reverse(token_len);

            //Encryption Response packet
            byte[] packet_id = getVarInt(0x01);
            byte[] encryption_response = concatBytes(packet_id, key_len, key_enc, token_len, token_enc);
            byte[] encryption_response_tosend = concatBytes(getVarInt(encryption_response.Length), encryption_response);
            Send(encryption_response_tosend);

            //Start client-side encryption
            s = CryptoHandler.getAesStream(c.GetStream(), secretKey, this);
            encrypted = true;

            //Read and skip the next packet
            int received_packet_size = readNextVarInt();
            int received_packet_id = readNextVarInt();
            bool encryption_success = (received_packet_id == 0x02);
            if (received_packet_id == 0) { handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected, ChatParser.ParseText(readNextString())); }
            else readData(received_packet_size - getVarInt(received_packet_id).Length);
            if (encryption_success) { StartUpdating(); }
            return encryption_success;
        }

        /// <summary>
        /// Useless padding packet for solving Mono issue.
        /// </summary>
        /// <returns>The padding packet</returns>

        public byte[] getPaddingPacket()
        {
            //Will generate a 15-bytes long padding packet
            byte[] id = getVarInt(0x17); //Plugin Message
            byte[] channel_name = Encoding.UTF8.GetBytes("MCC|Pad");
            byte[] channel_name_len = getVarInt(channel_name.Length);
            byte[] data = new byte[] { 0x00, 0x00, 0x00 };
            byte[] data_len = BitConverter.GetBytes((short)data.Length); Array.Reverse(data_len);
            byte[] packet_data = concatBytes(id, channel_name_len, channel_name, data_len, data);
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
                byte[] packet_id = getVarInt(0x01);
                byte[] message_val = Encoding.UTF8.GetBytes(message);
                byte[] message_len = getVarInt(message_val.Length);
                byte[] message_packet = concatBytes(packet_id, message_len, message_val);
                byte[] message_packet_tosend = concatBytes(getVarInt(message_packet.Length), message_packet);
                Send(message_packet_tosend);
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
                byte[] packet_id = getVarInt(0x16);
                byte[] action_id = new byte[] { 0 };
                byte[] respawn_packet = concatBytes(getVarInt(packet_id.Length + 1), packet_id, action_id);
                Send(respawn_packet);
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
                byte[] packet_id = getVarInt(0x40);
                byte[] message_val = Encoding.UTF8.GetBytes("\"disconnect.quitting\"");
                byte[] message_len = getVarInt(message_val.Length);
                byte[] disconnect_packet = concatBytes(packet_id, message_len, message_val);
                byte[] disconnect_packet_tosend = concatBytes(getVarInt(disconnect_packet.Length), disconnect_packet);
                Send(disconnect_packet_tosend);
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

            byte[] packet_id = getVarInt(0x14);
            byte[] tocomplete_val = Encoding.UTF8.GetBytes(BehindCursor);
            byte[] tocomplete_len = getVarInt(tocomplete_val.Length);
            byte[] tabcomplete_packet = concatBytes(packet_id, tocomplete_len, tocomplete_val);
            byte[] tabcomplete_packet_tosend = concatBytes(getVarInt(tabcomplete_packet.Length), tabcomplete_packet);

            autocomplete_received = false;
            autocomplete_result = BehindCursor;
            Send(tabcomplete_packet_tosend);

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

            Protocol17Handler ComTmp = new Protocol17Handler(tcp);
            if (ComTmp.readNextVarInt() > 0) //Read Response length
            {
                if (ComTmp.readNextVarInt() == 0x00) //Read Packet ID
                {
                    string result = ComTmp.readNextString(); //Get the Json data
                    if (result[0] == '{' && result.Contains("protocol\":") && result.Contains("name\":\""))
                    {
                        string[] tmp_ver = result.Split(new string[] { "protocol\":" }, StringSplitOptions.None);
                        string[] tmp_name = result.Split(new string[] { "name\":\"" }, StringSplitOptions.None);

                        if (tmp_ver.Length >= 2 && tmp_name.Length >= 2)
                        {
                            protocolversion = atoi(tmp_ver[1]);

                            // Handle if "name" exists twice, like when connecting to a server with another user logged in.
                            version = (tmp_name.Length == 2) ? tmp_name[1].Split('"')[0] : tmp_name[2].Split('"')[0];
                            if (result.Contains("modinfo\":"))
                            {
                                //Server is running Forge (which is not supported)
                                version = "Forge " + version;
                                protocolversion = 0;
                            }
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
