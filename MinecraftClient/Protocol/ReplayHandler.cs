using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MinecraftClient.Protocol.Handlers;
using System.Runtime.InteropServices;
using Ionic.Zip;
using MinecraftClient.Mapping;
using Org.BouncyCastle.Crypto.Utilities;
using MinecraftClient.Protocol.Handlers.PacketPalettes;
using System.Runtime.Remoting.Messaging;

namespace MinecraftClient.Protocol
{
    /// <summary>
    /// Record and save replay file that can be used by Replay mod
    /// </summary>
    public class ReplayHandler
    {
        public string ReplayFileName = @"whhhh.mcpr";
        public string ReplayFileDirectory = @"replay_recordings";
        public MetaDataHandler MetaData;
        public bool RecordRunning { get { return !cleanedUp; } }

        private readonly string recordingTmpFileName = @"recording.tmcpr";
        private readonly string temporaryCache = @"recording_cache";
        private DataTypes dataTypes;
        private PacketTypePalette packetType;
        private int protocolVersion;
        private BinaryWriter recordStream;
        private DateTime recordStartTime;
        private DateTime lastPacketTime;
        private bool cleanedUp = false;

        private static bool logOutput = true;

        private int playerEntityID;
        private Guid playerUUID;
        private Location playerLastPosition;
        private float playerLastYaw;
        private float playerLastPitch;

        public ReplayHandler(int protocolVersion)
        {
            Initialize(protocolVersion);
        }

        public ReplayHandler(int protocolVersion, string serverName, string recordingDirectory = @"replay_recordings")
        {
            Initialize(protocolVersion);
            this.MetaData.serverName = serverName;
            ReplayFileDirectory = recordingDirectory;
        }

        private void Initialize(int protocolVersion)
        {
            this.dataTypes = new DataTypes(protocolVersion);
            this.packetType = new PacketTypeHandler().GetTypeHandler(protocolVersion);
            this.protocolVersion = protocolVersion;

            if (!Directory.Exists(ReplayFileDirectory))
                Directory.CreateDirectory(ReplayFileDirectory);
            if (!Directory.Exists(temporaryCache))
                Directory.CreateDirectory(temporaryCache);

            recordStream = new BinaryWriter(new FileStream(Path.Combine(temporaryCache, recordingTmpFileName), FileMode.Create, FileAccess.ReadWrite));
            recordStartTime = DateTime.Now;

            MetaData = new MetaDataHandler();
            MetaData.date = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
            MetaData.protocol = protocolVersion;
            MetaData.mcversion = ProtocolHandler.ProtocolVersion2MCVer(protocolVersion);
            MetaData.SaveToFile();

            playerLastPosition = new Location(0, 0, 0);
            WriteLog("Start recording.");
        }

        ~ReplayHandler()
        {
            OnShutDown();
        }

        public void SetClientEntityID(int entityID)
        {
            playerEntityID = entityID;
        }

        public void SetClientPlayerUUID(Guid uuid)
        {
            playerUUID = uuid;
        }

        #region File and stream handling

        public void CloseRecordStream()
        {
            try
            {
                recordStream.Flush();
                recordStream.Close();
            }
            catch { }
        }

        /// <summary>
        /// Stop recording and save replay file. Should called once before program exit
        /// </summary>
        public void OnShutDown()
        {
            if (!cleanedUp)
            {
                MetaData.duration = Convert.ToInt32((lastPacketTime - recordStartTime).TotalMilliseconds);
                MetaData.SaveToFile();
                CloseRecordStream();
                CreateReplayFile();
                cleanedUp = true;
            }
        }

        /// <summary>
        /// Create the replay file for Replay mod to read
        /// </summary>
        public void CreateReplayFile()
        {
            string replayFileName = GetReplayDefaultName();
            CreateReplayFile(replayFileName);
        }

