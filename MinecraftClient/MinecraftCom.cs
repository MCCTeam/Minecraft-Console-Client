using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace MinecraftClient
{
    /// <summary>
    /// The class containing all the core functions needed to communicate with a Minecraft server.
    /// </summary>

    public class MinecraftCom : IAutoComplete
    {
        #region Login to Minecraft.net and get a new session ID

        public enum LoginResult { Error, Success, WrongPassword, Blocked, AccountMigrated, NotPremium };

        /// <summary>
        /// Allows to login to a premium Minecraft account using the Yggdrasil authentication scheme.
        /// </summary>
        /// <param name="user">Login</param>
        /// <param name="pass">Password</param>
        /// <param name="accesstoken">Will contain the access token returned by Minecraft.net, if the login is successful</param>
        /// <param name="uuid">Will contain the player's UUID, needed for multiplayer</param>
        /// <returns>Returns the status of the login (Success, Failure, etc.)</returns>

        public static LoginResult GetLogin(ref string user, string pass, ref string accesstoken, ref string uuid)
        {
            try
            {
                WebClient wClient = new WebClient();
                wClient.Headers.Add("Content-Type: application/json");
                string json_request = "{\"agent\": { \"name\": \"Minecraft\", \"version\": 1 }, \"username\": \"" + user + "\", \"password\": \"" + pass + "\" }";
                string result = wClient.UploadString("https://authserver.mojang.com/authenticate", json_request);
                if (result.Contains("availableProfiles\":[]}"))
                {
                    return LoginResult.NotPremium;
                }
                else
                {
                    string[] temp = result.Split(new string[] { "accessToken\":\"" }, StringSplitOptions.RemoveEmptyEntries);
                    if (temp.Length >= 2) { accesstoken = temp[1].Split('"')[0]; }
                    temp = result.Split(new string[] { "name\":\"" }, StringSplitOptions.RemoveEmptyEntries);
                    if (temp.Length >= 2) { user = temp[1].Split('"')[0]; }
                    temp = result.Split(new string[] { "availableProfiles\":[{\"id\":\"" }, StringSplitOptions.RemoveEmptyEntries);
                    if (temp.Length >= 2) { uuid = temp[1].Split('"')[0]; }
                    return LoginResult.Success;
                }
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError)
                {
                    HttpWebResponse response = (HttpWebResponse)e.Response;
                    if ((int)response.StatusCode == 403)
                    {
                        using (System.IO.StreamReader sr = new System.IO.StreamReader(response.GetResponseStream()))
                        {
                            string result = sr.ReadToEnd();
                            if (result.Contains("UserMigratedException"))
                            {
                                return LoginResult.AccountMigrated;
                            }
                            else return LoginResult.WrongPassword;
                        }
                    }
                    else return LoginResult.Blocked;
                }
                else return LoginResult.Error;
            }
        }

        #endregion

        #region Session checking when joining a server in online mode

        /// <summary>
        /// Check session using the Yggdrasil authentication scheme. Allow to join an online-mode server
        /// </summary>
        /// <param name="user">Username</param>
        /// <param name="accesstoken">Session ID</param>
        /// <param name="serverhash">Server ID</param>
        /// <returns>TRUE if session was successfully checked</returns>

        public static bool SessionCheck(string uuid, string accesstoken, string serverhash)
        {
            try
            {
                WebClient wClient = new WebClient();
                wClient.Headers.Add("Content-Type: application/json");
                string json_request = "{\"accessToken\":\"" + accesstoken + "\",\"selectedProfile\":\"" + uuid + "\",\"serverId\":\"" + serverhash + "\"}";
                return (wClient.UploadString("https://sessionserver.mojang.com/session/minecraft/join", json_request) == "");
            }
            catch (WebException) { return false; }
        }

        #endregion

        TcpClient c = new TcpClient();
        Crypto.AesStream s;

        public bool HasBeenKicked { get { return connectionlost; } }
        bool connectionlost = false;
        bool encrypted = false;
        int protocolversion;

        public bool Update()
        {
            for (int i = 0; i < bots.Count; i++) { bots[i].Update(); }
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
                            string message = readNextString();
                            //printstring("§8" + message, false); //Debug : Show the RAW JSON data
                            message = ChatParser.ParseText(message);
                            printstring(message, false);
                            for (int i = 0; i < bots.Count; i++) { bots[i].GetText(message); } break;
                        case 0x37:
                            int stats_count = readNextVarInt();
                            for (int i = 0; i < stats_count; i++)
                            {
                                string stat_name = readNextString();
                                readNextVarInt(); //stat value
                                if (stat_name == "stat.deaths")
                                    printstring("You are dead. Type /reco to respawn & reconnect.", false);
                            }
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
                                printstring("§8" + tab_list, false);
                            break;
                        case 0x40: string reason = ChatParser.ParseText(readNextString());
                            ConsoleIO.WriteLine("Disconnected by Server :");
                            printstring(reason, true);
                            connectionlost = true;
                            for (int i = 0; i < bots.Count; i++)
                                bots[i].OnDisconnect(ChatBot.DisconnectReason.InGameKick, reason);
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
        public void DebugDump()
        {
            byte[] cache = new byte[128000];
            Receive(cache, 0, 128000, SocketFlags.None);
            string dump = BitConverter.ToString(cache);
            System.IO.File.WriteAllText("debug.txt", dump);
            System.Diagnostics.Process.Start("debug.txt");
        }
        public bool OnConnectionLost()
        {
            if (!connectionlost)
            {
                connectionlost = true;
                for (int i = 0; i < bots.Count; i++)
                {
                    if (bots[i].OnDisconnect(ChatBot.DisconnectReason.ConnectionLost, "Connection has been lost."))
                    {
                        return true; //The client is about to restart
                    }
                }
            }
            return false;
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
            int length = readNextVarInt();
            if (length > 0)
            {
                byte[] cache = new byte[length];
                Receive(cache, 0, length, SocketFlags.None);
                string result = Encoding.UTF8.GetString(cache);
                return result;
            }
            else return "";
        }
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
        private static int atoi(string str)
        {
            return Int32.Parse(Regex.Match(str, @"\d+").Value);
        }

        private static void setcolor(char c)
        {
            switch (c)
            {
                case '0': Console.ForegroundColor = ConsoleColor.Gray; break; //Should be Black but Black is non-readable on a black background
                case '1': Console.ForegroundColor = ConsoleColor.DarkBlue; break;
                case '2': Console.ForegroundColor = ConsoleColor.DarkGreen; break;
                case '3': Console.ForegroundColor = ConsoleColor.DarkCyan; break;
                case '4': Console.ForegroundColor = ConsoleColor.DarkRed; break;
                case '5': Console.ForegroundColor = ConsoleColor.DarkMagenta; break;
                case '6': Console.ForegroundColor = ConsoleColor.DarkYellow; break;
                case '7': Console.ForegroundColor = ConsoleColor.Gray; break;
                case '8': Console.ForegroundColor = ConsoleColor.DarkGray; break;
                case '9': Console.ForegroundColor = ConsoleColor.Blue; break;
                case 'a': Console.ForegroundColor = ConsoleColor.Green; break;
                case 'b': Console.ForegroundColor = ConsoleColor.Cyan; break;
                case 'c': Console.ForegroundColor = ConsoleColor.Red; break;
                case 'd': Console.ForegroundColor = ConsoleColor.Magenta; break;
                case 'e': Console.ForegroundColor = ConsoleColor.Yellow; break;
                case 'f': Console.ForegroundColor = ConsoleColor.White; break;
                case 'r': Console.ForegroundColor = ConsoleColor.White; break;
            }
        }
        private static void printstring(string str, bool acceptnewlines)
        {
            if (!String.IsNullOrEmpty(str))
            {
                if (!acceptnewlines) { str = str.Replace('\n', ' '); }
                if (ConsoleIO.basicIO) { ConsoleIO.WriteLine(str); return; }
                string[] subs = str.Split(new char[] { '§' });
                if (subs[0].Length > 0) { ConsoleIO.Write(subs[0]); }
                for (int i = 1; i < subs.Length; i++)
                {
                    if (subs[i].Length > 0)
                    {
                        setcolor(subs[i][0]);
                        if (subs[i].Length > 1)
                        {
                            ConsoleIO.Write(subs[i].Substring(1, subs[i].Length - 1));
                        }
                    }
                }
                ConsoleIO.Write('\n');
            }
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        private bool autocomplete_received = false;
        private string autocomplete_result = "";
        public string AutoComplete(string behindcursor)
        {
            if (String.IsNullOrEmpty(behindcursor))
                return "";

            byte[] packet_id = getVarInt(0x14);
            byte[] tocomplete_val = Encoding.UTF8.GetBytes(behindcursor);
            byte[] tocomplete_len = getVarInt(tocomplete_val.Length);
            byte[] tabcomplete_packet = concatBytes(packet_id, tocomplete_len, tocomplete_val);
            byte[] tabcomplete_packet_tosend = concatBytes(getVarInt(tabcomplete_packet.Length), tabcomplete_packet);

            autocomplete_received = false;
            autocomplete_result = behindcursor;
            Send(tabcomplete_packet_tosend);

            int wait_left = 50; //do not wait more than 5 seconds (50 * 100 ms)
            while (wait_left > 0 && !autocomplete_received) { System.Threading.Thread.Sleep(100); wait_left--; }
            return autocomplete_result;
        }

        public void setVersion(int ver) { protocolversion = ver; }
        public void setClient(TcpClient n) { c = n; }
        private void setEncryptedClient(Crypto.AesStream n) { s = n; encrypted = true; }
        private void Receive(byte[] buffer, int start, int offset, SocketFlags f)
        {
            while (c.Client.Available < start + offset) { }
            if (encrypted)
            {
                s.Read(buffer, start, offset);
            }
            else c.Client.Receive(buffer, start, offset, f);
        }
        private void Send(byte[] buffer)
        {
            if (encrypted)
            {
                s.Write(buffer, 0, buffer.Length);
            }
            else c.Client.Send(buffer);
        }

        public static bool GetServerInfo(string serverIP, ref int protocolversion, ref string version)
        {
            try
            {
                string host; int port;
                string[] sip = serverIP.Split(':');
                host = sip[0];

                if (sip.Length == 1)
                {
                    port = 25565;
                }
                else
                {
                    try
                    {
                        port = Convert.ToInt32(sip[1]);
                    }
                    catch (FormatException) { port = 25565; }
                }

                TcpClient tcp = new TcpClient(host, port);
                
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

                MinecraftCom ComTmp = new MinecraftCom();
                ComTmp.setClient(tcp);
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
                                version = tmp_name[1].Split('"')[0];
                                Console.ForegroundColor = ConsoleColor.DarkGray;
                                //Console.WriteLine(result); //Debug: show the full Json string
                                Console.WriteLine("Server version : " + version + " (protocol v" + protocolversion + ").");
                                Console.ForegroundColor = ConsoleColor.Gray;
                                return true;
                            }
                        }
                    }
                }
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("Unexpected answer from the server (is that a MC 1.7+ server ?)");
                Console.ForegroundColor = ConsoleColor.Gray;
                return false;
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("An error occured while attempting to connect to this IP.");
                Console.ForegroundColor = ConsoleColor.Gray;
                return false;
            }
        }
        public bool Login(string username, string uuid, string sessionID, string host, int port)
        {
            byte[] packet_id = getVarInt(0);
            byte[] protocol_version = getVarInt(4);
            byte[] server_adress_val = Encoding.UTF8.GetBytes(host);
            byte[] server_adress_len = getVarInt(server_adress_val.Length);
            byte[] server_port = BitConverter.GetBytes((ushort)port); Array.Reverse(server_port);
            byte[] next_state = getVarInt(2);
            byte[] handshake_packet = concatBytes(packet_id, protocol_version, server_adress_len, server_adress_val, server_port, next_state);
            byte[] handshake_packet_tosend = concatBytes(getVarInt(handshake_packet.Length), handshake_packet);

            Send(handshake_packet_tosend);

            byte[] username_val = Encoding.UTF8.GetBytes(username);
            byte[] username_len = getVarInt(username_val.Length);
            byte[] login_packet = concatBytes(packet_id, username_len, username_val);
            byte[] login_packet_tosend = concatBytes(getVarInt(login_packet.Length), login_packet);

            Send(login_packet_tosend);

            readNextVarInt(); //Packet size
            int pid = readNextVarInt(); //Packet ID
            if (pid == 0x00) //Login rejected
            {
                Console.WriteLine("Login rejected by Server :");
                printstring(ChatParser.ParseText(readNextString()), true);
                return false;
            }
            else if (pid == 0x01) //Encryption request
            {
                string serverID = readNextString();
                byte[] Serverkey_RAW = readNextByteArray();
                byte[] token = readNextByteArray();
                var PublicServerkey = Crypto.GenerateRSAPublicKey(Serverkey_RAW);
                var SecretKey = Crypto.GenerateAESPrivateKey();
                return StartEncryption(uuid, sessionID, token, serverID, PublicServerkey, SecretKey);
            }
            else if (pid == 0x02) //Login successfull
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("Server is in offline mode.");
                Console.ForegroundColor = ConsoleColor.Gray;
                return true; //No need to check session or start encryption
            }
            else return false;
        }
        public bool StartEncryption(string uuid, string sessionID, byte[] token, string serverIDhash, java.security.PublicKey serverKey, javax.crypto.SecretKey secretKey)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            ConsoleIO.WriteLine("Crypto keys & hash generated.");
            Console.ForegroundColor = ConsoleColor.Gray;

            if (serverIDhash != "-")
            {
                Console.WriteLine("Checking Session...");
                if (!SessionCheck(uuid, sessionID, new java.math.BigInteger(Crypto.getServerHash(serverIDhash, serverKey, secretKey)).toString(16)))
                {
                    return false;
                }
            }

            //Encrypt the data
            byte[] key_enc = Crypto.Encrypt(serverKey, secretKey.getEncoded());
            byte[] token_enc = Crypto.Encrypt(serverKey, token);
            byte[] key_len = BitConverter.GetBytes((short)key_enc.Length); Array.Reverse(key_len);
            byte[] token_len = BitConverter.GetBytes((short)token_enc.Length); Array.Reverse(token_len);

            //Encryption Response packet
            byte[] packet_id = getVarInt(0x01);
            byte[] encryption_response = concatBytes(packet_id, key_len, key_enc, token_len, token_enc);
            byte[] encryption_response_tosend = concatBytes(getVarInt(encryption_response.Length), encryption_response);
            Send(encryption_response_tosend);

            //Start client-side encryption
            setEncryptedClient(Crypto.SwitchToAesMode(c.GetStream(), secretKey));

            //Get the next packet
            readNextVarInt(); //Skip Packet size (not needed)
            return (readNextVarInt() == 0x02); //Packet ID. 0x02 = Login Success
        }

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
        }
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
        public void Disconnect(string message)
        {
            if (message == null)
                message = "";

            try
            {
                byte[] packet_id = getVarInt(0x40);
                byte[] message_val = Encoding.UTF8.GetBytes(message);
                byte[] message_len = getVarInt(message_val.Length);
                byte[] disconnect_packet = concatBytes(packet_id, message_len, message_val);
                byte[] disconnect_packet_tosend = concatBytes(getVarInt(disconnect_packet.Length), disconnect_packet);
                Send(disconnect_packet_tosend);
            }
            catch (SocketException) { }
            catch (System.IO.IOException) { }
        }

        private List<ChatBot> bots = new List<ChatBot>();
        public void BotLoad(ChatBot b) { b.SetHandler(this); bots.Add(b); b.Initialize(); Settings.SingleCommand = ""; }
        public void BotUnLoad(ChatBot b) { bots.RemoveAll(item => object.ReferenceEquals(item, b)); }
        public void BotClear() { bots.Clear(); }
    }
}
