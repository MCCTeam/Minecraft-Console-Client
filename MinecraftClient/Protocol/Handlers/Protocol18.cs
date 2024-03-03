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
using MinecraftClient.Mapping.EntityPalettes;
using MinecraftClient.Protocol.Handlers.Forge;
using MinecraftClient.Inventory;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Diagnostics;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.PacketPalettes;
using MinecraftClient.Logger;
using System.Threading.Tasks;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Implementation for Minecraft 1.7.X+ Protocols
    /// </summary>
    /// <remarks>
    /// Typical update steps for implementing protocol changes for a new Minecraft version:
    ///  - Perform a diff between latest supported version in MCC and new stable version to support on https://wiki.vg/Protocol
    ///  - If there are any changes in packets implemented by MCC, add MCXXXVersion field below and implement new packet layouts
    ///  - Add the packet type palette for that Minecraft version. Please see PacketTypePalette.cs for more information
    ///  - Also see Material.cs and ItemType.cs for updating block and item data inside MCC
    /// </remarks>
    class Protocol18Handler : IMinecraftCom
    {
        internal const int MC18Version = 47;
        internal const int MC19Version = 107;
        internal const int MC191Version = 108;
        internal const int MC110Version = 210;
        internal const int MC1112Version = 316;
        internal const int MC112Version = 335;
        internal const int MC1122Version = 340;
        internal const int MC113Version = 393;
        internal const int MC114Version = 477;
        internal const int MC115Version = 573;
        internal const int MC1152Version = 578;
        internal const int MC116Version = 735;
        internal const int MC1161Version = 736;
        internal const int MC1162Version = 751;
        internal const int MC1163Version = 753;
        internal const int MC1165Version = 754;
        internal const int MC117Version = 755;

        private int compression_treshold = 0;
        private bool autocomplete_received = false;
        private int autocomplete_transaction_id = 0;
        private readonly List<string> autocomplete_result = new List<string>();
        private readonly Dictionary<int, short> window_actions = new Dictionary<int, short>();
        private bool login_phase = true;
        private int protocolversion;
        private int currentDimension;

        Protocol18Forge pForge;
        Protocol18Terrain pTerrain;
        IMinecraftComHandler handler;
        EntityPalette entityPalette;
        ItemPalette itemPalette;
        PacketTypePalette packetPalette;
        SocketWrapper socketWrapper;
        DataTypes dataTypes;
        Thread netRead; // main thread
        ILogger log;

        public Protocol18Handler(TcpClient Client, int protocolVersion, IMinecraftComHandler handler, ForgeInfo forgeInfo)
        {
            ConsoleIO.SetAutoCompleteEngine(this);
            ChatParser.InitTranslations();
            this.socketWrapper = new SocketWrapper(Client);
            this.dataTypes = new DataTypes(protocolVersion);
            this.protocolversion = protocolVersion;
            this.handler = handler;
            this.pForge = new Protocol18Forge(forgeInfo, protocolVersion, dataTypes, this, handler);
            this.pTerrain = new Protocol18Terrain(protocolVersion, dataTypes, handler);
            this.packetPalette = new PacketTypeHandler(protocolVersion, forgeInfo != null).GetTypeHandler();
            this.log = handler.GetLogger();

            if (handler.GetTerrainEnabled() && protocolversion > MC1165Version)
            {
                log.Error(Translations.Get("extra.terrainandmovement_disabled"));
                handler.SetTerrainEnabled(false);
            }

            if (handler.GetInventoryEnabled() && (protocolversion < MC110Version || protocolversion > MC1165Version))
            {
                log.Error(Translations.Get("extra.inventory_disabled"));
                handler.SetInventoryEnabled(false);
            }

            if (handler.GetEntityHandlingEnabled() && (protocolversion < MC110Version || protocolversion > MC1165Version))
            {
                log.Error(Translations.Get("extra.entity_disabled"));
                handler.SetEntityHandlingEnabled(false);
            }

            // Block palette
            if (protocolversion >= MC113Version)
            {
                if (protocolVersion > MC1165Version && handler.GetTerrainEnabled())
                    throw new NotImplementedException(Translations.Get("exception.palette.block"));
                if (protocolVersion >= MC116Version)
                    Block.Palette = new Palette116();
                else if (protocolVersion >= MC115Version)
                    Block.Palette = new Palette115();
                else if (protocolVersion >= MC114Version)
                    Block.Palette = new Palette114();
                else Block.Palette = new Palette113();
            }
            else Block.Palette = new Palette112();

            // Entity palette
            if (protocolversion >= MC113Version)
            {
                if (protocolversion > MC1165Version && handler.GetEntityHandlingEnabled())
                    throw new NotImplementedException(Translations.Get("exception.palette.entity"));
                if (protocolversion >= MC1162Version)
                    entityPalette = new EntityPalette1162();
                else if (protocolversion >= MC116Version)
                    entityPalette = new EntityPalette1161();
                else if (protocolversion >= MC115Version)
                    entityPalette = new EntityPalette115();
                else if (protocolVersion >= MC114Version)
                    entityPalette = new EntityPalette114();
                else entityPalette = new EntityPalette113();
            }
            else entityPalette = new EntityPalette112();

            // Item palette
            if (protocolversion >= MC116Version)
            {
                if (protocolversion > MC1165Version && handler.GetInventoryEnabled())
                    throw new NotImplementedException(Translations.Get("exception.palette.item"));
                if (protocolversion >= MC1162Version)
                    itemPalette = new ItemPalette1162();
                else itemPalette = new ItemPalette1161();
            }
            else itemPalette = new ItemPalette115();
        }

        /// <summary>
        /// Separate thread. Network reading loop.
        /// </summary>
        private void Updater()
        {
            try
            {
                bool keepUpdating = true;
                Stopwatch stopWatch = new Stopwatch();
                while (keepUpdating)
                {
                    stopWatch.Start();
                    keepUpdating = Update();
                    stopWatch.Stop();
                    int elapsed = stopWatch.Elapsed.Milliseconds;
                    stopWatch.Reset();
                    if (elapsed < 100)
                        Thread.Sleep(100 - elapsed);
                }
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
                    Queue<byte> packetData = new Queue<byte>();
                    ReadNextPacket(ref packetID, packetData);
                    HandlePacket(packetID, new Queue<byte>(packetData));
                }
            }
            catch (System.IO.IOException) { return false; }
            catch (SocketException) { return false; }
            catch (NullReferenceException) { return false; }
            catch (Ionic.Zlib.ZlibException) { return false; }
            return true;
        }

        /// <summary>
        /// Read the next packet from the network
        /// </summary>
        /// <param name="packetID">will contain packet ID</param>
        /// <param name="packetData">will contain raw packet Data</param>
        internal void ReadNextPacket(ref int packetID, Queue<byte> packetData)
        {
            packetData.Clear();
            int size = dataTypes.ReadNextVarIntRAW(socketWrapper); //Packet size
            byte[] rawpacket = socketWrapper.ReadDataRAW(size); //Packet contents
            for (int i = 0; i < rawpacket.Length; i++)
                packetData.Enqueue(rawpacket[i]);

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
                    for (int i = 0; i < uncompressed.Length; i++)
                        packetData.Enqueue(uncompressed[i]);
                }
            }

            packetID = dataTypes.ReadNextVarInt(packetData); //Packet ID

            if (handler.GetNetworkPacketCaptureEnabled())
            {
                List<byte> clone = packetData.ToList();
                handler.OnNetworkPacket(packetID, clone, login_phase, true);
            }
        }

        /// <summary>
        /// Handle the given packet
        /// </summary>
        /// <param name="packetID">Packet ID</param>
        /// <param name="packetData">Packet contents</param>
        /// <returns>TRUE if the packet was processed, FALSE if ignored or unknown</returns>
        internal bool HandlePacket(int packetID, Queue<byte> packetData)
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
                        case 0x04:
                            int messageId = dataTypes.ReadNextVarInt(packetData);
                            string channel = dataTypes.ReadNextString(packetData);
                            List<byte> responseData = new List<byte>();
                            bool understood = pForge.HandleLoginPluginRequest(channel, packetData, ref responseData);
                            SendLoginPluginResponse(messageId, understood, responseData.ToArray());
                            return understood;
                        default:
                            return false; //Ignored packet
                    }
                }
                // Regular in-game packets
                else switch (packetPalette.GetIncommingTypeById(packetID))
                {
                    case PacketTypesIn.KeepAlive:
                        SendPacket(PacketTypesOut.KeepAlive, packetData);
                        handler.OnServerKeepAlive();
                        break;
                    case PacketTypesIn.JoinGame:
                        handler.OnGameJoined();
                        int playerEntityID = dataTypes.ReadNextInt(packetData);
                        handler.OnReceivePlayerEntityID(playerEntityID);

                        if (protocolversion >= MC1162Version)
                            dataTypes.ReadNextBool(packetData);                       // Is hardcore - 1.16.2 and above

                        handler.OnGamemodeUpdate(Guid.Empty, dataTypes.ReadNextByte(packetData));

                        if (protocolversion >= MC116Version)
                        {
                            dataTypes.ReadNextByte(packetData);                       // Previous Gamemode - 1.16 and above
                            int worldCount = dataTypes.ReadNextVarInt(packetData);    // World Count - 1.16 and above
                            for (int i = 0; i < worldCount; i++)
                                dataTypes.ReadNextString(packetData);                 // World Names - 1.16 and above
                            dataTypes.ReadNextNbt(packetData);                        // Dimension Codec - 1.16 and above
                        }

                        //Current dimension - String identifier in 1.16, varInt below 1.16, byte below 1.9.1
                        if (protocolversion >= MC116Version)
                        {
                            if (protocolversion >= MC1162Version)
                                dataTypes.ReadNextNbt(packetData);
                            else
                                dataTypes.ReadNextString(packetData);
                            // TODO handle dimensions for 1.16+, needed for terrain handling
                            // TODO this data give min and max y which will be needed for chunk collumn handling
                            this.currentDimension = 0;
                        }
                        else if (protocolversion >= MC191Version)
                            this.currentDimension = dataTypes.ReadNextInt(packetData);
                        else
                            this.currentDimension = (sbyte)dataTypes.ReadNextByte(packetData);

                        if (protocolversion < MC114Version)
                            dataTypes.ReadNextByte(packetData);           // Difficulty - 1.13 and below
                        if (protocolversion >= MC116Version)
                            dataTypes.ReadNextString(packetData);         // World Name - 1.16 and above
                        if (protocolversion >= MC115Version)
                            dataTypes.ReadNextLong(packetData);           // Hashed world seed - 1.15 and above

                        if (protocolversion >= MC1162Version)
                            dataTypes.ReadNextVarInt(packetData);         // Max Players - 1.16.2 and above
                        else
                            dataTypes.ReadNextByte(packetData);           // Max Players - 1.16.1 and below

                        if (protocolversion < MC116Version)
                            dataTypes.ReadNextString(packetData);         // Level Type - 1.15 and below
                        if (protocolversion >= MC114Version)
                            dataTypes.ReadNextVarInt(packetData);         // View distance - 1.14 and above
                        if (protocolversion >= MC18Version)
                            dataTypes.ReadNextBool(packetData);           // Reduced debug info - 1.8 and above
                        if (protocolversion >= MC115Version)
                            dataTypes.ReadNextBool(packetData);           // Enable respawn screen - 1.15 and above

                        if (protocolversion >= MC116Version)
                        {
                            dataTypes.ReadNextBool(packetData);           // Is Debug - 1.16 and above
                            dataTypes.ReadNextBool(packetData);           // Is Flat - 1.16 and above
                        }
                        break;
                    case PacketTypesIn.ChatMessage:
                        string message = dataTypes.ReadNextString(packetData);
                        if (protocolversion >= MC18Version)
                        {
                            //Hide system messages or xp bar messages?
                            byte messageType = dataTypes.ReadNextByte(packetData);
                            if ((messageType == 1 && !Settings.DisplaySystemMessages)
                                || (messageType == 2 && !Settings.DisplayXPBarMessages))
                                break;
                        }
                        handler.OnTextReceived(message, true);
                        break;
                    case PacketTypesIn.Respawn:
                        if (protocolversion >= MC116Version)
                        {
                            // TODO handle dimensions for 1.16+, needed for terrain handling
                            dataTypes.ReadNextString(packetData);
                            this.currentDimension = 0;
                        }
                        else
                        {
                            // 1.15 and below
                            this.currentDimension = dataTypes.ReadNextInt(packetData);
                        }
                        if (protocolversion >= MC116Version)
                            dataTypes.ReadNextString(packetData);         // World Name - 1.16 and above
                        if (protocolversion < MC114Version)
                            dataTypes.ReadNextByte(packetData);           // Difficulty - 1.13 and below
                        if (protocolversion >= MC115Version)
                            dataTypes.ReadNextLong(packetData);           // Hashed world seed - 1.15 and above
                        dataTypes.ReadNextByte(packetData);               // Gamemode
                        if (protocolversion >= MC116Version)
                            dataTypes.ReadNextByte(packetData);           // Previous Game mode - 1.16 and above
                        if (protocolversion < MC116Version)
                            dataTypes.ReadNextString(packetData);         // Level Type - 1.15 and below
                        if (protocolversion >= MC116Version)
                        {
                            dataTypes.ReadNextBool(packetData);           // Is Debug - 1.16 and above
                            dataTypes.ReadNextBool(packetData);           // Is Flat - 1.16 and above
                            dataTypes.ReadNextBool(packetData);           // Copy metadata - 1.16 and above
                        }
                        handler.OnRespawn();
                        break;
                    case PacketTypesIn.PlayerPositionAndLook:
                        // These always need to be read, since we need the field after them for teleport confirm
                        double x = dataTypes.ReadNextDouble(packetData);
                        double y = dataTypes.ReadNextDouble(packetData);
                        double z = dataTypes.ReadNextDouble(packetData);
                        float yaw = dataTypes.ReadNextFloat(packetData);
                        float pitch = dataTypes.ReadNextFloat(packetData);
                        byte locMask = dataTypes.ReadNextByte(packetData);

                        // entity handling require player pos for distance calculating
                        if (handler.GetTerrainEnabled() || handler.GetEntityHandlingEnabled())
                        {
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
                            SendPacket(PacketTypesOut.TeleportConfirm, dataTypes.GetVarInt(teleportID));
                        }

                        if (protocolversion >= MC117Version) dataTypes.ReadNextBool(packetData);
                        break;
                    case PacketTypesIn.ChunkData: //TODO implement for 1.17, bit mask is not limited to 0-15 anymore
                        if (handler.GetTerrainEnabled())
                        {
                            int chunkX = dataTypes.ReadNextInt(packetData);
                            int chunkZ = dataTypes.ReadNextInt(packetData);
                            bool chunksContinuous = dataTypes.ReadNextBool(packetData);
                            if (protocolversion >= MC116Version && protocolversion <= MC1161Version)
                                dataTypes.ReadNextBool(packetData); // Ignore old data - 1.16 to 1.16.1 only
                            ushort chunkMask = protocolversion >= MC19Version
                                ? (ushort)dataTypes.ReadNextVarInt(packetData)
                                : dataTypes.ReadNextUShort(packetData);
                            if (protocolversion < MC18Version)
                            {
                                ushort addBitmap = dataTypes.ReadNextUShort(packetData);
                                int compressedDataSize = dataTypes.ReadNextInt(packetData);
                                byte[] compressed = dataTypes.ReadData(compressedDataSize, packetData);
                                byte[] decompressed = ZlibUtils.Decompress(compressed);
                                new Task(() => {
                                    pTerrain.ProcessChunkColumnData(chunkX, chunkZ, chunkMask, addBitmap, currentDimension == 0, chunksContinuous, currentDimension, new Queue<byte>(decompressed));
                                }).Start();
                            }
                            else
                            {
                                if (protocolversion >= MC114Version)
                                    dataTypes.ReadNextNbt(packetData);  // Heightmaps - 1.14 and above
                                int biomesLength = 0;
                                if (protocolversion >= MC1162Version)
                                    if (chunksContinuous)
                                        biomesLength = dataTypes.ReadNextVarInt(packetData); // Biomes length - 1.16.2 and above
                                if (protocolversion >= MC115Version && chunksContinuous)
                                {
                                    if (protocolversion >= MC1162Version)
                                    {
                                        for (int i = 0; i < biomesLength; i++)
                                        {
                                            // Biomes - 1.16.2 and above
                                            // Don't use ReadNextVarInt because it cost too much time
                                            dataTypes.SkipNextVarInt(packetData);
                                        }
                                    }
                                    else dataTypes.ReadData(1024 * 4, packetData); // Biomes - 1.15 and above
                                }
                                int dataSize = dataTypes.ReadNextVarInt(packetData);
                                new Task(() => {
                                    pTerrain.ProcessChunkColumnData(chunkX, chunkZ, chunkMask, 0, false, chunksContinuous, currentDimension, packetData);
                                }).Start();
                            }
                        }
                        break;
                    case PacketTypesIn.MapData:
                        int mapid = dataTypes.ReadNextVarInt(packetData);
                        byte scale = dataTypes.ReadNextByte(packetData);
                        bool trackingposition = protocolversion >= MC117Version ? false : dataTypes.ReadNextBool(packetData);
                        bool locked = false;
                        if (protocolversion >= MC114Version)
                        {
                            locked = dataTypes.ReadNextBool(packetData);
                        }
                        if (protocolversion >= MC117Version)
                        {
                            trackingposition = dataTypes.ReadNextBool(packetData);
                        }
                        int iconcount = dataTypes.ReadNextVarInt(packetData);
                        handler.OnMapData(mapid, scale, trackingposition, locked, iconcount);
                        break;
                    case PacketTypesIn.TradeList:
                        if ((protocolversion >= MC114Version) && (handler.GetInventoryEnabled()))
                        {
                            // MC 1.14 or greater
                            int windowID = dataTypes.ReadNextVarInt(packetData);
                            int size = dataTypes.ReadNextByte(packetData);
                            List<VillagerTrade> trades = new List<VillagerTrade>();
                            for (int tradeId = 0; tradeId < size; tradeId++)
                            {
                                VillagerTrade trade = dataTypes.ReadNextTrade(packetData, itemPalette);
                                    trades.Add(trade);
                            }
                            VillagerInfo villagerInfo = new VillagerInfo()
                            {
                                Level = dataTypes.ReadNextVarInt(packetData),
                                Experience = dataTypes.ReadNextVarInt(packetData),
                                IsRegularVillager = dataTypes.ReadNextBool(packetData),
                                CanRestock = dataTypes.ReadNextBool(packetData)
                            };
                            handler.OnTradeList(windowID, trades, villagerInfo);
                        }
                        break;
                    case PacketTypesIn.Title:
                        if (protocolversion >= MC18Version)
                        {
                            int action2 = dataTypes.ReadNextVarInt(packetData);
                            string titletext = String.Empty;
                            string subtitletext = String.Empty;
                            string actionbartext = String.Empty;
                            string json = String.Empty;
                            int fadein = -1;
                            int stay = -1;
                            int fadeout = -1;
                            if (protocolversion >= MC110Version)
                            {
                                if (action2 == 0)
                                {
                                    json = titletext;
                                    titletext = ChatParser.ParseText(dataTypes.ReadNextString(packetData));
                                }
                                else if (action2 == 1)
                                {
                                    json = subtitletext;
                                    subtitletext = ChatParser.ParseText(dataTypes.ReadNextString(packetData));
                                }
                                else if (action2 == 2)
                                {
                                    json = actionbartext;
                                    actionbartext = ChatParser.ParseText(dataTypes.ReadNextString(packetData));
                                }
                                else if (action2 == 3)
                                {
                                    fadein = dataTypes.ReadNextInt(packetData);
                                    stay = dataTypes.ReadNextInt(packetData);
                                    fadeout = dataTypes.ReadNextInt(packetData);
                                }
                            }
                            else
                            {
                                if (action2 == 0)
                                {
                                    json = titletext;
                                    titletext = ChatParser.ParseText(dataTypes.ReadNextString(packetData));
                                }
                                else if (action2 == 1)
                                {
                                    json = subtitletext;
                                    subtitletext = ChatParser.ParseText(dataTypes.ReadNextString(packetData));
                                }
                                else if (action2 == 2)
                                {
                                    fadein = dataTypes.ReadNextInt(packetData);
                                    stay = dataTypes.ReadNextInt(packetData);
                                    fadeout = dataTypes.ReadNextInt(packetData);
                                }
                            }
                            handler.OnTitle(action2, titletext, subtitletext, actionbartext, fadein, stay, fadeout, json);
                        }
                        break;
                    case PacketTypesIn.MultiBlockChange:
                        if (handler.GetTerrainEnabled())
                        {
                            if (protocolversion >= MC1162Version)
                            {
                                long chunkSection = dataTypes.ReadNextLong(packetData);
                                int sectionX = (int)(chunkSection >> 42);
                                int sectionY = (int)((chunkSection << 44) >> 44);
                                int sectionZ = (int)((chunkSection << 22) >> 42);
                                dataTypes.ReadNextBool(packetData); // Useless boolean
                                int blocksSize = dataTypes.ReadNextVarInt(packetData);
                                for (int i = 0; i < blocksSize; i++)
                                {
                                    ulong block = (ulong)dataTypes.ReadNextVarLong(packetData);
                                    int blockId = (int)(block >> 12);
                                    int localX = (int)((block >> 8) & 0x0F);
                                    int localZ = (int)((block >> 4) & 0x0F);
                                    int localY = (int)(block & 0x0F);

                                    Block b = new Block((ushort)blockId);
                                    int blockX = (sectionX * 16) + localX;
                                    int blockY = (sectionY * 16) + localY;
                                    int blockZ = (sectionZ * 16) + localZ;
                                    var l = new Location(blockX, blockY, blockZ);
                                    handler.GetWorld().SetBlock(l, b);
                                }
                            }
                            else
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
                        }
                        break;
                    case PacketTypesIn.BlockChange:
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
                            else
                            {
                                handler.GetWorld().SetBlock(dataTypes.ReadNextLocation(packetData), new Block((ushort)dataTypes.ReadNextVarInt(packetData)));
                            }
                        }
                        break;
                    case PacketTypesIn.MapChunkBulk:
                        if (protocolversion < MC19Version && handler.GetTerrainEnabled())
                        {
                            int chunkCount;
                            bool hasSkyLight;
                            Queue<byte> chunkData = packetData;

                            //Read global fields
                            if (protocolversion < MC18Version)
                            {
                                chunkCount = dataTypes.ReadNextShort(packetData);
                                int compressedDataSize = dataTypes.ReadNextInt(packetData);
                                hasSkyLight = dataTypes.ReadNextBool(packetData);
                                byte[] compressed = dataTypes.ReadData(compressedDataSize, packetData);
                                byte[] decompressed = ZlibUtils.Decompress(compressed);
                                chunkData = new Queue<byte>(decompressed);
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
                                pTerrain.ProcessChunkColumnData(chunkXs[chunkColumnNo], chunkZs[chunkColumnNo], chunkMasks[chunkColumnNo], addBitmaps[chunkColumnNo], hasSkyLight, true, currentDimension, chunkData);
                        }
                        break;
                    case PacketTypesIn.UnloadChunk:
                        if (protocolversion >= MC19Version && handler.GetTerrainEnabled())
                        {
                            int chunkX = dataTypes.ReadNextInt(packetData);
                            int chunkZ = dataTypes.ReadNextInt(packetData);
                            handler.GetWorld()[chunkX, chunkZ] = null;
                        }
                        break;
                    case PacketTypesIn.PlayerInfo:
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
                                        handler.OnGamemodeUpdate(uuid, dataTypes.ReadNextVarInt(packetData));
                                        dataTypes.ReadNextVarInt(packetData);
                                        if (dataTypes.ReadNextBool(packetData))
                                            dataTypes.ReadNextString(packetData);
                                        handler.OnPlayerJoin(uuid, name);
                                        break;
                                    case 0x01: //Update gamemode
                                        handler.OnGamemodeUpdate(uuid, dataTypes.ReadNextVarInt(packetData));
                                        break;
                                    case 0x02: //Update latency
                                        int latency = dataTypes.ReadNextVarInt(packetData);
                                        handler.OnLatencyUpdate(uuid, latency); //Update latency;
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
                    case PacketTypesIn.TabComplete:
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
                    case PacketTypesIn.PluginMessage:
                        String channel = dataTypes.ReadNextString(packetData);
                        // Length is unneeded as the whole remaining packetData is the entire payload of the packet.
                        if (protocolversion < MC18Version)
                            pForge.ReadNextVarShort(packetData);
                        handler.OnPluginChannelMessage(channel, packetData.ToArray());
                        return pForge.HandlePluginMessage(channel, packetData, ref currentDimension);
                    case PacketTypesIn.Disconnect:
                        handler.OnConnectionLost(ChatBot.DisconnectReason.InGameKick, ChatParser.ParseText(dataTypes.ReadNextString(packetData)));
                        return false;
                    case PacketTypesIn.SetCompression:
                        if (protocolversion >= MC18Version && protocolversion < MC19Version)
                            compression_treshold = dataTypes.ReadNextVarInt(packetData);
                        break;
                    case PacketTypesIn.OpenWindow:
                        if (handler.GetInventoryEnabled())
                        {
                            if (protocolversion < MC114Version)
                            {
                                // MC 1.13 or lower
                                byte windowID = dataTypes.ReadNextByte(packetData);
                                string type = dataTypes.ReadNextString(packetData).Replace("minecraft:", "").ToUpper();
                                ContainerTypeOld inventoryType = (ContainerTypeOld)Enum.Parse(typeof(ContainerTypeOld), type);
                                string title = dataTypes.ReadNextString(packetData);
                                byte slots = dataTypes.ReadNextByte(packetData);
                                Container inventory = new Container(windowID, inventoryType, ChatParser.ParseText(title));
                                handler.OnInventoryOpen(windowID, inventory);
                            }
                            else
                            {
                                // MC 1.14 or greater
                                int windowID = dataTypes.ReadNextVarInt(packetData);
                                int windowType = dataTypes.ReadNextVarInt(packetData);
                                string title = dataTypes.ReadNextString(packetData);
                                Container inventory = new Container(windowID, windowType, ChatParser.ParseText(title));
                                handler.OnInventoryOpen(windowID, inventory);
                            }
                        }
                        break;
                    case PacketTypesIn.CloseWindow:
                        if (handler.GetInventoryEnabled())
                        {
                            byte windowID = dataTypes.ReadNextByte(packetData);
                            lock (window_actions) { window_actions[windowID] = 0; }
                            handler.OnInventoryClose(windowID);
                        }
                        break;
                    case PacketTypesIn.WindowItems:
                        if (handler.GetInventoryEnabled())
                        {
                            byte windowId = dataTypes.ReadNextByte(packetData);
                            short elements = dataTypes.ReadNextShort(packetData);
                            Dictionary<int, Item> inventorySlots = new Dictionary<int, Item>();
                            for (short slotId = 0; slotId < elements; slotId++)
                            {
                                Item item = dataTypes.ReadNextItemSlot(packetData, itemPalette);
                                if (item != null)
                                    inventorySlots[slotId] = item;
                            }
                            handler.OnWindowItems(windowId, inventorySlots);
                        }
                        break;
                    case PacketTypesIn.SetSlot:
                        if (handler.GetInventoryEnabled())
                        {
                            byte windowID = dataTypes.ReadNextByte(packetData);
                            short slotID = dataTypes.ReadNextShort(packetData);
                            Item item = dataTypes.ReadNextItemSlot(packetData, itemPalette);
                            handler.OnSetSlot(windowID, slotID, item);
                        }
                        break;
                    case PacketTypesIn.WindowConfirmation:
                        if (handler.GetInventoryEnabled())
                        {
                            byte windowID = dataTypes.ReadNextByte(packetData);
                            short actionID = dataTypes.ReadNextShort(packetData);
                            bool accepted = dataTypes.ReadNextBool(packetData);
                            if (!accepted)
                            {
                                SendWindowConfirmation(windowID, actionID, accepted);
                            }
                        }
                        break;
                    case PacketTypesIn.ResourcePackSend:
                        string url = dataTypes.ReadNextString(packetData);
                        string hash = dataTypes.ReadNextString(packetData);
                        bool forced = true; // Assume forced for MC 1.16 and below
                        if (protocolversion >= MC117Version)
                        {
                            forced = dataTypes.ReadNextBool(packetData);
                            String forcedMessage = ChatParser.ParseText(dataTypes.ReadNextString(packetData));
                        }
                        // Some server plugins may send invalid resource packs to probe the client and we need to ignore them (issue #1056)
                        if (hash.Length != 40)
                            break;
                        //Send back "accepted" and "successfully loaded" responses for plugins or server config making use of resource pack mandatory
                        if (forced)
                        {
                            byte[] responseHeader = new byte[0];
                            if (protocolversion < MC110Version) //MC 1.10 does not include resource pack hash in responses
                                responseHeader = dataTypes.ConcatBytes(dataTypes.GetVarInt(hash.Length), Encoding.UTF8.GetBytes(hash));
                            SendPacket(PacketTypesOut.ResourcePackStatus, dataTypes.ConcatBytes(responseHeader, dataTypes.GetVarInt(3))); //Accepted pack
                            SendPacket(PacketTypesOut.ResourcePackStatus, dataTypes.ConcatBytes(responseHeader, dataTypes.GetVarInt(0))); //Successfully loaded
                        }
                        break;
                    case PacketTypesIn.SpawnEntity:
                        if (handler.GetEntityHandlingEnabled())
                        {
                            Entity entity = dataTypes.ReadNextEntity(packetData, entityPalette, false);
                            handler.OnSpawnEntity(entity);
                        }
                        break;
                    case PacketTypesIn.EntityEquipment:
                        if (handler.GetEntityHandlingEnabled())
                        {
                            int entityid = dataTypes.ReadNextVarInt(packetData);
                            if (protocolversion >= MC116Version)
                            {
                                bool hasNext;
                                do
                                {
                                    byte bitsData = dataTypes.ReadNextByte(packetData);
                                    //  Top bit set if another entry follows, and otherwise unset if this is the last item in the array
                                    hasNext = (bitsData >> 7) == 1 ? true : false;
                                    int slot2 = bitsData >> 1;
                                    Item item = dataTypes.ReadNextItemSlot(packetData, itemPalette);
                                    handler.OnEntityEquipment(entityid, slot2, item);
                                } while (hasNext);
                            }
                            else
                            {
                                int slot2 = dataTypes.ReadNextVarInt(packetData);
                                Item item = dataTypes.ReadNextItemSlot(packetData, itemPalette);
                                handler.OnEntityEquipment(entityid, slot2, item);
                            }
                        }
                        break;
                   case PacketTypesIn.SpawnLivingEntity:
                        if (handler.GetEntityHandlingEnabled())
                        {
                            Entity entity = dataTypes.ReadNextEntity(packetData, entityPalette, true);
                            // packet before 1.15 has metadata at the end
                            // this is not handled in dataTypes.ReadNextEntity()
                            // we are simply ignoring leftover data in packet
                            handler.OnSpawnEntity(entity);
                        }
                        break;
                    case PacketTypesIn.SpawnPlayer:
                        if (handler.GetEntityHandlingEnabled())
                        {
                            int EntityID = dataTypes.ReadNextVarInt(packetData);
                            Guid UUID = dataTypes.ReadNextUUID(packetData);
                            double X = dataTypes.ReadNextDouble(packetData);
                            double Y = dataTypes.ReadNextDouble(packetData);
                            double Z = dataTypes.ReadNextDouble(packetData);
                            byte Yaw = dataTypes.ReadNextByte(packetData);
                            byte Pitch = dataTypes.ReadNextByte(packetData);

                            Location EntityLocation = new Location(X, Y, Z);

                            handler.OnSpawnPlayer(EntityID, UUID, EntityLocation, Yaw, Pitch);
                        }
                        break;
                    case PacketTypesIn.EntityEffect:
                        if (handler.GetEntityHandlingEnabled())
                        {
                            int entityid = dataTypes.ReadNextVarInt(packetData);
                            Inventory.Effects effect = Effects.Speed;
                            if (Enum.TryParse(dataTypes.ReadNextByte(packetData).ToString(), out effect))
                            {
                                int amplifier = dataTypes.ReadNextByte(packetData);
                                int duration = dataTypes.ReadNextVarInt(packetData);
                                byte flags = dataTypes.ReadNextByte(packetData);
                                handler.OnEntityEffect(entityid, effect, amplifier, duration, flags);
                            }
                        }
                        break;
                    case PacketTypesIn.DestroyEntities:
                        if (handler.GetEntityHandlingEnabled())
                        {
                            int EntityCount = dataTypes.ReadNextVarInt(packetData);
                            int[] EntitiesList = new int[EntityCount];
                            for (int i = 0; i < EntityCount; i++)
                            {
                                EntitiesList[i] = dataTypes.ReadNextVarInt(packetData);
                            }
                            handler.OnDestroyEntities(EntitiesList);
                        }
                        break;
                    case PacketTypesIn.DestroyEntity:
                        if (handler.GetEntityHandlingEnabled())
                        {
                            handler.OnDestroyEntities(new [] { dataTypes.ReadNextVarInt(packetData) });
                        }
                        break;
                    case PacketTypesIn.EntityPosition:
                        if (handler.GetEntityHandlingEnabled())
                        {
                            int EntityID = dataTypes.ReadNextVarInt(packetData);
                            Double DeltaX = Convert.ToDouble(dataTypes.ReadNextShort(packetData));
                            Double DeltaY = Convert.ToDouble(dataTypes.ReadNextShort(packetData));
                            Double DeltaZ = Convert.ToDouble(dataTypes.ReadNextShort(packetData));
                            bool OnGround = dataTypes.ReadNextBool(packetData);
                            DeltaX = DeltaX / (128 * 32);
                            DeltaY = DeltaY / (128 * 32);
                            DeltaZ = DeltaZ / (128 * 32);
                            handler.OnEntityPosition(EntityID, DeltaX, DeltaY, DeltaZ, OnGround);
                        }
                        break;
                    case PacketTypesIn.EntityPositionAndRotation:
                        if (handler.GetEntityHandlingEnabled())
                        {
                            int EntityID = dataTypes.ReadNextVarInt(packetData);
                            Double DeltaX = Convert.ToDouble(dataTypes.ReadNextShort(packetData));
                            Double DeltaY = Convert.ToDouble(dataTypes.ReadNextShort(packetData));
                            Double DeltaZ = Convert.ToDouble(dataTypes.ReadNextShort(packetData));
                            byte _yaw = dataTypes.ReadNextByte(packetData);
                            byte _pitch = dataTypes.ReadNextByte(packetData);
                            bool OnGround = dataTypes.ReadNextBool(packetData);
                            DeltaX = DeltaX / (128 * 32);
                            DeltaY = DeltaY / (128 * 32);
                            DeltaZ = DeltaZ / (128 * 32);
                            handler.OnEntityPosition(EntityID, DeltaX, DeltaY, DeltaZ, OnGround);
                        }
                        break;
                    case PacketTypesIn.EntityProperties:
                        if (handler.GetEntityHandlingEnabled())
                        {
                            int EntityID = dataTypes.ReadNextVarInt(packetData);
                            int NumberOfProperties = protocolversion >= MC117Version ? dataTypes.ReadNextVarInt(packetData) : dataTypes.ReadNextInt(packetData);
                            Dictionary<string, Double> keys = new Dictionary<string, Double>();
                            for (int i = 0; i < NumberOfProperties; i++)
                            {
                                string _key = dataTypes.ReadNextString(packetData);
                                Double _value = dataTypes.ReadNextDouble(packetData);

                                List<double> op0 = new List<double>();
                                List<double> op1 = new List<double>();
                                List<double> op2 = new List<double>();
                                int NumberOfModifiers = dataTypes.ReadNextVarInt(packetData);
                                for (int j = 0; j < NumberOfModifiers; j++)
                                {
                                    dataTypes.ReadNextUUID(packetData);
                                    Double amount = dataTypes.ReadNextDouble(packetData);
                                    byte operation = dataTypes.ReadNextByte(packetData);
                                    switch (operation)
                                    {
                                        case 0: op0.Add(amount); break;
                                        case 1: op1.Add(amount); break;
                                        case 2: op2.Add(amount + 1); break;
                                    }
                                }
                                if (op0.Count > 0) _value += op0.Sum();
                                if (op1.Count > 0) _value *= 1 + op1.Sum();
                                if (op2.Count > 0) _value *= op2.Aggregate((a, _x) => a * _x);
                                keys.Add(_key, _value);
                            }
                            handler.OnEntityProperties(EntityID, keys);
                        }
                        break;
                    case PacketTypesIn.EntityMetadata:
                        if (handler.GetEntityHandlingEnabled())
                        {
                            int EntityID = dataTypes.ReadNextVarInt(packetData);
                            Dictionary<int, object> metadata = dataTypes.ReadNextMetadata(packetData, itemPalette);
                            int healthField = protocolversion >= MC114Version ? 8 : 7; // Health is field no. 7 in 1.10+ and 8 in 1.14+
                            if (metadata.ContainsKey(healthField) && metadata[healthField] != null && metadata[healthField].GetType() == typeof(float))
                                handler.OnEntityHealth(EntityID, (float)metadata[healthField]);
                            handler.OnEntityMetadata(EntityID, metadata);
                        }
                        break;
                    case PacketTypesIn.EntityStatus:
                        if (handler.GetEntityHandlingEnabled())
                        {
                            int entityId = dataTypes.ReadNextInt(packetData);
                            byte status = dataTypes.ReadNextByte(packetData);
                            handler.OnEntityStatus(entityId, status);
                        }
                        break;
                    case PacketTypesIn.TimeUpdate:
                        long WorldAge = dataTypes.ReadNextLong(packetData);
                        long TimeOfday = dataTypes.ReadNextLong(packetData);
                        handler.OnTimeUpdate(WorldAge, TimeOfday);
                        break;
                    case PacketTypesIn.EntityTeleport:
                        if (handler.GetEntityHandlingEnabled())
                        {
                            int EntityID = dataTypes.ReadNextVarInt(packetData);
                            Double X = dataTypes.ReadNextDouble(packetData);
                            Double Y = dataTypes.ReadNextDouble(packetData);
                            Double Z = dataTypes.ReadNextDouble(packetData);
                            byte EntityYaw = dataTypes.ReadNextByte(packetData);
                            byte EntityPitch = dataTypes.ReadNextByte(packetData);
                            bool OnGround = dataTypes.ReadNextBool(packetData);
                            handler.OnEntityTeleport(EntityID, X, Y, Z, OnGround);
                        }
                        break;
                    case PacketTypesIn.UpdateHealth:
                        float health = dataTypes.ReadNextFloat(packetData);
                        int food;
                        if (protocolversion >= MC18Version)
                            food = dataTypes.ReadNextVarInt(packetData);
                        else
                            food = dataTypes.ReadNextShort(packetData);
                        dataTypes.ReadNextFloat(packetData); // Food Saturation
                        handler.OnUpdateHealth(health, food);
                        break;
                    case PacketTypesIn.SetExperience:
                        float experiencebar = dataTypes.ReadNextFloat(packetData);
                        int level = dataTypes.ReadNextVarInt(packetData);
                        int totalexperience = dataTypes.ReadNextVarInt(packetData);
                        handler.OnSetExperience(experiencebar, level, totalexperience);
                        break;
                    case PacketTypesIn.Explosion:
                        Location explosionLocation = new Location(dataTypes.ReadNextFloat(packetData), dataTypes.ReadNextFloat(packetData), dataTypes.ReadNextFloat(packetData));
                        float explosionStrength = dataTypes.ReadNextFloat(packetData);
                        int explosionBlockCount = protocolversion >= MC117Version
                            ? dataTypes.ReadNextVarInt(packetData)
                            : dataTypes.ReadNextInt(packetData);
                        // Ignoring additional fields (records, pushback)
                        handler.OnExplosion(explosionLocation, explosionStrength, explosionBlockCount);
                        break;
                    case PacketTypesIn.HeldItemChange:
                        byte slot = dataTypes.ReadNextByte(packetData);
                        handler.OnHeldItemChange(slot);
                        break;
                    case PacketTypesIn.ScoreboardObjective:
                        string objectivename = dataTypes.ReadNextString(packetData);
                        byte mode = dataTypes.ReadNextByte(packetData);
                        string objectivevalue = String.Empty;
                        int type2 = -1;
                        if (mode == 0 || mode == 2)
                        {
                            objectivevalue = dataTypes.ReadNextString(packetData);
                            type2 = dataTypes.ReadNextVarInt(packetData);
                        }
                        handler.OnScoreboardObjective(objectivename, mode, objectivevalue, type2);
                        break;
                    case PacketTypesIn.UpdateScore:
                        string entityname = dataTypes.ReadNextString(packetData);
                        byte action3 = dataTypes.ReadNextByte(packetData);
                        string objectivename2 = null;
                        int value = -1;
                        if (action3 != 1 || protocolversion >= MC18Version)
                            objectivename2 = dataTypes.ReadNextString(packetData);
                        if (action3 != 1)
                            value = dataTypes.ReadNextVarInt(packetData);
                        handler.OnUpdateScore(entityname, action3, objectivename2, value);
                        break;
                    case PacketTypesIn.BlockBreakAnimation:
                        if (handler.GetEntityHandlingEnabled() && handler.GetTerrainEnabled())
                        {
                            int playerId = dataTypes.ReadNextVarInt(packetData);
                            Location blockLocation = dataTypes.ReadNextLocation(packetData);
                            byte stage = dataTypes.ReadNextByte(packetData);
                            handler.OnBlockBreakAnimation(playerId, blockLocation, stage);
                        }
                        break;
                    case PacketTypesIn.EntityAnimation:
                        if (handler.GetEntityHandlingEnabled())
                        {
                            int playerId2 = dataTypes.ReadNextVarInt(packetData);
                            byte animation = dataTypes.ReadNextByte(packetData);
                            handler.OnEntityAnimation(playerId2, animation);
                        }
                        break;
                    default:
                        return false; //Ignored packet
                }
                return true; //Packet processed
            }
            catch (Exception innerException)
            {
                if (innerException is ThreadAbortException || innerException is SocketException || innerException.InnerException is SocketException)
                    throw; //Thread abort or Connection lost rather than invalid data
                throw new System.IO.InvalidDataException(
                    Translations.Get("exception.packet_process",
                        packetPalette.GetIncommingTypeById(packetID),
                        packetID,
                        protocolversion,
                        login_phase,
                        innerException.GetType()),
                    innerException);
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
        /// Get net read thread (main thread) ID
        /// </summary>
        /// <returns>Net read thread ID</returns>
        public int GetNetReadThreadId()
        {
            return netRead != null ? netRead.ManagedThreadId : -1;
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
        /// Send a packet to the server. Packet ID, compression, and encryption will be handled automatically.
        /// </summary>
        /// <param name="packet">packet type</param>
        /// <param name="packetData">packet Data</param>
        private void SendPacket(PacketTypesOut packet, IEnumerable<byte> packetData)
        {
            SendPacket(packetPalette.GetOutgoingIdByType(packet), packetData);
        }

        /// <summary>
        /// Send a packet to the server. Compression and encryption will be handled automatically.
        /// </summary>
        /// <param name="packetID">packet ID</param>
        /// <param name="packetData">packet Data</param>
        private void SendPacket(int packetID, IEnumerable<byte> packetData)
        {
            if (handler.GetNetworkPacketCaptureEnabled())
            {
                List<byte> clone = packetData.ToList();
                handler.OnNetworkPacket(packetID, clone, login_phase, false);
            }
            
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
            byte[] server_port = dataTypes.GetUShort((ushort)handler.GetServerPort());
            byte[] next_state = dataTypes.GetVarInt(2);
            byte[] handshake_packet = dataTypes.ConcatBytes(protocol_version, dataTypes.GetString(server_address), server_port, next_state);

            SendPacket(0x00, handshake_packet);

            byte[] login_packet = dataTypes.GetString(handler.GetUsername());

            SendPacket(0x00, login_packet);

            int packetID = -1;
            Queue<byte> packetData = new Queue<byte>();
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
                    log.Info(Translations.Get("mcc.server_offline"));
                    login_phase = false;

                    if (!pForge.CompleteForgeHandshake())
                    {
                        log.Error(Translations.Get("error.forge"));
                        return false;
                    }

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

            log.Debug(Translations.Get("debug.crypto"));

            if (serverIDhash != "-")
            {
                log.Info(Translations.Get("mcc.session"));
                if (!ProtocolHandler.SessionCheck(uuid, sessionID, CryptoHandler.getServerHash(serverIDhash, serverKey, secretKey)))
                {
                    handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected, Translations.Get("mcc.session_fail"));
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
            int loopPrevention = UInt16.MaxValue;
            while (true)
            {
                int packetID = -1;
                Queue<byte> packetData = new Queue<byte>();
                ReadNextPacket(ref packetID, packetData);
                if (packetID < 0 || loopPrevention-- < 0) // Failed to read packet or too many iterations (issue #1150)
                {
                    handler.OnConnectionLost(ChatBot.DisconnectReason.ConnectionLost, Translations.Get("error.invalid_encrypt"));
                    return false;
                }
                else if (packetID == 0x00) //Login rejected
                {
                    handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected, ChatParser.ParseText(dataTypes.ReadNextString(packetData)));
                    return false;
                }
                else if (packetID == 0x02) //Login successful
                {
                    login_phase = false;

                    if (!pForge.CompleteForgeHandshake())
                    {
                        log.Error(Translations.Get("error.forge_encrypt"));
                        return false;
                    }

                    StartUpdating();
                    return true;
                }
                else HandlePacket(packetID, packetData);
            }
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
            SendPacket(PacketTypesOut.TabComplete, tabcomplete_packet);

            int wait_left = 50; //do not wait more than 5 seconds (50 * 100 ms)
            Thread t1 = new Thread(new ThreadStart(delegate
            {
                while (wait_left > 0 && !autocomplete_received) { System.Threading.Thread.Sleep(100); wait_left--; }
                if (autocomplete_result.Count > 0)
                    ConsoleIO.WriteLineFormatted("§8" + String.Join(" ", autocomplete_result), false);
            }));
            t1.Start();
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
            tcp.ReceiveTimeout = 30000; // 30 seconds
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
                Queue<byte> packetData = new Queue<byte>(socketWrapper.ReadDataRAW(packetLength));
                if (dataTypes.ReadNextVarInt(packetData) == 0x00) //Read Packet ID
                {
                    string result = dataTypes.ReadNextString(packetData); //Get the Json data

                    if (Settings.DebugMessages)
                    {
                        // May contain formatting codes, cannot use WriteLineFormatted
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        ConsoleIO.WriteLine(result);
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }

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
                                protocolversion = int.Parse(versionData.Properties["protocol"].StringValue);

                            // Check for forge on the server.
                            Protocol18Forge.ServerInfoCheckForge(jsonData, ref forgeInfo);

                            ConsoleIO.WriteLineFormatted(Translations.Get("mcc.server_protocol", version, protocolversion + (forgeInfo != null ? Translations.Get("mcc.with_forge") : "")));

                            return true;
                        }
                    }
                }
            }
            return false;
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
        /// Get the current protocol version.
        /// </summary>
        /// <remarks>
        /// Version-specific operations should be handled inside the Protocol handled whenever possible.
        /// </remarks>
        /// <returns>Minecraft Protocol version number</returns>
        public int GetProtocolVersion()
        {
            return protocolversion;
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
                SendPacket(PacketTypesOut.ChatMessage, message_packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendEntityAction(int PlayerEntityID, int ActionID)
        {
            try
            {
                List<byte> fields = new List<byte>();
                fields.AddRange(dataTypes.GetVarInt(PlayerEntityID));
                fields.AddRange(dataTypes.GetVarInt(ActionID));
                fields.AddRange(dataTypes.GetVarInt(0));
                SendPacket(PacketTypesOut.EntityAction, fields);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        /// <summary>
        /// Send a respawn packet to the server
        /// </summary>
        /// <returns>True if properly sent</returns>
        public bool SendRespawnPacket()
        {
            try
            {
                SendPacket(PacketTypesOut.ClientStatus, new byte[] { 0 });
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
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
                if (protocolversion >= MC117Version)
                    fields.Add(1);
                SendPacket(PacketTypesOut.ClientSettings, fields);
            }
            catch (SocketException) { }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
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
                PacketTypesOut packetType = PacketTypesOut.PlayerPosition;

                if (yaw.HasValue && pitch.HasValue)
                {
                    yawpitch = dataTypes.ConcatBytes(dataTypes.GetFloat(yaw.Value), dataTypes.GetFloat(pitch.Value));
                    packetType = PacketTypesOut.PlayerPositionAndRotation;
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
                catch (System.IO.IOException) { return false; }
                catch (ObjectDisposedException) { return false; }
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

                    SendPacket(PacketTypesOut.PluginMessage, dataTypes.ConcatBytes(dataTypes.GetString(channel), length, data));
                }
                else
                {
                    SendPacket(PacketTypesOut.PluginMessage, dataTypes.ConcatBytes(dataTypes.GetString(channel), data));
                }

                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        /// <summary>
        /// Send a Login Plugin Response packet (0x02)
        /// </summary>
        /// <param name="messageId">Login Plugin Request message Id </param>
        /// <param name="understood">TRUE if the request was understood</param>
        /// <param name="data">Response to the request</param>
        /// <returns>TRUE if successfully sent</returns>
        public bool SendLoginPluginResponse(int messageId, bool understood, byte[] data)
        {
            try
            {
                SendPacket(0x02, dataTypes.ConcatBytes(dataTypes.GetVarInt(messageId), dataTypes.GetBool(understood), data));
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        /// <summary>
        /// Send an Interact Entity Packet to server
        /// </summary>
        /// <param name="EntityID"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool SendInteractEntity(int EntityID, int type)
        {
            try
            {
                List<byte> fields = new List<byte>();
                fields.AddRange(dataTypes.GetVarInt(EntityID));
                fields.AddRange(dataTypes.GetVarInt(type));

                // Is player Sneaking (Only 1.16 and above)
                // Currently hardcoded to false
                // TODO: Update to reflect the real player state
                if (protocolversion >= MC116Version)
                    fields.AddRange(dataTypes.GetBool(false));

                SendPacket(PacketTypesOut.InteractEntity, fields);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        // TODO: Interact at block location (e.g. chest minecart)
        public bool SendInteractEntity(int EntityID, int type, float X, float Y, float Z, int hand)
        {
            try
            {
                List<byte> fields = new List<byte>();
                fields.AddRange(dataTypes.GetVarInt(EntityID));
                fields.AddRange(dataTypes.GetVarInt(type));
                fields.AddRange(dataTypes.GetFloat(X));
                fields.AddRange(dataTypes.GetFloat(Y));
                fields.AddRange(dataTypes.GetFloat(Z));
                fields.AddRange(dataTypes.GetVarInt(hand));
                // Is player Sneaking (Only 1.16 and above)
                // Currently hardcoded to false
                // TODO: Update to reflect the real player state
                if (protocolversion >= MC116Version)
                    fields.AddRange(dataTypes.GetBool(false));
                SendPacket(PacketTypesOut.InteractEntity, fields);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }
        public bool SendInteractEntity(int EntityID, int type, int hand)
        {
            try
            {
                List<byte> fields = new List<byte>();
                fields.AddRange(dataTypes.GetVarInt(EntityID));
                fields.AddRange(dataTypes.GetVarInt(type));
                fields.AddRange(dataTypes.GetVarInt(hand));
                // Is player Sneaking (Only 1.16 and above)
                // Currently hardcoded to false
                // TODO: Update to reflect the real player state
                if (protocolversion >= MC116Version)
                    fields.AddRange(dataTypes.GetBool(false));
                SendPacket(PacketTypesOut.InteractEntity, fields);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }
        public bool SendInteractEntity(int EntityID, int type, float X, float Y, float Z)
        {
            return false;
        }

        public bool SendUseItem(int hand)
        {
            if (protocolversion < MC19Version)
                return false; // Packet does not exist prior to MC 1.9
                              // According to https://wiki.vg/index.php?title=Protocol&oldid=5486#Player_Block_Placement
                              // MC 1.7 does this using Player Block Placement with special values
                              // TODO once Player Block Placement is implemented for older versions
            try
            {
                List<byte> packet = new List<byte>();
                packet.AddRange(dataTypes.GetVarInt(hand));
                SendPacket(PacketTypesOut.UseItem, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendPlayerDigging(int status, Location location, Direction face)
        {
            try
            {
                List<byte> packet = new List<byte>();
                packet.AddRange(dataTypes.GetVarInt(status));
                packet.AddRange(dataTypes.GetLocation(location));
                packet.AddRange(dataTypes.GetVarInt(dataTypes.GetBlockFace(face)));
                SendPacket(PacketTypesOut.PlayerDigging, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendPlayerBlockPlacement(int hand, Location location, Direction face)
        {
            if (protocolversion < MC114Version)
                return false; // NOT IMPLEMENTED for older MC versions
            try
            {
                List<byte> packet = new List<byte>();
                packet.AddRange(dataTypes.GetVarInt(hand));
                packet.AddRange(dataTypes.GetLocation(location));
                packet.AddRange(dataTypes.GetVarInt(dataTypes.GetBlockFace(face)));
                packet.AddRange(dataTypes.GetFloat(0.5f)); // cursorX
                packet.AddRange(dataTypes.GetFloat(0.5f)); // cursorY
                packet.AddRange(dataTypes.GetFloat(0.5f)); // cursorZ
                packet.Add(0); // insideBlock = false;
                SendPacket(PacketTypesOut.PlayerBlockPlacement, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendHeldItemChange(short slot)
        {
            try
            {
                List<byte> packet = new List<byte>();
                packet.AddRange(dataTypes.GetShort(slot));
                SendPacket(PacketTypesOut.HeldItemChange, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendWindowAction(int windowId, int slotId, WindowActionType action, Item item)
        {
            try
            {
                short actionNumber;
                lock (window_actions)
                {
                    if (!window_actions.ContainsKey(windowId))
                        window_actions[windowId] = 0;
                    actionNumber = (short)(window_actions[windowId] + 1);
                    window_actions[windowId] = actionNumber;
                }

                byte button = 0;
                byte mode = 0;

                switch (action)
                {
                    case WindowActionType.LeftClick:       button = 0;  break;
                    case WindowActionType.RightClick:      button = 1;  break;
                    case WindowActionType.MiddleClick:     button = 2;  mode = 3; break;
                    case WindowActionType.ShiftClick:      button = 0;  mode = 1; item = new Item(ItemType.Null, 0, null); break;
                    case WindowActionType.DropItem:        button = 0;  mode = 4; item = new Item(ItemType.Null, 0, null); break;
                    case WindowActionType.DropItemStack:   button = 1;  mode = 4; item = new Item(ItemType.Null, 0, null); break;
                    case WindowActionType.StartDragLeft:   button = 0;  mode = 5; item = new Item(ItemType.Null, 0, null); slotId = -999; break;
                    case WindowActionType.StartDragRight:  button = 4;  mode = 5; item = new Item(ItemType.Null, 0, null); slotId = -999; break;
                    case WindowActionType.StartDragMiddle: button = 8;  mode = 5; item = new Item(ItemType.Null, 0, null); slotId = -999; break;
                    case WindowActionType.EndDragLeft:     button = 2;  mode = 5; item = new Item(ItemType.Null, 0, null); slotId = -999; break;
                    case WindowActionType.EndDragRight:    button = 6;  mode = 5; item = new Item(ItemType.Null, 0, null); slotId = -999; break;
                    case WindowActionType.EndDragMiddle:   button = 10; mode = 5; item = new Item(ItemType.Null, 0, null); slotId = -999; break;
                    case WindowActionType.AddDragLeft:     button = 1;  mode = 5; item = new Item(ItemType.Null, 0, null); break;
                    case WindowActionType.AddDragRight:    button = 5;  mode = 5; item = new Item(ItemType.Null, 0, null); break;
                    case WindowActionType.AddDragMiddle:   button = 9;  mode = 5; item = new Item(ItemType.Null, 0, null); break;
                }

                List<byte> packet = new List<byte>();
                packet.Add((byte)windowId);
                packet.AddRange(dataTypes.GetShort((short)slotId));
                packet.Add(button);
                if (protocolversion < MC117Version) packet.AddRange(dataTypes.GetShort(actionNumber));
                if (protocolversion >= MC19Version)
                    packet.AddRange(dataTypes.GetVarInt(mode));
                else packet.Add(mode);
                packet.AddRange(dataTypes.GetItemSlot(item, itemPalette));
                SendPacket(PacketTypesOut.ClickWindow, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendCreativeInventoryAction(int slot, ItemType itemType, int count, Dictionary<string, object> nbt)
        {
            try
            {
                List<byte> packet = new List<byte>();
                packet.AddRange(dataTypes.GetShort((short)slot));
                packet.AddRange(dataTypes.GetItemSlot(new Item(itemType, count, nbt), itemPalette));
                SendPacket(PacketTypesOut.CreativeInventoryAction, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendAnimation(int animation, int playerid)
        {
            try
            {
                if (animation == 0 || animation == 1)
                {
                    List<byte> packet = new List<byte>();

                    if (protocolversion < MC18Version)
                    {
                        packet.AddRange(dataTypes.GetInt(playerid));
                        packet.Add((byte)1); // Swing arm
                    }
                    else if (protocolversion < MC19Version)
                    {
                        // No fields in 1.8.X
                    }
                    else // MC 1.9+
                    {
                        packet.AddRange(dataTypes.GetVarInt(animation));
                    }

                    SendPacket(PacketTypesOut.Animation, packet);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendCloseWindow(int windowId)
        {
            try
            {
                lock (window_actions)
                {
                    if (window_actions.ContainsKey(windowId))
                        window_actions[windowId] = 0;
                }
                SendPacket(PacketTypesOut.CloseWindow, new[] { (byte)windowId });
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendUpdateSign(Location sign, string line1, string line2, string line3, string line4)
        {
            try
            {
                if (line1.Length > 23)
                    line1 = line1.Substring(0, 23);
                if (line2.Length > 23)
                    line2 = line1.Substring(0, 23);
                if (line3.Length > 23)
                    line3 = line1.Substring(0, 23);
                if (line4.Length > 23)
                    line4 = line1.Substring(0, 23);

                List<byte> packet = new List<byte>();
                packet.AddRange(dataTypes.GetLocation(sign));
                packet.AddRange(dataTypes.GetString(line1));
                packet.AddRange(dataTypes.GetString(line2));
                packet.AddRange(dataTypes.GetString(line3));
                packet.AddRange(dataTypes.GetString(line4));
                SendPacket(PacketTypesOut.UpdateSign, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }
        
        public bool UpdateCommandBlock(Location location, string command, CommandBlockMode mode, CommandBlockFlags flags)
        {
            if (protocolversion <= MC113Version)
            {
                try
                {
                    List<byte> packet = new List<byte>();
                    packet.AddRange(dataTypes.GetLocation(location));
                    packet.AddRange(dataTypes.GetString(command));
                    packet.AddRange(dataTypes.GetVarInt((int)mode));
                    packet.Add((byte)flags);
                    SendPacket(PacketTypesOut.UpdateSign, packet);
                    return true;
                }
                catch (SocketException) { return false; }
                catch (System.IO.IOException) { return false; }
                catch (ObjectDisposedException) { return false; }
            }
            else { return false;  }
        }

        public bool SendWindowConfirmation(byte windowID, short actionID, bool accepted)
        {
            try
            {
                List<byte> packet = new List<byte>();
                packet.Add(windowID);
                packet.AddRange(dataTypes.GetShort(actionID));
                packet.Add(accepted ? (byte)1 : (byte)0);
                SendPacket(PacketTypesOut.WindowConfirmation, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SelectTrade(int selectedSlot)
        {
            // MC 1.13 or greater
            if (protocolversion >= MC113Version)
            {
                try
                {
                    List<byte> packet = new List<byte>();
                    packet.AddRange(dataTypes.GetVarInt(selectedSlot));
                    SendPacket(PacketTypesOut.SelectTrade, packet);
                    return true;
                }
                catch (SocketException) { return false; }
                catch (System.IO.IOException) { return false; }
                catch (ObjectDisposedException) { return false; }
            }
            else { return false; }
        }
    }
}