        /// <summary>
        /// Create the replay file for Replay mod to read
        /// </summary>
        /// <param name="replayFileName">Replay file name</param>
        public void CreateReplayFile(string replayFileName)
        {
            WriteLog("Creating replay file.");

            using (Stream recordingFile = new FileStream(Path.Combine(temporaryCache, recordingTmpFileName), FileMode.Open))
            {
                using (Stream metaDataFile = new FileStream(Path.Combine(temporaryCache, MetaData.MetaDataFileName), FileMode.Open))
                {
                    using (ZipOutputStream zs = new ZipOutputStream(Path.Combine(ReplayFileDirectory, replayFileName)))
                    {
                        zs.PutNextEntry(recordingTmpFileName);
                        recordingFile.CopyTo(zs);
                        zs.PutNextEntry(MetaData.MetaDataFileName);
                        metaDataFile.CopyTo(zs);
                        zs.Close();
                    }
                }
            }

            File.Delete(Path.Combine(temporaryCache, recordingTmpFileName));
            File.Delete(Path.Combine(temporaryCache, MetaData.MetaDataFileName));

            WriteLog("Replay file created.");
        }

        /// <summary>
        /// Create a backup replay file while recording
        /// </summary>
        /// <param name="replayFileName"></param>
        public void CreateBackupReplay(string replayFileName)
        {
            if (cleanedUp)
                return;
            WriteDebugLog("Creating backup replay file.");

            MetaData.duration = Convert.ToInt32((lastPacketTime - recordStartTime).TotalMilliseconds);
            MetaData.SaveToFile();

            using (Stream metaDataFile = new FileStream(Path.Combine(temporaryCache, MetaData.MetaDataFileName), FileMode.Open))
            {
                using (ZipOutputStream zs = new ZipOutputStream(replayFileName))
                {
                    zs.PutNextEntry(recordingTmpFileName);
                    // .CopyTo() method start from stream current position
                    // We need to reset position in order to get full content
                    var lastPosition = recordStream.BaseStream.Position;
                    recordStream.BaseStream.Position = 0;
                    recordStream.BaseStream.CopyTo(zs);
                    recordStream.BaseStream.Position = lastPosition;

                    zs.PutNextEntry(MetaData.MetaDataFileName);
                    metaDataFile.CopyTo(zs);
                    zs.Close();
                }
            }

            WriteDebugLog("Backup replay file created.");
        }

        /// <summary>
        /// Get the default mcpr file name by current time
        /// </summary>
        /// <returns></returns>
        public string GetReplayDefaultName()
        {
            var now = DateTime.Now;
            return string.Format("{0}_{1}_{2}_{3}_{4}_{5}.mcpr", now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second); // yyyy_mm_dd_hh_mm_ss
        }

        #endregion

        #region Packet related method

        /// <summary>
        /// Add a packet from network
        /// </summary>
        /// <param name="packetID"></param>
        /// <param name="packetData"></param>
        /// <param name="isLogin"></param>
        /// <param name="isInbound"></param>
        public void AddPacket(int packetID, IEnumerable<byte> packetData, bool isLogin, bool isInbound)
        {
            try 
            {
                if (isInbound)
                    HandleInBoundPacket(packetID, packetData, isLogin);
                else return;

                if (PacketShouldSave(packetID, isLogin, isInbound))
                    AddPacket(packetID, packetData);
            }
            catch (Exception e)
            {
                WriteDebugLog("Exception while adding packet: " + e.Message + "\n" + e.StackTrace);
            }
        }

        /// <summary>
        /// Add packet directly without checking (internal use only)
        /// </summary>
        /// <param name="packetID"></param>
        /// <param name="packetData"></param>
        private void AddPacket(int packetID, IEnumerable<byte> packetData)
        {
            lastPacketTime = DateTime.Now;
            // build raw packet
            // format: packetID + packetData
            List<byte> rawPacket = new List<byte>();
            rawPacket.AddRange(dataTypes.GetVarInt(packetID).ToArray());
            rawPacket.AddRange(packetData.ToArray());
            // build format
            // format: timestamp + packetLength + RawPacket
            List<byte> line = new List<byte>();
            int nowTime = Convert.ToInt32((lastPacketTime - recordStartTime).TotalMilliseconds);
            line.AddRange(BitConverter.GetBytes((Int32)nowTime).Reverse().ToArray());
            line.AddRange(BitConverter.GetBytes((Int32)rawPacket.Count).Reverse().ToArray());
            line.AddRange(rawPacket.ToArray());
            // Write out to the file
            recordStream.Write(line.ToArray());
        }

