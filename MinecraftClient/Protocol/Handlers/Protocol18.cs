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

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Implementation for Minecraft 1.7.X, 1.8.X, 1.9.X, 1.10.X Protocols
    /// </summary>
    class Protocol18Handler : IMinecraftCom
    {
        private const int MC18Version = 47;
        private const int MC19Version = 107;
        private const int MC191Version = 108;
        private const int MC110Version = 210;
        private const int MC111Version = 315;

        private int compression_treshold = 0;
        private bool autocomplete_received = false;
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

        public Protocol18Handler(TcpClient Client, int ProtocolVersion, IMinecraftComHandler Handler, ForgeInfo ForgeInfo)
        {
            ConsoleIO.SetAutoCompleteEngine(this);
            ChatParser.InitTranslations();
            this.c = Client;
            this.protocolversion = ProtocolVersion;
            this.handler = Handler;
            this.forgeInfo = ForgeInfo;
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
        /// <returns>FALSE if an error occured, TRUE otherwise.</returns>
        private bool Update()
        {
            handler.OnUpdate();
            if (c.Client == null || !c.Connected) { return false; }
            try
            {
                while (c.Client.Available > 0)
                {
                    int packetID = 0;
                    List<byte> packetData = new List<byte>();
                    readNextPacket(ref packetID, packetData);
                    handlePacket(packetID, new List<byte>(packetData));
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
        private void readNextPacket(ref int packetID, List<byte> packetData)
        {
            packetData.Clear();
            int size = readNextVarIntRAW(); //Packet size
            packetData.AddRange(readDataRAW(size)); //Packet contents

            //Handle packet decompression
            if (protocolversion >= MC18Version
                && compression_treshold > 0)
            {
                int sizeUncompressed = readNextVarInt(packetData);
                if (sizeUncompressed != 0) // != 0 means compressed, let's decompress
                {
                    byte[] toDecompress = packetData.ToArray();
                    byte[] uncompressed = ZlibUtils.Decompress(toDecompress, sizeUncompressed);
                    packetData.Clear();
                    packetData.AddRange(uncompressed);
                }
            }

            packetID = readNextVarInt(packetData); //Packet ID
        }

        /// <summary>
        /// Abstract incoming packet numbering
        /// </summary>
        private enum PacketIncomingType
        {
            KeepAlive,
            JoinGame,
            ChatMessage,
            Respawn,
            PlayerPositionAndLook,
            ChunkData,
            MultiBlockChange,
            BlockChange,
            MapChunkBulk,
            UnloadChunk,
            PlayerListUpdate,
            TabCompleteResult,
            PluginMessage,
            KickPacket,
            NetworkCompressionTreshold,
            ResourcePackSend,
            UnknownPacket
        }

        /// <summary>
        /// Get abstract numbering ov the specified packet ID
        /// </summary>
        /// <param name="packetID">Packet ID</param>
        /// <param name="protocol">Protocol version</param>
        /// <returns>Abstract numbering</returns>
        private PacketIncomingType getPacketIncomingType(int packetID, int protocol)
        {
            if (protocol < MC19Version)
            {
                switch (packetID)
                {
                    case 0x00: return PacketIncomingType.KeepAlive;
                    case 0x01: return PacketIncomingType.JoinGame;
                    case 0x02: return PacketIncomingType.ChatMessage;
                    case 0x07: return PacketIncomingType.Respawn;
                    case 0x08: return PacketIncomingType.PlayerPositionAndLook;
                    case 0x21: return PacketIncomingType.ChunkData;
                    case 0x22: return PacketIncomingType.MultiBlockChange;
                    case 0x23: return PacketIncomingType.BlockChange;
                    case 0x26: return PacketIncomingType.MapChunkBulk;
                    //UnloadChunk does not exists prior to 1.9
                    case 0x38: return PacketIncomingType.PlayerListUpdate;
                    case 0x3A: return PacketIncomingType.TabCompleteResult;
                    case 0x3F: return PacketIncomingType.PluginMessage;
                    case 0x40: return PacketIncomingType.KickPacket;
                    case 0x46: return PacketIncomingType.NetworkCompressionTreshold;
                    case 0x48: return PacketIncomingType.ResourcePackSend;
                    default: return PacketIncomingType.UnknownPacket;
                }
            }
            else
            {
                switch (packetID)
                {
                    case 0x1F: return PacketIncomingType.KeepAlive;
                    case 0x23: return PacketIncomingType.JoinGame;
                    case 0x0F: return PacketIncomingType.ChatMessage;
                    case 0x33: return PacketIncomingType.Respawn;
                    case 0x2E: return PacketIncomingType.PlayerPositionAndLook;
                    case 0x20: return PacketIncomingType.ChunkData;
                    case 0x10: return PacketIncomingType.MultiBlockChange;
                    case 0x0B: return PacketIncomingType.BlockChange;
                    //MapChunkBulk removed in 1.9
                    case 0x1D: return PacketIncomingType.UnloadChunk;
                    case 0x2D: return PacketIncomingType.PlayerListUpdate;
                    case 0x0E: return PacketIncomingType.TabCompleteResult;
                    case 0x18: return PacketIncomingType.PluginMessage;
                    case 0x1A: return PacketIncomingType.KickPacket;
                    //NetworkCompressionTreshold removed in 1.9
                    case 0x32: return PacketIncomingType.ResourcePackSend;
                    default: return PacketIncomingType.UnknownPacket;
                }
            }
        }

        /// <summary>
        /// Handle the given packet
        /// </summary>
        /// <param name="packetID">Packet ID</param>
        /// <param name="packetData">Packet contents</param>
        /// <returns>TRUE if the packet was processed, FALSE if ignored or unknown</returns>
        private bool handlePacket(int packetID, List<byte> packetData)
        {
            if (login_phase)
            {
                switch (packetID) //Packet IDs are different while logging in
                {
                    case 0x03:
                        if (protocolversion >= MC18Version)
                            compression_treshold = readNextVarInt(packetData);
                        break;
                    default:
                        return false; //Ignored packet
                }
            }
            // Regular in-game packets
            switch (getPacketIncomingType(packetID, protocolversion))
            {
                case PacketIncomingType.KeepAlive:
                    SendPacket(protocolversion >= MC19Version ? 0x0B : 0x00, packetData);
                    break;
                case PacketIncomingType.JoinGame:
                    handler.OnGameJoined();
                    readNextInt(packetData);
                    readNextByte(packetData);
                    if (protocolversion >= MC191Version)
                        this.currentDimension = readNextInt(packetData);
                    else
                        this.currentDimension = (sbyte)readNextByte(packetData);
                    readNextByte(packetData);
                    readNextByte(packetData);
                    readNextString(packetData);
                    if (protocolversion >= MC18Version)
                        readNextBool(packetData);  // Reduced debug info - 1.8 and above
                    break;
                case PacketIncomingType.ChatMessage:
                    string message = readNextString(packetData);
                    try
                    {
                        //Hide system messages or xp bar messages?
                        byte messageType = readNextByte(packetData);
                        if ((messageType == 1 && !Settings.DisplaySystemMessages)
                            || (messageType == 2 && !Settings.DisplayXPBarMessages))
                            break;
                    }
                    catch (ArgumentOutOfRangeException) { /* No message type */ }
                    List<string> links = new List<string>();
                    handler.OnTextReceived(ChatParser.ParseText(message, links), links);
                    break;
                case PacketIncomingType.Respawn:
                    this.currentDimension = readNextInt(packetData);
                    readNextByte(packetData);
                    readNextByte(packetData);
                    readNextString(packetData);
                    break;
                case PacketIncomingType.PlayerPositionAndLook:
                    if (Settings.TerrainAndMovements)
                    {
                        double x = readNextDouble(packetData);
                        double y = readNextDouble(packetData);
                        double z = readNextDouble(packetData);
                        readData(8, packetData); //Ignore look
                        byte locMask = readNextByte(packetData);

                        if (protocolversion >= MC18Version)
                        {
                            Location location = handler.GetCurrentLocation();
                            location.X = (locMask & 1 << 0) != 0 ? location.X + x : x;
                            location.Y = (locMask & 1 << 1) != 0 ? location.Y + y : y;
                            location.Z = (locMask & 1 << 2) != 0 ? location.Z + z : z;
                            handler.UpdateLocation(location);
                        }
                        else handler.UpdateLocation(new Location(x, y, z));

                        if (protocolversion >= MC19Version)
                        {
                            int teleportID = readNextVarInt(packetData);
                            // Teleport confirm packet
                            SendPacket(0x00, getVarInt(teleportID));
                        }
                    }
                    break;
                case PacketIncomingType.ChunkData:
                    if (Settings.TerrainAndMovements)
                    {
                        int chunkX = readNextInt(packetData);
                        int chunkZ = readNextInt(packetData);
                        bool chunksContinuous = readNextBool(packetData);
                        ushort chunkMask = protocolversion >= MC19Version
                            ? (ushort)readNextVarInt(packetData)
                            : readNextUShort(packetData);
                        if (protocolversion < MC18Version)
                        {
                            ushort addBitmap = readNextUShort(packetData);
                            int compressedDataSize = readNextInt(packetData);
                            byte[] compressed = readData(compressedDataSize, packetData);
                            byte[] decompressed = ZlibUtils.Decompress(compressed);
                            ProcessChunkColumnData(chunkX, chunkZ, chunkMask, addBitmap, currentDimension == 0, chunksContinuous, new List<byte>(decompressed));
                        }
                        else
                        {
                            int dataSize = readNextVarInt(packetData);
                            ProcessChunkColumnData(chunkX, chunkZ, chunkMask, 0, false, chunksContinuous, packetData);
                        }
                    }
                    break;
                case PacketIncomingType.MultiBlockChange:
                    if (Settings.TerrainAndMovements)
                    {
                        int chunkX = readNextInt(packetData);
                        int chunkZ = readNextInt(packetData);
                        int recordCount = protocolversion < MC18Version
                            ? (int)readNextShort(packetData)
                            : readNextVarInt(packetData);

                        for (int i = 0; i < recordCount; i++)
                        {
                            byte locationXZ;
                            ushort blockIdMeta;
                            int blockY;

                            if (protocolversion < MC18Version)
                            {
                                blockIdMeta = readNextUShort(packetData);
                                blockY = (ushort)readNextByte(packetData);
                                locationXZ = readNextByte(packetData);
                            }
                            else
                            {
                                locationXZ = readNextByte(packetData);
                                blockY = (ushort)readNextByte(packetData);
                                blockIdMeta = (ushort)readNextVarInt(packetData);
                            }

                            int blockX = locationXZ >> 4;
                            int blockZ = locationXZ & 0x0F;
                            Block block = new Block(blockIdMeta);
                            handler.GetWorld().SetBlock(new Location(chunkX, chunkZ, blockX, blockY, blockZ), block);
                        }
                    }
                    break;
                case PacketIncomingType.BlockChange:
                    if (Settings.TerrainAndMovements)
                        if (protocolversion < MC18Version)
                        {
                            int blockX = readNextInt(packetData);
                            int blockY = readNextByte(packetData);
                            int blockZ = readNextInt(packetData);
                            short blockId = (short)readNextVarInt(packetData);
                            byte blockMeta = readNextByte(packetData);
                            handler.GetWorld().SetBlock(new Location(blockX, blockY, blockZ), new Block(blockId, blockMeta));
                        }
                        else handler.GetWorld().SetBlock(Location.FromLong(readNextULong(packetData)), new Block((ushort)readNextVarInt(packetData)));
                    break;
                case PacketIncomingType.MapChunkBulk:
                    if (protocolversion < MC19Version && Settings.TerrainAndMovements)
                    {
                        int chunkCount;
                        bool hasSkyLight;
                        List<byte> chunkData = packetData;

                        //Read global fields
                        if (protocolversion < MC18Version)
                        {
                            chunkCount = readNextShort(packetData);
                            int compressedDataSize = readNextInt(packetData);
                            hasSkyLight = readNextBool(packetData);
                            byte[] compressed = readData(compressedDataSize, packetData);
                            byte[] decompressed = ZlibUtils.Decompress(compressed);
                            chunkData = new List<byte>(decompressed);
                        }
                        else
                        {
                            hasSkyLight = readNextBool(packetData);
                            chunkCount = readNextVarInt(packetData);
                        }

                        //Read chunk records
                        int[] chunkXs = new int[chunkCount];
                        int[] chunkZs = new int[chunkCount];
                        ushort[] chunkMasks = new ushort[chunkCount];
                        ushort[] addBitmaps = new ushort[chunkCount];
                        for (int chunkColumnNo = 0; chunkColumnNo < chunkCount; chunkColumnNo++)
                        {
                            chunkXs[chunkColumnNo] = readNextInt(packetData);
                            chunkZs[chunkColumnNo] = readNextInt(packetData);
                            chunkMasks[chunkColumnNo] = readNextUShort(packetData);
                            addBitmaps[chunkColumnNo] = protocolversion < MC18Version
                                ? readNextUShort(packetData)
                                : (ushort)0;
                        }

                        //Process chunk records
                        for (int chunkColumnNo = 0; chunkColumnNo < chunkCount; chunkColumnNo++)
                            ProcessChunkColumnData(chunkXs[chunkColumnNo], chunkZs[chunkColumnNo], chunkMasks[chunkColumnNo], addBitmaps[chunkColumnNo], hasSkyLight, true, chunkData);
                    }
                    break;
                case PacketIncomingType.UnloadChunk:
                    if (protocolversion >= MC19Version && Settings.TerrainAndMovements)
                    {
                        int chunkX = readNextInt(packetData);
                        int chunkZ = readNextInt(packetData);
                        handler.GetWorld()[chunkX, chunkZ] = null;
                    }
                    break;
                case PacketIncomingType.PlayerListUpdate:
                    if (protocolversion >= MC18Version)
                    {
                        int action = readNextVarInt(packetData);
                        int numActions = readNextVarInt(packetData);
                        for (int i = 0; i < numActions; i++)
                        {
                            Guid uuid = readNextUUID(packetData);
                            switch (action)
                            {
                                case 0x00: //Player Join
                                    string name = readNextString(packetData);
                                    int propNum = readNextVarInt(packetData);
                                    for (int p = 0; p < propNum; p++)
                                    {
                                        string key = readNextString(packetData);
                                        string val = readNextString(packetData);
                                        if (readNextBool(packetData))
                                            readNextString(packetData);
                                    }
                                    readNextVarInt(packetData);
                                    readNextVarInt(packetData);
                                    if (readNextBool(packetData))
                                        readNextString(packetData);
                                    handler.OnPlayerJoin(uuid, name);
                                    break;
                                case 0x01: //Update gamemode
                                case 0x02: //Update latency
                                    readNextVarInt(packetData);
                                    break;
                                case 0x03: //Update display name
                                    if (readNextBool(packetData))
                                        readNextString(packetData);
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
                        string name = readNextString(packetData);
                        bool online = readNextBool(packetData);
                        short ping = readNextShort(packetData);
                        Guid FakeUUID = new Guid(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(name)).Take(16).ToArray());
                        if (online)
                            handler.OnPlayerJoin(FakeUUID, name);
                        else handler.OnPlayerLeave(FakeUUID);
                    }
                    break;
                case PacketIncomingType.TabCompleteResult:
                    int autocomplete_count = readNextVarInt(packetData);
                    autocomplete_result.Clear();
                    for (int i = 0; i < autocomplete_count; i++)
                        autocomplete_result.Add(readNextString(packetData));
                    autocomplete_received = true;
                    break;
                case PacketIncomingType.PluginMessage:
                    String channel = readNextString(packetData);
                    if (protocolversion < MC18Version)
                    {
                        if (forgeInfo == null)
                        {
                            // 1.7 and lower prefix plugin channel packets with the length.
                            // We can skip it, though.
                            readNextShort(packetData);
                        }
                        else
                        {
                            // Forge does something even weirder with the length.
                            readNextVarShort(packetData);
                        }
                    }

                    // The remaining data in the array is the entire payload of the packet.
                    handler.OnPluginChannelMessage(channel, packetData.ToArray());

                    #region Forge Login
                    if (forgeInfo != null && fmlHandshakeState != FMLHandshakeClientState.DONE)
                    {
                        if (channel == "FML|HS")
                        {
                            FMLHandshakeDiscriminator discriminator = (FMLHandshakeDiscriminator)readNextByte(packetData);

                            if (discriminator == FMLHandshakeDiscriminator.HandshakeReset)
                            {
                                fmlHandshakeState = FMLHandshakeClientState.START;
                                return true;
                            }

                            switch (fmlHandshakeState)
                            {
                                case FMLHandshakeClientState.START:
                                    if (discriminator != FMLHandshakeDiscriminator.ServerHello)
                                        return false;

                                    // Send the plugin channel registration.
                                    // REGISTER is somewhat special in that it doesn't actually include length information,
                                    // and is also \0-separated.
                                    // Also, yes, "FML" is there twice.  Don't ask me why, but that's the way forge does it.
                                    string[] channels = { "FML|HS", "FML", "FML|MP", "FML", "FORGE" };
                                    SendPluginChannelPacket("REGISTER", Encoding.UTF8.GetBytes(string.Join("\0", channels)));

                                    byte fmlProtocolVersion = readNextByte(packetData);

                                    if (Settings.DebugMessages)
                                        ConsoleIO.WriteLineFormatted("§8Forge protocol version : " + fmlProtocolVersion);

                                    if (fmlProtocolVersion >= 1)
                                        this.currentDimension = readNextInt(packetData);

                                    // Tell the server we're running the same version.
                                    SendForgeHandshakePacket(FMLHandshakeDiscriminator.ClientHello, new byte[] { fmlProtocolVersion });

                                    // Then tell the server that we're running the same mods.
                                    if (Settings.DebugMessages)
                                        ConsoleIO.WriteLineFormatted("§8Sending falsified mod list to server...");
                                    byte[][] mods = new byte[forgeInfo.Mods.Count][];
                                    for (int i = 0; i < forgeInfo.Mods.Count; i++)
                                    {
                                        ForgeInfo.ForgeMod mod = forgeInfo.Mods[i];
                                        mods[i] = concatBytes(getString(mod.ModID), getString(mod.Version));
                                    }
                                    SendForgeHandshakePacket(FMLHandshakeDiscriminator.ModList,
                                        concatBytes(getVarInt(forgeInfo.Mods.Count), concatBytes(mods)));

                                    fmlHandshakeState = FMLHandshakeClientState.WAITINGSERVERDATA;

                                    return true;
                                case FMLHandshakeClientState.WAITINGSERVERDATA:
                                    if (discriminator != FMLHandshakeDiscriminator.ModList)
                                        return false;

                                    Thread.Sleep(2000);

                                    if (Settings.DebugMessages)
                                        ConsoleIO.WriteLineFormatted("§8Accepting server mod list...");
                                    // Tell the server that yes, we are OK with the mods it has
                                    // even though we don't actually care what mods it has.

                                    SendForgeHandshakePacket(FMLHandshakeDiscriminator.HandshakeAck,
                                        new byte[] { (byte)FMLHandshakeClientState.WAITINGSERVERDATA });

                                    fmlHandshakeState = FMLHandshakeClientState.WAITINGSERVERCOMPLETE;
                                    return false;
                                case FMLHandshakeClientState.WAITINGSERVERCOMPLETE:
                                    // The server now will tell us a bunch of registry information.
                                    // We need to read it all, though, until it says that there is no more.
                                    if (discriminator != FMLHandshakeDiscriminator.RegistryData)
                                        return false;

                                    if (protocolversion < MC18Version)
                                    {
                                        // 1.7.10 and below have one registry
                                        // with blocks and items.
                                        int registrySize = readNextVarInt(packetData);

                                        if (Settings.DebugMessages)
                                            ConsoleIO.WriteLineFormatted("§8Received registry with " + registrySize + " entries");

                                        fmlHandshakeState = FMLHandshakeClientState.PENDINGCOMPLETE;
                                    }
                                    else
                                    {
                                        // 1.8+ has more than one registry.

                                        bool hasNextRegistry = readNextBool(packetData);
                                        string registryName = readNextString(packetData);
                                        int registrySize = readNextVarInt(packetData);
                                        if (Settings.DebugMessages)
                                            ConsoleIO.WriteLineFormatted("§8Received registry " + registryName + " with " + registrySize + " entries");
                                        if (!hasNextRegistry)
                                            fmlHandshakeState = FMLHandshakeClientState.PENDINGCOMPLETE;
                                    }

                                    return false;
                                case FMLHandshakeClientState.PENDINGCOMPLETE:
                                    // The server will ask us to accept the registries.
                                    // Just say yes.
                                    if (discriminator != FMLHandshakeDiscriminator.HandshakeAck)
                                        return false;
                                    if (Settings.DebugMessages)
                                        ConsoleIO.WriteLineFormatted("§8Accepting server registries...");
                                    SendForgeHandshakePacket(FMLHandshakeDiscriminator.HandshakeAck,
                                        new byte[] { (byte)FMLHandshakeClientState.PENDINGCOMPLETE });
                                    fmlHandshakeState = FMLHandshakeClientState.COMPLETE;

                                    return true;
                                case FMLHandshakeClientState.COMPLETE:
                                    // One final "OK".  On the actual forge source, a packet is sent from
                                    // the client to the client saying that the connection was complete, but
                                    // we don't need to do that.

                                    SendForgeHandshakePacket(FMLHandshakeDiscriminator.HandshakeAck,
                                        new byte[] { (byte)FMLHandshakeClientState.COMPLETE });
                                    if (Settings.DebugMessages)
                                        ConsoleIO.WriteLine("Forge server connection complete!");
                                    fmlHandshakeState = FMLHandshakeClientState.DONE;
                                    return true;
                            }
                        }
                    }
                    #endregion

                    return false;
                case PacketIncomingType.KickPacket:
                    handler.OnConnectionLost(ChatBot.DisconnectReason.InGameKick, ChatParser.ParseText(readNextString(packetData)));
                    return false;
                case PacketIncomingType.NetworkCompressionTreshold:
                    if (protocolversion >= MC18Version && protocolversion < MC19Version)
                        compression_treshold = readNextVarInt(packetData);
                    break;
                case PacketIncomingType.ResourcePackSend:
                    string url = readNextString(packetData);
                    string hash = readNextString(packetData);
                    //Send back "accepted" and "successfully loaded" responses for plugins making use of resource pack mandatory
                    byte[] responseHeader = new byte[0];
                    if (protocolversion < MC110Version) //MC 1.10 does not include resource pack hash in responses
                        responseHeader = concatBytes(getVarInt(hash.Length), Encoding.UTF8.GetBytes(hash));
                    int packResponsePid = protocolversion >= MC19Version ? 0x16 : 0x19; //ID changed in 1.9
                    SendPacket(packResponsePid, concatBytes(responseHeader, getVarInt(3))); //Accepted pack
                    SendPacket(packResponsePid, concatBytes(responseHeader, getVarInt(0))); //Successfully loaded
                    break;
                default:
                    return false; //Ignored packet
            }
            return true; //Packet processed
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
                        byte bitsPerBlock = readNextByte(cache);
                        bool usePalette = (bitsPerBlock <= 8);

                        int paletteLength = readNextVarInt(cache);
                        int[] palette = new int[paletteLength];
                        for (int i = 0; i < paletteLength; i++)
                        {
                            palette[i] = readNextVarInt(cache);
                        }

                        // Bit mask covering bitsPerBlock bits
                        // EG, if bitsPerBlock = 5, valueMask = 00011111 in binary
                        uint valueMask = (uint)((1 << bitsPerBlock) - 1);

                        ulong[] dataArray = readNextULongArray(cache);

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
                        readData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ) / 2, cache);

                        //Skip sky light
                        if (this.currentDimension == 0)
                            // Sky light is not sent in the nether or the end
                            readData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ) / 2, cache);
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
                            Queue<ushort> queue = new Queue<ushort>(readNextUShortsLittleEndian(Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ, cache));
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
                            readData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ) / 2, cache);

                            //Skip sky light
                            if (hasSkyLight)
                                readData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ) / 2, cache);
                        }
                    }

                    //Skip biome metadata
                    if (chunksContinuous)
                        readData(Chunk.SizeX * Chunk.SizeZ, cache);
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
                    Queue<byte> blockTypes = new Queue<byte>(readData(Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ * sectionCount, cache));
                    Queue<byte> blockMeta = new Queue<byte>();
                    foreach (byte packed in readData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ * sectionCount) / 2, cache))
                    {
                        byte hig = (byte)(packed >> 4);
                        byte low = (byte)(packed & (byte)0x0F);
                        blockMeta.Enqueue(hig);
                        blockMeta.Enqueue(low);
                    }

                    //Skip data we don't need
                    readData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ * sectionCount) / 2, cache);          //Block light
                    if (hasSkyLight)
                        readData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ * sectionCount) / 2, cache);      //Sky light
                    readData((Chunk.SizeX * Chunk.SizeY * Chunk.SizeZ * addDataSectionCount) / 2, cache);   //BlockAdd
                    if (chunksContinuous)
                        readData(Chunk.SizeX * Chunk.SizeZ, cache);                                         //Biomes

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
        private static byte[] readData(int offset, List<byte> cache)
        {
            byte[] result = cache.Take(offset).ToArray();
            cache.RemoveRange(0, offset);
            return result;
        }

        /// <summary>
        /// Read a string from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The string</returns>
        private static string readNextString(List<byte> cache)
        {
            int length = readNextVarInt(cache);
            if (length > 0)
            {
                return Encoding.UTF8.GetString(readData(length, cache));
            }
            else return "";
        }

        /// <summary>
        /// Read a boolean from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The boolean value</returns>
        private static bool readNextBool(List<byte> cache)
        {
            return readNextByte(cache) != 0x00;
        }

        /// <summary>
        /// Read a short integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The short integer value</returns>
        private static short readNextShort(List<byte> cache)
        {
            byte[] rawValue = readData(2, cache);
            Array.Reverse(rawValue); //Endianness
            return BitConverter.ToInt16(rawValue, 0);
        }

        /// <summary>
        /// Read an integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The integer value</returns>
        private static int readNextInt(List<byte> cache)
        {
            byte[] rawValue = readData(4, cache);
            Array.Reverse(rawValue); //Endianness
            return BitConverter.ToInt32(rawValue, 0);
        }

        /// <summary>
        /// Read an unsigned short integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The unsigned short integer value</returns>
        private static ushort readNextUShort(List<byte> cache)
        {
            byte[] rawValue = readData(2, cache);
            Array.Reverse(rawValue); //Endianness
            return BitConverter.ToUInt16(rawValue, 0);
        }

        /// <summary>
        /// Read an unsigned long integer from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The unsigned long integer value</returns>
        private static ulong readNextULong(List<byte> cache)
        {
            byte[] rawValue = readData(8, cache);
            Array.Reverse(rawValue); //Endianness
            return BitConverter.ToUInt64(rawValue, 0);
        }

        /// <summary>
        /// Read several little endian unsigned short integers at once from a cache of bytes and remove them from the cache
        /// </summary>
        /// <returns>The unsigned short integer value</returns>
        private static ushort[] readNextUShortsLittleEndian(int amount, List<byte> cache)
        {
            byte[] rawValues = readData(2 * amount, cache);
            ushort[] result = new ushort[amount];
            for (int i = 0; i < amount; i++)
                result[i] = BitConverter.ToUInt16(rawValues, i * 2);
            return result;
        }

        /// <summary>
        /// Read a uuid from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The uuid</returns>
        private static Guid readNextUUID(List<byte> cache)
        {
            return new Guid(readData(16, cache));
        }

        /// <summary>
        /// Read a byte array from a cache of bytes and remove it from the cache
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The byte array</returns>
        private byte[] readNextByteArray(List<byte> cache)
        {
            int len = protocolversion >= MC18Version
                ? readNextVarInt(cache)
                : readNextShort(cache);
            return readData(len, cache);
        }

        /// <summary>
        /// Reads a length-prefixed array of unsigned long integers and removes it from the cache
        /// </summary>
        /// <returns>The unsigned long integer values</returns>
        private static ulong[] readNextULongArray(List<byte> cache)
        {
            int len = readNextVarInt(cache);
            ulong[] result = new ulong[len];
            for (int i = 0; i < len; i++)
                result[i] = readNextULong(cache);
            return result;
        }

        /// <summary>
        /// Read a double from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The double value</returns>
        private static double readNextDouble(List<byte> cache)
        {
            byte[] rawValue = readData(8, cache);
            Array.Reverse(rawValue); //Endianness
            return BitConverter.ToDouble(rawValue, 0);
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
        private static int readNextVarInt(List<byte> cache)
        {
            int i = 0;
            int j = 0;
            int k = 0;
            while (true)
            {
                k = readNextByte(cache);
                i |= (k & 0x7F) << j++ * 7;
                if (j > 5) throw new OverflowException("VarInt too big");
                if ((k & 0x80) != 128) break;
            }
            return i;
        }

        /// <summary>
        /// Read an "extended short", which is actually an int of some kind, from the cache of bytes.
        /// This is only done with forge.  It looks like it's a normal short, except that if the high
        /// bit is set, it has an extra byte.
        /// </summary>
        /// <param name="cache">Cache of bytes to read from</param>
        /// <returns>The int</returns>
        private static int readNextVarShort(List<byte> cache)
        {
            ushort low = readNextUShort(cache);
            byte high = 0;
            if ((low & 0x8000) != 0)
            {
                low &= 0x7FFF;
                high = readNextByte(cache);
            }
            return ((high & 0xFF) << 15) | low;
        }

        /// <summary>
        /// Read a single byte from a cache of bytes and remove it from the cache
        /// </summary>
        /// <returns>The byte that was read</returns>
        private static byte readNextByte(List<byte> cache)
        {
            byte result = cache[0];
            cache.RemoveAt(0);
            return result;
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
        /// Get byte array representing a double
        /// </summary>
        /// <param name="array">Array to process</param>
        /// <returns>Array ready to send</returns>
        private byte[] getDouble(double number)
        {
            byte[] theDouble = BitConverter.GetBytes(number);
            Array.Reverse(theDouble); //Endianness
            return theDouble;
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
        /// Get a byte array from the given string for sending over the network, with length information prepended.
        /// </summary>
        /// <param name="array">String to process</param>
        /// <returns>Array ready to send</returns>
        private byte[] getString(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);

            return concatBytes(getVarInt(bytes.Length), bytes);
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
        /// Send a forge plugin channel packet ("FML|HS").  Compression and encryption will be handled automatically
        /// </summary>
        /// <param name="discriminator">Discriminator to use.</param>
        /// <param name="data">packet Data</param>
        private void SendForgeHandshakePacket(FMLHandshakeDiscriminator discriminator, byte[] data)
        {
            SendPluginChannelPacket("FML|HS", concatBytes(new byte[] { (byte)discriminator }, data));
        }

        /// <summary>
        /// Send a packet to the server, compression and encryption will be handled automatically
        /// </summary>
        /// <param name="packetID">packet ID</param>
        /// <param name="packetData">packet Data</param>
        private void SendPacket(int packetID, IEnumerable<byte> packetData)
        {
            //The inner packet
            byte[] the_packet = concatBytes(getVarInt(packetID), packetData.ToArray());

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
            byte[] server_adress_val = Encoding.UTF8.GetBytes(handler.GetServerHost() + (forgeInfo != null ? "\0FML\0" : ""));
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
            List<byte> packetData = new List<byte>();
            while (true)
            {
                readNextPacket(ref packetID, packetData);
                if (packetID == 0x00) //Login rejected
                {
                    handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected, ChatParser.ParseText(readNextString(packetData)));
                    return false;
                }
                else if (packetID == 0x01) //Encryption request
                {
                    string serverID = readNextString(packetData);
                    byte[] Serverkey = readNextByteArray(packetData);
                    byte[] token = readNextByteArray(packetData);
                    return StartEncryption(handler.GetUserUUID(), handler.GetSessionID(), token, serverID, Serverkey);
                }
                else if (packetID == 0x02) //Login successful
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
                else handlePacket(packetID, packetData);
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
                    handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected, ChatParser.ParseText(readNextString(packetData)));
                    return false;
                }
                else
                {
                    handlePacket(packetID, packetData);
                }
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
            byte[] key_enc = getArray(RSAService.Encrypt(secretKey, false));
            byte[] token_enc = getArray(RSAService.Encrypt(token, false));

            //Encryption Response packet
            SendPacket(0x01, concatBytes(key_enc, token_enc));

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
                    handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected, ChatParser.ParseText(readNextString(packetData)));
                    return false;
                }
                else if (packetID == 0x02) //Login successful
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
                else handlePacket(packetID, packetData);
            }
        }

        /// <summary>
        /// Get max length for chat messages
        /// </summary>
        /// <returns>Max length, in characters</returns>
        public int GetMaxChatMessageLength()
        {
            return protocolversion >= MC111Version
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
                byte[] message_val = Encoding.UTF8.GetBytes(message);
                byte[] message_len = getVarInt(message_val.Length);
                byte[] message_packet = concatBytes(message_len, message_val);
                SendPacket(protocolversion >= MC19Version ? 0x02 : 0x01, message_packet);
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
                SendPacket(protocolversion >= MC19Version ? 0x03 : 0x16, new byte[] { 0 });
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

            return SendPluginChannelPacket("MC|Brand", getString(brandInfo));
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
                fields.AddRange(getString(language));
                fields.Add(viewDistance);
                fields.AddRange(protocolversion >= MC19Version
                    ? getVarInt(chatMode)
                    : new byte[] { chatMode });
                fields.Add(chatColors ? (byte)1 : (byte)0);
                if (protocolversion < MC18Version)
                {
                    fields.Add(difficulty);
                    fields.Add((byte)(skinParts & 0x1)); //show cape
                }
                else fields.Add(skinParts);
                if (protocolversion >= MC19Version)
                    fields.AddRange(getVarInt(mainHand));
                SendPacket(protocolversion >= MC19Version ? 0x04 : 0x15, fields);
            }
            catch (SocketException) { }
            return false;
        }

        /// <summary>
        /// Send a location update to the server
        /// </summary>
        /// <param name="location">The new location of the player</param>
        /// <param name="onGround">True if the player is on the ground</param>
        /// <returns>True if the location update was successfully sent</returns>
        public bool SendLocationUpdate(Location location, bool onGround)
        {
            if (Settings.TerrainAndMovements)
            {
                try
                {
                    SendPacket(protocolversion >= MC19Version ? 0x0C : 0x04, concatBytes(
                        getDouble(location.X),
                        getDouble(location.Y),
                        protocolversion < MC18Version
                            ? getDouble(location.Y + 1.62)
                            : new byte[0],
                        getDouble(location.Z),
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

                    SendPacket(0x17, concatBytes(getString(channel), length, data));
                }
                else
                {
                    SendPacket(protocolversion >= MC19Version ? 0x09 : 0x17, concatBytes(getString(channel), data));
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
        IEnumerable<string> IAutoComplete.AutoComplete(string BehindCursor)
        {
            if (String.IsNullOrEmpty(BehindCursor))
                return new string[] { };

            byte[] tocomplete_val = Encoding.UTF8.GetBytes(BehindCursor);
            byte[] tocomplete_len = getVarInt(tocomplete_val.Length);
            byte[] assume_command = new byte[] { 0x00 };
            byte[] has_position = new byte[] { 0x00 };
            byte[] tabcomplete_packet = protocolversion >= MC18Version
                ? protocolversion >= MC19Version
                    ? concatBytes(tocomplete_len, tocomplete_val, assume_command, has_position)
                    : concatBytes(tocomplete_len, tocomplete_val, has_position)
                : concatBytes(tocomplete_len, tocomplete_val);

            autocomplete_received = false;
            autocomplete_result.Clear();
            autocomplete_result.Add(BehindCursor);
            SendPacket(protocolversion >= MC19Version ? 0x01 : 0x14, tabcomplete_packet);

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
                List<byte> packetData = new List<byte>(ComTmp.readDataRAW(packetLength));
                if (readNextVarInt(packetData) == 0x00) //Read Packet ID
                {
                    string result = readNextString(packetData); //Get the Json data

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
