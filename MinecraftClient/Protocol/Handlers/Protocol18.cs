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
using MinecraftClient.Mapping.BlockPalettes;
using MinecraftClient.Protocol.Handlers.Forge;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Implementation for Minecraft 1.7.X+ Protocols
    /// </summary>
    /// <remarks>
    /// Typical update steps for implementing protocol changes for a new Minecraft version:
    ///  - Perform a diff between latest supported version in MCC and new stable version to support on https://wiki.vg/Protocol
    ///  - If there are any changes in packets implemented by MCC, add MCXXXVersion field below and implement new packet layouts
    ///  - If packet IDs were changed, also update getPacketIncomingType() and getPacketOutgoingID() inside Protocol18PacketTypes.cs
    /// </remarks>
    class Protocol18Handler : IMinecraftCom
    {
        internal const int MC18Version = 47;
        internal const int MC19Version = 107;
        internal const int MC191Version = 108;
        internal const int MC110Version = 210;
        internal const int MC1112Version = 316;
        internal const int MC112Version = 335;
        internal const int MC1121Version = 338;
        internal const int MC1122Version = 340;
        internal const int MC113Version = 393;
        internal const int MC114Version = 477;

        private int compression_treshold = 0;
        private bool autocomplete_received = false;
        private int autocomplete_transaction_id = 0;
        private readonly List<string> autocomplete_result = new List<string>();
        private bool login_phase = true;
        private int protocolversion;
        private int currentDimension;

        Protocol18Forge pForge;
        IMinecraftComHandler handler;
        SocketWrapper socketWrapper;
        DataTypes dataTypes;
        Thread netRead;

        public Protocol18Handler(TcpClient Client, int protocolVersion, IMinecraftComHandler handler, ForgeInfo forgeInfo)
        {
            ConsoleIO.SetAutoCompleteEngine(this);
            ChatParser.InitTranslations();
            this.socketWrapper = new SocketWrapper(Client);
            this.dataTypes = new DataTypes(protocolVersion);
            this.protocolversion = protocolVersion;
            this.handler = handler;
            this.pForge = new Protocol18Forge(forgeInfo, protocolVersion, dataTypes, this, handler);
            if (protocolversion >= MC113Version)
                Block.Palette = new Palette113();
            else Block.Palette = new Palette112();

            if (handler.GetTerrainEnabled() && protocolversion >= MC114Version)
            {
                ConsoleIO.WriteLineFormatted("§8Terrain & Movements currently not handled for that MC version.");
                handler.SetTerrainEnabled(false);
            }
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
        /// <returns>FALSE if an error occured, TRUE otherwise.</returns>
        private bool Update()
        {
            handler.OnUpdate();
            if (!socketWrapper.IsConnected())
                return false;
            try
            {
                while (socketWrapper.HasDataAvailable())
                {
                    int packetID = 0;
                    List<byte> packetData = new List<byte>();
                    ReadNextPacket(ref packetID, packetData);
                    HandlePacket(packetID, new List<byte>(packetData));
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
        internal void ReadNextPacket(ref int packetID, List<byte> packetData)
        {
            packetData.Clear();
            int size = dataTypes.ReadNextVarIntRAW(socketWrapper); //Packet size
            packetData.AddRange(socketWrapper.ReadDataRAW(size)); //Packet contents

            //Handle packet decompression
            if (protocolversion >= MC18Version
                && compression_treshold > 0)
            {
                int sizeUncompressed = dataTypes.ReadNextVarInt(packetData);
                if (sizeUncompressed != 0) // != 0 means compressed, let's decompress
                {
                    byte[] toDecompress = packetData.ToArray();
                    byte[] uncompressed = ZlibUtils.Decompress(toDecompress, sizeUncompressed);
                    packetData.Clear();
                    packetData.AddRange(uncompressed);
                }
            }

            packetID = dataTypes.ReadNextVarInt(packetData); //Packet ID
        }

        /// <summary>
        /// Handle the given packet
        /// </summary>
        /// <param name="packetID">Packet ID</param>
        /// <param name="packetData">Packet contents</param>
        /// <returns>TRUE if the packet was processed, FALSE if ignored or unknown</returns>
        internal bool HandlePacket(int packetID, List<byte> packetData)
        {
            try
            {
                if (login_phase)
                {
                    switch (packetID) //Packet IDs are different while logging in
                    {
                        case 0x03:
                            if (protocolversion >= MC18Version)
                                compression_treshold = dataTypes.ReadNextVarInt(packetData);
                            break;
                        default:
                            return false; //Ignored packet
                    }
                }
                // Regular in-game packets
                switch (Protocol18PacketTypes.GetPacketIncomingType(packetID, protocolversion))
                {
                    case PacketIncomingType.KeepAlive:
                        SendPacket(PacketOutgoingType.KeepAlive, packetData);
                        break;
                    case PacketIncomingType.JoinGame:
                        handler.OnGameJoined();
                        dataTypes.ReadNextInt(packetData);
                        dataTypes.ReadNextByte(packetData);
                        if (protocolversion >= MC191Version)
                            this.currentDimension = dataTypes.ReadNextInt(packetData);
                        else
                            this.currentDimension = (sbyte)dataTypes.ReadNextByte(packetData);
                        if (protocolversion < MC114Version)
                            dataTypes.ReadNextByte(packetData);           // Difficulty - 1.13 and below
                        dataTypes.ReadNextByte(packetData);
                        dataTypes.ReadNextString(packetData);
                        if (protocolversion >= MC114Version)
                            dataTypes.ReadNextVarInt(packetData);         // View distance - 1.14 and above
                        if (protocolversion >= MC18Version)
                            dataTypes.ReadNextBool(packetData);           // Reduced debug info - 1.8 and above
                        break;
                    case PacketIncomingType.ChatMessage:
                        string message = dataTypes.ReadNextString(packetData);
                        try
                        {
                            //Hide system messages or xp bar messages?
                            byte messageType = dataTypes.ReadNextByte(packetData);
                            if ((messageType == 1 && !Settings.DisplaySystemMessages)
                                || (messageType == 2 && !Settings.DisplayXPBarMessages))
                                break;
                        }
                        catch (ArgumentOutOfRangeException) { /* No message type */ }
                        handler.OnTextReceived(message, true);
                        break;
                    case PacketIncomingType.Respawn:
                        this.currentDimension = dataTypes.ReadNextInt(packetData);
                        if (protocolversion < MC114Version)
                            dataTypes.ReadNextByte(packetData);           // Difficulty - 1.13 and below
                        dataTypes.ReadNextByte(packetData);
                        dataTypes.ReadNextString(packetData);
                        handler.OnRespawn();
                        break;
                    case PacketIncomingType.PlayerPositionAndLook:
                        if (handler.GetTerrainEnabled())
                        {
                            double x = dataTypes.ReadNextDouble(packetData);
                            double y = dataTypes.ReadNextDouble(packetData);
                            double z = dataTypes.ReadNextDouble(packetData);
                            float yaw = dataTypes.ReadNextFloat(packetData);
                            float pitch = dataTypes.ReadNextFloat(packetData);
                            byte locMask = dataTypes.ReadNextByte(packetData);

                            if (protocolversion >= MC18Version)
                            {
                                Location location = handler.GetCurrentLocation();
                                location.X = (locMask & 1 << 0) != 0 ? location.X + x : x;
                                location.Y = (locMask & 1 << 1) != 0 ? location.Y + y : y;
                                location.Z = (locMask & 1 << 2) != 0 ? location.Z + z : z;
                                handler.UpdateLocation(location, yaw, pitch);
                            }
                            else handler.UpdateLocation(new Location(x, y, z), yaw, pitch);
                        }

                        if (protocolversion >= MC19Version)
                        {
                            int teleportID = dataTypes.ReadNextVarInt(packetData);
                            // Teleport confirm packet
                            SendPacket(PacketOutgoingType.TeleportConfirm, dataTypes.GetVarInt(teleportID));
                        }
                        break;
                    case PacketIncomingType.ChunkData:
                        if (handler.GetTerrainEnabled())
                        {
                            int chunkX = dataTypes.ReadNextInt(packetData);
                            int chunkZ = dataTypes.ReadNextInt(packetData);
                            bool chunksContinuous = dataTypes.ReadNextBool(packetData);
                            ushort chunkMask = protocolversion >= MC19Version
                                ? (ushort)dataTypes.ReadNextVarInt(packetData)
                                : dataTypes.ReadNextUShort(packetData);
                            if (protocolversion < MC18Version)
                            {
                                ushort addBitmap = dataTypes.ReadNextUShort(packetData);
                                int compressedDataSize = dataTypes.ReadNextInt(packetData);
                                byte[] compressed = dataTypes.ReadData(compressedDataSize, packetData);
                                byte[] decompressed = ZlibUtils.Decompress(compressed);
                                ProcessChunkColumnData(chunkX, chunkZ, chunkMask, addBitmap, currentDimension == 0, chunksContinuous, new List<byte>(decompressed));
                            }
                            else
                            {
                                //TODO skip NBT Heightmaps field for 1.14
                                //if (protocolversion >= MC114Version)
                                //    dataTypes.ReadNextNBT(packetData);
                                //TODO update Material.cs for 1.14
                                int dataSize = dataTypes.ReadNextVarInt(packetData);
                                ProcessChunkColumnData(chunkX, chunkZ, chunkMask, 0, false, chunksContinuous, packetData);
                            }
                        }
                        break;
                    case PacketIncomingType.MultiBlockChange:
                        if (handler.GetTerrainEnabled())
                        {
                            int chunkX = dataTypes.ReadNextInt(packetData);
                            int chunkZ = dataTypes.ReadNextInt(packetData);
                            int recordCount = protocolversion < MC18Version
                                ? (int)dataTypes.ReadNextShort(packetData)
                                : dataTypes.ReadNextVarInt(packetData);

                            for (int i = 0; i < recordCount; i++)
                            {
                                byte locationXZ;
                                ushort blockIdMeta;
                                int blockY;

                                if (protocolversion < MC18Version)
                                {
                                    blockIdMeta = dataTypes.ReadNextUShort(packetData);
                                    blockY = (ushort)dataTypes.ReadNextByte(packetData);
                                    locationXZ = dataTypes.ReadNextByte(packetData);
                                }
                                else
                                {
                                    locationXZ = dataTypes.ReadNextByte(packetData);
                                    blockY = (ushort)dataTypes.ReadNextByte(packetData);
                                    blockIdMeta = (ushort)dataTypes.ReadNextVarInt(packetData);
                                }

                                int blockX = locationXZ >> 4;
                                int blockZ = locationXZ & 0x0F;
                                Block block = new Block(blockIdMeta);
                                handler.GetWorld().SetBlock(new Location(chunkX, chunkZ, blockX, blockY, blockZ), block);
                            }
                        }
                        break;
                    case PacketIncomingType.BlockChange:
                        if (handler.GetTerrainEnabled())
                        {
                            if (protocolversion < MC18Version)
                            {
                                int blockX = dataTypes.ReadNextInt(packetData);
                                int blockY = dataTypes.ReadNextByte(packetData);
                                int blockZ = dataTypes.ReadNextInt(packetData);
                                short blockId = (short)dataTypes.ReadNextVarInt(packetData);
                                byte blockMeta = dataTypes.ReadNextByte(packetData);
                                handler.GetWorld().SetBlock(new Location(blockX, blockY, blockZ), new Block(blockId, blockMeta));
                            }
                            else handler.GetWorld().SetBlock(dataTypes.ReadNextLocation(packetData), new Block((ushort)dataTypes.ReadNextVarInt(packetData)));
                        }
                        break;
                    case PacketIncomingType.MapChunkBulk:
                        if (protocolversion < MC19Version && handler.GetTerrainEnabled())
                        {
                            int chunkCount;
                            bool hasSkyLight;
                            List<byte> chunkData = packetData;

                            //Read global fields
                            if (protocolversion < MC18Version)
                            {
                                chunkCount = dataTypes.ReadNextShort(packetData);
                                int compressedDataSize = dataTypes.ReadNextInt(packetData);
                                hasSkyLight = dataTypes.ReadNextBool(packetData);
                                byte[] compressed = dataTypes.ReadData(compressedDataSize, packetData);
                                byte[] decompressed = ZlibUtils.Decompress(compressed);
                                chunkData = new List<byte>(decompressed);
                            }
                            else
                            {
                                hasSkyLight = dataTypes.ReadNextBool(packetData);
                                chunkCount = dataTypes.ReadNextVarInt(packetData);
                            }

                            //Read chunk records
                            int[] chunkXs = new int[chunkCount];
                            int[] chunkZs = new int[chunkCount];
                            ushort[] chunkMasks = new ushort[chunkCount];
                            ushort[] addBitmaps = new ushort[chunkCount];
                            for (int chunkColumnNo = 0; chunkColumnNo < chunkCount; chunkColumnNo++)
                            {
                                chunkXs[chunkColumnNo] = dataTypes.ReadNextInt(packetData);
                                chunkZs[chunkColumnNo] = dataTypes.ReadNextInt(packetData);
                                chunkMasks[chunkColumnNo] = dataTypes.ReadNextUShort(packetData);
                                addBitmaps[chunkColumnNo] = protocolversion < MC18Version
                                    ? dataTypes.ReadNextUShort(packetData)
                                    : (ushort)0;
                            }

                            //Process chunk records
                            for (int chunkColumnNo = 0; chunkColumnNo < chunkCount; chunkColumnNo++)
                                ProcessChunkColumnData(chunkXs[chunkColumnNo], chunkZs[chunkColumnNo], chunkMasks[chunkColumnNo], addBitmaps[chunkColumnNo], hasSkyLight, true, chunkData);
                        }
                        break;
                    case PacketIncomingType.UnloadChunk:
                        if (protocolversion >= MC19Version && handler.GetTerrainEnabled())
                        {
                            int chunkX = dataTypes.ReadNextInt(packetData);
                            int chunkZ = dataTypes.ReadNextInt(packetData);
                            handler.GetWorld()[chunkX, chunkZ] = null;
                        }
                        break;
                    case PacketIncomingType.PlayerListUpdate:
                        if (protocolversion >= MC18Version)
                        {
                            int action = dataTypes.ReadNextVarInt(packetData);
                            int numActions = dataTypes.ReadNextVarInt(packetData);
                            for (int i = 0; i < numActions; i++)
                            {
                                Guid uuid = dataTypes.ReadNextUUID(packetData);
                                switch (action)
                                {
                                    case 0x00: //Player Join
                                        string name = dataTypes.ReadNextString(packetData);
                                        int propNum = dataTypes.ReadNextVarInt(packetData);
                                        for (int p = 0; p < propNum; p++)
                                        {
                                            string key = dataTypes.ReadNextString(packetData);
                                            string val = dataTypes.ReadNextString(packetData);
                                            if (dataTypes.ReadNextBool(packetData))
                                                dataTypes.ReadNextString(packetData);
                                        }
                                        dataTypes.ReadNextVarInt(packetData);
                                        dataTypes.ReadNextVarInt(packetData);
                                        if (dataTypes.ReadNextBool(packetData))
                                            dataTypes.ReadNextString(packetData);
                                        handler.OnPlayerJoin(uuid, name);
                                        break;
                                    case 0x01: //Update gamemode
                                    case 0x02: //Update latency
                                        dataTypes.ReadNextVarInt(packetData);
                                        break;
                                    case 0x03: //Update display name
                                        if (dataTypes.ReadNextBool(packetData))
                                            dataTypes.ReadNextString(packetData);
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
                            string name = dataTypes.ReadNextString(packetData);
                            bool online = dataTypes.ReadNextBool(packetData);
                            short ping = dataTypes.ReadNextShort(packetData);
                            Guid FakeUUID = new Guid(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(name)).Take(16).ToArray());
                            if (online)
                                handler.OnPlayerJoin(FakeUUID, name);
                            else handler.OnPlayerLeave(FakeUUID);
                        }
                        break;
                    case PacketIncomingType.TabCompleteResult:
                        if (protocolversion >= MC113Version)
                        {
                            autocomplete_transaction_id = dataTypes.ReadNextVarInt(packetData);
                            dataTypes.ReadNextVarInt(packetData); // Start of text to replace
                            dataTypes.ReadNextVarInt(packetData); // Length of text to replace
                        }

                        int autocomplete_count = dataTypes.ReadNextVarInt(packetData);
                        autocomplete_result.Clear();

                        for (int i = 0; i < autocomplete_count; i++)
                        {
                            autocomplete_result.Add(dataTypes.ReadNextString(packetData));
                            if (protocolversion >= MC113Version)
                            {
                                // Skip optional tooltip for each tab-complete result
                                if (dataTypes.ReadNextBool(packetData))
                                    dataTypes.ReadNextString(packetData);
                            }
                        }

                        autocomplete_received = true;
                        break;
                    case PacketIncomingType.PluginMessage:
                        String channel = dataTypes.ReadNextString(packetData);
                        // Length is unneeded as the whole remaining packetData is the entire payload of the packet.
                        if (protocolversion < MC18Version)
                            pForge.ReadNextVarShort(packetData);
                        handler.OnPluginChannelMessage(channel, packetData.ToArray());
                        return pForge.HandlePluginMessage(channel, packetData, ref currentDimension);
                    case PacketIncomingType.KickPacket:
                        handler.OnConnectionLost(ChatBot.DisconnectReason.InGameKick, ChatParser.ParseText(dataTypes.ReadNextString(packetData)));
                        return false;
                    case PacketIncomingType.NetworkCompressionTreshold:
                        if (protocolversion >= MC18Version && protocolversion < MC19Version)
                            compression_treshold = dataTypes.ReadNextVarInt(packetData);
                        break;
                    case PacketIncomingType.ResourcePackSend:
                        string url = dataTypes.ReadNextString(packetData);
                        string hash = dataTypes.ReadNextString(packetData);
                        //Send back "accepted" and "successfully loaded" responses for plugins making use of resource pack mandatory
                        byte[] responseHeader = new byte[0];
                        if (protocolversion < MC110Version) //MC 1.10 does not include resource pack hash in responses
                            responseHeader = dataTypes.ConcatBytes(dataTypes.GetVarInt(hash.Length), Encoding.UTF8.GetBytes(hash));
                        SendPacket(PacketOutgoingType.ResourcePackStatus, dataTypes.ConcatBytes(responseHeader, dataTypes.GetVarInt(3))); //Accepted pack
                        SendPacket(PacketOutgoingType.ResourcePackStatus, dataTypes.ConcatBytes(responseHeader, dataTypes.GetVarInt(0))); //Successfully loaded
                        break;
                    default:
                        return false; //Ignored packet
                }
                return true; //Packet processed
            }
            catch (Exception innerException)
            {
                throw new System.IO.InvalidDataException(
                    String.Format("Failed to process incoming packet of type {0}. (PacketID: {1}, Protocol: {2}, LoginPhase: {3}, InnerException: {4}).",
                        Protocol18PacketTypes.GetPacketIncomingType(packetID, protocolversion),
                        packetID,
                        protocolversion,
                        login_phase,
                        innerException.GetType()),
                    innerException);
            }
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
            if (protocolversion >= MC19Version)
            {
                // 1.9 and above chunk format
                // Unloading chunks is handled by a separate packet
                for (int chunkY = 0; chunkY < ChunkColumn.ColumnSize; chunkY++)
                {
                    if ((chunkMask & (1 << chunkY)) != 0)
                    {
                        byte bitsPerBlock = dataTypes.ReadNextByte(cache);
                        bool usePalette = (bitsPerBlock <= 8);

                        // Vanilla Minecraft will use at least 4 bits per block
                        if (bitsPerBlock < 4)
                            bitsPerBlock = 4;

                        // MC 1.9 to 1.12 will set palette length field to 0 when palette
                        // is not used, MC 1.13+ does not send the field at all in this case
                        int paletteLength = 0; // Assume zero when length is absent
                        if (usePalette || protocolversion < MC113Version)
                            paletteLength = dataTypes.ReadNextVarInt(cache);

                        int[] palette = new int[paletteLength];
                        for (int i = 0; i < paletteLength; i++)
                        {
                            palette[i] = dataTypes.ReadNextVarInt(cache);
                        }

                        // Bit mask covering bitsPerBlock bits
                        // EG, if bitsPerBlock = 5, valueMask = 00011111 in binary
                        uint valueMask = (uint)((1 << bitsPerBlock) - 1);

                        ulong[] dataArray = dataTypes.ReadNextULongArray(cache);

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
                                        // the Block code may need to change if block state IDs go beyond 65535
                                        ushort blockId;
                                        if (startLong == endLong)
                                        {
                                            blockId = (ushort)((dataArray[startLong] >> startOffset) & valueMask);
                                        }
                                        else
                                        {
                                            int endOffset = 64 - startOffset;
                                            blockId = (ushort)((dataArray[startLong] >> startOffset | dataArray[endLong] << endOffset) & valueMask);
                                        }

                                        if (usePalette)
                                        {
                                            // Get the real block ID out of the palette
                                            blockId = (ushort)palette[blockId];
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
                        dataTypes.ReadData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ) / 2, cache);

                        //Skip sky light
                        if (this.currentDimension == 0)
                            // Sky light is not sent in the nether or the end
                            dataTypes.ReadData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ) / 2, cache);
                    }
                }

                // Don't worry about skipping remaining data since there is no useful data afterwards in 1.9
                // (plus, it would require parsing the tile entity lists' NBT)
            }
            else if (protocolversion >= MC18Version)
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
                            Queue<ushort> queue = new Queue<ushort>(dataTypes.ReadNextUShortsLittleEndian(Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ, cache));
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
                            dataTypes.ReadData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ) / 2, cache);

                            //Skip sky light
                            if (hasSkyLight)
                                dataTypes.ReadData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ) / 2, cache);
                        }
                    }

                    //Skip biome metadata
                    if (chunksContinuous)
                        dataTypes.ReadData(Chunk.SizeX * Chunk.SizeZ, cache);
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
                    Queue<byte> blockTypes = new Queue<byte>(dataTypes.ReadData(Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ * sectionCount, cache));
                    Queue<byte> blockMeta = new Queue<byte>();
                    foreach (byte packed in dataTypes.ReadData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ * sectionCount) / 2, cache))
                    {
                        byte hig = (byte)(packed >> 4);
                        byte low = (byte)(packed & (byte)0x0F);
                        blockMeta.Enqueue(hig);
                        blockMeta.Enqueue(low);
                    }

                    //Skip data we don't need
                    dataTypes.ReadData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ * sectionCount) / 2, cache);          //Block light
                    if (hasSkyLight)
                        dataTypes.ReadData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ * sectionCount) / 2, cache);      //Sky light
                    dataTypes.ReadData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ * addDataSectionCount) / 2, cache);   //BlockAdd
                    if (chunksContinuous)
                        dataTypes.ReadData(Chunk.SizeX * Chunk.SizeZ, cache);                                         //Biomes

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
                    socketWrapper.Disconnect();
                }
            }
            catch { }
        }

        /// <summary>
        /// Send a packet to the server.  Packet ID, compression, and encryption will be handled automatically.
        /// </summary>
        /// <param name="packet">packet type</param>
        /// <param name="packetData">packet Data</param>
        private void SendPacket(PacketOutgoingType packet, IEnumerable<byte> packetData)
        {
            SendPacket(Protocol18PacketTypes.GetPacketOutgoingID(packet, protocolversion), packetData);
        }

        /// <summary>
        /// Send a packet to the server.  Compression and encryption will be handled automatically.
        /// </summary>
        /// <param name="packetID">packet ID</param>
        /// <param name="packetData">packet Data</param>
        private void SendPacket(int packetID, IEnumerable<byte> packetData)
        {
            //The inner packet
            byte[] the_packet = dataTypes.ConcatBytes(dataTypes.GetVarInt(packetID), packetData.ToArray());

            if (compression_treshold > 0) //Compression enabled?
            {
                if (the_packet.Length >= compression_treshold) //Packet long enough for compressing?
                {
                    byte[] compressed_packet = ZlibUtils.Compress(the_packet);
                    the_packet = dataTypes.ConcatBytes(dataTypes.GetVarInt(the_packet.Length), compressed_packet);
                }
                else
                {
                    byte[] uncompressed_length = dataTypes.GetVarInt(0); //Not compressed (short packet)
                    the_packet = dataTypes.ConcatBytes(uncompressed_length, the_packet);
                }
            }

            socketWrapper.SendDataRAW(dataTypes.ConcatBytes(dataTypes.GetVarInt(the_packet.Length), the_packet));
        }

        /// <summary>
        /// Do the Minecraft login.
        /// </summary>
        /// <returns>True if login successful</returns>
        public bool Login()
        {
            byte[] protocol_version = dataTypes.GetVarInt(protocolversion);
            string server_address = pForge.GetServerAddress(handler.GetServerHost());
            byte[] server_port = BitConverter.GetBytes((ushort)handler.GetServerPort()); Array.Reverse(server_port);
            byte[] next_state = dataTypes.GetVarInt(2);
            byte[] handshake_packet = dataTypes.ConcatBytes(protocol_version, dataTypes.GetString(server_address), server_port, next_state);

            SendPacket(0x00, handshake_packet);

            byte[] login_packet = dataTypes.GetString(handler.GetUsername());

            SendPacket(0x00, login_packet);

            int packetID = -1;
            List<byte> packetData = new List<byte>();
            while (true)
            {
                ReadNextPacket(ref packetID, packetData);
                if (packetID == 0x00) //Login rejected
                {
                    handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected, ChatParser.ParseText(dataTypes.ReadNextString(packetData)));
                    return false;
                }
                else if (packetID == 0x01) //Encryption request
                {
                    string serverID = dataTypes.ReadNextString(packetData);
                    byte[] Serverkey = dataTypes.ReadNextByteArray(packetData);
                    byte[] token = dataTypes.ReadNextByteArray(packetData);
                    return StartEncryption(handler.GetUserUUID(), handler.GetSessionID(), token, serverID, Serverkey);
                }
                else if (packetID == 0x02) //Login successful
                {
                    ConsoleIO.WriteLineFormatted("§8Server is in offline mode.");
                    login_phase = false;

                    if (!pForge.CompleteForgeHandshake())
                        return false;

                    StartUpdating();
                    return true; //No need to check session or start encryption
                }
                else HandlePacket(packetID, packetData);
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
            byte[] key_enc = dataTypes.GetArray(RSAService.Encrypt(secretKey, false));
            byte[] token_enc = dataTypes.GetArray(RSAService.Encrypt(token, false));

            //Encryption Response packet
            SendPacket(0x01, dataTypes.ConcatBytes(key_enc, token_enc));

            //Start client-side encryption
            socketWrapper.SwitchToEncrypted(secretKey);

            //Process the next packet
            int packetID = -1;
            List<byte> packetData = new List<byte>();
            while (true)
            {
                ReadNextPacket(ref packetID, packetData);
                if (packetID == 0x00) //Login rejected
                {
                    handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected, ChatParser.ParseText(dataTypes.ReadNextString(packetData)));
                    return false;
                }
                else if (packetID == 0x02) //Login successful
                {
                    login_phase = false;

                    if (!pForge.CompleteForgeHandshake())
                        return false;

                    StartUpdating();
                    return true;
                }
                else HandlePacket(packetID, packetData);
            }
        }

        /// <summary>
        /// Get max length for chat messages
        /// </summary>
        /// <returns>Max length, in characters</returns>
        public int GetMaxChatMessageLength()
        {
            return protocolversion > MC110Version
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
            try
            {
                byte[] message_packet = dataTypes.GetString(message);
                SendPacket(PacketOutgoingType.ChatMessage, message_packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
        }

        /// <summary>
        /// Send a respawn packet to the server
        /// </summary>
        /// <returns>True if properly sent</returns>
        public bool SendRespawnPacket()
        {
            try
            {
                SendPacket(PacketOutgoingType.ClientStatus, new byte[] { 0 });
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
            // Plugin channels were significantly changed between Minecraft 1.12 and 1.13
            // https://wiki.vg/index.php?title=Pre-release_protocol&oldid=14132#Plugin_Channels
            if (protocolversion >= MC113Version)
            {
                return SendPluginChannelPacket("minecraft:brand", dataTypes.GetString(brandInfo));
            }
            else
            {
                return SendPluginChannelPacket("MC|Brand", dataTypes.GetString(brandInfo));
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
            try
            {
                List<byte> fields = new List<byte>();
                fields.AddRange(dataTypes.GetString(language));
                fields.Add(viewDistance);
                fields.AddRange(protocolversion >= MC19Version
                    ? dataTypes.GetVarInt(chatMode)
                    : new byte[] { chatMode });
                fields.Add(chatColors ? (byte)1 : (byte)0);
                if (protocolversion < MC18Version)
                {
                    fields.Add(difficulty);
                    fields.Add((byte)(skinParts & 0x1)); //show cape
                }
                else fields.Add(skinParts);
                if (protocolversion >= MC19Version)
                    fields.AddRange(dataTypes.GetVarInt(mainHand));
                SendPacket(PacketOutgoingType.ClientSettings, fields);
            }
            catch (SocketException) { }
            return false;
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
            if (handler.GetTerrainEnabled())
            {
                byte[] yawpitch = new byte[0];
                PacketOutgoingType packetType = PacketOutgoingType.PlayerPosition;

                if (yaw.HasValue && pitch.HasValue)
                {
                    yawpitch = dataTypes.ConcatBytes(dataTypes.GetFloat(yaw.Value), dataTypes.GetFloat(pitch.Value));
                    packetType = PacketOutgoingType.PlayerPositionAndLook;
                }

                try
                {
                    SendPacket(packetType, dataTypes.ConcatBytes(
                        dataTypes.GetDouble(location.X),
                        dataTypes.GetDouble(location.Y),
                        protocolversion < MC18Version
                            ? dataTypes.GetDouble(location.Y + 1.62)
                            : new byte[0],
                        dataTypes.GetDouble(location.Z),
                        yawpitch,
                        new byte[] { onGround ? (byte)1 : (byte)0 }));
                    return true;
                }
                catch (SocketException) { return false; }
            }
            else return false;
        }

        /// <summary>
        /// Send a plugin channel packet (0x17) to the server, compression and encryption will be handled automatically
        /// </summary>
        /// <param name="channel">Channel to send packet on</param>
        /// <param name="data">packet Data</param>
        public bool SendPluginChannelPacket(string channel, byte[] data)
        {
            try
            {
                // In 1.7, length needs to be included.
                // In 1.8, it must not be.
                if (protocolversion < MC18Version)
                {
                    byte[] length = BitConverter.GetBytes((short)data.Length);
                    Array.Reverse(length);

                    SendPacket(PacketOutgoingType.PluginMessage, dataTypes.ConcatBytes(dataTypes.GetString(channel), length, data));
                }
                else
                {
                    SendPacket(PacketOutgoingType.PluginMessage, dataTypes.ConcatBytes(dataTypes.GetString(channel), data));
                }

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
            socketWrapper.Disconnect();
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

            byte[] transaction_id = dataTypes.GetVarInt(autocomplete_transaction_id);
            byte[] assume_command = new byte[] { 0x00 };
            byte[] has_position = new byte[] { 0x00 };

            byte[] tabcomplete_packet = new byte[] { };

            if (protocolversion >= MC18Version)
            {
                if (protocolversion >= MC113Version)
                {
                    tabcomplete_packet = dataTypes.ConcatBytes(tabcomplete_packet, transaction_id);
                    tabcomplete_packet = dataTypes.ConcatBytes(tabcomplete_packet, dataTypes.GetString(BehindCursor));
                }
                else
                {
                    tabcomplete_packet = dataTypes.ConcatBytes(tabcomplete_packet, dataTypes.GetString(BehindCursor));

                    if (protocolversion >= MC19Version)
                    {
                        tabcomplete_packet = dataTypes.ConcatBytes(tabcomplete_packet, assume_command);
                    }

                    tabcomplete_packet = dataTypes.ConcatBytes(tabcomplete_packet, has_position);
                }
            }
            else
            {
                tabcomplete_packet = dataTypes.ConcatBytes(dataTypes.GetString(BehindCursor));
            }

            autocomplete_received = false;
            autocomplete_result.Clear();
            autocomplete_result.Add(BehindCursor);
            SendPacket(PacketOutgoingType.TabComplete, tabcomplete_packet);

            int wait_left = 50; //do not wait more than 5 seconds (50 * 100 ms)
            while (wait_left > 0 && !autocomplete_received) { System.Threading.Thread.Sleep(100); wait_left--; }
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
            SocketWrapper socketWrapper = new SocketWrapper(tcp);
            DataTypes dataTypes = new DataTypes(MC18Version);

            byte[] packet_id = dataTypes.GetVarInt(0);
            byte[] protocol_version = dataTypes.GetVarInt(-1);
            byte[] server_port = BitConverter.GetBytes((ushort)port); Array.Reverse(server_port);
            byte[] next_state = dataTypes.GetVarInt(1);
            byte[] packet = dataTypes.ConcatBytes(packet_id, protocol_version, dataTypes.GetString(host), server_port, next_state);
            byte[] tosend = dataTypes.ConcatBytes(dataTypes.GetVarInt(packet.Length), packet);

            socketWrapper.SendDataRAW(tosend);

            byte[] status_request = dataTypes.GetVarInt(0);
            byte[] request_packet = dataTypes.ConcatBytes(dataTypes.GetVarInt(status_request.Length), status_request);

            socketWrapper.SendDataRAW(request_packet);

            int packetLength = dataTypes.ReadNextVarIntRAW(socketWrapper);
            if (packetLength > 0) //Read Response length
            {
                List<byte> packetData = new List<byte>(socketWrapper.ReadDataRAW(packetLength));
                if (dataTypes.ReadNextVarInt(packetData) == 0x00) //Read Packet ID
                {
                    string result = dataTypes.ReadNextString(packetData); //Get the Json data

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
                                protocolversion = dataTypes.Atoi(versionData.Properties["protocol"].StringValue);

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
