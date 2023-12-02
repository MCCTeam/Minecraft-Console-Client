using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using MinecraftClient.Crypto;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Message;
using MinecraftClient.Protocol.ProfileKey;
using MinecraftClient.Protocol.Session;
using MinecraftClient.Proxy;
using MinecraftClient.Scripting;
using static MinecraftClient.Settings;
using static MinecraftClient.Settings.MainConfigHealper.MainConfig.GeneralConfig;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Implementation for Minecraft 1.4.X, 1.5.X, 1.6.X Protocols
    /// </summary>

    class Protocol16Handler : IMinecraftCom
    {
        readonly IMinecraftComHandler handler;
        private bool encrypted = false;
        private readonly int protocolversion;
        private Tuple<Thread, CancellationTokenSource>? netRead = null;
        Crypto.AesCfb8Stream? s;
        readonly TcpClient c;

        public Protocol16Handler(TcpClient Client, int ProtocolVersion, IMinecraftComHandler Handler)
        {
            ConsoleIO.SetAutoCompleteEngine(this);
            if (protocolversion >= 72)
                ChatParser.InitTranslations();
            c = Client;
            protocolversion = ProtocolVersion;
            handler = Handler;

            if (Handler.GetTerrainEnabled())
            {
                ConsoleIO.WriteLineFormatted("§c" + Translations.extra_terrainandmovement_disabled, acceptnewlines: true);
                Handler.SetTerrainEnabled(false);
            }

            if (handler.GetInventoryEnabled())
            {
                ConsoleIO.WriteLineFormatted("§c" + Translations.extra_inventory_disabled, acceptnewlines: true);
                handler.SetInventoryEnabled(false);
            }

            if (handler.GetEntityHandlingEnabled())
            {
                ConsoleIO.WriteLineFormatted("§c" + Translations.extra_entity_disabled, acceptnewlines: true);
                handler.SetEntityHandlingEnabled(false);
            }
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        // "IMinecraftComHandler handler" will not be used here.
        private Protocol16Handler(TcpClient Client)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            c = Client;
        }

        private void Updater(object? o)
        {
            if (((CancellationToken)o!).IsCancellationRequested)
                return;

            try
            {
                while (!((CancellationToken)o!).IsCancellationRequested)
                {
                    do
                    {
                        Thread.Sleep(100);
                    } while (Update());
                }
            }
            catch (System.IO.IOException) { }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }

            if (((CancellationToken)o!).IsCancellationRequested)
                return;

            handler.OnConnectionLost(ChatBot.DisconnectReason.ConnectionLost, "");
        }

        private bool Update()
        {
            handler.OnUpdate();
            bool connection_ok = true;
            while (c.Client.Available > 0 && connection_ok)
            {
                byte id = ReadNextByte();
                connection_ok = ProcessPacket(id);
            }
            return connection_ok;
        }

        private bool ProcessPacket(byte id)
        {
            int nbr;
            switch (id)
            {
                case 0x00:
                    byte[] keepalive = new byte[5] { 0, 0, 0, 0, 0 };
                    Receive(keepalive, 1, 4, SocketFlags.None);
                    handler.OnServerKeepAlive();
                    Send(keepalive); break;
                case 0x01: ReadData(4); ReadNextString(); ReadData(5); break;
                case 0x02: ReadData(1); ReadNextString(); ReadNextString(); ReadData(4); break;
                case 0x03:
                    string message = ReadNextString();
                    handler.OnTextReceived(new ChatMessage(message, null, protocolversion >= 72, -1, Guid.Empty)); break;
                case 0x04: ReadData(16); break;
                case 0x05: ReadData(6); ReadNextItemSlot(); break;
                case 0x06: ReadData(12); break;
                case 0x07: ReadData(9); break;
                case 0x08: if (protocolversion >= 72) { ReadData(10); } else ReadData(8); break;
                case 0x09: ReadData(8); ReadNextString(); break;
                case 0x0A: ReadData(1); break;
                case 0x0B: ReadData(33); break;
                case 0x0C: ReadData(9); break;
                case 0x0D: ReadData(41); break;
                case 0x0E: ReadData(11); break;
                case 0x0F: ReadData(10); ReadNextItemSlot(); ReadData(3); break;
                case 0x10: ReadData(2); break;
                case 0x11: ReadData(14); break;
                case 0x12: ReadData(5); break;
                case 0x13: if (protocolversion >= 72) { ReadData(9); } else ReadData(5); break;
                case 0x14: ReadData(4); ReadNextString(); ReadData(16); ReadNextEntityMetaData(); break;
                case 0x16: ReadData(8); break;
                case 0x17: ReadData(19); ReadNextObjectData(); break;
                case 0x18: ReadData(26); ReadNextEntityMetaData(); break;
                case 0x19: ReadData(4); ReadNextString(); ReadData(16); break;
                case 0x1A: ReadData(18); break;
                case 0x1B: if (protocolversion >= 72) { ReadData(10); } break;
                case 0x1C: ReadData(10); break;
                case 0x1D: nbr = (int)ReadNextByte(); ReadData(nbr * 4); break;
                case 0x1E: ReadData(4); break;
                case 0x1F: ReadData(7); break;
                case 0x20: ReadData(6); break;
                case 0x21: ReadData(9); break;
                case 0x22: ReadData(18); break;
                case 0x23: ReadData(5); break;
                case 0x26: ReadData(5); break;
                case 0x27: if (protocolversion >= 72) { ReadData(9); } else ReadData(8); break;
                case 0x28: ReadData(4); ReadNextEntityMetaData(); break;
                case 0x29: ReadData(8); break;
                case 0x2A: ReadData(5); break;
                case 0x2B: ReadData(8); break;
                case 0x2C: if (protocolversion >= 72) { ReadNextEntityProperties(protocolversion); } break;
                case 0x33: ReadData(13); nbr = ReadNextInt(); ReadData(nbr); break;
                case 0x34: ReadData(10); nbr = ReadNextInt(); ReadData(nbr); break;
                case 0x35: ReadData(12); break;
                case 0x36: ReadData(14); break;
                case 0x37: ReadData(17); break;
                case 0x38: ReadNextChunkBulkData(); break;
                case 0x3C: ReadData(28); nbr = ReadNextInt(); ReadData(3 * nbr); ReadData(12); break;
                case 0x3D: ReadData(18); break;
                case 0x3E: ReadNextString(); ReadData(17); break;
                case 0x3F: if (protocolversion > 51) { ReadNextString(); ReadData(32); } break;
                case 0x46: ReadData(2); break;
                case 0x47: ReadData(17); break;
                case 0x64: ReadNextWindowData(protocolversion); break;
                case 0x65: ReadData(1); break;
                case 0x66: ReadData(7); ReadNextItemSlot(); break;
                case 0x67: ReadData(3); ReadNextItemSlot(); break;
                case 0x68: ReadData(1); for (nbr = ReadNextShort(); nbr > 0; nbr--) { ReadNextItemSlot(); } break;
                case 0x69: ReadData(5); break;
                case 0x6A: ReadData(4); break;
                case 0x6B: ReadData(2); ReadNextItemSlot(); break;
                case 0x6C: ReadData(2); break;
                case 0x82: ReadData(10); ReadNextString(); ReadNextString(); ReadNextString(); ReadNextString(); break;
                case 0x83: ReadData(4); nbr = ReadNextShort(); ReadData(nbr); break;
                case 0x84: ReadData(11); nbr = ReadNextShort(); if (nbr > 0) { ReadData(nbr); } break;
                case 0x85: if (protocolversion >= 74) { ReadData(13); } break;
                case 0xC8:
                    if (ReadNextInt() == 2022) { ConsoleIO.WriteLogLine(Translations.mcc_player_dead, acceptnewlines: true); }
                    if (protocolversion >= 72) { ReadData(4); } else ReadData(1);
                    break;
                case 0xC9:
                    string name = ReadNextString(); bool online = ReadNextByte() != 0x00; ReadData(2);
                    Guid FakeUUID = new(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(name)).Take(16).ToArray());
                    if (online) { handler.OnPlayerJoin(new PlayerInfo(name, FakeUUID)); } else { handler.OnPlayerLeave(FakeUUID); }
                    break;
                case 0xCA: if (protocolversion >= 72) { ReadData(9); } else ReadData(3); break;
                case 0xCB:
                    string resultString = ReadNextString();
                    if (!string.IsNullOrEmpty(resultString))
                    {
                        string[] result = resultString.Split((char)0x00);
                        handler.OnAutoCompleteDone(0, result);
                    }
                    break;
                case 0xCC: ReadNextString(); ReadData(4); break;
                case 0xCD: ReadData(1); break;
                case 0xCE: if (protocolversion > 51) { ReadNextString(); ReadNextString(); ReadData(1); } break;
                case 0xCF: if (protocolversion > 51) { ReadNextString(); ReadData(1); ReadNextString(); } ReadData(4); break;
                case 0xD0: if (protocolversion > 51) { ReadData(1); ReadNextString(); } break;
                case 0xD1: if (protocolversion > 51) { ReadNextTeamData(); } break;
                case 0xFA:
                    string channel = ReadNextString();
                    byte[] payload = ReadNextByteArray();
                    handler.OnPluginChannelMessage(channel, payload);
                    break;
                case 0xFF:
                    string reason = ReadNextString();
                    handler.OnConnectionLost(ChatBot.DisconnectReason.InGameKick, reason); break;
                default: return false; //unknown packet!
            }
            return true; //packet has been successfully skipped
        }

        private void StartUpdating()
        {
            netRead = new(new Thread(new ParameterizedThreadStart(Updater)), new CancellationTokenSource());
            netRead.Item1.Name = "ProtocolPacketHandler";
            netRead.Item1.Start(netRead.Item2.Token);
        }

        /// <summary>
        /// Get net read thread (main thread) ID
        /// </summary>
        /// <returns>Net read thread ID</returns>
        public int GetNetMainThreadId()
        {
            return netRead != null ? netRead.Item1.ManagedThreadId : -1;
        }

        public void Dispose()
        {
            try
            {
                if (netRead != null)
                {
                    netRead.Item2.Cancel();
                    c.Close();
                }
            }
            catch { }
        }

        private void ReadData(int offset)
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

        private string ReadNextString()
        {
            ushort length = (ushort)ReadNextShort();
            if (length > 0)
            {
                byte[] cache = new byte[length * 2];
                Receive(cache, 0, length * 2, SocketFlags.None);
                string result = Encoding.BigEndianUnicode.GetString(cache);
                return result;
            }
            else return "";
        }
        public bool SendEntityAction(int PlayerEntityID, int ActionID)
        {
            return false;
        }

        private byte[] ReadNextByteArray()
        {
            short len = ReadNextShort();
            byte[] data = new byte[len];
            Receive(data, 0, len, SocketFlags.None);
            return data;
        }

        private short ReadNextShort()
        {
            byte[] tmp = new byte[2];
            Receive(tmp, 0, 2, SocketFlags.None);
            Array.Reverse(tmp);
            return BitConverter.ToInt16(tmp, 0);
        }

        private int ReadNextInt()
        {
            byte[] tmp = new byte[4];
            Receive(tmp, 0, 4, SocketFlags.None);
            Array.Reverse(tmp);
            return BitConverter.ToInt32(tmp, 0);
        }

        private byte ReadNextByte()
        {
            byte[] result = new byte[1];
            Receive(result, 0, 1, SocketFlags.None);
            return result[0];
        }

        private void ReadNextItemSlot()
        {
            short itemid = ReadNextShort();
            //If slot not empty (item ID != -1)
            if (itemid != -1)
            {
                ReadData(1); //Item count
                ReadData(2); //Item damage
                short length = ReadNextShort();
                //If length of optional NBT data > 0, read it
                if (length > 0) { ReadData(length); }
            }
        }

        private void ReadNextEntityMetaData()
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
                    case 0: ReadData(1); break;        //Byte
                    case 1: ReadData(2); break;        //Short
                    case 2: ReadData(4); break;        //Int
                    case 3: ReadData(4); break;        //Float
                    case 4: ReadNextString(); break;   //String
                    case 5: ReadNextItemSlot(); break; //Slot
                    case 6: ReadData(12); break;       //Vector (3 Int)
                }
            } while (true);
        }

        private void ReadNextObjectData()
        {
            int id = ReadNextInt();
            if (id != 0) { ReadData(6); }
        }

        private void ReadNextTeamData()
        {
            ReadNextString(); //Internal Name
            byte mode = ReadNextByte();

            if (mode == 0 || mode == 2)
            {
                ReadNextString(); //Display Name
                ReadNextString(); //Prefix
                ReadNextString(); //Suffix
                ReadData(1); //Friendly Fire
            }

            if (mode == 0 || mode == 3 || mode == 4)
            {
                short count = ReadNextShort();
                for (int i = 0; i < count; i++)
                {
                    ReadNextString(); //Players
                }
            }
        }

        private void ReadNextEntityProperties(int protocolversion)
        {
            if (protocolversion >= 72)
            {
                if (protocolversion >= 74)
                {
                    //Minecraft 1.6.2
                    ReadNextInt(); //Entity ID
                    int count = ReadNextInt();
                    for (int i = 0; i < count; i++)
                    {
                        ReadNextString(); //Property name
                        ReadData(8); //Property value (Double)
                        short othercount = ReadNextShort();
                        ReadData(25 * othercount);
                    }
                }
                else
                {
                    //Minecraft 1.6.0 / 1.6.1
                    ReadNextInt(); //Entity ID
                    int count = ReadNextInt();
                    for (int i = 0; i < count; i++)
                    {
                        ReadNextString(); //Property name
                        ReadData(8); //Property value (Double)
                    }
                }
            }
        }

        private void ReadNextWindowData(int protocolversion)
        {
            ReadData(1);
            byte windowtype = ReadNextByte();
            ReadNextString();
            ReadData(1);
            if (protocolversion > 51)
            {
                ReadData(1);
                if (protocolversion >= 72 && windowtype == 0xb)
                {
                    ReadNextInt();
                }
            }
        }

        private void ReadNextChunkBulkData()
        {
            short chunkcount = ReadNextShort();
            int datalen = ReadNextInt();
            ReadData(1);
            ReadData(datalen);
            ReadData(12 * (chunkcount));
        }

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

        private void Send(byte[] buffer)
        {
            if (encrypted)
                s!.Write(buffer, 0, buffer.Length);
            else
                c.Client.Send(buffer);
        }

        private bool Handshake(string uuid, string username, string sessionID, string host, int port, SessionToken session)
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
                ProcessPacket(pid[0]);
                Receive(pid, 0, 1, SocketFlags.None);
            }
            if (pid[0] == 0xFD)
            {
                string serverID = ReadNextString();
                byte[] PublicServerkey = ReadNextByteArray();
                byte[] token = ReadNextByteArray();

                if (serverID == "-")
                    ConsoleIO.WriteLineFormatted("§8" + Translations.mcc_server_offline, acceptnewlines: true);
                else if (Settings.Config.Logging.DebugMessages)
                    ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.mcc_handshake, serverID));

                return StartEncryption(uuid, username, sessionID, Config.Main.General.AccountType, token, serverID, PublicServerkey, session);
            }
            else
            {
                ConsoleIO.WriteLineFormatted("§8" + Translations.error_invalid_response, acceptnewlines: true);
                return false;
            }
        }

        private bool StartEncryption(string uuid, string username, string sessionID, LoginType type, byte[] token, string serverIDhash, byte[] serverPublicKey, SessionToken session)
        {
            RSACryptoServiceProvider RSAService = CryptoHandler.DecodeRSAPublicKey(serverPublicKey)!;
            byte[] secretKey = CryptoHandler.ClientAESPrivateKey ?? CryptoHandler.GenerateAESPrivateKey();

            if (Settings.Config.Logging.DebugMessages)
                ConsoleIO.WriteLineFormatted("§8" + Translations.debug_crypto, acceptnewlines: true);

            if (serverIDhash != "-")
            {
                ConsoleIO.WriteLine(Translations.mcc_session);
                string serverHash = CryptoHandler.GetServerHash(serverIDhash, serverPublicKey, secretKey);

                bool needCheckSession = true;
                if (session.ServerPublicKey != null && session.SessionPreCheckTask != null
                    && serverIDhash == session.ServerIDhash && Enumerable.SequenceEqual(serverPublicKey, session.ServerPublicKey))
                {
                    session.SessionPreCheckTask.Wait();
                    if (session.SessionPreCheckTask.Result) // PreCheck Successed
                        needCheckSession = false;
                }

                if (needCheckSession)
                {
                    if (ProtocolHandler.SessionCheck(uuid, sessionID, serverHash, type))
                    {
                        session.ServerIDhash = serverIDhash;
                        session.ServerPublicKey = serverPublicKey;
                        SessionCache.Store(InternalConfig.Account.Login.ToLower(), session);
                    }
                    else
                    {
                        handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected, Translations.mcc_session_fail);
                        return false;
                    }
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
                ReadData(4);
                s = new AesCfb8Stream(c.GetStream(), secretKey);
                encrypted = true;
                return true;
            }
            else
            {
                ConsoleIO.WriteLineFormatted("§8" + Translations.error_invalid_encrypt, acceptnewlines: true);
                return false;
            }
        }

        public bool Login(PlayerKeyPair? playerKeyPair, SessionToken session)
        {
            if (Handshake(handler.GetUserUuidStr(), handler.GetUsername(), handler.GetSessionID(), handler.GetServerHost(), handler.GetServerPort(), session))
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
                                ProcessPacket(pid[0]);
                                Receive(pid, 0, 1, SocketFlags.None);
                            }
                            if (pid[0] == (byte)1)
                            {
                                ReadData(4); ReadNextString(); ReadData(5);
                                StartUpdating();
                                return true; //The Server accepted the request
                            }
                            else if (pid[0] == (byte)0xFF)
                            {
                                string reason = ReadNextString();
                                handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected, reason);
                                return false;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        //Connection failed
                        ConsoleIO.WriteLineFormatted("§8" + e.GetType().Name + ": " + e.Message);
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

        public int GetProtocolVersion()
        {
            return protocolversion;
        }

        public bool SendChatMessage(string message, PlayerKeyPair? playerKeyPair)
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

        public bool SendUpdateSign(Location location, string line1, string line2, string line3, string line4, bool isFrontText = true)
        {
            return false; //Currently not implemented
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

        public bool SendInteractEntity(int EntityID, int type)
        {
            return false; //Currently not implemented
        }

        public bool SendInteractEntity(int EntityID, int type, float X, float Y, float Z, int hand)
        {
            return false; //Currently not implemented
        }

        public bool SendInteractEntity(int EntityID, int type, float X, float Y, float Z)
        {
            return false; //Currently not implemented
        }

        public bool SendInteractEntity(int EntityID, int type, int hand)
        {
            return false; //Currently not implemented
        }

        public bool UpdateCommandBlock(Location location, string command, CommandBlockMode mode, CommandBlockFlags flags)
        {
            return false;  //Currently not implemented
        }

        public bool SendUseItem(int hand, int sequenceId)
        {
            return false; //Currently not implemented
        }

        public bool SendWindowAction(int windowId, int slotId, WindowActionType action, Item? item, List<Tuple<short, Item?>> changedSlots, int stateId)
        {
            return false; //Currently not implemented
        }

        public bool SendAnimation(int animation, int playerid)
        {
            return false; //Currently not implemented
        }

        public bool SendCreativeInventoryAction(int slot, ItemType item, int count, Dictionary<string, object>? nbt)
        {
            return false; //Currently not implemented
        }

        public bool ClickContainerButton(int windowId, int buttonId)
        {
            return false; //Currently not implemented
        }

        public bool SendCloseWindow(int windowId)
        {
            return false; //Currently not implemented
        }

        public bool SendPlayerBlockPlacement(int hand, Location location, Direction face, int sequenceId)
        {
            return false; //Currently not implemented
        }

        public bool SendHeldItemChange(short slot)
        {
            return false; //Currently not implemented
        }

        public bool SendPlayerDigging(int status, Location location, Direction face, int sequenceId)
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
            try
            {
                byte[] channelLength = BitConverter.GetBytes((short)channel.Length);
                Array.Reverse(channelLength);

                byte[] channelData = Encoding.BigEndianUnicode.GetBytes(channel);

                byte[] dataLength = BitConverter.GetBytes((short)data.Length);
                Array.Reverse(dataLength);

                Send(ConcatBytes(new byte[] { 0xFA }, channelLength, channelData, dataLength, data));

                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
        }

        int IAutoComplete.AutoComplete(string BehindCursor)
        {
            if (String.IsNullOrEmpty(BehindCursor))
                return -1;

            byte[] autocomplete = new byte[3 + (BehindCursor.Length * 2)];
            autocomplete[0] = 0xCB;
            byte[] msglen = BitConverter.GetBytes((short)BehindCursor.Length);
            Array.Reverse(msglen);
            msglen.CopyTo(autocomplete, 1);
            byte[] msg = Encoding.BigEndianUnicode.GetBytes(BehindCursor);
            msg.CopyTo(autocomplete, 3);
            ConsoleIO.AutoCompleteDone = false;
            Send(autocomplete);
            return 0;
        }

        private static byte[] ConcatBytes(params byte[][] bytes)
        {
            List<byte> result = new();
            foreach (byte[] array in bytes)
                result.AddRange(array);
            return result.ToArray();
        }

        public static bool DoPing(string host, int port, ref int protocolversion)
        {
            try
            {
                string version = "";
                TcpClient tcp = ProxyHandler.NewTcpClient(host, port);
                tcp.ReceiveTimeout = 30000; // 30 seconds
                tcp.ReceiveTimeout = 5000; //MC 1.7.2+ SpigotMC servers won't respond, so we need a reasonable timeout.
                byte[] ping = new byte[2] { 0xfe, 0x01 };
                tcp.Client.Send(ping, SocketFlags.None);
                tcp.Client.Receive(ping, 0, 1, SocketFlags.None);

                if (ping[0] == 0xff)
                {
                    Protocol16Handler ComTmp = new(tcp);
                    string result = ComTmp.ReadNextString();

                    if (Settings.Config.Logging.DebugMessages)
                    {
                        // May contain formatting codes, cannot use WriteLineFormatted
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        ConsoleIO.WriteLine(result);
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }

                    if (result.Length > 2 && result[0] == '§' && result[1] == '1')
                    {
                        string[] tmp = result.Split((char)0x00);
                        protocolversion = (byte)Int16.Parse(tmp[1], NumberStyles.Any, CultureInfo.CurrentCulture);
                        version = tmp[2];

                        if (protocolversion == 127) //MC 1.7.2+
                            return false;
                    }
                    else
                    {
                        protocolversion = (byte)39;
                        version = "B1.8.1 - 1.3.2";
                    }

                    ConsoleIO.WriteLineFormatted(string.Format(Translations.mcc_use_version, version, protocolversion));

                    return true;
                }
                else return false;
            }
            catch { return false; }
        }

        public bool SelectTrade(int selectedSlot)
        {
            return false; //MC 1.13+
        }

        public bool SendSpectate(Guid UUID)
        {
            return false; //Currently not implemented
        }
        
        public bool SendRenameItem(string itemName)
        {
            return false;
        }

        public bool SendPlayerSession(PlayerKeyPair? playerKeyPair)
        {
            return false;
        }
    }
}
