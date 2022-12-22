using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MinecraftClient.Protocol.Handlers.Forge;
using MinecraftClient.Protocol.Message;
using MinecraftClient.Protocol.PacketPipeline;
using MinecraftClient.Scripting;
using static MinecraftClient.Protocol.Handlers.Protocol18Handler;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Handler for the Minecraft Forge protocol
    /// </summary>
    class Protocol18Forge
    {
        private readonly int protocolversion;
        private readonly DataTypes dataTypes;
        private readonly Protocol18Handler protocol18;
        private readonly IMinecraftComHandler mcHandler;

        private readonly ForgeInfo? forgeInfo;
        private FMLHandshakeClientState fmlHandshakeState = FMLHandshakeClientState.START;
        private bool ForgeEnabled() { return forgeInfo != null; }

        /// <summary>
        /// Initialize a new Forge protocol handler
        /// </summary>
        /// <param name="forgeInfo">Forge Server Information</param>
        /// <param name="protocolVersion">Minecraft protocol version</param>
        /// <param name="dataTypes">Minecraft data types handler</param>
        public Protocol18Forge(ForgeInfo? forgeInfo, int protocolVersion, DataTypes dataTypes, Protocol18Handler protocol18, IMinecraftComHandler mcHandler)
        {
            this.forgeInfo = forgeInfo;
            protocolversion = protocolVersion;
            this.dataTypes = dataTypes;
            this.protocol18 = protocol18;
            this.mcHandler = mcHandler;
        }

        /// <summary>
        /// Get Forge-Tagged server address
        /// </summary>
        /// <param name="serverAddress">Server Address</param>
        /// <returns>Forge-Tagged server address</returns>
        public string GetServerAddress(string serverAddress)
        {
            if (ForgeEnabled())
                return serverAddress + "\0" + forgeInfo!.Version + "\0";
            return serverAddress;
        }

        /// <summary>
        /// Completes the Minecraft Forge handshake (Forge Protocol version 1: FML)
        /// </summary>
        /// <returns>Whether the handshake was successful.</returns>
        public async Task<bool> CompleteForgeHandshake(SocketWrapper socketWrapper)
        {
            if (ForgeEnabled() && forgeInfo!.Version == FMLVersion.FML)
            {
                while (fmlHandshakeState != FMLHandshakeClientState.DONE)
                {
                    (int packetID, PacketStream packetStream) = await socketWrapper.GetNextPacket(handleCompress: true);

                    if (packetID == 0x40) // Disconnect
                    {
                        mcHandler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected, ChatParser.ParseText(await dataTypes.ReadNextStringAsync(packetStream)));
                        return false;
                    }
                    else
                    {
                        // Send back regular packet to the vanilla protocol handler
                        await protocol18.HandlePacket(packetID, packetStream);
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Read Forge VarShort field
        /// </summary>
        /// <param name="packetData">Packet data to read from</param>
        /// <returns>Length from packet data</returns>
        public int ReadNextVarShort(PacketStream packetData)
        {
            if (ForgeEnabled())
            {
                // Forge special VarShort field.
                return (int)dataTypes.ReadNextVarShort(packetData);
            }
            else
            {
                // Vanilla regular Short field
                return (int)dataTypes.ReadNextShort(packetData);
            }
        }

        /// <summary>
        /// Handle Forge plugin messages (Forge Protocol version 1: FML)
        /// </summary>
        /// <param name="channel">Plugin message channel</param>
        /// <param name="packetData">Plugin message data</param>
        /// <param name="currentDimension">Current world dimension</param>
        /// <returns>TRUE if the plugin message was recognized and handled</returns>
        public async Task<Tuple<bool, int>> HandlePluginMessage(string channel, byte[] packetDataArr, int currentDimension)
        {
            if (ForgeEnabled() && forgeInfo!.Version == FMLVersion.FML && fmlHandshakeState != FMLHandshakeClientState.DONE)
            {
                Queue<byte> packetData = new(packetDataArr);
                if (channel == "FML|HS")
                {
                    FMLHandshakeDiscriminator discriminator = (FMLHandshakeDiscriminator)dataTypes.ReadNextByte(packetData);

                    if (discriminator == FMLHandshakeDiscriminator.HandshakeReset)
                    {
                        fmlHandshakeState = FMLHandshakeClientState.START;
                        return new(true, currentDimension);
                    }

                    switch (fmlHandshakeState)
                    {
                        case FMLHandshakeClientState.START:
                            if (discriminator != FMLHandshakeDiscriminator.ServerHello)
                                return new(false, currentDimension);

                            // Send the plugin channel registration.
                            // REGISTER is somewhat special in that it doesn't actually include length information,
                            // and is also \0-separated.
                            // Also, yes, "FML" is there twice.  Don't ask me why, but that's the way forge does it.
                            string[] channels = { "FML|HS", "FML", "FML|MP", "FML", "FORGE" };
                            await protocol18.SendPluginChannelPacket("REGISTER", Encoding.UTF8.GetBytes(string.Join("\0", channels)));

                            byte fmlProtocolVersion = dataTypes.ReadNextByte(packetData);

                            if (Settings.Config.Logging.DebugMessages)
                                ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.forge_version, fmlProtocolVersion));

                            if (fmlProtocolVersion >= 1)
                                currentDimension = dataTypes.ReadNextInt(packetData);

                            // Tell the server we're running the same version.
                            await SendForgeHandshakePacket(FMLHandshakeDiscriminator.ClientHello, new byte[] { fmlProtocolVersion });

                            // Then tell the server that we're running the same mods.
                            if (Settings.Config.Logging.DebugMessages)
                                ConsoleIO.WriteLineFormatted("§8" + Translations.forge_send_mod, acceptnewlines: true);
                            byte[][] mods = new byte[forgeInfo.Mods.Count][];
                            for (int i = 0; i < forgeInfo.Mods.Count; i++)
                            {
                                ForgeInfo.ForgeMod mod = forgeInfo.Mods[i];
                                mods[i] = dataTypes.ConcatBytes(dataTypes.GetString(mod.ModID!), dataTypes.GetString(mod.Version ?? mod.ModMarker ?? string.Empty));
                            }
                            await SendForgeHandshakePacket(FMLHandshakeDiscriminator.ModList,
                                dataTypes.ConcatBytes(dataTypes.GetVarInt(forgeInfo.Mods.Count), dataTypes.ConcatBytes(mods)));

                            fmlHandshakeState = FMLHandshakeClientState.WAITINGSERVERDATA;

                            return new(true, currentDimension);
                        case FMLHandshakeClientState.WAITINGSERVERDATA:
                            if (discriminator != FMLHandshakeDiscriminator.ModList)
                                return new(false, currentDimension);

                            Thread.Sleep(2000);

                            if (Settings.Config.Logging.DebugMessages)
                                ConsoleIO.WriteLineFormatted("§8" + Translations.forge_accept, acceptnewlines: true);
                            // Tell the server that yes, we are OK with the mods it has
                            // even though we don't actually care what mods it has.

                            await SendForgeHandshakePacket(FMLHandshakeDiscriminator.HandshakeAck,
                                new byte[] { (byte)FMLHandshakeClientState.WAITINGSERVERDATA });

                            fmlHandshakeState = FMLHandshakeClientState.WAITINGSERVERCOMPLETE;
                            return new(false, currentDimension);
                        case FMLHandshakeClientState.WAITINGSERVERCOMPLETE:
                            // The server now will tell us a bunch of registry information.
                            // We need to read it all, though, until it says that there is no more.
                            if (discriminator != FMLHandshakeDiscriminator.RegistryData)
                                return new(false, currentDimension);

                            if (protocolversion < Protocol18Handler.MC_1_8_Version)
                            {
                                // 1.7.10 and below have one registry
                                // with blocks and items.
                                int registrySize = dataTypes.ReadNextVarInt(packetData);

                                if (Settings.Config.Logging.DebugMessages)
                                    ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.forge_registry, registrySize));

                                fmlHandshakeState = FMLHandshakeClientState.PENDINGCOMPLETE;
                            }
                            else
                            {
                                // 1.8+ has more than one registry.

                                bool hasNextRegistry = dataTypes.ReadNextBool(packetData);
                                string registryName = dataTypes.ReadNextString(packetData);
                                int registrySize = dataTypes.ReadNextVarInt(packetData);
                                if (Settings.Config.Logging.DebugMessages)
                                    ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.forge_registry_2, registryName, registrySize));
                                if (!hasNextRegistry)
                                    fmlHandshakeState = FMLHandshakeClientState.PENDINGCOMPLETE;
                            }

                            return new(false, currentDimension);
                        case FMLHandshakeClientState.PENDINGCOMPLETE:
                            // The server will ask us to accept the registries.
                            // Just say yes.
                            if (discriminator != FMLHandshakeDiscriminator.HandshakeAck)
                                return new(false, currentDimension);
                            if (Settings.Config.Logging.DebugMessages)
                                ConsoleIO.WriteLineFormatted("§8" + Translations.forge_accept_registry, acceptnewlines: true);
                            await SendForgeHandshakePacket(FMLHandshakeDiscriminator.HandshakeAck,
                                new byte[] { (byte)FMLHandshakeClientState.PENDINGCOMPLETE });
                            fmlHandshakeState = FMLHandshakeClientState.COMPLETE;

                            return new(true, currentDimension);
                        case FMLHandshakeClientState.COMPLETE:
                            // One final "OK".  On the actual forge source, a packet is sent from
                            // the client to the client saying that the connection was complete, but
                            // we don't need to do that.

                            await SendForgeHandshakePacket(FMLHandshakeDiscriminator.HandshakeAck,
                                new byte[] { (byte)FMLHandshakeClientState.COMPLETE });
                            if (Settings.Config.Logging.DebugMessages)
                                ConsoleIO.WriteLine(Translations.forge_complete);
                            fmlHandshakeState = FMLHandshakeClientState.DONE;
                            return new(true, currentDimension);
                    }
                }
            }
            return new(false, currentDimension);
        }

        /// <summary>
        /// Handle Forge plugin messages during login phase (Forge Protocol version 2: FML2)
        /// </summary>
        /// <param name="channel">Plugin message channel</param>
        /// <param name="packetData">Plugin message data</param>
        /// <param name="responseData">Response data to return to server</param>
        /// <returns>TRUE/FALSE depending on whether the packet was understood or not</returns>
        public async Task<Tuple<bool, List<byte>>> HandleLoginPluginRequest(string channel, PacketStream packetData)
        {
            List<byte> responseData = new();
            if (ForgeEnabled() && forgeInfo!.Version == FMLVersion.FML2 && channel == "fml:loginwrapper")
            {
                // Forge Handshake handler source code used to implement the FML2 packets:
                // https://github.com/MinecraftForge/MinecraftForge/blob/master/src/main/java/net/minecraftforge/fml/network/FMLNetworkConstants.java
                // https://github.com/MinecraftForge/MinecraftForge/blob/master/src/main/java/net/minecraftforge/fml/network/FMLHandshakeHandler.java
                // https://github.com/MinecraftForge/MinecraftForge/blob/master/src/main/java/net/minecraftforge/fml/network/NetworkInitialization.java
                // https://github.com/MinecraftForge/MinecraftForge/blob/master/src/main/java/net/minecraftforge/fml/network/FMLLoginWrapper.java
                // https://github.com/MinecraftForge/MinecraftForge/blob/master/src/main/java/net/minecraftforge/fml/network/FMLHandshakeMessages.java
                //
                // During Login, Forge will send a set of LoginPluginRequest packets and we need to respond accordingly.
                // Each login plugin message contains in its payload field an inner packet created by FMLLoginWrapper.java:
                //
                // [ ResourceLocation ][ String ] // FML Channel name
                // [   Inner Packet   ][ Packet ] // Minecraft-Like packet
                //
                // The channel name allows identifying which handler in Forge will process the packet
                // For instance, the handshake channel is fml:handshake as per FMLNetworkConstants.java
                //
                // The inner packet has the same layout as a Minecraft packet.
                // Forge uses Minecraft's PacketBuffer class to decode the packet.
                // We assume no network compression is active at this point.
                //
                // [  Length  ][ VarInt ]
                // [ PacketID ][ VarInt ]
                // [   Data   ][  ....  ]
                //
                // Once decoded, the packet ID for fml:handshake is mapped in NetworkInitialization.java:
                //
                // 99 = Client to Server - Client ACK
                // 1 = Server to Client - Mod List
                // 2 = Client to Server - Mod List
                // 3 = Server to Client - Registry
                // 4 = Server to Client - Config
                //
                // The content of each message is mapped into a class inside FMLHandshakeMessages.java
                // FMLHandshakeHandler will then process the packet, e.g. handleServerModListOnClient() for Server Mod List.

                string fmlChannel = await dataTypes.ReadNextStringAsync(packetData);
                dataTypes.SkipNextVarInt(packetData); // Packet length
                int packetID = dataTypes.ReadNextVarInt(packetData);

                if (fmlChannel == "fml:handshake")
                {
                    bool fmlResponseReady = false;
                    List<byte> fmlResponsePacket = new();

                    switch (packetID)
                    {
                        case 1:
                            // Server Mod List: FMLHandshakeMessages.java > S2CModList > decode()
                            //
                            // [      Mod Count ][ VarInt ]
                            //      [  Mod Name ][ String ]  // Amount of entries according to Mod Count
                            // [  Channel Count ][ VarInt ]
                            //      [ Chan Name ][ String ]  // Amount of entries according to Channel Count
                            //      [   Version ][ String ]  // Each entry is a pair of Channel Name + Version (1)
                            // [ Registry Count ][ VarInt ]
                            //      [  Registry ][ String ]  // Amount of entries according to Registry Count
                            //
                            // [1]: Version is usually set to "FML2" for FML stuff and "1" for mods

                            if (Settings.Config.Logging.DebugMessages)
                                ConsoleIO.WriteLineFormatted("§8" + Translations.forge_fml2_mod, acceptnewlines: true);

                            List<string> mods = new();
                            int modCount = dataTypes.ReadNextVarInt(packetData);
                            for (int i = 0; i < modCount; i++)
                                mods.Add(await dataTypes.ReadNextStringAsync(packetData));

                            Dictionary<string, string> channels = new();
                            int channelCount = dataTypes.ReadNextVarInt(packetData);
                            for (int i = 0; i < channelCount; i++)
                                channels.Add(await dataTypes.ReadNextStringAsync(packetData), await dataTypes.ReadNextStringAsync(packetData));

                            List<string> registries = new();
                            int registryCount = dataTypes.ReadNextVarInt(packetData);
                            for (int i = 0; i < registryCount; i++)
                                registries.Add(await dataTypes.ReadNextStringAsync(packetData));

                            // Server Mod List Reply: FMLHandshakeMessages.java > C2SModListReply > encode()
                            //
                            // [      Mod Count ][ VarInt ]
                            //      [  Mod Name ][ String ]  // Amount of entries according to Mod Count
                            // [  Channel Count ][ VarInt ]
                            //      [ Chan Name ][ String ]  // Amount of entries according to Channel Count
                            //      [   Version ][ String ]  // Each entry is a pair of Channel Name + Version
                            // [ Registry Count ][ VarInt ]
                            //      [  Registry ][ String ]  // Amount of entries according to Registry Count
                            //      [   Version ][ String ]  // Each entry is a pair of Registry Name + Version
                            //
                            // We are supposed to validate server info against our set of installed mods, then reply with our list
                            // In MCC, we just want to send a valid response so we'll reply back with data collected from the server.

                            if (Settings.Config.Logging.DebugMessages)
                                ConsoleIO.WriteLineFormatted("§8" + Translations.forge_fml2_mod_send, acceptnewlines: true);

                            // Packet ID 2: Client to Server Mod List
                            fmlResponsePacket.AddRange(dataTypes.GetVarInt(2));
                            fmlResponsePacket.AddRange(dataTypes.GetVarInt(mods.Count));
                            foreach (string mod in mods)
                                fmlResponsePacket.AddRange(dataTypes.GetString(mod));

                            fmlResponsePacket.AddRange(dataTypes.GetVarInt(channels.Count));
                            foreach (KeyValuePair<string, string> item in channels)
                            {
                                fmlResponsePacket.AddRange(dataTypes.GetString(item.Key));
                                fmlResponsePacket.AddRange(dataTypes.GetString(item.Value));
                            }

                            fmlResponsePacket.AddRange(dataTypes.GetVarInt(registries.Count));
                            foreach (string registry in registries)
                            {
                                fmlResponsePacket.AddRange(dataTypes.GetString(registry));
                                // We don't have Registry mapping from server, leave it empty
                                fmlResponsePacket.AddRange(dataTypes.GetString(""));
                            }
                            fmlResponseReady = true;
                            break;

                        case 3:
                            // Server Registry: FMLHandshakeMessages.java > S2CRegistry > decode()
                            //
                            // [    Registry Name ][ String ]
                            // [ Snapshot Present ][  Bool  ]
                            //    [ Snapshot data ][  ....  ] // Only if "Snapshot Present" is True
                            //
                            // Registry Snapshot: ForgeRegistry.java > Snapshot > read(PacketBuffer)
                            // Not documented yet. We're ignoring this packet in MCC

                            if (Settings.Config.Logging.DebugMessages)
                            {
                                string registryName = await dataTypes.ReadNextStringAsync(packetData);
                                ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.forge_fml2_registry, registryName));
                            }

                            fmlResponsePacket.AddRange(dataTypes.GetVarInt(99));
                            fmlResponseReady = true;
                            break;

                        case 4:
                            // Server Config: FMLHandshakeMessages.java > S2CConfigData > decode()
                            //
                            // [ Config Name ][ String ]
                            // [ Config Data ][  ....  ] // Remaining packet data (1)
                            //
                            // [1] Config data may containt a standard Minecraft string readable with dataTypes.readNextString()
                            // We're ignoring this packet in MCC

                            if (Settings.Config.Logging.DebugMessages)
                            {
                                string configName = await dataTypes.ReadNextStringAsync(packetData);
                                ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.forge_fml2_config, configName));
                            }

                            fmlResponsePacket.AddRange(dataTypes.GetVarInt(99));
                            fmlResponseReady = true;
                            break;

                        default:
                            if (Settings.Config.Logging.DebugMessages)
                                ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.forge_fml2_unknown, packetID));
                            break;
                    }

                    if (fmlResponseReady)
                    {
                        // Wrap our FML packet into a LoginPluginResponse payload
                        responseData.AddRange(dataTypes.GetString(fmlChannel));
                        responseData.AddRange(dataTypes.GetVarInt(fmlResponsePacket.Count));
                        responseData.AddRange(fmlResponsePacket);
                        return new(true, responseData);
                    }
                }
                else if (Settings.Config.Logging.DebugMessages)
                {
                    ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.forge_fml2_unknown_channel, fmlChannel));
                }
            }
            return new(false, responseData);
        }

        /// <summary>
        /// Send a forge plugin channel packet ("FML|HS"). Compression and encryption will be handled automatically. (Forge Protocol version 1: FML)
        /// </summary>
        /// <param name="discriminator">Discriminator to use.</param>
        /// <param name="data">packet Data</param>
        private async Task SendForgeHandshakePacket(FMLHandshakeDiscriminator discriminator, byte[] data)
        {
            await protocol18.SendPluginChannelPacket("FML|HS", dataTypes.ConcatBytes(new byte[] { (byte)discriminator }, data));
        }

        /// <summary>
        /// Server Info: Check for For Forge versions 1 and 2 on a Minecraft server Ping result
        /// </summary>
        /// <param name="jsonData">JSON data returned by the server</param>
        /// <param name="forgeInfo">ForgeInfo to populate</param>
        /// <returns>True if the server is running Forge</returns>
        public static bool ServerInfoCheckForge(PingResult jsonData, ref ForgeInfo? forgeInfo)
        {
            return ServerInfoCheckForgeSubFML1(jsonData, ref forgeInfo)   // MC 1.12 and lower
                || ServerInfoCheckForgeSubFML2(jsonData, ref forgeInfo); // MC 1.13 and greater
        }

        /// <summary>
        /// Server Info: Check if we can force-enable Forge support for this Minecraft version without using server Ping
        /// </summary>
        /// <param name="protocolVersion">Minecraft protocol version</param>
        /// <returns>TRUE if we can force-enable Forge support without using server Ping</returns>
        public static bool ServerMayForceForge(int protocolVersion)
        {
            return protocolVersion >= ProtocolHandler.MCVer2ProtocolVersion("1.13");
        }

        /// <summary>
        /// Server Info: Consider Forge to be enabled regardless of server Ping
        /// </summary>
        /// <param name="protocolVersion">Minecraft protocol version</param>
        /// <returns>ForgeInfo item stating that Forge is enabled</returns>
        public static ForgeInfo ServerForceForge(int protocolVersion)
        {
            if (ServerMayForceForge(protocolVersion))
            {
                return new ForgeInfo(FMLVersion.FML2);
            }
            else throw new InvalidOperationException(Translations.error_forgeforce);
        }

        /// <summary>
        /// Server Info: Check for For Forge on a Minecraft server Ping result (Handles FML and FML2
        /// </summary>
        /// <param name="jsonData">JSON data returned by the server</param>
        /// <param name="forgeInfo">ForgeInfo to populate</param>
        /// <returns>True if the server is running Forge</returns>
        private static bool ServerInfoCheckForgeSubFML1(PingResult jsonData, ref ForgeInfo? forgeInfo)
        {
            if (jsonData.modinfo != null)
            {
                if (jsonData.modinfo.type == "FML")
                {
                    if (jsonData.modinfo.modList == null || jsonData.modinfo.modList.Length == 0)
                    {
                        forgeInfo = null;
                        ConsoleIO.WriteLineFormatted("§8" + Translations.forge_no_mod, acceptnewlines: true);
                    }
                    else
                    {
                        forgeInfo = new ForgeInfo(jsonData.modinfo.modList, FMLVersion.FML);
                        ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.forge_with_mod, forgeInfo.Mods.Count));
                        if (Settings.Config.Logging.DebugMessages)
                        {
                            ConsoleIO.WriteLineFormatted("§8" + Translations.forge_mod_list, acceptnewlines: true);
                            foreach (ForgeInfo.ForgeMod mod in forgeInfo.Mods)
                                ConsoleIO.WriteLineFormatted("§8  " + mod.ToString());
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Server Info: Check for For Forge on a Minecraft server Ping result (Handles FML and FML2
        /// </summary>
        /// <param name="jsonData">JSON data returned by the server</param>
        /// <param name="forgeInfo">ForgeInfo to populate</param>
        /// <returns>True if the server is running Forge</returns>
        private static bool ServerInfoCheckForgeSubFML2(PingResult jsonData, ref ForgeInfo? forgeInfo)
        {
            if (jsonData.forgeData != null)
            {
                if (jsonData.forgeData.fmlNetworkVersion == "2")
                {
                    if (jsonData.forgeData.mods == null || jsonData.forgeData.mods.Length == 0)
                    {
                        forgeInfo = null;
                        ConsoleIO.WriteLineFormatted("§8" + Translations.forge_no_mod, acceptnewlines: true);
                    }
                    else
                    {
                        forgeInfo = new ForgeInfo(jsonData.forgeData.mods, FMLVersion.FML2);
                        ConsoleIO.WriteLineFormatted("§8" + string.Format(Translations.forge_with_mod, forgeInfo.Mods.Count));
                        if (Settings.Config.Logging.DebugMessages)
                        {
                            ConsoleIO.WriteLineFormatted("§8" + Translations.forge_mod_list, acceptnewlines: true);
                            foreach (ForgeInfo.ForgeMod mod in forgeInfo.Mods)
                                ConsoleIO.WriteLineFormatted("§8  " + mod.ToString());
                        }
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
