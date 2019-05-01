using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MinecraftClient.Protocol.Handlers.Forge;
using System.Threading;

namespace MinecraftClient.Protocol.Handlers
{
    /// <summary>
    /// Handler for the Minecraft Forge protocol
    /// </summary>
    class Protocol18Forge
    {
        private int protocolversion;
        private DataTypes dataTypes;
        private Protocol18Handler protocol18;
        private IMinecraftComHandler mcHandler;

        private ForgeInfo forgeInfo;
        private FMLHandshakeClientState fmlHandshakeState = FMLHandshakeClientState.START;
        private bool ForgeEnabled() { return forgeInfo != null; }

        /// <summary>
        /// Initialize a new Forge protocol handler
        /// </summary>
        /// <param name="forgeInfo">Forge Server Information</param>
        /// <param name="protocolversion">Minecraft protocol version</param>
        /// <param name="datatypes">Minecraft data types handler</param>
        public Protocol18Forge(ForgeInfo forgeInfo, int protocolVersion, DataTypes dataTypes, Protocol18Handler protocol18, IMinecraftComHandler mcHandler)
        {
            this.forgeInfo = forgeInfo;
            this.protocolversion = protocolVersion;
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
                return serverAddress + "\0FML\0";
            return serverAddress;
        }

        /// <summary>
        /// Completes the Minecraft Forge handshake.
        /// </summary>
        /// <returns>Whether the handshake was successful.</returns>
        public bool CompleteForgeHandshake()
        {
            if (ForgeEnabled())
            {
                int packetID = -1;
                List<byte> packetData = new List<byte>();

                while (fmlHandshakeState != FMLHandshakeClientState.DONE)
                {
                    protocol18.ReadNextPacket(ref packetID, packetData);

                    if (packetID == 0x40) // Disconnect
                    {
                        mcHandler.OnConnectionLost(ChatBot.DisconnectReason.LoginRejected, ChatParser.ParseText(dataTypes.ReadNextString(packetData)));
                        return false;
                    }
                    else
                    {
                        // Send back regular packet to the vanilla protocol handler
                        protocol18.HandlePacket(packetID, packetData);
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
        public int ReadNextVarShort(List<byte> packetData)
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
        /// Handle Forge plugin messages
        /// </summary>
        /// <param name="channel">Plugin message channel</param>
        /// <param name="packetData">Plugin message data</param>
        /// <param name="currentdimension">Current world dimension</param>
        /// <returns>TRUE if the plugin message was recognized and handled</returns>
        public bool HandlePluginMessage(string channel, List<byte> packetData, ref int currentDimension)
        {
            if (ForgeEnabled() && fmlHandshakeState != FMLHandshakeClientState.DONE)
            {
                if (channel == "FML|HS")
                {
                    FMLHandshakeDiscriminator discriminator = (FMLHandshakeDiscriminator)dataTypes.ReadNextByte(packetData);

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
                            protocol18.SendPluginChannelPacket("REGISTER", Encoding.UTF8.GetBytes(string.Join("\0", channels)));

                            byte fmlProtocolVersion = dataTypes.ReadNextByte(packetData);

                            if (Settings.DebugMessages)
                                ConsoleIO.WriteLineFormatted("§8Forge protocol version : " + fmlProtocolVersion);

                            if (fmlProtocolVersion >= 1)
                                currentDimension = dataTypes.ReadNextInt(packetData);

                            // Tell the server we're running the same version.
                            SendForgeHandshakePacket(FMLHandshakeDiscriminator.ClientHello, new byte[] { fmlProtocolVersion });

                            // Then tell the server that we're running the same mods.
                            if (Settings.DebugMessages)
                                ConsoleIO.WriteLineFormatted("§8Sending falsified mod list to server...");
                            byte[][] mods = new byte[forgeInfo.Mods.Count][];
                            for (int i = 0; i < forgeInfo.Mods.Count; i++)
                            {
                                ForgeInfo.ForgeMod mod = forgeInfo.Mods[i];
                                mods[i] = dataTypes.ConcatBytes(dataTypes.GetString(mod.ModID), dataTypes.GetString(mod.Version));
                            }
                            SendForgeHandshakePacket(FMLHandshakeDiscriminator.ModList,
                                dataTypes.ConcatBytes(dataTypes.GetVarInt(forgeInfo.Mods.Count), dataTypes.ConcatBytes(mods)));

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

                            if (protocolversion < Protocol18Handler.MC18Version)
                            {
                                // 1.7.10 and below have one registry
                                // with blocks and items.
                                int registrySize = dataTypes.ReadNextVarInt(packetData);

                                if (Settings.DebugMessages)
                                    ConsoleIO.WriteLineFormatted("§8Received registry with " + registrySize + " entries");

                                fmlHandshakeState = FMLHandshakeClientState.PENDINGCOMPLETE;
                            }
                            else
                            {
                                // 1.8+ has more than one registry.

                                bool hasNextRegistry = dataTypes.ReadNextBool(packetData);
                                string registryName = dataTypes.ReadNextString(packetData);
                                int registrySize = dataTypes.ReadNextVarInt(packetData);
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
            return false;
        }

        /// <summary>
        /// Send a forge plugin channel packet ("FML|HS"). Compression and encryption will be handled automatically.
        /// </summary>
        /// <param name="discriminator">Discriminator to use.</param>
        /// <param name="data">packet Data</param>
        private void SendForgeHandshakePacket(FMLHandshakeDiscriminator discriminator, byte[] data)
        {
            protocol18.SendPluginChannelPacket("FML|HS", dataTypes.ConcatBytes(new byte[] { (byte)discriminator }, data));
        }

        /// <summary>
        /// Server Info: Check for For Forge on a Minecraft server Ping result
        /// </summary>
        /// <param name="jsonData">JSON data returned by the server</param>
        /// <param name="forgeInfo">ForgeInfo to populate</param>
        public static void ServerInfoCheckForge(Json.JSONData jsonData, ref ForgeInfo forgeInfo)
        {
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
        }
    }
}