        /// <summary>
        /// Add a player's UUID to the MetaData
        /// </summary>
        /// <param name="uuid"></param>
        /// <param name="name"></param>
        public void OnPlayerSpawn(Guid uuid)
        {
            // Metadata has a field for storing uuid for all players entered client render range
            MetaData.AddPlayerUUID(uuid);
        }

        /// <summary>
        /// Determine a packet should be saved
        /// </summary>
        /// <param name="packetID"></param>
        /// <param name="isLogin"></param>
        /// <param name="isInbound"></param>
        /// <returns></returns>
        private bool PacketShouldSave(int packetID, bool isLogin, bool isInbound)
        {
            if (!isInbound) // save inbound only
                return false;
            if (!isLogin) // save all play state packet
            {
                return true;
            }
            else
            { // is login
                if (packetID == 0x02) // login success
                {
                    return true;
                }
                else return false;
            }
        }

        /// <summary>
        /// Used to gather information needed
        /// </summary>
        /// <remarks>
        /// Also for converting client side packet to server side packet
        /// </remarks>
        /// <param name="packetID"></param>
        /// <param name="packetData"></param>
        /// <param name="isLogin"></param>
        private void HandleInBoundPacket(int packetID, IEnumerable<byte> packetData, bool isLogin)
        {
            Queue<byte> p = new Queue<byte>(packetData);
            PacketTypesIn pType = packetType.GetIncommingTypeById(packetID);
            // Login success. Get player UUID
            if (isLogin && packetID == 0x02)
            {
                Guid uuid;
                if (protocolVersion < Protocol18Handler.MC116Version)
                {
                    if (Guid.TryParse(dataTypes.ReadNextString(p), out uuid))
                    {
                        SetClientPlayerUUID(uuid);
                        WriteDebugLog("User UUID: " + uuid.ToString());
                    }
                }
                else
                {
                    var uuid2 = dataTypes.ReadNextUUID(p);
                    SetClientPlayerUUID(uuid2);
                    WriteDebugLog("User UUID: " + uuid2.ToString());
                }
                return;
            }

            if (!isLogin && pType == PacketTypesIn.JoinGame)
            {
                // Get client player entity ID
                SetClientEntityID(dataTypes.ReadNextInt(p));
                return;
            }

            if (!isLogin && pType == PacketTypesIn.SpawnPlayer)
            {
                dataTypes.ReadNextVarInt(p);
                OnPlayerSpawn(dataTypes.ReadNextUUID(p));
                return;
            }

            // Get client player location for calculating movement delta later
            if (pType == PacketTypesIn.PlayerPositionAndLook)
            {
                double x = dataTypes.ReadNextDouble(p);
                double y = dataTypes.ReadNextDouble(p);
                double z = dataTypes.ReadNextDouble(p);
                float yaw = dataTypes.ReadNextFloat(p);
                float pitch = dataTypes.ReadNextFloat(p);
                byte locMask = dataTypes.ReadNextByte(p);

                playerLastPitch = pitch;
                playerLastYaw = yaw;
                if (protocolVersion >= Protocol18Handler.MC18Version)
                {
                    playerLastPosition.X = (locMask & 1 << 0) != 0 ? playerLastPosition.X + x : x;
                    playerLastPosition.Y = (locMask & 1 << 1) != 0 ? playerLastPosition.Y + y : y;
                    playerLastPosition.Z = (locMask & 1 << 2) != 0 ? playerLastPosition.Z + z : z;
                }
                else
                {
                    playerLastPosition.X = x;
                    playerLastPosition.Y = y;
                    playerLastPosition.Z = z;
                }
                return;
            }
        }

