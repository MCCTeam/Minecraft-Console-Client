using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace MinecraftClient
{
    /// <summary>
    /// The class containing all the core functions needed to communicate with a Minecraft server.
    /// </summary>

    public class MinecraftCom : IAutoComplete
    {
        #region Login to Minecraft.net, Obtaining a session ID

        public enum LoginResult { Error, Success, WrongPassword, Blocked, AccountMigrated, NotPremium, BadRequest };

        /// <summary>
        /// Allows to login to a premium Minecraft account, and retrieve the session ID.
        /// </summary>
        /// <param name="user">Login</param>
        /// <param name="pass">Password</param>
        /// <param name="outdata">Will contain the data returned by Minecraft.net, if the login is successful : Version:UpdateTicket:Username:SessionID</param>
        /// <returns>Returns the status of the login (Success, Failure, etc.)</returns>

        public static LoginResult GetLogin(string user, string pass, ref string outdata)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                WebClient wClient = new WebClient();
                Console.WriteLine("https://login.minecraft.net/?user=" + user + "&password=<******>&version=13");
                string result = Encoding.ASCII.GetString(wClient.UploadValues("https://login.minecraft.net/", new System.Collections.Specialized.NameValueCollection() { { "user", user }, { "password", pass }, { "version", "13" } } ));
                outdata = result;
                Console.WriteLine(result);
                Console.ForegroundColor = ConsoleColor.Gray;
                if (result == "Bad login") { return LoginResult.WrongPassword; }
                if (result == "Bad request") { return LoginResult.BadRequest; }
                if (result == "User not premium") { return LoginResult.NotPremium; }
                if (result == "Too many failed logins") { return LoginResult.Blocked; }
                if (result == "Account migrated, use e-mail as username.") { return LoginResult.AccountMigrated; }
                else return LoginResult.Success;
            }
            catch (WebException) { return LoginResult.Error; }
        }

        #endregion

        #region Keep-Alive for a Minecraft.net session, should be called every 5 minutes (currently unused)

        /// <summary>
        /// The session ID will expire within 5 minutes unless this function is called every 5 minutes
        /// </summary>
        /// <param name="user">Username</param>
        /// <param name="sessionID">Session ID to keep alive</param>

        public static void SessionKeepAlive(string user, string sessionID)
        {
            new WebClient().DownloadString("https://login.minecraft.net/session?name=" + user + "&session=" + sessionID);
        }

        #endregion

        #region Session checking when joining a server in online mode

        /// <summary>
        /// This method allows to join an online-mode server.
        /// It Should be called between the handshake and the login attempt.
        /// </summary>
        /// <param name="user">Username</param>
        /// <param name="sessionID">A valid session ID for this username</param>
        /// <param name="hash">Hash returned by the server during the handshake</param>
        /// <returns>Returns true if the check was successful</returns>

        public static bool SessionCheck(string user, string sessionID, string hash)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            WebClient client = new WebClient();
            Console.Write("http://session.minecraft.net/game/joinserver.jsp?user=" + user + "&sessionId=" + sessionID + "&serverId=" + hash + " ... ");
            string result = client.DownloadString("http://session.minecraft.net/game/joinserver.jsp?user=" + user + "&sessionId=" + sessionID + "&serverId=" + hash);
            Console.WriteLine(result);
            Console.ForegroundColor = ConsoleColor.Gray;
            return (result == "OK");
        }

        #endregion

        #region Server-side session checking (programmed for testing purposes)

        /// <summary>
        /// Reproduces the username checking done by an online-mode server during the login process.
        /// </summary>
        /// <param name="user">Username</param>
        /// <param name="hash">Hash sent by the server during the handshake</param>
        /// <returns>Returns true if the user is allowed to join the server</returns>

        public static bool ServerSessionCheck(string user, string hash)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            WebClient client = new WebClient();
            ConsoleIO.WriteLine("http://session.minecraft.net/game/checkserver.jsp?user=" + user + "&serverId=" + hash);
            string result = client.DownloadString("http://session.minecraft.net/game/checkserver.jsp?user=" + user + "&serverId=" + hash);
            ConsoleIO.WriteLine(result);
            Console.ForegroundColor = ConsoleColor.Gray;
            return (result == "YES");
        }

        #endregion

        TcpClient c = new TcpClient();
        Crypto.AesStream s;

        public bool HasBeenKicked { get { return connectionlost; } }
        bool connectionlost = false;
        bool encrypted = false;
        byte protocolversion;

        public bool Update()
        {
            for (int i = 0; i < bots.Count; i++) { bots[i].Update(); }
            if (c.Client == null || !c.Connected) { return false; }
            byte id = 0;

            try
            {
                while (c.Client.Available > 0)
                {
                    id = readNextByte();
                    ProcessResult result = processPacket(id);

                    //Debug : Print packet IDs that are beign processed. Green = OK, Red = Unknown packet
                    //If the client gets out of sync, check the last green packet processing code.
                    //if (result == ProcessResult.OK) { printstring("§a0x" + id.ToString("X"), false); }
                    //else { printstring("§c0x" + id.ToString("X"), false); }

                    if (result == ProcessResult.ConnectionLost)
                    {
                        return false;
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

        private enum ProcessResult { OK, ConnectionLost, UnknownPacket }
        private ProcessResult processPacket(int id)
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
                    if (protocolversion >= 72)
                    {
                        //printstring("§8" + message, false); //Debug : Show the RAW JSON data
                        message = ChatParser.ParseText(message);
                        printstring(message, false);
                    }
                    else printstring(message, false);
                    for (int i = 0; i < bots.Count; i++) { bots[i].GetText(message); } break;
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
                    if (readNextInt() == 2022) { printstring("You are dead. Type /reco to respawn & reconnect.", false); }
                    if (protocolversion >= 72) { readData(4); } else readData(1);
                    break;
                case 0xC9: readNextString(); readData(3); break;
                case 0xCA: if (protocolversion >= 72) { readData(9); } else readData(3); break;
                case 0xCB: autocomplete_result = readNextString(); autocomplete_received = true; break;
                case 0xCC: readNextString(); readData(4); break;
                case 0xCD: readData(1); break;
                case 0xCE: if (protocolversion > 51) { readNextString(); readNextString(); readData(1); } break;
                case 0xCF: if (protocolversion > 51) { readNextString(); readData(1); readNextString(); } readData(4); break;
                case 0xD0: if (protocolversion > 51) { readData(1); readNextString(); } break;
                case 0xD1: if (protocolversion > 51) { readNextTeamData(); } break;
                case 0xFA: readNextString(); nbr = readNextShort(); readData(nbr); break;
                case 0xFF: string reason = readNextString();
                    ConsoleIO.WriteLine("Disconnected by Server :"); printstring(reason, true); connectionlost = true;
                    for (int i = 0; i < bots.Count; i++) { bots[i].OnDisconnect(ChatBot.DisconnectReason.InGameKick, reason); } return ProcessResult.ConnectionLost;
                default: return ProcessResult.UnknownPacket; //unknown packet!
            }
            return ProcessResult.OK; //packet has been successfully skipped
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

            byte[] autocomplete = new byte[3 + (behindcursor.Length * 2)];
            autocomplete[0] = 0xCB;
            byte[] msglen = BitConverter.GetBytes((short)behindcursor.Length);
            Array.Reverse(msglen); msglen.CopyTo(autocomplete, 1);
            byte[] msg = Encoding.BigEndianUnicode.GetBytes(behindcursor);
            msg.CopyTo(autocomplete, 3);

            autocomplete_received = false;
            autocomplete_result = behindcursor;
            Send(autocomplete);

            int wait_left = 50; //do not wait more than 5 seconds (50 * 100 ms)
            while (wait_left > 0 && !autocomplete_received) { System.Threading.Thread.Sleep(100); wait_left--; }
            string[] results = autocomplete_result.Split((char)0x00);
            return results[0];
        }

        public void setVersion(byte ver) { protocolversion = ver; }
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

        public static bool GetServerInfo(string serverIP, ref byte protocolversion, ref string version)
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
                byte[] ping = new byte[2] { 0xfe, 0x01 };
                tcp.Client.Send(ping, SocketFlags.None);

                tcp.Client.Receive(ping, 0, 1, SocketFlags.None);
                if (ping[0] == 0xff)
                {
                    MinecraftCom ComTmp = new MinecraftCom();
                    ComTmp.setClient(tcp);
                    string result = ComTmp.readNextString();
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    //Console.WriteLine(result.Replace((char)0x00, ' '));
                    if (result.Length > 2 && result[0] == '§' && result[1] == '1')
                    {
                        string[] tmp = result.Split((char)0x00);
                        protocolversion = (byte)Int16.Parse(tmp[1]);
                        version = tmp[2];
                    }
                    else
                    {
                        protocolversion = (byte)39;
                        version = "B1.8.1 - 1.3.2";
                    }
                    Console.WriteLine("Server version : MC " + version + " (protocol v" + protocolversion + ").");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return true;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("Unexpected answer from the server (is that a Minecraft server ?)");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return false;
                }
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("An error occured while attempting to connect to this IP.");
                Console.ForegroundColor = ConsoleColor.Gray;
                return false;
            }
        }
        public bool Handshake(string username, string sessionID, ref string serverID, ref byte[] token, string host, int port)
        {
            //array
            byte[] data = new byte[10 + (username.Length + host.Length) * 2];

            //packet id
            data[0] = (byte)2;

            //Protocol Version
            data[1] = protocolversion;

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
                serverID = readNextString();
                byte[] Serverkey_RAW = readNextByteArray();
                token = readNextByteArray();

                if (serverID == "-")
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("Server is in offline mode.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return true; //No need to check session or start encryption
                }
                else
                {
                    var PublicServerkey = Crypto.GenerateRSAPublicKey(Serverkey_RAW);
                    var SecretKey = Crypto.GenerateAESPrivateKey();
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("Handshake sussessful. (Server ID: " + serverID + ')');
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return StartEncryption(username, sessionID, token, serverID, PublicServerkey, SecretKey);
                }
            }
            else return false;
        }
        public bool StartEncryption(string username, string sessionID, byte[] token, string serverIDhash, java.security.PublicKey serverKey, javax.crypto.SecretKey secretKey)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            ConsoleIO.WriteLine("Crypto keys & hash generated.");
            Console.ForegroundColor = ConsoleColor.Gray;

            if (serverIDhash != "-")
            {
                Console.WriteLine("Checking Session...");
                if (!SessionCheck(username, sessionID, new java.math.BigInteger(Crypto.getServerHash(serverIDhash, serverKey, secretKey)).toString(16)))
                {
                    return false;
                }
            }

            //Encrypt the data
            byte[] key_enc = Crypto.Encrypt(serverKey, secretKey.getEncoded());
            byte[] token_enc = Crypto.Encrypt(serverKey, token);
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
                setEncryptedClient(Crypto.SwitchToAesMode(c.GetStream(), secretKey));
                return true;
            }
            else return false;
        }
        public bool FinalizeLogin()
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
                            return true; //The Server accepted the request
                        }
                        else if (pid[0] == (byte)0xFF)
                        {
                            string reason = readNextString();
                            Console.WriteLine("Login rejected by Server :"); printstring(reason, true);
                            for (int i = 0; i < bots.Count; i++) { bots[i].OnDisconnect(ChatBot.DisconnectReason.LoginRejected, reason); }
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
                //Network error
                Console.WriteLine("Connection Lost.");
                return false;
            }
            return false; //Login was unsuccessful (received a kick...)
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
        public void Disconnect(string message)
        {
            if (message == null)
                message = "";

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

        private List<ChatBot> bots = new List<ChatBot>();
        public void BotLoad(ChatBot b) { b.SetHandler(this); bots.Add(b); b.Initialize(); Settings.SingleCommand = ""; }
        public void BotUnLoad(ChatBot b) { bots.RemoveAll(item => object.ReferenceEquals(item, b)); }
        public void BotClear() { bots.Clear(); }
    }
}
