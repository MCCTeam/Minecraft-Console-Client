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
using System.Diagnostics;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.PacketPalettes;
using MinecraftClient.Logger;
using System.Threading.Tasks;
using MinecraftClient.Protocol.Keys;
using System.Text.RegularExpressions;
using MinecraftClient.Protocol.Message;

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
        internal const int MC_1_8_Version = 47;
        internal const int MC_1_9_Version = 107;
        internal const int MC_1_9_1_Version = 108;
        internal const int MC_1_10_Version = 210;
        internal const int MC_1_11_2_Version = 316;
        internal const int MC_1_12_Version = 335;
        internal const int MC_1_12_2_Version = 340;
        internal const int MC_1_13_Version = 393;
        internal const int MC_1_14_Version = 477;
        internal const int MC_1_15_Version = 573;
        internal const int MC_1_15_2_Version = 578;
        internal const int MC_1_16_Version = 735;
        internal const int MC_1_16_1_Version = 736;
        internal const int MC_1_16_2_Version = 751;
        internal const int MC_1_16_3_Version = 753;
        internal const int MC_1_16_5_Version = 754;
        internal const int MC_1_17_Version = 755;
        internal const int MC_1_17_1_Version = 756;
        internal const int MC_1_18_1_Version = 757;
        internal const int MC_1_18_2_Version = 758;
        internal const int MC_1_19_Version = 759;
        internal const int MC_1_19_2_Version = 760;

        private int compression_treshold = 0;
        private bool autocomplete_received = false;
        private int autocomplete_transaction_id = 0;
        private readonly List<string> autocomplete_result = new List<string>();
        private readonly Dictionary<int, short> window_actions = new Dictionary<int, short>();
        private bool login_phase = true;
        private int protocolversion;
        private int currentDimension;

        private int pendingAcknowledgments = 0;
        private LastSeenMessagesCollector lastSeenMessagesCollector = new(5);
        private LastSeenMessageList.Entry? lastReceivedMessage = null;

        Protocol18Forge pForge;
        Protocol18Terrain pTerrain;
        IMinecraftComHandler handler;
        EntityPalette entityPalette;
        ItemPalette itemPalette;
        PacketTypePalette packetPalette;
        SocketWrapper socketWrapper;
        DataTypes dataTypes;
        Tuple<Thread, CancellationTokenSource>? netRead = null; // main thread
        ILogger log;
        RandomNumberGenerator randomGen;

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
            this.randomGen = RandomNumberGenerator.Create();

            if (handler.GetTerrainEnabled() && protocolversion > MC_1_18_2_Version)
            {
                log.Error(Translations.Get("extra.terrainandmovement_disabled"));
                handler.SetTerrainEnabled(false);
            }

            if (handler.GetInventoryEnabled() && (protocolversion < MC_1_10_Version || protocolversion > MC_1_18_2_Version))
            {
                log.Error(Translations.Get("extra.inventory_disabled"));
                handler.SetInventoryEnabled(false);
            }

            if (handler.GetEntityHandlingEnabled() && (protocolversion < MC_1_10_Version || protocolversion > MC_1_18_2_Version))
            {
                log.Error(Translations.Get("extra.entity_disabled"));
                handler.SetEntityHandlingEnabled(false);
            }

            // Block palette
            if (protocolversion >= MC_1_13_Version)
            {
                if (protocolVersion > MC_1_18_2_Version && handler.GetTerrainEnabled())
                    throw new NotImplementedException(Translations.Get("exception.palette.block"));
                if (protocolVersion >= MC_1_17_Version)
                    Block.Palette = new Palette117();
                else if (protocolVersion >= MC_1_16_Version)
                    if (protocolVersion >= MC_1_16_Version)
                        Block.Palette = new Palette116();
                    else if (protocolVersion >= MC_1_15_Version)
                        Block.Palette = new Palette115();
                    else if (protocolVersion >= MC_1_14_Version)
                        Block.Palette = new Palette114();
                    else Block.Palette = new Palette113();
            }
            else Block.Palette = new Palette112();

            // Entity palette
            if (protocolversion >= MC_1_13_Version)
            {
                if (protocolversion > MC_1_18_2_Version && handler.GetEntityHandlingEnabled())
                    throw new NotImplementedException(Translations.Get("exception.palette.entity"));
                if (protocolversion >= MC_1_17_Version)
                    entityPalette = new EntityPalette117();
                else if (protocolversion >= MC_1_16_2_Version)
                    if (protocolversion >= MC_1_16_2_Version)
                        entityPalette = new EntityPalette1162();
                    else if (protocolversion >= MC_1_16_Version)
                        entityPalette = new EntityPalette1161();
                    else if (protocolversion >= MC_1_15_Version)
                        entityPalette = new EntityPalette115();
                    else if (protocolVersion >= MC_1_14_Version)
                        entityPalette = new EntityPalette114();
                    else entityPalette = new EntityPalette113();
            }
            else entityPalette = new EntityPalette112();

            // Item palette
            if (protocolversion >= MC_1_16_2_Version)
            {
                if (protocolversion > MC_1_18_2_Version && handler.GetInventoryEnabled())
                    throw new NotImplementedException(Translations.Get("exception.palette.item"));
                if (protocolversion >= MC_1_18_1_Version)
                    itemPalette = new ItemPalette118();
                else if (protocolversion >= MC_1_17_Version)
                    itemPalette = new ItemPalette117();
                else if (protocolversion >= MC_1_16_2_Version)
                    if (protocolversion >= MC_1_16_2_Version)
                        itemPalette = new ItemPalette1162();
                    else itemPalette = new ItemPalette1161();
            }
            else itemPalette = new ItemPalette115();

            // MessageType 
            // You can find it in https://wiki.vg/Protocol#Player_Chat_Message or /net/minecraft/network/message/MessageType.java
            if (protocolversion >= MC_1_19_2_Version)
            {
                ChatParser.ChatId2Type = new()
                {
                    { 0,  ChatParser.MessageType.CHAT },
                    { 1,  ChatParser.MessageType.SAY_COMMAND },
                    { 2,  ChatParser.MessageType.MSG_COMMAND_INCOMING },
                    { 3,  ChatParser.MessageType.MSG_COMMAND_OUTGOING },
                    { 4,  ChatParser.MessageType.TEAM_MSG_COMMAND_INCOMING },
                    { 5,  ChatParser.MessageType.TEAM_MSG_COMMAND_OUTGOING },
                    { 6,  ChatParser.MessageType.EMOTE_COMMAND },
                };
            }
            else if (protocolversion >= MC_1_19_Version)
            {
                ChatParser.ChatId2Type = new()
                {
                    { 0,  ChatParser.MessageType.CHAT },
                    { 1,  ChatParser.MessageType.RAW_MSG },
                    { 2,  ChatParser.MessageType.RAW_MSG },
                    { 3,  ChatParser.MessageType.SAY_COMMAND },
                    { 4,  ChatParser.MessageType.MSG_COMMAND_INCOMING },
                    { 5,  ChatParser.MessageType.TEAM_MSG_COMMAND_INCOMING },
                    { 6,  ChatParser.MessageType.EMOTE_COMMAND },
                    { 7,  ChatParser.MessageType.RAW_MSG },
                };
            }
        }

        /// <summary>
        /// Separate thread. Network reading loop.
        /// </summary>
        private void Updater(object? o)
        {

            if (((CancellationToken)o!).IsCancellationRequested)
                return;

            try
            {

                bool keepUpdating = true;
                Stopwatch stopWatch = new Stopwatch();
                while (keepUpdating)
                {

                    ((CancellationToken)o!).ThrowIfCancellationRequested();

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
            catch (OperationCanceledException) { }

            if (((CancellationToken)o!).IsCancellationRequested)
                return;

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
            if (protocolversion >= MC_1_8_Version
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
                            if (protocolversion >= MC_1_8_Version)
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
                            //log.Info("KeepAlive");
                            SendPacket(PacketTypesOut.KeepAlive, packetData);
                            handler.OnServerKeepAlive();
                            break;
                        case PacketTypesIn.JoinGame:
                            handler.OnGameJoined();
                            int playerEntityID = dataTypes.ReadNextInt(packetData);
                            handler.OnReceivePlayerEntityID(playerEntityID);

                            if (protocolversion >= MC_1_16_2_Version)
                                dataTypes.ReadNextBool(packetData);                       // Is hardcore - 1.16.2 and above

                            handler.OnGamemodeUpdate(Guid.Empty, dataTypes.ReadNextByte(packetData));

                            if (protocolversion >= MC_1_16_Version)
                            {
                                dataTypes.ReadNextByte(packetData);                       // Previous Gamemode - 1.16 and above
                                int worldCount = dataTypes.ReadNextVarInt(packetData);    // Dimension Count (World Count) - 1.16 and above
                                for (int i = 0; i < worldCount; i++)
                                    dataTypes.ReadNextString(packetData);                 // Dimension Names (World Names) - 1.16 and above
                                dataTypes.ReadNextNbt(packetData);                        // Registry Codec (Dimension Codec) - 1.16 and above
                            }

                            string? currentDimensionName = null;
                            Dictionary<string, object>? currentDimensionType = null;

                            // Current dimension
                            //   NBT Tag Compound: 1.16.2 and above
                            //   String identifier: 1.16 and 1.16.1
                            //   varInt: [1.9.1 to 1.15.2]
                            //   byte: below 1.9.1
                            if (protocolversion >= MC_1_16_Version)
                            {
                                if (protocolversion >= MC_1_19_Version)
                                {
                                    dataTypes.ReadNextString(packetData); // Dimension Type: Identifier
                                    currentDimensionType = new Dictionary<string, object>();
                                }
                                else if (protocolversion >= MC_1_16_2_Version)
                                    currentDimensionType = dataTypes.ReadNextNbt(packetData); // Dimension Type: NBT Tag Compound
                                else
                                    dataTypes.ReadNextString(packetData);
                                this.currentDimension = 0;
                            }
                            else if (protocolversion >= MC_1_9_1_Version)
                                this.currentDimension = dataTypes.ReadNextInt(packetData);
                            else
                                this.currentDimension = (sbyte)dataTypes.ReadNextByte(packetData);

                            if (protocolversion < MC_1_14_Version)
                                dataTypes.ReadNextByte(packetData);           // Difficulty - 1.13 and below

                            if (protocolversion >= MC_1_16_Version)
                                currentDimensionName = dataTypes.ReadNextString(packetData); // Dimension Name (World Name) - 1.16 and above

                            if (protocolversion >= MC_1_16_2_Version)
                                World.SetDimension(currentDimensionName, currentDimensionType);

                            if (protocolversion >= MC_1_15_Version)
                                dataTypes.ReadNextLong(packetData);           // Hashed world seed - 1.15 and above
                            if (protocolversion >= MC_1_16_2_Version)
                                dataTypes.ReadNextVarInt(packetData);         // Max Players - 1.16.2 and above
                            else
                                dataTypes.ReadNextByte(packetData);           // Max Players - 1.16.1 and below
                            if (protocolversion < MC_1_16_Version)
                                dataTypes.ReadNextString(packetData);         // Level Type - 1.15 and below
                            if (protocolversion >= MC_1_14_Version)
                                dataTypes.ReadNextVarInt(packetData);         // View distance - 1.14 and above
                            if (protocolversion >= MC_1_18_1_Version)
                                dataTypes.ReadNextVarInt(packetData);         // Simulation Distance - 1.18 and above
                            if (protocolversion >= MC_1_8_Version)
                                dataTypes.ReadNextBool(packetData);           // Reduced debug info - 1.8 and above
                            if (protocolversion >= MC_1_15_Version)
                                dataTypes.ReadNextBool(packetData);           // Enable respawn screen - 1.15 and above
                            if (protocolversion >= MC_1_16_Version)
                            {
                                dataTypes.ReadNextBool(packetData);           // Is Debug - 1.16 and above
                                dataTypes.ReadNextBool(packetData);           // Is Flat - 1.16 and above
                            }
                            if (protocolversion >= MC_1_19_Version)
                            {
                                bool hasDeathLocation = dataTypes.ReadNextBool(packetData); // Has death location
                                if (hasDeathLocation)
                                {
                                    dataTypes.ReadNextString(packetData); // Death dimension name: Identifier
                                    dataTypes.ReadNextLocation(packetData); // Death location
                                }
                            }
                            break;
                        case PacketTypesIn.ChatMessage:
                            int messageType = 0;

                            if (protocolversion <= MC_1_18_2_Version) // 1.18 and bellow
                            {
                                string message = dataTypes.ReadNextString(packetData);

                                Guid senderUUID;
                                if (protocolversion >= MC_1_8_Version)
                                {
                                    //Hide system messages or xp bar messages?
                                    messageType = dataTypes.ReadNextByte(packetData);
                                    if ((messageType == 1 && !Settings.DisplaySystemMessages)
                                        || (messageType == 2 && !Settings.DisplayXPBarMessages))
                                        break;

                                    if (protocolversion >= MC_1_16_5_Version)
                                        senderUUID = dataTypes.ReadNextUUID(packetData);
                                    else senderUUID = Guid.Empty;
                                }
                                else
                                    senderUUID = Guid.Empty;

                                handler.OnTextReceived(new(message, true, messageType, senderUUID));
                            }
                            else if (protocolversion == MC_1_19_Version) // 1.19
                            {
                                string signedChat = dataTypes.ReadNextString(packetData);

                                bool hasUnsignedChatContent = dataTypes.ReadNextBool(packetData);
                                string? unsignedChatContent = hasUnsignedChatContent ? dataTypes.ReadNextString(packetData) : null;

                                messageType = dataTypes.ReadNextVarInt(packetData);
                                if ((messageType == 1 && !Settings.DisplaySystemMessages)
                                        || (messageType == 2 && !Settings.DisplayXPBarMessages))
                                    break;

                                Guid senderUUID = dataTypes.ReadNextUUID(packetData);
                                string senderDisplayName = ChatParser.ParseText(dataTypes.ReadNextString(packetData));

                                bool hasSenderTeamName = dataTypes.ReadNextBool(packetData);
                                string? senderTeamName = hasSenderTeamName ? ChatParser.ParseText(dataTypes.ReadNextString(packetData)) : null;

                                long timestamp = dataTypes.ReadNextLong(packetData);

                                long salt = dataTypes.ReadNextLong(packetData);

                                byte[] messageSignature = dataTypes.ReadNextByteArray(packetData);

                                bool verifyResult;
                                if (senderUUID == handler.GetUserUuid())
                                    verifyResult = true;
                                else
                                {
                                    PlayerInfo? player = handler.GetPlayerInfo(senderUUID);
                                    verifyResult = player == null ? false : player.VerifyMessage(signedChat, timestamp, salt, ref messageSignature);
                                }

                                handler.OnTextReceived(new(signedChat, true, messageType, senderUUID, unsignedChatContent, senderDisplayName, senderTeamName, timestamp, messageSignature, verifyResult));
                            }
                            else // 1.19.1 +
                            {
                                byte[]? precedingSignature = dataTypes.ReadNextBool(packetData) ? dataTypes.ReadNextByteArray(packetData) : null;
                                Guid senderUUID = dataTypes.ReadNextUUID(packetData);
                                byte[] headerSignature = dataTypes.ReadNextByteArray(packetData);

                                string signedChat = dataTypes.ReadNextString(packetData);
                                string? decorated = dataTypes.ReadNextBool(packetData) ? dataTypes.ReadNextString(packetData) : null;

                                long timestamp = dataTypes.ReadNextLong(packetData);
                                long salt = dataTypes.ReadNextLong(packetData);

                                int lastSeenMessageListLen = dataTypes.ReadNextVarInt(packetData);
                                LastSeenMessageList.Entry[] lastSeenMessageList = new LastSeenMessageList.Entry[lastSeenMessageListLen];
                                for (int i = 0; i < lastSeenMessageListLen; ++i)
                                {
                                    Guid user = dataTypes.ReadNextUUID(packetData);
                                    byte[] lastSignature = dataTypes.ReadNextByteArray(packetData);
                                    lastSeenMessageList[i] = new(user, lastSignature);
                                }
                                LastSeenMessageList lastSeenMessages = new(lastSeenMessageList);

                                string? unsignedChatContent = dataTypes.ReadNextBool(packetData) ? dataTypes.ReadNextString(packetData) : null;

                                int filterEnum = dataTypes.ReadNextVarInt(packetData);
                                if (filterEnum == 2) // PARTIALLY_FILTERED
                                    dataTypes.ReadNextULongArray(packetData);

                                int chatTypeId = dataTypes.ReadNextVarInt(packetData);
                                string chatName = dataTypes.ReadNextString(packetData);
                                string? targetName = dataTypes.ReadNextBool(packetData) ? dataTypes.ReadNextString(packetData) : null;

                                bool verifyResult;
                                if (senderUUID == handler.GetUserUuid())
                                    verifyResult = true;
                                else
                                {
                                    PlayerInfo? player = handler.GetPlayerInfo(senderUUID);
                                    if (player == null || !player.IsMessageChainLegal())
                                        verifyResult = false;
                                    else
                                    {
                                        bool lastVerifyResult = player.IsMessageChainLegal();
                                        verifyResult = player.VerifyMessage(signedChat, timestamp, salt, ref headerSignature, ref precedingSignature, lastSeenMessages);
                                        if (lastVerifyResult && !verifyResult)
                                            log.Warn("Player " + player.DisplayName + "'s message chain is broken!");
                                    }
                                }

                                Dictionary<string, Json.JSONData> chatInfo = Json.ParseJson(chatName).Properties;
                                string senderDisplayName = (chatInfo.ContainsKey("insertion") ? chatInfo["insertion"] : chatInfo["text"]).StringValue;
                                string? senderTeamName = null;
                                ChatParser.MessageType messageTypeEnum = ChatParser.ChatId2Type![chatTypeId];
                                if (targetName != null && 
                                    (messageTypeEnum == ChatParser.MessageType.TEAM_MSG_COMMAND_INCOMING || messageTypeEnum == ChatParser.MessageType.TEAM_MSG_COMMAND_OUTGOING))
                                    senderTeamName = Json.ParseJson(targetName).Properties["with"].DataArray[0].Properties["text"].StringValue;

                                ChatMessage chat = new(signedChat, false, chatTypeId, senderUUID, unsignedChatContent, senderDisplayName, senderTeamName, timestamp, headerSignature, verifyResult);
                                if (!chat.lacksSender())
                                    this.acknowledge(chat);
                                handler.OnTextReceived(chat);
                            }
                            break;
                        case PacketTypesIn.MessageHeader:
                            if (protocolversion >= MC_1_19_2_Version)
                            {
                                byte[]? precedingSignature = dataTypes.ReadNextBool(packetData) ? dataTypes.ReadNextByteArray(packetData) : null;
                                Guid senderUUID = dataTypes.ReadNextUUID(packetData);
                                byte[] headerSignature = dataTypes.ReadNextByteArray(packetData);
                                byte[] bodyDigest = dataTypes.ReadNextByteArray(packetData);

                                bool verifyResult;
                                if (senderUUID == handler.GetUserUuid())
                                    verifyResult = true;
                                else
                                {
                                    PlayerInfo? player = handler.GetPlayerInfo(senderUUID);
                                    if (player == null || !player.IsMessageChainLegal())
                                        verifyResult = false;
                                    else
                                    {
                                        bool lastVerifyResult = player.IsMessageChainLegal();
                                        verifyResult = player.VerifyMessageHead(ref precedingSignature, ref headerSignature, ref bodyDigest);
                                        if (lastVerifyResult && !verifyResult)
                                            log.Warn("Player " + player.DisplayName + "'s message chain is broken!");
                                    }
                                }
                            }
                            break;
                        case PacketTypesIn.Respawn:
                            string? dimensionNameInRespawn = null;
                            Dictionary<string, object> dimensionTypeInRespawn = null;
                            if (protocolversion >= MC_1_16_Version)
                            {
                                if (protocolversion >= MC_1_19_Version)
                                {
                                    dataTypes.ReadNextString(packetData); // Dimension Type: Identifier
                                    dimensionTypeInRespawn = new Dictionary<string, object>();
                                }
                                else if (protocolversion >= MC_1_16_2_Version)
                                    dimensionTypeInRespawn = dataTypes.ReadNextNbt(packetData); // Dimension Type: NBT Tag Compound
                                else
                                    dataTypes.ReadNextString(packetData);
                                this.currentDimension = 0;
                            }
                            else
                            {
                                // 1.15 and below
                                this.currentDimension = dataTypes.ReadNextInt(packetData);
                            }
                            if (protocolversion >= MC_1_16_Version)
                                dimensionNameInRespawn = dataTypes.ReadNextString(packetData); // Dimension Name (World Name) - 1.16 and above

                            if (protocolversion >= MC_1_16_2_Version)
                                World.SetDimension(dimensionNameInRespawn, dimensionTypeInRespawn);

                            if (protocolversion < MC_1_14_Version)
                                dataTypes.ReadNextByte(packetData);           // Difficulty - 1.13 and below
                            if (protocolversion >= MC_1_15_Version)
                                dataTypes.ReadNextLong(packetData);           // Hashed world seed - 1.15 and above
                            dataTypes.ReadNextByte(packetData);               // Gamemode
                            if (protocolversion >= MC_1_16_Version)
                                dataTypes.ReadNextByte(packetData);           // Previous Game mode - 1.16 and above
                            if (protocolversion < MC_1_16_Version)
                                dataTypes.ReadNextString(packetData);         // Level Type - 1.15 and below
                            if (protocolversion >= MC_1_16_Version)
                            {
                                dataTypes.ReadNextBool(packetData);           // Is Debug - 1.16 and above
                                dataTypes.ReadNextBool(packetData);           // Is Flat - 1.16 and above
                                dataTypes.ReadNextBool(packetData);           // Copy metadata - 1.16 and above
                            }
                            if (protocolversion >= MC_1_19_Version)
                            {
                                bool hasDeathLocation = dataTypes.ReadNextBool(packetData); // Has death location
                                if (hasDeathLocation)
                                {
                                    dataTypes.ReadNextString(packetData); // Death dimension name: Identifier
                                    dataTypes.ReadNextLocation(packetData); // Death location
                                }
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
                                if (protocolversion >= MC_1_8_Version)
                                {
                                    Location location = handler.GetCurrentLocation();
                                    location.X = (locMask & 1 << 0) != 0 ? location.X + x : x;
                                    location.Y = (locMask & 1 << 1) != 0 ? location.Y + y : y;
                                    location.Z = (locMask & 1 << 2) != 0 ? location.Z + z : z;
                                    handler.UpdateLocation(location, yaw, pitch);
                                }
                                else handler.UpdateLocation(new Location(x, y, z), yaw, pitch);
                            }

                            if (protocolversion >= MC_1_9_Version)
                            {
                                int teleportID = dataTypes.ReadNextVarInt(packetData);
                                // Teleport confirm packet
                                SendPacket(PacketTypesOut.TeleportConfirm, dataTypes.GetVarInt(teleportID));
                            }

                            if (protocolversion >= MC_1_17_Version)
                                dataTypes.ReadNextBool(packetData); // Dismount Vehicle    - 1.17 and above
                            break;
                        case PacketTypesIn.ChunkData:
                            if (handler.GetTerrainEnabled())
                            {
                                int chunkX = dataTypes.ReadNextInt(packetData);
                                int chunkZ = dataTypes.ReadNextInt(packetData);
                                if (protocolversion >= MC_1_17_Version)
                                {
                                    ulong[]? verticalStripBitmask = null;

                                    if (protocolversion == MC_1_17_Version || protocolversion == MC_1_17_1_Version)
                                        verticalStripBitmask = dataTypes.ReadNextULongArray(packetData); // Bit Mask Le:ngth  and  Primary Bit Mask

                                    dataTypes.ReadNextNbt(packetData); // Heightmaps

                                    if (protocolversion == MC_1_17_Version || protocolversion == MC_1_17_1_Version)
                                    {
                                        int biomesLength = dataTypes.ReadNextVarInt(packetData); // Biomes length
                                        for (int i = 0; i < biomesLength; i++)
                                        {
                                            dataTypes.SkipNextVarInt(packetData); // Biomes
                                        }
                                    }

                                    int dataSize = dataTypes.ReadNextVarInt(packetData); // Size

                                    Interlocked.Increment(ref handler.GetWorld().chunkCnt);
                                    Interlocked.Increment(ref handler.GetWorld().chunkLoadNotCompleted);
                                    new Task(() =>
                                    {
                                        pTerrain.ProcessChunkColumnData(chunkX, chunkZ, verticalStripBitmask, packetData);
                                        Interlocked.Decrement(ref handler.GetWorld().chunkLoadNotCompleted);
                                    }).Start();
                                }
                                else
                                {
                                    bool chunksContinuous = dataTypes.ReadNextBool(packetData);
                                    if (protocolversion >= MC_1_16_Version && protocolversion <= MC_1_16_1_Version)
                                        dataTypes.ReadNextBool(packetData); // Ignore old data - 1.16 to 1.16.1 only
                                    ushort chunkMask = protocolversion >= MC_1_9_Version
                                        ? (ushort)dataTypes.ReadNextVarInt(packetData)
                                        : dataTypes.ReadNextUShort(packetData);
                                    if (protocolversion < MC_1_8_Version)
                                    {
                                        ushort addBitmap = dataTypes.ReadNextUShort(packetData);
                                        int compressedDataSize = dataTypes.ReadNextInt(packetData);
                                        byte[] compressed = dataTypes.ReadData(compressedDataSize, packetData);
                                        byte[] decompressed = ZlibUtils.Decompress(compressed);
                                        new Task(() =>
                                        {
                                            pTerrain.ProcessChunkColumnData(chunkX, chunkZ, chunkMask, addBitmap, currentDimension == 0, chunksContinuous, currentDimension, new Queue<byte>(decompressed));
                                        }).Start();
                                    }
                                    else
                                    {
                                        if (protocolversion >= MC_1_14_Version)
                                            dataTypes.ReadNextNbt(packetData);  // Heightmaps - 1.14 and above
                                        int biomesLength = 0;
                                        if (protocolversion >= MC_1_16_2_Version)
                                            if (chunksContinuous)
                                                biomesLength = dataTypes.ReadNextVarInt(packetData); // Biomes length - 1.16.2 and above
                                        if (protocolversion >= MC_1_15_Version && chunksContinuous)
                                        {
                                            if (protocolversion >= MC_1_16_2_Version)
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
                                        new Task(() =>
                                        {
                                            pTerrain.ProcessChunkColumnData(chunkX, chunkZ, chunkMask, 0, false, chunksContinuous, currentDimension, packetData);
                                        }).Start();
                                    }
                                }
                            }
                            break;
                        case PacketTypesIn.MapData:
                            int mapid = dataTypes.ReadNextVarInt(packetData);
                            byte scale = dataTypes.ReadNextByte(packetData);
                            bool trackingposition = protocolversion >= MC_1_17_Version ? false : dataTypes.ReadNextBool(packetData);
                            bool locked = false;
                            if (protocolversion >= MC_1_14_Version)
                            {
                                locked = dataTypes.ReadNextBool(packetData);
                            }
                            if (protocolversion >= MC_1_17_Version)
                            {
                                trackingposition = dataTypes.ReadNextBool(packetData);
                            }
                            int iconcount = dataTypes.ReadNextVarInt(packetData);
                            handler.OnMapData(mapid, scale, trackingposition, locked, iconcount);
                            break;
                        case PacketTypesIn.TradeList:
                            if ((protocolversion >= MC_1_14_Version) && (handler.GetInventoryEnabled()))
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
                            if (protocolversion >= MC_1_8_Version)
                            {
                                int action2 = dataTypes.ReadNextVarInt(packetData);
                                string titletext = String.Empty;
                                string subtitletext = String.Empty;
                                string actionbartext = String.Empty;
                                string json = String.Empty;
                                int fadein = -1;
                                int stay = -1;
                                int fadeout = -1;
                                if (protocolversion >= MC_1_10_Version)
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
                                if (protocolversion >= MC_1_16_2_Version)
                                {
                                    long chunkSection = dataTypes.ReadNextLong(packetData);
                                    int sectionX = (int)(chunkSection >> 42);
                                    int sectionY = (int)((chunkSection << 44) >> 44);
                                    int sectionZ = (int)((chunkSection << 22) >> 42);
                                    dataTypes.ReadNextBool(packetData); // Useless boolean (Related to light update)
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
                                    int recordCount = protocolversion < MC_1_8_Version
                                        ? (int)dataTypes.ReadNextShort(packetData)
                                        : dataTypes.ReadNextVarInt(packetData);

                                    for (int i = 0; i < recordCount; i++)
                                    {
                                        byte locationXZ;
                                        ushort blockIdMeta;
                                        int blockY;

                                        if (protocolversion < MC_1_8_Version)
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
                        case PacketTypesIn.ServerData:
                            string motd = "-";
                            bool hasMotd = dataTypes.ReadNextBool(packetData);
                            if (hasMotd)
                                motd = ChatParser.ParseText(dataTypes.ReadNextString(packetData));

                            string iconBase64 = "-";
                            bool hasIcon = dataTypes.ReadNextBool(packetData);
                            if (hasIcon)
                                iconBase64 = dataTypes.ReadNextString(packetData);

                            bool previewsChat = dataTypes.ReadNextBool(packetData);

                            handler.OnServerDataRecived(hasMotd, motd, hasIcon, iconBase64, previewsChat);
                            break;
                        case PacketTypesIn.BlockChange:
                            if (handler.GetTerrainEnabled())
                            {
                                if (protocolversion < MC_1_8_Version)
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
                        case PacketTypesIn.SetDisplayChatPreview:
                            bool previewsChatSetting = dataTypes.ReadNextBool(packetData);
                            handler.OnChatPreviewSettingUpdate(previewsChatSetting);
                            break;
                        case PacketTypesIn.ChatPreview:
                            int queryID = dataTypes.ReadNextInt(packetData);
                            bool componentIsPresent = dataTypes.ReadNextBool(packetData);

                            // Currently noy implemented
                            log.Debug("New chat preview: ");
                            log.Debug(">> Query ID: " + queryID);
                            log.Debug(">> Component is present: " + componentIsPresent);
                            if (componentIsPresent)
                            {
                                string message = dataTypes.ReadNextString(packetData);
                                log.Debug(">> Component: " + ChatParser.ParseText(message));
                                //handler.OnTextReceived(message, true);
                            }

                            break;
                        case PacketTypesIn.ChatSuggestions:
                            break;
                        case PacketTypesIn.MapChunkBulk:
                            if (protocolversion < MC_1_9_Version && handler.GetTerrainEnabled())
                            {
                                int chunkCount;
                                bool hasSkyLight;
                                Queue<byte> chunkData = packetData;

                                //Read global fields
                                if (protocolversion < MC_1_8_Version)
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
                                    addBitmaps[chunkColumnNo] = protocolversion < MC_1_8_Version
                                        ? dataTypes.ReadNextUShort(packetData)
                                        : (ushort)0;
                                }

                                //Process chunk records
                                for (int chunkColumnNo = 0; chunkColumnNo < chunkCount; chunkColumnNo++)
                                    pTerrain.ProcessChunkColumnData(chunkXs[chunkColumnNo], chunkZs[chunkColumnNo], chunkMasks[chunkColumnNo], addBitmaps[chunkColumnNo], hasSkyLight, true, currentDimension, chunkData);
                            }
                            break;
                        case PacketTypesIn.UnloadChunk:
                            if (protocolversion >= MC_1_9_Version && handler.GetTerrainEnabled())
                            {
                                int chunkX = dataTypes.ReadNextInt(packetData);
                                int chunkZ = dataTypes.ReadNextInt(packetData);

                                // Warning: It is legal to include unloaded chunks in the UnloadChunk packet.
                                // Since chunks that have not been loaded are not recorded, this may result
                                // in loading chunks that should be unloaded and inaccurate statistics.
                                if (handler.GetWorld()[chunkX, chunkZ] != null)
                                    Interlocked.Decrement(ref handler.GetWorld().chunkCnt);

                                handler.GetWorld()[chunkX, chunkZ] = null;
                            }
                            break;
                        case PacketTypesIn.PlayerInfo:
                            if (protocolversion >= MC_1_8_Version)
                            {
                                int action = dataTypes.ReadNextVarInt(packetData);                                      // Action Name
                                int numberOfPlayers = dataTypes.ReadNextVarInt(packetData);                             // Number Of Players 

                                for (int i = 0; i < numberOfPlayers; i++)
                                {
                                    Guid uuid = dataTypes.ReadNextUUID(packetData);                                     // Player UUID

                                    switch (action)
                                    {
                                        case 0x00: //Player Join (Add player since 1.19)
                                            string name = dataTypes.ReadNextString(packetData);                         // Player name
                                            int propNum = dataTypes.ReadNextVarInt(packetData);                         // Number of properties in the following array

                                            Tuple<string, string, string>[]? property = null; // Property: Tuple<Name, Value, Signature(empty if there is no signature)
                                            for (int p = 0; p < propNum; p++)
                                            {
                                                string key = dataTypes.ReadNextString(packetData);                      // Name
                                                string val = dataTypes.ReadNextString(packetData);                      // Value

                                                if (dataTypes.ReadNextBool(packetData))                                 // Is Signed
                                                    dataTypes.ReadNextString(packetData);                               // Signature
                                            }

                                            int gameMode = dataTypes.ReadNextVarInt(packetData);                        // Gamemode
                                            handler.OnGamemodeUpdate(uuid, gameMode);

                                            int ping = dataTypes.ReadNextVarInt(packetData);                            // Ping

                                            string? displayName = null;
                                            if (dataTypes.ReadNextBool(packetData))                                     // Has display name
                                                displayName = dataTypes.ReadNextString(packetData);                     // Display name

                                            // 1.19 Additions
                                            long? keyExpiration = null;
                                            byte[]? publicKey = null, signature = null;
                                            if (protocolversion >= MC_1_19_Version)
                                            {
                                                if (dataTypes.ReadNextBool(packetData))                                 // Has Sig Data (if true, red the following fields)
                                                {
                                                    keyExpiration = dataTypes.ReadNextLong(packetData);                 // Timestamp

                                                    int publicKeyLength = dataTypes.ReadNextVarInt(packetData);         // Public Key Length 
                                                    if (publicKeyLength > 0)
                                                        publicKey = dataTypes.ReadData(publicKeyLength, packetData);    // Public key

                                                    int signatureLength = dataTypes.ReadNextVarInt(packetData);         // Signature Length 
                                                    if (signatureLength > 0)
                                                        signature = dataTypes.ReadData(signatureLength, packetData);    // Public key
                                                }
                                            }

                                            handler.OnPlayerJoin(new PlayerInfo(uuid, name, property, gameMode, ping, displayName, keyExpiration, publicKey, signature));
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
                                    handler.OnPlayerJoin(new PlayerInfo(name, FakeUUID));
                                else handler.OnPlayerLeave(FakeUUID);
                            }
                            break;
                        case PacketTypesIn.TabComplete:
                            if (protocolversion >= MC_1_13_Version)
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
                                if (protocolversion >= MC_1_13_Version)
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
                            if (protocolversion < MC_1_8_Version)
                                pForge.ReadNextVarShort(packetData);
                            handler.OnPluginChannelMessage(channel, packetData.ToArray());
                            return pForge.HandlePluginMessage(channel, packetData, ref currentDimension);
                        case PacketTypesIn.Disconnect:
                            handler.OnConnectionLost(ChatBot.DisconnectReason.InGameKick, ChatParser.ParseText(dataTypes.ReadNextString(packetData)));
                            return false;
                        case PacketTypesIn.SetCompression:
                            if (protocolversion >= MC_1_8_Version && protocolversion < MC_1_9_Version)
                                compression_treshold = dataTypes.ReadNextVarInt(packetData);
                            break;
                        case PacketTypesIn.OpenWindow:
                            if (handler.GetInventoryEnabled())
                            {
                                if (protocolversion < MC_1_14_Version)
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
                                int stateId = -1;
                                int elements = 0;

                                if (protocolversion >= MC_1_17_1_Version)
                                {
                                    // State ID and Elements as VarInt - 1.17.1 and above
                                    stateId = dataTypes.ReadNextVarInt(packetData);
                                    elements = dataTypes.ReadNextVarInt(packetData);
                                }
                                else
                                {
                                    // Elements as Short - 1.17.0 and below
                                    dataTypes.ReadNextShort(packetData);
                                }

                                Dictionary<int, Item> inventorySlots = new Dictionary<int, Item>();
                                for (int slotId = 0; slotId < elements; slotId++)
                                {
                                    Item item = dataTypes.ReadNextItemSlot(packetData, itemPalette);
                                    if (item != null)
                                        inventorySlots[slotId] = item;
                                }

                                if (protocolversion >= MC_1_17_1_Version) // Carried Item - 1.17.1 and above
                                    dataTypes.ReadNextItemSlot(packetData, itemPalette);

                                handler.OnWindowItems(windowId, inventorySlots, stateId);
                            }
                            break;
                        case PacketTypesIn.SetSlot:
                            if (handler.GetInventoryEnabled())
                            {
                                byte windowID = dataTypes.ReadNextByte(packetData);
                                int stateId = -1;
                                if (protocolversion >= MC_1_17_1_Version)
                                    stateId = dataTypes.ReadNextVarInt(packetData); // State ID - 1.17.1 and above
                                short slotID = dataTypes.ReadNextShort(packetData);
                                Item item = dataTypes.ReadNextItemSlot(packetData, itemPalette);
                                handler.OnSetSlot(windowID, slotID, item, stateId);
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
                            if (protocolversion >= MC_1_17_Version)
                            {
                                forced = dataTypes.ReadNextBool(packetData);
                                string forcedMessage = ChatParser.ParseText(dataTypes.ReadNextString(packetData));
                                dataTypes.ReadNextBool(packetData);   // Has Prompt Message (Boolean) - 1.17 and above
                                dataTypes.ReadNextString(packetData); // Prompt Message (Optional Chat) - 1.17 and above
                            }
                            // Some server plugins may send invalid resource packs to probe the client and we need to ignore them (issue #1056)
                            if (!url.StartsWith("http") && hash.Length != 40) // Some server may have null hash value
                                break;
                            //Send back "accepted" and "successfully loaded" responses for plugins or server config making use of resource pack mandatory
                            byte[] responseHeader = new byte[0];
                            if (protocolversion < MC_1_10_Version) //MC 1.10 does not include resource pack hash in responses
                                responseHeader = dataTypes.ConcatBytes(dataTypes.GetVarInt(hash.Length), Encoding.UTF8.GetBytes(hash));
                            SendPacket(PacketTypesOut.ResourcePackStatus, dataTypes.ConcatBytes(responseHeader, dataTypes.GetVarInt(3))); //Accepted pack
                            SendPacket(PacketTypesOut.ResourcePackStatus, dataTypes.ConcatBytes(responseHeader, dataTypes.GetVarInt(0))); //Successfully loaded
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
                                if (protocolversion >= MC_1_16_Version)
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

                                    bool hasFactorData = false;
                                    Dictionary<string, object>? factorCodec = null;

                                    if (protocolversion >= MC_1_19_Version)
                                    {
                                        hasFactorData = dataTypes.ReadNextBool(packetData);
                                        factorCodec = dataTypes.ReadNextNbt(packetData);
                                    }

                                    handler.OnEntityEffect(entityid, effect, amplifier, duration, flags, hasFactorData, factorCodec);
                                }
                            }
                            break;
                        case PacketTypesIn.DestroyEntities:
                            if (handler.GetEntityHandlingEnabled())
                            {
                                int entityCount = 1; // 1.17.0 has only one entity per packet
                                if (protocolversion != MC_1_17_Version)
                                    entityCount = dataTypes.ReadNextVarInt(packetData); // All other versions have a "count" field
                                int[] entityList = new int[entityCount];
                                for (int i = 0; i < entityCount; i++)
                                {
                                    entityList[i] = dataTypes.ReadNextVarInt(packetData);
                                }
                                handler.OnDestroyEntities(entityList);
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
                                int NumberOfProperties = protocolversion >= MC_1_17_Version ? dataTypes.ReadNextVarInt(packetData) : dataTypes.ReadNextInt(packetData);
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

                                // See https://wiki.vg/Entity_metadata#Living_Entity
                                int healthField = 7; // From 1.10 to 1.13.2
                                if (protocolversion >= MC_1_14_Version)
                                    healthField = 8; // 1.14 and above
                                if (protocolversion >= MC_1_17_Version)
                                    healthField = 9; // 1.17 and above
                                if (protocolversion > MC_1_18_2_Version)
                                    throw new NotImplementedException(Translations.Get("exception.palette.healthfield"));

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
                        case PacketTypesIn.SystemChat:
                            string systemMessage = dataTypes.ReadNextString(packetData);
                            int msgType = dataTypes.ReadNextVarInt(packetData);
                            if ((msgType == 1 && !Settings.DisplaySystemMessages))
                                break;
                            handler.OnTextReceived(new(systemMessage, true, msgType, Guid.Empty, true));
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
                            if (protocolversion >= MC_1_8_Version)
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
                            Location explosionLocation;
                            if (protocolversion >= MC_1_19_Version)
                                explosionLocation = new(dataTypes.ReadNextDouble(packetData), dataTypes.ReadNextDouble(packetData), dataTypes.ReadNextDouble(packetData));
                            else
                                explosionLocation = new(dataTypes.ReadNextFloat(packetData), dataTypes.ReadNextFloat(packetData), dataTypes.ReadNextFloat(packetData));
                            float explosionStrength = dataTypes.ReadNextFloat(packetData);
                            int explosionBlockCount = protocolversion >= MC_1_17_Version
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
                            int action3 = protocolversion >= MC_1_18_2_Version
                                    ? dataTypes.ReadNextVarInt(packetData)
                                    : dataTypes.ReadNextByte(packetData);
                            string objectivename2 = string.Empty;
                            int value = -1;
                            if (action3 != 1 || protocolversion >= MC_1_8_Version)
                                objectivename2 = dataTypes.ReadNextString(packetData);
                            if (action3 != 1)
                                value = dataTypes.ReadNextVarInt(packetData);
                            handler.OnUpdateScore(entityname, action3, objectivename2, value);
                            break;
                        case PacketTypesIn.BlockChangedAck:
                            handler.OnBlockChangeAck(dataTypes.ReadNextVarInt(packetData));
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

            netRead = new Tuple<Thread, CancellationTokenSource>(new Thread(new ParameterizedThreadStart(Updater)), new CancellationTokenSource());
            netRead.Item1.Name = "ProtocolPacketHandler";
            netRead.Item1.Start(netRead.Item2.Token);
        }

        /// <summary>
        /// Get net read thread (main thread) ID
        /// </summary>
        /// <returns>Net read thread ID</returns>
        public int GetNetReadThreadId()
        {
            return netRead != null ? netRead.Item1.ManagedThreadId : -1;
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
                    netRead.Item2.Cancel();
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
            // log.Info("[C -> S] Sending packet " + packetID + " > " + dataTypes.ByteArrayToString(packetData.ToArray()));

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

            //log.Debug("[C -> S] Sending packet " + packetID + " > " + dataTypes.ByteArrayToString(dataTypes.ConcatBytes(dataTypes.GetVarInt(the_packet.Length), the_packet)));
            socketWrapper.SendDataRAW(dataTypes.ConcatBytes(dataTypes.GetVarInt(the_packet.Length), the_packet));
        }

        /// <summary>
        /// Do the Minecraft login.
        /// </summary>
        /// <returns>True if login successful</returns>
        public bool Login(PlayerKeyPair? playerKeyPair)
        {
            byte[] protocol_version = dataTypes.GetVarInt(protocolversion);
            string server_address = pForge.GetServerAddress(handler.GetServerHost());
            byte[] server_port = dataTypes.GetUShort((ushort)handler.GetServerPort());
            byte[] next_state = dataTypes.GetVarInt(2);
            byte[] handshake_packet = dataTypes.ConcatBytes(protocol_version, dataTypes.GetString(server_address), server_port, next_state);
            SendPacket(0x00, handshake_packet);

            List<byte> fullLoginPacket = new List<byte>();
            fullLoginPacket.AddRange(dataTypes.GetString(handler.GetUsername()));                             // Username
            if (protocolversion >= MC_1_19_Version)
            {
                if (playerKeyPair == null)
                    fullLoginPacket.AddRange(dataTypes.GetBool(false));                                       // Has Sig Data
                else
                {
                    fullLoginPacket.AddRange(dataTypes.GetBool(true));                                        // Has Sig Data
                    fullLoginPacket.AddRange(dataTypes.GetLong(playerKeyPair.GetExpirationMilliseconds()));   // Expiration time
                    fullLoginPacket.AddRange(dataTypes.GetArray(playerKeyPair.PublicKey.Key));                // Public key received from Microsoft API
                    if (protocolversion >= MC_1_19_2_Version)
                        fullLoginPacket.AddRange(dataTypes.GetArray(playerKeyPair.PublicKey.SignatureV2!));   // Public key signature received from Microsoft API
                    else
                        fullLoginPacket.AddRange(dataTypes.GetArray(playerKeyPair.PublicKey.Signature!));      // Public key signature received from Microsoft API
                }
            }
            if (protocolversion >= MC_1_19_2_Version)
            {
                string uuid = handler.GetUserUuidStr();
                if (uuid == "0")
                    fullLoginPacket.AddRange(dataTypes.GetBool(false));                                       // Has UUID
                else
                {
                    fullLoginPacket.AddRange(dataTypes.GetBool(true));                                        // Has UUID
                    fullLoginPacket.AddRange(dataTypes.GetUUID(Guid.Parse(uuid)));                            // UUID
                }
            }
            SendPacket(0x00, fullLoginPacket);

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
                    byte[] serverPublicKey = dataTypes.ReadNextByteArray(packetData);
                    byte[] token = dataTypes.ReadNextByteArray(packetData);
                    return StartEncryption(handler.GetUserUuidStr(), handler.GetSessionID(), token, serverID, serverPublicKey, playerKeyPair);
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
        private bool StartEncryption(string uuid, string sessionID, byte[] token, string serverIDhash, byte[] serverPublicKey, PlayerKeyPair? playerKeyPair)
        {
            System.Security.Cryptography.RSACryptoServiceProvider RSAService = CryptoHandler.DecodeRSAPublicKey(serverPublicKey);
            byte[] secretKey = CryptoHandler.GenerateAESPrivateKey();

            log.Debug(Translations.Get("debug.crypto"));

            if (serverIDhash != "-")
            {
                log.Info(Translations.Get("mcc.session"));
                if (!ProtocolHandler.SessionCheck(uuid, sessionID, CryptoHandler.getServerHash(serverIDhash, serverPublicKey, secretKey)))
                {
                    handler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected, Translations.Get("mcc.session_fail"));
                    return false;
                }
            }


            // Encryption Response packet
            List<byte> encryptionResponse = new();
            encryptionResponse.AddRange(dataTypes.GetArray(RSAService.Encrypt(secretKey, false)));     // Shared Secret
            if (protocolversion >= Protocol18Handler.MC_1_19_Version)
            {
                if (playerKeyPair == null)
                {
                    encryptionResponse.AddRange(dataTypes.GetBool(true));                              // Has Verify Token
                    encryptionResponse.AddRange(dataTypes.GetArray(RSAService.Encrypt(token, false))); // Verify Token
                }
                else
                {
                    byte[] salt = GenerateSalt();
                    byte[] messageSignature = playerKeyPair.PrivateKey.SignData(dataTypes.ConcatBytes(token, salt));

                    encryptionResponse.AddRange(dataTypes.GetBool(false));                            // Has Verify Token
                    encryptionResponse.AddRange(salt);                                                // Salt
                    encryptionResponse.AddRange(dataTypes.GetArray(messageSignature));                // Message Signature
                }
            }
            else
            {
                encryptionResponse.AddRange(dataTypes.GetArray(RSAService.Encrypt(token, false)));    // Verify Token
            }
            SendPacket(0x01, encryptionResponse);

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
                    Guid uuidReceived = dataTypes.ReadNextUUID(packetData);
                    string userName = dataTypes.ReadNextString(packetData);
                    Tuple<string, string, string>[]? playerProperty = null;
                    if (protocolversion >= Protocol18Handler.MC_1_19_Version)
                    {
                        int count = dataTypes.ReadNextVarInt(packetData); // Number Of Properties
                        playerProperty = new Tuple<string, string, string>[count];
                        for (int i = 0; i < count; ++i)
                        {
                            string name = dataTypes.ReadNextString(packetData);
                            string value = dataTypes.ReadNextString(packetData);
                            bool isSigned = dataTypes.ReadNextBool(packetData);
                            string signature = isSigned ? dataTypes.ReadNextString(packetData) : String.Empty;
                            playerProperty[i] = new Tuple<string, string, string>(name, value, signature);
                        }
                    }
                    handler.OnLoginSuccess(uuidReceived, userName, playerProperty);

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

            if (protocolversion >= MC_1_8_Version)
            {
                if (protocolversion >= MC_1_13_Version)
                {
                    tabcomplete_packet = dataTypes.ConcatBytes(tabcomplete_packet, transaction_id);
                    tabcomplete_packet = dataTypes.ConcatBytes(tabcomplete_packet, dataTypes.GetString(BehindCursor));
                }
                else
                {
                    tabcomplete_packet = dataTypes.ConcatBytes(tabcomplete_packet, dataTypes.GetString(BehindCursor));

                    if (protocolversion >= MC_1_9_Version)
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
        public static bool doPing(string host, int port, ref int protocolversion, ref ForgeInfo? forgeInfo)
        {
            string version = "";
            TcpClient tcp = ProxyHandler.newTcpClient(host, port);
            tcp.ReceiveTimeout = 30000; // 30 seconds
            tcp.ReceiveBufferSize = 1024 * 1024;
            SocketWrapper socketWrapper = new SocketWrapper(tcp);
            DataTypes dataTypes = new DataTypes(MC_1_8_Version);

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
            return protocolversion > MC_1_10_Version
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
        /// Send MessageAcknowledgment packet
        /// </summary>
        /// <param name="acknowledgment">Message acknowledgment</param>
        /// <returns>True if properly sent</returns>
        public bool SendMessageAcknowledgment(LastSeenMessageList.Acknowledgment acknowledgment)
        {
            try
            {
                byte[] fields = dataTypes.GetAcknowledgment(acknowledgment);

                SendPacket(PacketTypesOut.MessageAcknowledgment, fields);

                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public LastSeenMessageList.Acknowledgment consumeAcknowledgment()
        {
            this.pendingAcknowledgments = 0;
            return new LastSeenMessageList.Acknowledgment(this.lastSeenMessagesCollector.GetLastSeenMessages(), this.lastReceivedMessage);
        }

        public void acknowledge(ChatMessage message)
        {
            LastSeenMessageList.Entry? entry = message.toLastSeenMessageEntry();

            if (entry != null)
            {
                lastSeenMessagesCollector.Add(entry);
                lastReceivedMessage = null;

                if (pendingAcknowledgments++ > 64)
                    SendMessageAcknowledgment(this.consumeAcknowledgment());
            }
        }

        /// <summary>
        /// The signable argument names and their values from command
        /// Signature will used in Vanilla's say, me, msg, teammsg, ban, banip, and kick commands.
        /// https://gist.github.com/kennytv/ed783dd244ca0321bbd882c347892874#signed-command-arguments
        /// You can find all the commands that need to be signed by searching for "MessageArgumentType.getSignedMessage" in the source code.
        /// Don't forget to handle the redirected commands, e.g. /tm, /w
        /// 
        /// </summary>
        /// <param name="command">Command</param>
        /// <returns> List< Argument Name, Argument Value > </returns>
        private static List<Tuple<string, string>> CollectCommandArguments(string command)
        {
            List<Tuple<string, string>> needSigned = new();

            if (!Settings.SignMessageInCommand)
                return needSigned;

            string[] argStage1 = command.Split(' ', 2, StringSplitOptions.None);
            if (argStage1.Length == 2)
            {
                /* /me      <action>
                   /say     <message>
                   /teammsg <message>
                   /tm      <message> */
                if (argStage1[0] == "me")
                    needSigned.Add(new("action", argStage1[1]));
                else if (argStage1[0] == "say" || argStage1[0] == "teammsg" || argStage1[0] == "tm")
                    needSigned.Add(new("message", argStage1[1]));
                else if (argStage1[0] == "msg" || argStage1[0] == "tell" || argStage1[0] == "w" || 
                    argStage1[0] == "ban" || argStage1[0] == "ban-ip" || argStage1[0] == "kick")
                {
                    /* /msg    <targets> <message>
                       /tell   <targets> <message>
                       /w      <targets> <message>
                       /ban    <target>  [<reason>]
                       /ban-ip <target>  [<reason>]
                       /kick   <target>  [<reason>] */
                    string[] argStage2 = argStage1[1].Split(' ', 2, StringSplitOptions.None);
                    if (argStage2.Length == 2)
                    {
                        if (argStage1[0] == "msg" || argStage1[0] == "tell" || argStage1[0] == "w")
                            needSigned.Add(new("message", argStage2[1]));
                        else if (argStage1[0] == "ban" || argStage1[0] == "ban-ip" || argStage1[0] == "kick")
                            needSigned.Add(new("reason", argStage2[1]));
                    }
                }
            }

            return needSigned;
        }

        /// <summary>
        /// Send a chat command to the server
        /// </summary>
        /// <param name="command">Command</param>
        /// <param name="playerKeyPair">PlayerKeyPair</param>
        /// <returns>True if properly sent</returns>
        public bool SendChatCommand(string command, PlayerKeyPair? playerKeyPair)
        {
            if (String.IsNullOrEmpty(command))
                return true;

            command = Regex.Replace(command, @"\s+", " ");
            command = Regex.Replace(command, @"\s$", string.Empty);

            log.Debug("chat command = " + command);

            try
            {
                LastSeenMessageList.Acknowledgment? acknowledgment = (protocolversion >= MC_1_19_2_Version) ? this.consumeAcknowledgment() : null;

                List<byte> fields = new();

                // Command: String
                fields.AddRange(dataTypes.GetString(command));

                // Timestamp: Instant(Long)
                DateTimeOffset timeNow = DateTimeOffset.UtcNow;
                fields.AddRange(dataTypes.GetLong(timeNow.ToUnixTimeMilliseconds()));

                List<Tuple<string, string>> needSigned = CollectCommandArguments(command); // List< Argument Name, Argument Value >
                if (needSigned.Count == 0 || playerKeyPair == null || !Settings.SignMessageInCommand)
                {
                    fields.AddRange(dataTypes.GetLong(0));                    // Salt: Long
                    fields.AddRange(dataTypes.GetVarInt(0));                  // Signature Length: VarInt
                }
                else
                {
                    Guid uuid = handler.GetUserUuid();
                    byte[] salt = GenerateSalt();
                    fields.AddRange(salt);                                    // Salt: Long
                    fields.AddRange(dataTypes.GetVarInt(needSigned.Count));   // Signature Length: VarInt
                    foreach (var argument in needSigned)
                    {
                        fields.AddRange(dataTypes.GetString(argument.Item1)); // Argument name: String
                        byte[] sign = (protocolversion >= MC_1_19_2_Version) ?
                            playerKeyPair.PrivateKey.SignMessage(argument.Item2, uuid, timeNow, ref salt, acknowledgment!.lastSeen) :
                            playerKeyPair.PrivateKey.SignMessage(argument.Item2, uuid, timeNow, ref salt);
                        fields.AddRange(dataTypes.GetVarInt(sign.Length));    // Signature length: VarInt
                        fields.AddRange(sign);                                // Signature: Byte Array
                    }
                }

                // Signed Preview: Boolean
                fields.AddRange(dataTypes.GetBool(false));

                if (protocolversion >= MC_1_19_2_Version)
                {
                    // Message Acknowledgment
                    fields.AddRange(dataTypes.GetAcknowledgment(acknowledgment!));
                }

                SendPacket(PacketTypesOut.ChatCommand, fields);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        /// <summary>
        /// Send a chat message to the server
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="playerKeyPair">PlayerKeyPair</param>
        /// <returns>True if properly sent</returns>
        public bool SendChatMessage(string message, PlayerKeyPair? playerKeyPair)
        {
            if (String.IsNullOrEmpty(message))
                return true;

            // Process Chat Command - 1.19 and above
            if (protocolversion >= MC_1_19_Version && message.StartsWith('/'))
                return SendChatCommand(message[1..], playerKeyPair);

            try
            {
                LastSeenMessageList.Acknowledgment? acknowledgment = (protocolversion >= MC_1_19_2_Version) ? this.consumeAcknowledgment() : null;

                List<byte> fields = new();

                // 	Message: String (up to 256 chars)
                fields.AddRange(dataTypes.GetString(message));

                if (protocolversion >= MC_1_19_Version)
                {
                    // Timestamp: Instant(Long)
                    DateTimeOffset timeNow = DateTimeOffset.UtcNow;
                    fields.AddRange(dataTypes.GetLong(timeNow.ToUnixTimeMilliseconds()));

                    if (playerKeyPair == null || !Settings.SignChat)
                    {
                        fields.AddRange(dataTypes.GetLong(0));   // Salt: Long
                        fields.AddRange(dataTypes.GetVarInt(0)); // Signature Length: VarInt
                    }
                    else
                    {
                        // Salt: Long
                        byte[] salt = GenerateSalt();
                        fields.AddRange(salt);

                        // Signature Length & Signature: (VarInt) and Byte Array
                        Guid uuid = handler.GetUserUuid();
                        byte[] sign = (protocolversion >= MC_1_19_2_Version) ?
                            playerKeyPair.PrivateKey.SignMessage(message, uuid, timeNow, ref salt, acknowledgment!.lastSeen) :
                            playerKeyPair.PrivateKey.SignMessage(message, uuid, timeNow, ref salt);
                        fields.AddRange(dataTypes.GetVarInt(sign.Length));
                        fields.AddRange(sign);
                    }

                    // Signed Preview: Boolean
                    fields.AddRange(dataTypes.GetBool(false));

                    if (protocolversion >= MC_1_19_2_Version)
                    {
                        // Message Acknowledgment
                        fields.AddRange(dataTypes.GetAcknowledgment(acknowledgment!));
                    }
                }
                SendPacket(PacketTypesOut.ChatMessage, fields);
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
            if (protocolversion >= MC_1_13_Version)
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

                if (protocolversion >= MC_1_9_Version)
                    fields.AddRange(dataTypes.GetVarInt(chatMode));
                else
                    fields.AddRange(new byte[] { chatMode });

                fields.Add(chatColors ? (byte)1 : (byte)0);
                if (protocolversion < MC_1_8_Version)
                {
                    fields.Add(difficulty);
                    fields.Add((byte)(skinParts & 0x1)); //show cape
                }
                else fields.Add(skinParts);
                if (protocolversion >= MC_1_9_Version)
                    fields.AddRange(dataTypes.GetVarInt(mainHand));
                if (protocolversion >= MC_1_17_Version)
                {
                    if (protocolversion >= MC_1_18_1_Version)
                        fields.Add(0); // 1.18 and above - Enable text filtering. (Always false)
                    else
                        fields.Add(1); // 1.17 and 1.17.1 - Disable text filtering. (Always true)
                }
                if (protocolversion >= MC_1_18_1_Version)
                    fields.Add(1); // 1.18 and above - Allow server listings
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
                        protocolversion < MC_1_8_Version
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
                if (protocolversion < MC_1_8_Version)
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
                if (protocolversion >= MC_1_16_Version)
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
                if (protocolversion >= MC_1_16_Version)
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
                if (protocolversion >= MC_1_16_Version)
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

        public bool SendUseItem(int hand, int sequenceId)
        {
            if (protocolversion < MC_1_9_Version)
                return false; // Packet does not exist prior to MC 1.9
                              // According to https://wiki.vg/index.php?title=Protocol&oldid=5486#Player_Block_Placement
                              // MC 1.7 does this using Player Block Placement with special values
                              // TODO once Player Block Placement is implemented for older versions
            try
            {
                List<byte> packet = new List<byte>();
                packet.AddRange(dataTypes.GetVarInt(hand));
                if (protocolversion >= MC_1_19_Version)
                    packet.AddRange(dataTypes.GetVarInt(sequenceId));
                SendPacket(PacketTypesOut.UseItem, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendPlayerDigging(int status, Location location, Direction face, int sequenceId)
        {
            try
            {
                List<byte> packet = new List<byte>();
                packet.AddRange(dataTypes.GetVarInt(status));
                packet.AddRange(dataTypes.GetLocation(location));
                packet.AddRange(dataTypes.GetVarInt(dataTypes.GetBlockFace(face)));
                if (protocolversion >= MC_1_19_Version)
                    packet.AddRange(dataTypes.GetVarInt(sequenceId));
                SendPacket(PacketTypesOut.PlayerDigging, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendPlayerBlockPlacement(int hand, Location location, Direction face, int sequenceId)
        {
            if (protocolversion < MC_1_14_Version)
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
                if (protocolversion >= MC_1_19_Version)
                    packet.AddRange(dataTypes.GetVarInt(sequenceId));
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

        public bool SendWindowAction(int windowId, int slotId, WindowActionType action, Item item, List<Tuple<short, Item>> changedSlots, int stateId)
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
                    case WindowActionType.LeftClick: button = 0; break;
                    case WindowActionType.RightClick: button = 1; break;
                    case WindowActionType.MiddleClick: button = 2; mode = 3; break;
                    case WindowActionType.ShiftClick: button = 0; mode = 1; item = new Item(ItemType.Null, 0, null); break;
                    case WindowActionType.DropItem: button = 0; mode = 4; item = new Item(ItemType.Null, 0, null); break;
                    case WindowActionType.DropItemStack: button = 1; mode = 4; item = new Item(ItemType.Null, 0, null); break;
                    case WindowActionType.StartDragLeft: button = 0; mode = 5; item = new Item(ItemType.Null, 0, null); slotId = -999; break;
                    case WindowActionType.StartDragRight: button = 4; mode = 5; item = new Item(ItemType.Null, 0, null); slotId = -999; break;
                    case WindowActionType.StartDragMiddle: button = 8; mode = 5; item = new Item(ItemType.Null, 0, null); slotId = -999; break;
                    case WindowActionType.EndDragLeft: button = 2; mode = 5; item = new Item(ItemType.Null, 0, null); slotId = -999; break;
                    case WindowActionType.EndDragRight: button = 6; mode = 5; item = new Item(ItemType.Null, 0, null); slotId = -999; break;
                    case WindowActionType.EndDragMiddle: button = 10; mode = 5; item = new Item(ItemType.Null, 0, null); slotId = -999; break;
                    case WindowActionType.AddDragLeft: button = 1; mode = 5; item = new Item(ItemType.Null, 0, null); break;
                    case WindowActionType.AddDragRight: button = 5; mode = 5; item = new Item(ItemType.Null, 0, null); break;
                    case WindowActionType.AddDragMiddle: button = 9; mode = 5; item = new Item(ItemType.Null, 0, null); break;
                }

                List<byte> packet = new List<byte>();
                packet.Add((byte)windowId); // Window ID

                // 1.18+
                if (protocolversion >= MC_1_18_1_Version)
                {
                    packet.AddRange(dataTypes.GetVarInt(stateId)); // State ID
                    packet.AddRange(dataTypes.GetShort((short)slotId)); // Slot ID
                }
                // 1.17.1
                else if (protocolversion == MC_1_17_1_Version)
                {
                    packet.AddRange(dataTypes.GetShort((short)slotId)); // Slot ID
                    packet.AddRange(dataTypes.GetVarInt(stateId)); // State ID
                }
                // Older
                else
                {
                    packet.AddRange(dataTypes.GetShort((short)slotId)); // Slot ID
                }

                packet.Add(button); // Button

                if (protocolversion < MC_1_17_Version)
                    packet.AddRange(dataTypes.GetShort(actionNumber));

                if (protocolversion >= MC_1_9_Version)
                    packet.AddRange(dataTypes.GetVarInt(mode)); // Mode
                else packet.Add(mode);

                // 1.17+  Array of changed slots
                if (protocolversion >= MC_1_17_Version)
                {
                    packet.AddRange(dataTypes.GetVarInt(changedSlots.Count)); // Length of the array
                    foreach (var slot in changedSlots)
                    {
                        packet.AddRange(dataTypes.GetShort(slot.Item1)); // slot ID
                        packet.AddRange(dataTypes.GetItemSlot(slot.Item2, itemPalette)); // slot Data
                    }
                }

                packet.AddRange(dataTypes.GetItemSlot(item, itemPalette)); // Carried item (Clicked item)

                SendPacket(PacketTypesOut.ClickWindow, packet);
                return true;
            }
            catch (SocketException) { return false; }
            catch (System.IO.IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }

        public bool SendCreativeInventoryAction(int slot, ItemType itemType, int count, Dictionary<string, object>? nbt)
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

                    if (protocolversion < MC_1_8_Version)
                    {
                        packet.AddRange(dataTypes.GetInt(playerid));
                        packet.Add((byte)1); // Swing arm
                    }
                    else if (protocolversion < MC_1_9_Version)
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
            if (protocolversion <= MC_1_13_Version)
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
            else { return false; }
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
            if (protocolversion >= MC_1_13_Version)
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

        public bool SendSpectate(Guid UUID)
        {
            // MC 1.8 or greater
            if (protocolversion >= MC_1_8_Version)
            {
                try
                {
                    List<byte> packet = new List<byte>();
                    packet.AddRange(dataTypes.GetUUID(UUID));
                    SendPacket(PacketTypesOut.Spectate, packet);
                    return true;
                }
                catch (SocketException) { return false; }
                catch (System.IO.IOException) { return false; }
                catch (ObjectDisposedException) { return false; }
            }
            else { return false; }
        }

        private byte[] GenerateSalt()
        {
            byte[] salt = new byte[8];
            randomGen.GetNonZeroBytes(salt);
            return salt;
        }
    }
}