        /// <summary>
        /// Handle outbound packet (i.e. client player movement)
        /// </summary>
        /// <param name="packetID"></param>
        /// <param name="packetData"></param>
        /// <param name="isLogin"></param>
        private void HandleOutBoundPacket(int packetID, IEnumerable<byte> packetData, bool isLogin)
        {
            var packetType = this.packetType.GetOutgoingTypeById(packetID);
            if (packetType == PacketTypesOut.PlayerPosition
                || packetType == PacketTypesOut.PlayerPositionAndRotation)
            {
                // translate them to incoming entitymovement packet then save them
            }
        }

        private byte[] GetSpawnPlayerPacket(int entityID, Guid playerUUID, Location location, double pitch, double yaw)
        {
            List<byte> packet = new List<byte>();
            packet.AddRange(dataTypes.GetVarInt(entityID));
            packet.AddRange(playerUUID.ToBigEndianBytes());
            packet.AddRange(dataTypes.GetDouble(location.X));
            packet.AddRange(dataTypes.GetDouble(location.Y));
            packet.AddRange(dataTypes.GetDouble(location.Z));
            packet.Add((byte)0);
            packet.Add((byte)0);
            return packet.ToArray();
        }

        #endregion

        #region Helper method

        private static void WriteLog(string t)
        {
            if (logOutput)
                ConsoleIO.WriteLogLine("[Replay] " + t);
        }

        private static void WriteDebugLog(string t)
        {
            if (Settings.DebugMessages && logOutput)
                WriteLog(t);
        }

        #endregion
    }

    /// <summary>
    /// Handle MetaData used by Replay mod
    /// </summary>
    public class MetaDataHandler
    {
        public readonly string MetaDataFileName = @"metaData.json";
        public readonly string temporaryCache = @"recording_cache";

        public bool singlePlayer = false;
        public string serverName;
        public int duration = 0; // duration of the whole replay
        public long date; // start time of the recording in unix timestamp milliseconds
        public string mcversion = "0.0"; // e.g. 1.15.2
        public string fileFormat = "MCPR";
        public int fileFormatVersion = 14; // 14 is what I found in metadata generated in 1.15.2 replay mod
        public int protocol;
        public string generator = "MCC"; // The program which generated the file (MCC have more popularity now :P)
        public int selfId = -1; // I saw -1 in medaData file generated by Replay mod. Not sure what is this for
        public List<string> players; // Array of UUIDs of all players which can be seen in the replay

        public MetaDataHandler()
        {
            players = new List<string>();
        }

        /// <summary>
        /// Add a player's UUID who appeared in the replay
        /// </summary>
        /// <param name="uuid"></param>
        public void AddPlayerUUID(Guid uuid)
        {
            players.Add(uuid.ToString());
        }

        /// <summary>
        /// Export metadata to JSON string
        /// </summary>
        /// <returns>JSON string</returns>
        public string ToJson()
        {
            return String.Concat(new[] { "{"
                , "\"singleplayer\":" , singlePlayer.ToString().ToLower() , ","
                , "\"serverName\":\"" , serverName , "\","
                , "\"duration\":" , duration.ToString() , ","
                , "\"date\":" , date.ToString() , ","
                , "\"mcversion\":\"" , mcversion , "\","
                , "\"fileFormat\":\"" , fileFormat , "\","
                , "\"fileFormatVersion\":" , fileFormatVersion.ToString() , ","
                , "\"protocol\":" , protocol.ToString() , ","
                , "\"generator\":\"" , generator , "\","
                , "\"selfId\":" , selfId.ToString() + ","
                , "\"player\":" , GetPlayersJsonArray()
                , "}"
            });
        }

        /// <summary>
        /// Save metadata to disk file
        /// </summary>
        public void SaveToFile()
        {
            File.WriteAllText(Path.Combine(temporaryCache, MetaDataFileName), ToJson());
        }

        /// <summary>
        /// Get players UUID JSON array string
        /// </summary>
        /// <returns></returns>
        private string GetPlayersJsonArray()
        {
            if (players.Count == 0)
                return "[]";

            // Place between brackets the comma-separated list of player names placed between quotes
            return String.Format("[{0}]",
                String.Join(",",
                    players.Select(player => String.Format("\"{0}\"", player))
                )
            );
        }
    }
}
