using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MinecraftClient.Crypto;
using MinecraftClient.Inventory;
using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Message;
using MinecraftClient.Protocol.PacketPipeline;
using MinecraftClient.Protocol.ProfileKey;
using MinecraftClient.Protocol.Session;
using MinecraftClient.Proxy;
using MinecraftClient.Scripting;
using static ConsoleInteractive.ConsoleReader;
using static MinecraftClient.Settings;

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

        AesStream? s;
        readonly TcpClient c;

        private readonly CancellationToken CancelToken;

        public Protocol16Handler(CancellationToken cancelToken, TcpClient Client, int ProtocolVersion, IMinecraftComHandler Handler)
        {
            CancelToken = cancelToken;
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

        public async Task StartUpdating()
        {
            if (CancelToken.IsCancellationRequested)
                return;

            try
            {
                while (!CancelToken.IsCancellationRequested)
                {
                    do
                    {
                        Thread.Sleep(100);
                    } while (await Update());
                }
            }
            catch (System.IO.IOException) { }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }

            if (CancelToken.IsCancellationRequested)
                return;

            handler.OnConnectionLost(ChatBot.DisconnectReason.ConnectionLost, "");
        }

        private async Task<bool> Update()
        {
            await handler.OnUpdate();
            bool connection_ok = true;
            while (c.Client.Available > 0 && connection_ok)
            {
                byte id = await ReadNextByte();
                connection_ok = await ProcessPacket(id);
            }
            return connection_ok;
        }

        private async Task<bool> ProcessPacket(byte id)
        {
            int nbr;
            switch (id)
            {
                case 0x00:
                    byte[] keepalive = new byte[5] { 0, 0, 0, 0, 0 };
                    await ReceiveAsync(keepalive, 1, 4);
                    handler.OnServerKeepAlive();
                    await Send(keepalive); break;
                case 0x01: await ReadData(4); await ReadNextString(); await ReadData(5); break;
                case 0x02: await ReadData(1); await ReadNextString(); await ReadNextString(); await ReadData(4); break;
                case 0x03:
                    string message = await ReadNextString();
                    await handler.OnTextReceivedAsync(new ChatMessage(message, protocolversion >= 72, 0, Guid.Empty)); break;
                case 0x04: await ReadData(16); break;
                case 0x05: await ReadData(6); await ReadNextItemSlot(); break;
                case 0x06: await ReadData(12); break;
                case 0x07: await ReadData(9); break;
                case 0x08: if (protocolversion >= 72) { await ReadData(10); } else await ReadData(8); break;
                case 0x09: await ReadData(8); await ReadNextString(); break;
                case 0x0A: await ReadData(1); break;
                case 0x0B: await ReadData(33); break;
                case 0x0C: await ReadData(9); break;
                case 0x0D: await ReadData(41); break;
                case 0x0E: await ReadData(11); break;
                case 0x0F: await ReadData(10); await ReadNextItemSlot(); await ReadData(3); break;
                case 0x10: await ReadData(2); break;
                case 0x11: await ReadData(14); break;
                case 0x12: await ReadData(5); break;
                case 0x13: if (protocolversion >= 72) { await ReadData(9); } else await ReadData(5); break;
                case 0x14: await ReadData(4); await ReadNextString(); await ReadData(16); await ReadNextEntityMetaData(); break;
                case 0x16: await ReadData(8); break;
                case 0x17: await ReadData(19); await ReadNextObjectData(); break;
                case 0x18: await ReadData(26); await ReadNextEntityMetaData(); break;
                case 0x19: await ReadData(4); await ReadNextString(); await ReadData(16); break;
                case 0x1A: await ReadData(18); break;
                case 0x1B: if (protocolversion >= 72) { await ReadData(10); } break;
                case 0x1C: await ReadData(10); break;
                case 0x1D: nbr = (int)(await ReadNextByte()); await ReadData(nbr * 4); break;
                case 0x1E: await ReadData(4); break;
                case 0x1F: await ReadData(7); break;
                case 0x20: await ReadData(6); break;
                case 0x21: await ReadData(9); break;
                case 0x22: await ReadData(18); break;
                case 0x23: await ReadData(5); break;
                case 0x26: await ReadData(5); break;
                case 0x27: if (protocolversion >= 72) { await ReadData(9); } else await ReadData(8); break;
                case 0x28: await ReadData(4); await ReadNextEntityMetaData(); break;
                case 0x29: await ReadData(8); break;
                case 0x2A: await ReadData(5); break;
                case 0x2B: await ReadData(8); break;
                case 0x2C: if (protocolversion >= 72) { await ReadNextEntityProperties(protocolversion); } break;
                case 0x33: await ReadData(13); nbr = await ReadNextInt(); await ReadData(nbr); break;
                case 0x34: await ReadData(10); nbr = await ReadNextInt(); await ReadData(nbr); break;
                case 0x35: await ReadData(12); break;
                case 0x36: await ReadData(14); break;
                case 0x37: await ReadData(17); break;
                case 0x38: await ReadNextChunkBulkData(); break;
                case 0x3C: await ReadData(28); nbr = await ReadNextInt(); await ReadData(3 * nbr); await ReadData(12); break;
                case 0x3D: await ReadData(18); break;
                case 0x3E: await ReadNextString(); await ReadData(17); break;
                case 0x3F: if (protocolversion > 51) { await ReadNextString(); await ReadData(32); } break;
                case 0x46: await ReadData(2); break;
                case 0x47: await ReadData(17); break;
                case 0x64: await ReadNextWindowData(protocolversion); break;
                case 0x65: await ReadData(1); break;
                case 0x66: await ReadData(7); await ReadNextItemSlot(); break;
                case 0x67: await ReadData(3); await ReadNextItemSlot(); break;
                case 0x68: await ReadData(1); for (nbr = await ReadNextShort(); nbr > 0; nbr--) { await ReadNextItemSlot(); } break;
                case 0x69: await ReadData(5); break;
                case 0x6A: await ReadData(4); break;
                case 0x6B: await ReadData(2); await ReadNextItemSlot(); break;
                case 0x6C: await ReadData(2); break;
                case 0x82: await ReadData(10); await ReadNextString(); await ReadNextString(); await ReadNextString(); await ReadNextString(); break;
                case 0x83: await ReadData(4); nbr = await ReadNextShort(); await ReadData(nbr); break;
                case 0x84: await ReadData(11); nbr = await ReadNextShort(); if (nbr > 0) { await ReadData(nbr); } break;
                case 0x85: if (protocolversion >= 74) { await ReadData(13); } break;
                case 0xC8:
                    if (await ReadNextInt() == 2022) { ConsoleIO.WriteLogLine(Translations.mcc_player_dead, acceptnewlines: true); }
                    if (protocolversion >= 72) { await ReadData(4); } else await ReadData(1);
                    break;
                case 0xC9:
                    string name = await ReadNextString(); bool online = await ReadNextByte() != 0x00; await ReadData(2);
                    Guid FakeUUID = new(MD5.HashData(Encoding.UTF8.GetBytes(name)).Take(16).ToArray());
                    if (online) { await handler.OnPlayerJoinAsync(new PlayerInfo(name, FakeUUID)); } else { await handler.OnPlayerLeaveAsync(FakeUUID); }
                    break;
                case 0xCA: if (protocolversion >= 72) { await ReadData(9); } else await ReadData(3); break;
                case 0xCB:
                    string resultString = await ReadNextString();
                    if (!string.IsNullOrEmpty(resultString))
                    {
                        string[] result = resultString.Split((char)0x00);
                        handler.OnAutoCompleteDone(0, result);
                    }
                    break;
                case 0xCC: await ReadNextString(); await ReadData(4); break;
                case 0xCD: await ReadData(1); break;
                case 0xCE: if (protocolversion > 51) { await ReadNextString(); await ReadNextString(); await ReadData(1); } break;
                case 0xCF: if (protocolversion > 51) { await ReadNextString(); await ReadData(1); await ReadNextString(); } await ReadData(4); break;
                case 0xD0: if (protocolversion > 51) { await ReadData(1); await ReadNextString(); } break;
                case 0xD1: if (protocolversion > 51) { await ReadNextTeamData(); } break;
                case 0xFA:
                    string channel = await ReadNextString();
                    byte[] payload = await ReadNextByteArray();
                    handler.OnPluginChannelMessage(channel, payload);
                    break;
                case 0xFF:
                    string reason = await ReadNextString();
                    handler.OnConnectionLost(ChatBot.DisconnectReason.InGameKick, reason); break;
                default: return false; //unknown packet!
            }
            return true; //packet has been successfully skipped
        }

        public void Dispose()
        {
            try
            {
                c.Close();
            }
            catch { }
        }

        private async Task ReadData(int offset)
        {
            if (offset > 0)
            {
                try
                {
                    byte[] cache = new byte[offset];
                    await ReceiveAsync(cache, 0, offset);
                }
                catch (OutOfMemoryException) { }
            }
        }

        private async Task<string> ReadNextString()
        {
            ushort length = (ushort)(await ReadNextShort());
            if (length > 0)
            {
                byte[] cache = new byte[length * 2];
                await ReceiveAsync(cache, 0, length * 2);
                string result = Encoding.BigEndianUnicode.GetString(cache);
                return result;
            }
            else
                return string.Empty;
        }

        private async Task<string> ReadNextStringAsync(CancellationToken cancellationToken = default)
        {
            ushort length = (ushort)(await ReadNextShortAsync());
            if (length > 0)
            {
                byte[] cache = new byte[length * 2];
                await ReceiveAsync(cache, 0, length * 2);
                if (cancellationToken.IsCancellationRequested)
                    return string.Empty;
                string result = Encoding.BigEndianUnicode.GetString(cache);
                return result;
            }
            else
                return string.Empty;
        }
        public async Task<bool> SendEntityAction(int PlayerEntityID, int ActionID)
        {
            return await Task.FromResult(false);
        }

        private async Task<byte[]> ReadNextByteArray()
        {
            short len = await ReadNextShort();
            byte[] data = new byte[len];
            await ReceiveAsync(data, 0, len);
            return data;
        }

        private async Task<short> ReadNextShort()
        {
            byte[] tmp = new byte[2];
            await ReceiveAsync(tmp, 0, 2);
            Array.Reverse(tmp);
            return BitConverter.ToInt16(tmp, 0);
        }

        private async Task<short> ReadNextShortAsync()
        {
            byte[] tmp = new byte[2];
            await ReceiveAsync(tmp, 0, 2);
            Array.Reverse(tmp);
            return BitConverter.ToInt16(tmp, 0);
        }

        private async Task<int> ReadNextInt()
        {
            byte[] tmp = new byte[4];
            await ReceiveAsync(tmp, 0, 4);
            Array.Reverse(tmp);
            return BitConverter.ToInt32(tmp, 0);
        }

        private async Task<byte> ReadNextByte()
        {
            byte[] result = new byte[1];
            await ReceiveAsync(result, 0, 1);
            return result[0];
        }

        private async Task ReadNextItemSlot()
        {
            short itemid = await ReadNextShort();
            //If slot not empty (item ID != -1)
            if (itemid != -1)
            {
                await ReadData(1); //Item count
                await ReadData(2); //Item damage
                short length = await ReadNextShort();
                //If length of optional NBT data > 0, read it
                if (length > 0) { await ReadData(length); }
            }
        }

        private async Task ReadNextEntityMetaData()
        {
            do
            {
                byte[] id = new byte[1];
                await ReceiveAsync(id, 0, 1);
                if (id[0] == 0x7F) { break; }
                int index = id[0] & 0x1F;
                int type = id[0] >> 5;
                switch (type)
                {
                    case 0: await ReadData(1); break;        //Byte
                    case 1: await ReadData(2); break;        //Short
                    case 2: await ReadData(4); break;        //Int
                    case 3: await ReadData(4); break;        //Float
                    case 4: await ReadNextString(); break;   //String
                    case 5: await ReadNextItemSlot(); break; //Slot
                    case 6: await ReadData(12); break;       //Vector (3 Int)
                }
            } while (true);
        }

        private async Task ReadNextObjectData()
        {
            int id = await ReadNextInt();
            if (id != 0) { await ReadData(6); }
        }

        private async Task ReadNextTeamData()
        {
            await ReadNextString(); //Internal Name
            byte mode = await ReadNextByte();

            if (mode == 0 || mode == 2)
            {
                await ReadNextString(); //Display Name
                await ReadNextString(); //Prefix
                await ReadNextString(); //Suffix
                await ReadData(1); //Friendly Fire
            }

            if (mode == 0 || mode == 3 || mode == 4)
            {
                short count = await ReadNextShort();
                for (int i = 0; i < count; i++)
                {
                    await ReadNextString(); //Players
                }
            }
        }

        private async Task ReadNextEntityProperties(int protocolversion)
        {
            if (protocolversion >= 72)
            {
                if (protocolversion >= 74)
                {
                    //Minecraft 1.6.2
                    await ReadNextInt(); //Entity ID
                    int count = await ReadNextInt();
                    for (int i = 0; i < count; i++)
                    {
                        await ReadNextString(); //Property name
                        await ReadData(8); //Property value (Double)
                        short othercount = await ReadNextShort();
                        await ReadData(25 * othercount);
                    }
                }
                else
                {
                    //Minecraft 1.6.0 / 1.6.1
                    await ReadNextInt(); //Entity ID
                    int count = await ReadNextInt();
                    for (int i = 0; i < count; i++)
                    {
                        await ReadNextString(); //Property name
                        await ReadData(8); //Property value (Double)
                    }
                }
            }
        }

        private async Task ReadNextWindowData(int protocolversion)
        {
            await ReadData(1);
            byte windowtype = await ReadNextByte();
            await ReadNextString();
            await ReadData(1);
            if (protocolversion > 51)
            {
                await ReadData(1);
                if (protocolversion >= 72 && windowtype == 0xb)
                {
                    await ReadNextInt();
                }
            }
        }

        private async Task ReadNextChunkBulkData()
        {
            short chunkcount = await ReadNextShort();
            int datalen = await ReadNextInt();
            await ReadData(1);
            await ReadData(datalen);
            await ReadData(12 * (chunkcount));
        }

        /// <summary>
        /// Network reading method. Read bytes from the socket or encrypted socket.
        /// </summary>
        private async Task ReceiveAsync(byte[] buffer, int start, int offset)
        {
            int read = 0;
            while (read < offset)
            {
                if (encrypted)
                    read += await s!.ReadAsync(buffer.AsMemory().Slice(start + read, offset - read));
                else
                    read += await c.Client.ReceiveAsync(new ArraySegment<byte>(buffer, start + read, offset - read));
            }
        }

        private async Task Send(byte[] buffer)
        {
            if (encrypted)
                await s!.WriteAsync(buffer);
            else
                await c.Client.SendAsync(buffer);
        }

        private async Task<bool> Handshake(HttpClient httpClient, string uuid, string username, string sessionID, string host, int port, SessionToken session)
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

            await Send(data);

            byte[] pid = new byte[1];
            await ReceiveAsync(pid, 0, 1);
            while (pid[0] == 0xFA) //Skip some early plugin messages
            {
                await ProcessPacket(pid[0]);
                await ReceiveAsync(pid, 0, 1);
            }
            if (pid[0] == 0xFD)
            {
                string serverID = await ReadNextString();
                byte[] PublicServerkey = await ReadNextByteArray();
                byte[] token = await ReadNextByteArray();

                if (serverID == "-")
                    ConsoleIO.WriteLineFormatted("§8" + Translations.mcc_server_offline, acceptnewlines: true);
                else if (Settings.Config.Logging.DebugMessages)
                    ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.mcc_handshake, serverID));

                return await StartEncryption(httpClient, uuid, sessionID, token, serverID, PublicServerkey, session);
            }
            else
            {
                ConsoleIO.WriteLineFormatted("§8" + Translations.error_invalid_response, acceptnewlines: true);
                return false;
            }
        }

        private async Task<bool> StartEncryption(HttpClient httpClient, string uuid, string sessionID, byte[] token, string serverIDhash, byte[] serverPublicKey, SessionToken session)
        {
            RSACryptoServiceProvider RSAService = CryptoHandler.DecodeRSAPublicKey(serverPublicKey)!;
            byte[] secretKey = CryptoHandler.ClientAESPrivateKey ?? CryptoHandler.GenerateAESPrivateKey();

            if (Settings.Config.Logging.DebugMessages)
                ConsoleIO.WriteLineFormatted("§8" + Translations.debug_crypto, acceptnewlines: true);

            if (serverIDhash != "-")
            {
                ConsoleIO.WriteLine(Translations.mcc_session);

                string serverHash = CryptoHandler.GetServerHash(serverIDhash, serverPublicKey, secretKey);
                if (session.SessionPreCheckTask != null && session.ServerInfoHash != null && serverHash == session.ServerInfoHash)
                {
                    (bool preCheckResult, string? error) = await session.SessionPreCheckTask;
                    if (!preCheckResult)
                    {
                        handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected,
                            string.IsNullOrEmpty(error) ? Translations.mcc_session_fail : $"{Translations.mcc_session_fail} Error: {error}.");
                        return false;
                    }
                    session.SessionPreCheckTask = null;
                }
                else
                {
                    (bool sessionCheck, string? error) = await ProtocolHandler.SessionCheckAsync(httpClient, uuid, sessionID, serverHash);
                    if (sessionCheck)
                        SessionCache.StoreServerInfo($"{InternalConfig.ServerIP}:{InternalConfig.ServerPort}", serverIDhash, serverPublicKey);
                    else
                    {
                        handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected,
                            string.IsNullOrEmpty(error) ? Translations.mcc_session_fail : $"{Translations.mcc_session_fail} Error: {error}.");
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
            await Send(data);

            //Getting the next packet
            byte[] pid = new byte[1];
            await ReceiveAsync(pid, 0, 1);
            if (pid[0] == 0xFC)
            {
                await ReadData(4);
                s = new AesStream(c.Client, secretKey);
                encrypted = true;
                return true;
            }
            else
            {
                ConsoleIO.WriteLineFormatted("§8" + Translations.error_invalid_encrypt, acceptnewlines: true);
                return false;
            }
        }

        public async Task<bool> Login(HttpClient httpClient, PlayerKeyPair? playerKeyPair, SessionToken session)
        {
            if (await Handshake(httpClient, handler.GetUserUuidStr(), handler.GetUsername(), handler.GetSessionID(), handler.GetServerHost(), handler.GetServerPort(), session))
            {
                await Send(new byte[] { 0xCD, 0 });
                try
                {
                    byte[] pid = new byte[1];
                    try
                    {
                        if (c.Connected)
                        {
                            await ReceiveAsync(pid, 0, 1);
                            while (pid[0] >= 0xC0 && pid[0] != 0xFF) //Skip some early packets or plugin messages
                            {
                                await ProcessPacket(pid[0]);
                                await ReceiveAsync(pid, 0, 1);
                            }
                            if (pid[0] == (byte)1)
                            {
                                await ReadData(4); await ReadNextString(); await ReadData(5);
                                return true; //The Server accepted the request
                            }
                            else if (pid[0] == (byte)0xFF)
                            {
                                string reason = await ReadNextString();
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

                Send(reason).Wait();
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

        public async Task<bool> SendChatMessage(string message, PlayerKeyPair? playerKeyPair)
        {
            if (string.IsNullOrEmpty(message))
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

                await Send(chat);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
        }

        public async Task<bool> SendRespawnPacket()
        {
            try
            {
                await Send(new byte[] { 0xCD, 1 });
                return true;
            }
            catch (SocketException) { return false; }
        }

        public async Task<bool> SendUpdateSign(Location location, string line1, string line2, string line3, string line4)
        {
            return await Task.FromResult(false); //Currently not implemented
        }

        public async Task<bool> SendBrandInfo(string brandInfo)
        {
            return await Task.FromResult(false); //Only supported since MC 1.7
        }

        public async Task<bool> SendClientSettings(string language, byte viewDistance, byte difficulty, byte chatMode, bool chatColors, byte skinParts, byte mainHand)
        {
            return await Task.FromResult(false); //Currently not implemented
        }

        public async Task<bool> SendLocationUpdate(Location location, bool onGround, float? yaw, float? pitch)
        {
            return await Task.FromResult(false); //Currently not implemented
        }

        public async Task<bool> SendInteractEntity(int EntityID, int type)
        {
            return await Task.FromResult(false); //Currently not implemented
        }

        public async Task<bool> SendInteractEntity(int EntityID, int type, float X, float Y, float Z, int hand)
        {
            return await Task.FromResult(false); //Currently not implemented
        }

        public async Task<bool> SendInteractEntity(int EntityID, int type, float X, float Y, float Z)
        {
            return await Task.FromResult(false); //Currently not implemented
        }

        public async Task<bool> SendInteractEntity(int EntityID, int type, int hand)
        {
            return await Task.FromResult(false); //Currently not implemented
        }

        public async Task<bool> UpdateCommandBlock(Location location, string command, CommandBlockMode mode, CommandBlockFlags flags)
        {
            return await Task.FromResult(false);  //Currently not implemented
        }

        public async Task<bool> SendUseItem(int hand, int sequenceId)
        {
            return await Task.FromResult(false); //Currently not implemented
        }

        public async Task<bool> SendWindowAction(int windowId, int slotId, WindowActionType action, Item? item, List<Tuple<short, Item?>> changedSlots, int stateId)
        {
            return await Task.FromResult(false); //Currently not implemented
        }

        public async Task<bool> SendAnimation(int animation, int playerid)
        {
            return await Task.FromResult(false); //Currently not implemented
        }

        public async Task<bool> SendCreativeInventoryAction(int slot, ItemType item, int count, Dictionary<string, object>? nbt)
        {
            return await Task.FromResult(false); //Currently not implemented
        }

        public async Task<bool> ClickContainerButton(int windowId, int buttonId)
        {
            return await Task.FromResult(false); //Currently not implemented
        }

        public async Task<bool> SendCloseWindow(int windowId)
        {
            return await Task.FromResult(false); //Currently not implemented
        }

        public async Task<bool> SendPlayerBlockPlacement(int hand, Location location, Direction face, int sequenceId)
        {
            return await Task.FromResult(false); //Currently not implemented
        }

        public async Task<bool> SendHeldItemChange(short slot)
        {
            return await Task.FromResult(false); //Currently not implemented
        }

        public async Task<bool> SendPlayerDigging(int status, Location location, Direction face, int sequenceId)
        {
            return await Task.FromResult(false); //Currently not implemented
        }

        /// <summary>
        /// Send a plugin channel packet to the server.
        /// </summary>
        /// <param name="channel">Channel to send packet on</param>
        /// <param name="data">packet Data</param>
        public async Task<bool> SendPluginChannelPacket(string channel, byte[] data)
        {
            try
            {
                byte[] channelLength = BitConverter.GetBytes((short)channel.Length);
                Array.Reverse(channelLength);

                byte[] channelData = Encoding.BigEndianUnicode.GetBytes(channel);

                byte[] dataLength = BitConverter.GetBytes((short)data.Length);
                Array.Reverse(dataLength);

                await Send(ConcatBytes(new byte[] { 0xFA }, channelLength, channelData, dataLength, data));

                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
        }

        async Task<int> IAutoComplete.AutoComplete(string BehindCursor)
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
            await Send(autocomplete);
            return 0;
        }

        private static byte[] ConcatBytes(params byte[][] bytes)
        {
            List<byte> result = new();
            foreach (byte[] array in bytes)
                result.AddRange(array);
            return result.ToArray();
        }

        public static async Task<Tuple<bool, int, Forge.ForgeInfo?>> DoPing(string host, int port, CancellationToken cancelToken)
        {
            try
            {
                TcpClient tcpClient = ProxyHandler.NewTcpClient(host, port, ProxyHandler.ClientType.Ingame);
                tcpClient.ReceiveBufferSize = 1024 * 1024;
                tcpClient.ReceiveTimeout = 5000; // MC 1.7.2+ SpigotMC servers won't respond, so we need a reasonable timeout.

                string version = "";
                byte[] ping = new byte[2] { 0xfe, 0x01 };
                await tcpClient.Client.SendAsync(ping, SocketFlags.None, cancelToken);
                await tcpClient.Client.ReceiveAsync(new ArraySegment<byte>(ping, 0, 1), cancelToken);

                if (ping[0] == 0xff)
                {
                    Protocol16Handler ComTmp = new(tcpClient);
                    string result = await ComTmp.ReadNextStringAsync(cancelToken);

                    if (Config.Logging.DebugMessages)
                    {
                        // May contain formatting codes, cannot use WriteLineFormatted
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        ConsoleIO.WriteLine(result);
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }

                    int protocolversion;
                    if (result.Length > 2 && result[0] == '§' && result[1] == '1')
                    {
                        string[] tmp = result.Split((char)0x00);
                        protocolversion = short.Parse(tmp[1], NumberStyles.Any, CultureInfo.CurrentCulture);
                        version = tmp[2];

                        if (protocolversion == 127) // MC 1.7.2+
                            return new(false, 0, null);
                    }
                    else
                    {
                        protocolversion = 39;
                        version = "B1.8.1 - 1.3.2";
                    }

                    ConsoleIO.WriteLineFormatted(string.Format(Translations.mcc_use_version, version, protocolversion));

                    return new(true, protocolversion, null);
                }
            }
            catch { }
            return new(false, 0, null);
        }

        public async Task<bool> SelectTrade(int selectedSlot)
        {
            return await Task.FromResult(false); //MC 1.13+
        }

        public async Task<bool> SendSpectate(Guid UUID)
        {
            return await Task.FromResult(false); //Currently not implemented
        }
    }
}
