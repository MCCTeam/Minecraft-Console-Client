using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using MinecraftClient.Crypto;
using MinecraftClient.Proxy;
using System.Security.Cryptography;
using MinecraftClient.Protocol.Handlers.Forge;
using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Packets;
using MinecraftClient.Protocol.Packets.Inbound;
using MinecraftClient.Protocol.Packets.Inbound.ChunkData;
using MinecraftClient.Protocol.Packets.Inbound.JoinGame;
using MinecraftClient.Protocol.Packets.Outbound;
using MinecraftClient.Protocol.Packets.Outbound.ChatMessage;
using MinecraftClient.Protocol.Packets.Outbound.ClientSettings;
using MinecraftClient.Protocol.Packets.Outbound.PluginMessage;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Implementation for Minecraft 1.7.X+ Protocols
    /// </summary>
    class Protocol18Handler : IMinecraftCom, IProtocol
    {
        private int compression_treshold = 0;
        private bool autocomplete_received = false;
        private int autocomplete_transaction_id = 0;
        private readonly List<string> autocomplete_result = new List<string>();
        private bool login_phase = true;
        private bool encrypted = false;
        private int protocolversion;

        // Server forge info -- may be null.
        private ForgeInfo forgeInfo;
        private FMLHandshakeClientState fmlHandshakeState = FMLHandshakeClientState.START;

        IMinecraftComHandler handler;
        Thread netRead;
        IAesStream s;
        TcpClient c;

        int currentDimension;

        private Dictionary<int, IInboundGamePacketHandler> _inboundHandlers;
        private Dictionary<OutboundTypes, IOutboundGamePacket> _outboundPackets;

        public Protocol18Handler(TcpClient Client, int ProtocolVersion, IMinecraftComHandler Handler, ForgeInfo ForgeInfo)
        {
            ConsoleIO.SetAutoCompleteEngine(this);
            ChatParser.InitTranslations();
            this.c = Client;
            this.protocolversion = ProtocolVersion;
            this.handler = Handler;
            this.forgeInfo = ForgeInfo;
            this.initHandlers();
        }

        private void initHandlers()
        {
            _inboundHandlers = PacketFactory.InboundHandlers(protocolversion);
            ConsoleIO.WriteLine("Loaded inbound handlers:");
            foreach (var inboundGamePacketHandler in _inboundHandlers)
            {
                ConsoleIO.WriteLineFormatted($"Type: {inboundGamePacketHandler.Value.Type()} " +
                                             $"Implementation: {inboundGamePacketHandler.Value.GetType().Name} " +
                                             $"Packet: 0x{inboundGamePacketHandler.Key:X2}");
            }

            _outboundPackets = PacketFactory.OutboundHandlers(protocolversion);

            ConsoleIO.WriteLine("Loaded outbound packets:");
            foreach (var outboundPacket in _outboundPackets)
            {
                ConsoleIO.WriteLineFormatted($"Type: {outboundPacket.Value.Type()} " +
                                             $"Implementation: {outboundPacket.Value.GetType().Name} " +
                                             $"Packet: 0x{outboundPacket.Value.PacketId():X2}");
            }
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
                } while (Update());
            }
            catch (System.IO.IOException)
            {
            }
            catch (SocketException)
            {
            }
            catch (ObjectDisposedException)
            {
            }

            handler.OnConnectionLost(ChatBot.DisconnectReason.ConnectionLost, "");
        }

        /// <summary>
        /// Read data from the network. Should be called on a separate thread.
        /// </summary>
        /// <returns>FALSE if an error occured, TRUE otherwise.</returns>
        private bool Update()
        {
            handler.OnUpdate();
            if (c.Client == null || !c.Connected)
            {
                return false;
            }

            try
            {
                while (c.Client.Available > 0)
                {
                    int packetID = 0;
                    List<byte> packetData = new List<byte>();
                    readNextPacket(ref packetID, packetData);

                    try
                    {
                        handleIncomingPacket(packetID, new List<byte>(packetData));
                    }
                    catch (Exception e)
                    {
                        ConsoleIO.WriteLineFormatted(
                            $"Failed to process packet 0x{packetID:X2}: {e.Message}");
                        ConsoleIO.WriteLine(e.ToString());
                        throw;
                    }
                }
            }
            catch (SocketException)
            {
                return false;
            }
            catch (NullReferenceException)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Read the next packet from the network
        /// </summary>
        /// <param name="packetID">will contain packet ID</param>
        /// <param name="packetData">will contain raw packet Data</param>
        private void readNextPacket(ref int packetID, List<byte> packetData)
        {
            packetData.Clear();
            int size = readNextVarIntRAW(); //Packet size
            packetData.AddRange(readDataRAW(size)); //Packet contents

            //Handle packet decompression
            if (protocolversion >= PacketUtils.MC18Version
                && compression_treshold > 0)
            {
                int sizeUncompressed = PacketUtils.readNextVarInt(packetData);
                if (sizeUncompressed != 0) // != 0 means compressed, let's decompress
                {
                    byte[] toDecompress = packetData.ToArray();
                    byte[] uncompressed = ZlibUtils.Decompress(toDecompress, sizeUncompressed);
                    packetData.Clear();
                    packetData.AddRange(uncompressed);
                }
            }

            packetID = PacketUtils.readNextVarInt(packetData); //Packet ID
        }

        public int Dimension()
        {
            return currentDimension;
        }

        public bool SendPacketOut(OutboundTypes type, IEnumerable<byte> packetData, IOutboundRequest data)
        {
            if (!_outboundPackets.TryGetValue(type, out var packet))
            {
                throw new NotSupportedException();
            }

            if (null == packetData)
            {
                packetData = new byte[] {0};
            }

            try
            {
                SendPacket(packet.PacketId(), packet.TransformData(packetData, data));
            }
            catch (SocketException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Handle the given packet
        /// </summary>
        /// <param name="packetId">Packet ID</param>
        /// <param name="packetData">Packet contents</param>
        /// <returns>TRUE if the packet was processed, FALSE if ignored or unknown</returns>
        private bool handleIncomingPacket(int packetId, List<byte> packetData)
        {
            if (login_phase)
            {
                switch (packetId) //Packet IDs are different while logging in
                {
                    case 0x03:
                        if (protocolversion >= PacketUtils.MC18Version)
                            compression_treshold = PacketUtils.readNextVarInt(packetData);
                        break;
                    default:
                        return false; //Ignored packet
                }

                return true;
            }

            if (!_inboundHandlers.TryGetValue(packetId, out var pHandler))
            {
                return false;
            }

            var data = pHandler.Handle(this, handler, packetData);
            switch (pHandler.Type())
            {
                case InboundTypes.JoinGame:
                {
                    currentDimension = ((JoinGameResult) data).Dimension;
                }
                    break;

                case InboundTypes.ChunkData:
                {
                    if (null == data)
                        break;
                    ProcessChunkColumnData(((ChunkDataResult) data).ChunkX, ((ChunkDataResult) data).ChunkZ,
                        ((ChunkDataResult) data).ChunkMask, ((ChunkDataResult) data).ChunkMask2,
                        ((ChunkDataResult) data).HasLights, ((ChunkDataResult) data).ChunksContinuous,
                        ((ChunkDataResult) data).Cache);
                }
                    break;
            }

            return true;
        }

        /// <summary>
        /// Process chunk column data from the server and (un)load the chunk from the Minecraft world
        /// </summary>
        /// <param name="chunkX">Chunk X location</param>
        /// <param name="chunkZ">Chunk Z location</param>
        /// <param name="chunkMask">Chunk mask for reading data</param>
        /// <param name="chunkMask2">Chunk mask for some additional 1.7 metadata</param>
        /// <param name="hasSkyLight">Contains skylight info</param>
        /// <param name="chunksContinuous">Are the chunk continuous</param>
        /// <param name="cache">Cache for reading chunk data</param>
        private void ProcessChunkColumnData(int chunkX, int chunkZ, ushort chunkMask, ushort chunkMask2, bool hasSkyLight, bool chunksContinuous, List<byte> cache)
        {
            if (protocolversion >= PacketUtils.MC19Version)
            {
                // 1.9 and above chunk format
                // Unloading chunks is handled by a separate packet
                for (int chunkY = 0; chunkY < ChunkColumn.ColumnSize; chunkY++)
                {
                    if ((chunkMask & (1 << chunkY)) != 0)
                    {
                        byte bitsPerBlock = PacketUtils.readNextByte(cache);
                        bool usePalette = (bitsPerBlock <= 8);

                        int paletteLength = PacketUtils.readNextVarInt(cache);
                        int[] palette = new int[paletteLength];
                        for (int i = 0; i < paletteLength; i++)
                        {
                            palette[i] = PacketUtils.readNextVarInt(cache);
                        }

                        // Bit mask covering bitsPerBlock bits
                        // EG, if bitsPerBlock = 5, valueMask = 00011111 in binary
                        uint valueMask = (uint) ((1 << bitsPerBlock) - 1);

                        ulong[] dataArray = PacketUtils.readNextULongArray(cache);

                        Chunk chunk = new Chunk();

                        if (dataArray.Length > 0)
                        {
                            for (int blockY = 0; blockY < Chunk.SizeY; blockY++)
                            {
                                for (int blockZ = 0; blockZ < Chunk.SizeZ; blockZ++)
                                {
                                    for (int blockX = 0; blockX < Chunk.SizeX; blockX++)
                                    {
                                        int blockNumber = (blockY * Chunk.SizeZ + blockZ) * Chunk.SizeX + blockX;

                                        int startLong = (blockNumber * bitsPerBlock) / 64;
                                        int startOffset = (blockNumber * bitsPerBlock) % 64;
                                        int endLong = ((blockNumber + 1) * bitsPerBlock - 1) / 64;

                                        // TODO: In the future a single ushort may not store the entire block id;
                                        // the Block code may need to change.
                                        ushort blockId;
                                        if (startLong == endLong)
                                        {
                                            blockId = (ushort) ((dataArray[startLong] >> startOffset) & valueMask);
                                        }
                                        else
                                        {
                                            int endOffset = 64 - startOffset;
                                            blockId = (ushort) ((dataArray[startLong] >> startOffset |
                                                                 dataArray[endLong] << endOffset) & valueMask);
                                        }

                                        if (usePalette)
                                        {
                                            // Get the real block ID out of the palette
                                            blockId = (ushort) palette[blockId];
                                        }

                                        chunk[blockX, blockY, blockZ] = new Block(blockId);
                                    }
                                }
                            }
                        }

                        //We have our chunk, save the chunk into the world
                        if (handler.GetWorld()[chunkX, chunkZ] == null)
                            handler.GetWorld()[chunkX, chunkZ] = new ChunkColumn();
                        handler.GetWorld()[chunkX, chunkZ][chunkY] = chunk;

                        //Skip block light
                        PacketUtils.readData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ) / 2, cache);

                        //Skip sky light
                        if (this.currentDimension == 0)
                            // Sky light is not sent in the nether or the end
                            PacketUtils.readData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ) / 2, cache);
                    }
                }

                // Don't worry about skipping remaining data since there is no useful data afterwards in 1.9
                // (plus, it would require parsing the tile entity lists' NBT)
            }
            else if (protocolversion >= PacketUtils.MC18Version)
            {
                // 1.8 chunk format
                if (chunksContinuous && chunkMask == 0)
                {
                    //Unload the entire chunk column
                    handler.GetWorld()[chunkX, chunkZ] = null;
                }
                else
                {
                    //Load chunk data from the server
                    for (int chunkY = 0; chunkY < ChunkColumn.ColumnSize; chunkY++)
                    {
                        if ((chunkMask & (1 << chunkY)) != 0)
                        {
                            Chunk chunk = new Chunk();

                            //Read chunk data, all at once for performance reasons, and build the chunk object
                            Queue<ushort> queue = new Queue<ushort>(
                                PacketUtils.readNextUShortsLittleEndian(Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ,
                                    cache));
                            for (int blockY = 0; blockY < Chunk.SizeY; blockY++)
                                for (int blockZ = 0; blockZ < Chunk.SizeZ; blockZ++)
                                    for (int blockX = 0; blockX < Chunk.SizeX; blockX++)
                                        chunk[blockX, blockY, blockZ] = new Block(queue.Dequeue());

                            //We have our chunk, save the chunk into the world
                            if (handler.GetWorld()[chunkX, chunkZ] == null)
                                handler.GetWorld()[chunkX, chunkZ] = new ChunkColumn();
                            handler.GetWorld()[chunkX, chunkZ][chunkY] = chunk;
                        }
                    }

                    //Skip light information
                    for (int chunkY = 0; chunkY < ChunkColumn.ColumnSize; chunkY++)
                    {
                        if ((chunkMask & (1 << chunkY)) != 0)
                        {
                            //Skip block light
                            PacketUtils.readData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ) / 2, cache);

                            //Skip sky light
                            if (hasSkyLight)
                                PacketUtils.readData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ) / 2, cache);
                        }
                    }

                    //Skip biome metadata
                    if (chunksContinuous)
                        PacketUtils.readData(Chunk.SizeX * Chunk.SizeZ, cache);
                }
            }
            else
            {
                // 1.7 chunk format
                if (chunksContinuous && chunkMask == 0)
                {
                    //Unload the entire chunk column
                    handler.GetWorld()[chunkX, chunkZ] = null;
                }
                else
                {
                    //Count chunk sections
                    int sectionCount = 0;
                    int addDataSectionCount = 0;
                    for (int chunkY = 0; chunkY < ChunkColumn.ColumnSize; chunkY++)
                    {
                        if ((chunkMask & (1 << chunkY)) != 0)
                            sectionCount++;
                        if ((chunkMask2 & (1 << chunkY)) != 0)
                            addDataSectionCount++;
                    }

                    //Read chunk data, unpacking 4-bit values into 8-bit values for block metadata
                    Queue<byte> blockTypes =
                        new Queue<byte>(PacketUtils.readData(Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ * sectionCount,
                            cache));
                    Queue<byte> blockMeta = new Queue<byte>();
                    foreach (byte packed in PacketUtils.readData(
                        (Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ * sectionCount) / 2, cache))
                    {
                        byte hig = (byte) (packed >> 4);
                        byte low = (byte) (packed & (byte) 0x0F);
                        blockMeta.Enqueue(hig);
                        blockMeta.Enqueue(low);
                    }

                    //Skip data we don't need
                    PacketUtils.readData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ * sectionCount) / 2,
                        cache); //Block light
                    if (hasSkyLight)
                        PacketUtils.readData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ * sectionCount) / 2,
                            cache); //Sky light
                    PacketUtils.readData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ * addDataSectionCount) / 2,
                        cache); //BlockAdd
                    if (chunksContinuous)
                        PacketUtils.readData(Chunk.SizeX * Chunk.SizeZ, cache); //Biomes

                    //Load chunk data
                    for (int chunkY = 0; chunkY < ChunkColumn.ColumnSize; chunkY++)
                    {
                        if ((chunkMask & (1 << chunkY)) != 0)
                        {
                            Chunk chunk = new Chunk();

                            for (int blockY = 0; blockY < Chunk.SizeY; blockY++)
                                for (int blockZ = 0; blockZ < Chunk.SizeZ; blockZ++)
                                    for (int blockX = 0; blockX < Chunk.SizeX; blockX++)
                                        chunk[blockX, blockY, blockZ] = new Block(blockTypes.Dequeue(), blockMeta.Dequeue());

                            if (handler.GetWorld()[chunkX, chunkZ] == null)
                                handler.GetWorld()[chunkX, chunkZ] = new ChunkColumn();
                            handler.GetWorld()[chunkX, chunkZ][chunkY] = chunk;
                        }
                    }
                }
            }
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
            catch
            {
            }
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
                catch (OutOfMemoryException)
                {
                }
            }

            return new byte[] { };
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
        /// Send a packet to the server.  Compression and encryption will be handled automatically.
        /// </summary>
        /// <param name="packetID">packet ID</param>
        /// <param name="packetData">packet Data</param>
        private void SendPacket(int packetID, IEnumerable<byte> packetData)
        {
            //The inner packet
            byte[] the_packet = PacketUtils.concatBytes(PacketUtils.getVarInt(packetID), packetData.ToArray());

            if (compression_treshold > 0) //Compression enabled?
            {
                if (the_packet.Length >= compression_treshold) //Packet long enough for compressing?
                {
                    byte[] compressed_packet = ZlibUtils.Compress(the_packet);
                    the_packet = PacketUtils.concatBytes(PacketUtils.getVarInt(the_packet.Length), compressed_packet);
                }
                else
                {
                    byte[] uncompressed_length = PacketUtils.getVarInt(0); //Not compressed (short packet)
                    the_packet = PacketUtils.concatBytes(uncompressed_length, the_packet);
                }
            }

            SendRAW(PacketUtils.concatBytes(PacketUtils.getVarInt(the_packet.Length), the_packet));
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
            byte[] protocol_version = PacketUtils.getVarInt(protocolversion);
            string server_address = handler.GetServerHost() + (forgeInfo != null ? "\0FML\0" : "");
            byte[] server_port = BitConverter.GetBytes((ushort) handler.GetServerPort());
            Array.Reverse(server_port);
            byte[] next_state = PacketUtils.getVarInt(2);
            byte[] handshake_packet = PacketUtils.concatBytes(protocol_version, PacketUtils.getString(server_address),
                server_port, next_state);

            SendPacket(0x00, handshake_packet);

            byte[] login_packet = PacketUtils.getString(handler.GetUsername());

            SendPacket(0x00, login_packet);

            int packetID = -1;
            List<byte> packetData = new List<byte>();
            while (true)
            {
                readNextPacket(ref packetID, packetData);
                if (packetID == 0x00) //Login rejected
                {
                    handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected,
                        ChatParser.ParseText(PacketUtils.readNextString(packetData)));
                    return false;
                }
                else if (packetID == 0x01) //Encryption request
                {
                    string serverID = PacketUtils.readNextString(packetData);
                    byte[] Serverkey = PacketUtils.readNextByteArray(protocolversion, packetData);
                    byte[] token = PacketUtils.readNextByteArray(protocolversion, packetData);
                    return StartEncryption(handler.GetUserUUID(), handler.GetSessionID(), token, serverID, Serverkey);
                }
                else
                {
                    if (packetID == 0x02) //Login successful
                    {
                        ConsoleIO.WriteLineFormatted("§8Server is in offline mode.");
                        login_phase = false;

                        if (forgeInfo != null)
                        {
                            // Do the forge handshake.
                            if (!CompleteForgeHandshake())
                            {
                                return false;
                            }
                        }

                        StartUpdating();
                        return true; //No need to check session or start encryption
                    }

                    handleIncomingPacket(packetID, packetData);
                }
            }
        }

        /// <summary>
        /// Completes the Minecraft Forge handshake.
        /// </summary>
        /// <returns>Whether the handshake was successful.</returns>
        private bool CompleteForgeHandshake()
        {
            int packetID = -1;
            List<byte> packetData = new List<byte>();

            while (fmlHandshakeState != FMLHandshakeClientState.DONE)
            {
                readNextPacket(ref packetID, packetData);

                if (packetID == 0x40) // Disconnect
                {
                    handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected,
                        ChatParser.ParseText(PacketUtils.readNextString(packetData)));
                    return false;
                }

                handleIncomingPacket(packetID, packetData);
            }

            return true;
        }

        /// <summary>
        /// Start network encryption. Automatically called by Login() if the server requests encryption.
        /// </summary>
        /// <returns>True if encryption was successful</returns>
        private bool StartEncryption(string uuid, string sessionID, byte[] token, string serverIDhash, byte[] serverKey)
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
                    handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected, "Failed to check session.");
                    return false;
                }
            }

            //Encrypt the data
            byte[] key_enc = PacketUtils.getArray(protocolversion, RSAService.Encrypt(secretKey, false));
            byte[] token_enc = PacketUtils.getArray(protocolversion, RSAService.Encrypt(token, false));

            //Encryption Response packet
            SendPacket(0x01, PacketUtils.concatBytes(key_enc, token_enc));

            //Start client-side encryption
            s = CryptoHandler.getAesStream(c.GetStream(), secretKey);
            encrypted = true;

            //Process the next packet
            int packetID = -1;
            List<byte> packetData = new List<byte>();
            while (true)
            {
                readNextPacket(ref packetID, packetData);
                if (packetID == 0x00) //Login rejected
                {
                    handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected, ChatParser.ParseText(PacketUtils.readNextString(packetData)));
                    return false;
                }
                else
                {
                    if (packetID == 0x02) //Login successful
                    {
                        login_phase = false;

                        if (forgeInfo != null)
                        {
                            // Do the forge handshake.
                            if (!CompleteForgeHandshake())
                            {
                                return false;
                            }
                        }

                        StartUpdating();
                        return true;
                    }

                    handleIncomingPacket(packetID, packetData);
                }
            }
        }

        /// <summary>
        /// Get max length for chat messages
        /// </summary>
        /// <returns>Max length, in characters</returns>
        public int GetMaxChatMessageLength()
        {
            return protocolversion >= PacketUtils.MC111Version
                ? 256
                : 100;
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

            return SendPacketOut(OutboundTypes.ChatMessage, null,
                new ChatMessageRequest {Message = message});
        }

        /// <summary>
        /// Send a respawn packet to the server
        /// </summary>
        /// <returns>True if properly sent</returns>
        public bool SendRespawnPacket()
        {
            return SendPacketOut(OutboundTypes.ClientStatus, null, null);
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
            // Plugin channels were significantly changed between Minecraft 1.12 and 1.13
            // https://wiki.vg/index.php?title=Pre-release_protocol&oldid=14132#Plugin_Channels
            if (protocolversion >= PacketUtils.MC113Version)
            {
                return SendPluginChannelPacket("minecraft:brand", PacketUtils.getString(brandInfo));
            }
            else
            {
                return SendPluginChannelPacket("MC|Brand", PacketUtils.getString(brandInfo));
            }
        }

        /// <summary>
        /// Inform the server of the client's Minecraft settings
        /// </summary>
        /// <param name="language">Client language eg en_US</param>
        /// <param name="viewDistance">View distance, in chunks</param>
        /// <param name="difficulty">Game difficulty (client-side...)</param>
        /// <param name="chatMode">Chat mode (allows muting yourself)</param>
        /// <param name="chatColors">Show chat colors</param>
        /// <param name="skinParts">Show skin layers</param>
        /// <param name="mainHand">1.9+ main hand</param>
        /// <returns>True if client settings were successfully sent</returns>
        public bool SendClientSettings(string language, byte viewDistance, byte difficulty, byte chatMode, bool chatColors, byte skinParts, byte mainHand)
        {
            var req = new ClientSettingsRequest
            {
                Language = language,
                Difficulty = difficulty,
                ChatMode = chatMode,
                MainHand = mainHand,
                SkinParts = skinParts,
                ChatColors = chatColors,
                ViewDistance = viewDistance,
            };

            return SendPacketOut(OutboundTypes.ClientSettings, null, req);
        }

        /// <summary>
        /// Send a location update to the server
        /// </summary>
        /// <param name="location">The new location of the player</param>
        /// <param name="onGround">True if the player is on the ground</param>
        /// <param name="yaw">Optional new yaw for updating player look</param>
        /// <param name="pitch">Optional new pitch for updating player look</param>
        /// <returns>True if the location update was successfully sent</returns>
        public bool SendLocationUpdate(Location location, bool onGround, float? yaw = null, float? pitch = null)
        {
//            if (Settings.TerrainAndMovements)
//            {
//                byte[] yawpitch = new byte[0];
//                PacketOutgoingType packetType = PacketOutgoingType.PlayerPosition;
//
//                if (yaw.HasValue && pitch.HasValue)
//                {
//                    yawpitch = PacketUtils.concatBytes(PacketUtils.getFloat(yaw.Value),
//                        PacketUtils.getFloat(pitch.Value));
//                    packetType = PacketOutgoingType.PlayerPositionAndLook;
//                }
//
//                try
//                {
//                    SendPacket(packetType, PacketUtils.concatBytes(
//                        PacketUtils.getDouble(location.X),
//                        PacketUtils.getDouble(location.Y),
//                        protocolversion < PacketUtils.MC18Version
//                            ? PacketUtils.getDouble(location.Y + 1.62)
//                            : new byte[0],
//                        PacketUtils.getDouble(location.Z),
//                        yawpitch,
//                        new byte[] {onGround ? (byte) 1 : (byte) 0}));
//                    return true;
//                }
//                catch (SocketException)
//                {
//                    return false;
//                }
//            }
//            else
            return false;
        }

        /// <summary>
        /// Send a plugin channel packet (0x17) to the server, compression and encryption will be handled automatically
        /// </summary>
        /// <param name="channel">Channel to send packet on</param>
        /// <param name="data">packet Data</param>
        public bool SendPluginChannelPacket(string channel, byte[] data)
        {
            return SendPacketOut(OutboundTypes.PluginMessage, data, new PluginMessageRequest {Channel = channel});
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
            catch (SocketException)
            {
            }
            catch (System.IO.IOException)
            {
            }
            catch (NullReferenceException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        /// <summary>
        /// Autocomplete text while typing username or command
        /// </summary>
        /// <param name="BehindCursor">Text behind cursor</param>
        /// <returns>Completed text</returns>
        IEnumerable<string> IAutoComplete.AutoComplete(string BehindCursor)
        {
            if (String.IsNullOrEmpty(BehindCursor))
                return new string[] { };

            byte[] transaction_id = PacketUtils.getVarInt(autocomplete_transaction_id);
            byte[] assume_command = new byte[] {0x00};
            byte[] has_position = new byte[] {0x00};

            byte[] tabcomplete_packet = new byte[] { };

            if (protocolversion >= PacketUtils.MC18Version)
            {
                if (protocolversion >= PacketUtils.MC17w46aVersion)
                {
                    tabcomplete_packet = PacketUtils.concatBytes(tabcomplete_packet, transaction_id);
                    tabcomplete_packet =
                        PacketUtils.concatBytes(tabcomplete_packet, PacketUtils.getString(BehindCursor));
                }
                else
                {
                    tabcomplete_packet =
                        PacketUtils.concatBytes(tabcomplete_packet, PacketUtils.getString(BehindCursor));

                    if (protocolversion >= PacketUtils.MC19Version)
                    {
                        tabcomplete_packet = PacketUtils.concatBytes(tabcomplete_packet, assume_command);
                    }

                    tabcomplete_packet = PacketUtils.concatBytes(tabcomplete_packet, has_position);
                }
            }
            else
            {
                tabcomplete_packet = PacketUtils.concatBytes(PacketUtils.getString(BehindCursor));
            }

            autocomplete_received = false;
            autocomplete_result.Clear();
            autocomplete_result.Add(BehindCursor);
            SendPacketOut(OutboundTypes.TabComplete, tabcomplete_packet, null);

            int wait_left = 50; //do not wait more than 5 seconds (50 * 100 ms)
            while (wait_left > 0 && !autocomplete_received)
            {
                System.Threading.Thread.Sleep(100);
                wait_left--;
            }

            if (autocomplete_result.Count > 0)
                ConsoleIO.WriteLineFormatted("§8" + String.Join(" ", autocomplete_result), false);
            return autocomplete_result;
        }

        /// <summary>
        /// Ping a Minecraft server to get information about the server
        /// </summary>
        /// <returns>True if ping was successful</returns>
        public static bool doPing(string host, int port, ref int protocolversion, ref ForgeInfo forgeInfo)
        {
            string version = "";
            TcpClient tcp = ProxyHandler.newTcpClient(host, port);
            tcp.ReceiveBufferSize = 1024 * 1024;

            byte[] packet_id = PacketUtils.getVarInt(0);
            byte[] protocol_version = PacketUtils.getVarInt(-1);
            byte[] server_port = BitConverter.GetBytes((ushort) port);
            Array.Reverse(server_port);
            byte[] next_state = PacketUtils.getVarInt(1);
            byte[] packet = PacketUtils.concatBytes(packet_id, protocol_version, PacketUtils.getString(host), server_port, next_state);
            byte[] tosend = PacketUtils.concatBytes(PacketUtils.getVarInt(packet.Length), packet);

            tcp.Client.Send(tosend, SocketFlags.None);

            byte[] status_request = PacketUtils.getVarInt(0);
            byte[] request_packet = PacketUtils.concatBytes(PacketUtils.getVarInt(status_request.Length), status_request);

            tcp.Client.Send(request_packet, SocketFlags.None);

            Protocol18Handler ComTmp = new Protocol18Handler(tcp);
            int packetLength = ComTmp.readNextVarIntRAW();
            if (packetLength > 0) //Read Response length
            {
                List<byte> packetData = new List<byte>(ComTmp.readDataRAW(packetLength));
                if (PacketUtils.readNextVarInt(packetData) == 0x00) //Read Packet ID
                {
                    string result = PacketUtils.readNextString(packetData); //Get the Json data

                    if (!String.IsNullOrEmpty(result) && result.StartsWith("{") && result.EndsWith("}"))
                    {
                        Json.JSONData jsonData = Json.ParseJson(result);
                        if (jsonData.Type == Json.JSONData.DataType.Object && jsonData.Properties.ContainsKey("version"))
                        {
                            Json.JSONData versionData = jsonData.Properties["version"];

                            //Retrieve display name of the Minecraft version
                            if (versionData.Properties.ContainsKey("name"))
                                version = versionData.Properties["name"].StringValue;

                            //Retrieve protocol version number for handling this server
                            if (versionData.Properties.ContainsKey("protocol"))
                                protocolversion = atoi(versionData.Properties["protocol"].StringValue);

                            //Automatic fix for BungeeCord 1.8 reporting itself as 1.7...
                            if (protocolversion < 47 && version.Split(' ', '/').Contains("1.8"))
                                protocolversion = ProtocolHandler.MCVer2ProtocolVersion("1.8.0");

                            // Check for forge on the server.
                            if (jsonData.Properties.ContainsKey("modinfo") && jsonData.Properties["modinfo"].Type == Json.JSONData.DataType.Object)
                            {
                                Json.JSONData modData = jsonData.Properties["modinfo"];
                                if (modData.Properties.ContainsKey("type") && modData.Properties["type"].StringValue == "FML")
                                {
                                    forgeInfo = new ForgeInfo(modData);

                                    if (forgeInfo.Mods.Any())
                                    {
                                        if (Settings.DebugMessages)
                                        {
                                            ConsoleIO.WriteLineFormatted("§8Server is running Forge. Mod list:");
                                            foreach (ForgeInfo.ForgeMod mod in forgeInfo.Mods)
                                            {
                                                ConsoleIO.WriteLineFormatted("§8  " + mod.ToString());
                                            }
                                        }
                                        else ConsoleIO.WriteLineFormatted("§8Server is running Forge.");
                                    }
                                    else forgeInfo = null;
                                }
                            }

                            ConsoleIO.WriteLineFormatted("§8Server version : " + version + " (protocol v" + protocolversion + (forgeInfo != null ? ", with Forge)." : ")."));

                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}