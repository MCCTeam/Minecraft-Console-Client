using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Handlers;
using MinecraftClient.Protocol.Handlers.PacketPalettes;

namespace MinecraftClient.Protocol
{
    /// <summary>
    /// Record and save replay files that can be used by Replay Mod.
    /// </summary>
    public class ReplayHandler : IDisposable
    {
        private const string DefaultReplayDirectory = "replay_recordings";
        private const string WorkingRootDirectory = "recording_cache";
        private const string RecordingEntryName = "recording.tmcpr";
        private const string BackupFileName = "REPLAY_BACKUP.mcpr";

        private static readonly bool logOutput = true;

        private readonly Lock _sync = new();
        private readonly DataTypes _dataTypes;
        private readonly PacketTypePalette _packetType;
        private readonly int _protocolVersion;
        private readonly string _instanceToken;
        private readonly string _workingDirectory;
        private readonly string _recordingFilePath;
        private readonly string _backupReplayPath;
        private readonly EventHandler _processExitHandler;
        private readonly FileStream _recordStream;
        private readonly DateTime _recordStartTime;

        private ReplayRecordingState _state = ReplayRecordingState.Recording;
        private bool _recordStreamClosed;
        private bool _disposed;
        private DateTime _lastPacketTime;

        private int _playerEntityId = -1;
        private Guid _playerUuid;
        private Location _playerLastPosition;
        private float _playerLastYaw;
        private float _playerLastPitch;

        public string ReplayFileName { get; private set; } = string.Empty;
        public string ReplayFileDirectory { get; }
        public MetaDataHandler MetaData { get; }

        public bool RecordRunning
        {
            get
            {
                lock (_sync)
                    return _state == ReplayRecordingState.Recording;
            }
        }

        public ReplayHandler(int protocolVersion)
            : this(protocolVersion, null, DefaultReplayDirectory)
        {
        }

        public ReplayHandler(int protocolVersion, string? serverName, string recordingDirectory = DefaultReplayDirectory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(recordingDirectory);

            _dataTypes = new DataTypes(protocolVersion);
            _packetType = new PacketTypeHandler().GetTypeHandler(protocolVersion);
            _protocolVersion = protocolVersion;
            ReplayFileDirectory = recordingDirectory;
            Directory.CreateDirectory(ReplayFileDirectory);

            _instanceToken = Path.GetRandomFileName().Replace(".", string.Empty, StringComparison.Ordinal);
            _workingDirectory = Path.Combine(WorkingRootDirectory, $"{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_{Environment.ProcessId}_{_instanceToken}");
            Directory.CreateDirectory(_workingDirectory);

            _recordingFilePath = Path.Combine(_workingDirectory, RecordingEntryName);
            _backupReplayPath = Path.Combine(_workingDirectory, BackupFileName);
            _recordStream = new FileStream(_recordingFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            _processExitHandler = (_, _) => FinalizeOnProcessExit();

            _recordStartTime = DateTime.UtcNow;
            _lastPacketTime = _recordStartTime;

            MetaData = new MetaDataHandler(_workingDirectory)
            {
                serverName = serverName,
                date = new DateTimeOffset(_recordStartTime).ToUnixTimeMilliseconds(),
                protocol = protocolVersion,
                mcversion = ProtocolHandler.ProtocolVersion2MCVer(protocolVersion)
            };
            MetaData.SaveToFile();

            _playerLastPosition = new Location(0, 0, 0);
            AppDomain.CurrentDomain.ProcessExit += _processExitHandler;

            WriteLog("Start recording.");
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                OnShutDown();
            }
            finally
            {
                AppDomain.CurrentDomain.ProcessExit -= _processExitHandler;
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        public void SetClientEntityID(int entityID)
        {
            lock (_sync)
            {
                _playerEntityId = entityID;
                if (entityID >= 0)
                    MetaData.selfId = entityID;
            }
        }

        public void SetClientPlayerUUID(Guid uuid)
        {
            lock (_sync)
            {
                _playerUuid = uuid;
                MetaData.AddPlayerUUID(uuid);
            }
        }

        public string GetBackupReplayPath() => _backupReplayPath;

        /// <summary>
        /// Stop recording and save the replay file.
        /// </summary>
        public void OnShutDown()
        {
            lock (_sync)
            {
                EnsureNotDisposed();

                if (_state != ReplayRecordingState.Recording)
                    return;

                string replayFileName = GetReplayDefaultName();
                string replayFilePath = ResolveReplayPath(replayFileName);

                WriteLog("Creating replay file.");
                _state = ReplayRecordingState.Finalizing;
                try
                {
                    CloseRecordStreamUnsafe();
                    WriteReplayArchiveUnsafe(replayFilePath, readFromActiveStream: false);
                    ReplayFileName = replayFileName;
                    _state = ReplayRecordingState.Stopped;
                    CleanupWorkingFilesUnsafe();
                    WriteLog("Replay file created.");
                }
                catch
                {
                    _state = ReplayRecordingState.Stopped;
                    throw;
                }
            }
        }

        /// <summary>
        /// Create a snapshot replay file while the recording is still running.
        /// </summary>
        public void CreateBackupReplay(string replayFileName)
        {
            lock (_sync)
            {
                EnsureNotDisposed();

                if (_state != ReplayRecordingState.Recording)
                    return;

                WriteDebugLog("Creating backup replay file.");
                WriteReplayArchiveUnsafe(ResolveReplayPath(replayFileName), readFromActiveStream: true);
                WriteDebugLog("Backup replay file created.");
            }
        }

        /// <summary>
        /// Get a default unique replay file name for the current recording.
        /// </summary>
        public string GetReplayDefaultName()
        {
            string version = ProtocolHandler.ProtocolVersion2MCVer(_protocolVersion).Replace('.', '_');
            return $"{DateTime.UtcNow:yyyy_MM_dd_HH_mm_ss_fff}_{version}_{Environment.ProcessId}_{_instanceToken}.mcpr";
        }

        /// <summary>
        /// Add a packet from network capture.
        /// </summary>
        public void AddPacket(int packetID, IEnumerable<byte> packetData, bool isLogin, bool isInbound)
        {
            byte[] packetBytes = packetData as byte[] ?? [.. packetData];

            lock (_sync)
            {
                if (_disposed || _state != ReplayRecordingState.Recording)
                    return;

                try
                {
                    if (!isInbound)
                        return;

                    HandleInBoundPacket(packetID, packetBytes, isLogin);

                    if (PacketShouldSave(packetID, isLogin, isInbound))
                        AddPacketUnsafe(packetID, packetBytes);
                }
                catch (Exception e)
                {
                    WriteDebugLog("Exception while adding packet: " + e.Message + "\n" + e.StackTrace);
                }
            }
        }

        /// <summary>
        /// Add a player's UUID to the metadata.
        /// </summary>
        public void OnPlayerSpawn(Guid uuid)
        {
            lock (_sync)
            {
                MetaData.AddPlayerUUID(uuid);
            }
        }

        private void AddPacketUnsafe(int packetID, byte[] packetData)
        {
            _lastPacketTime = DateTime.UtcNow;

            byte[] packetId = [.. DataTypes.GetVarInt(packetID)];
            byte[] rawPacket = new byte[packetId.Length + packetData.Length];
            packetId.CopyTo(rawPacket, 0);
            packetData.CopyTo(rawPacket, packetId.Length);

            int elapsedMilliseconds = Math.Max(0, Convert.ToInt32((_lastPacketTime - _recordStartTime).TotalMilliseconds));
            Span<byte> header = stackalloc byte[8];
            BinaryPrimitives.WriteInt32BigEndian(header, elapsedMilliseconds);
            BinaryPrimitives.WriteInt32BigEndian(header[4..], rawPacket.Length);

            _recordStream.Write(header);
            _recordStream.Write(rawPacket);
        }

        private bool PacketShouldSave(int packetID, bool isLogin, bool isInbound)
        {
            if (!isInbound)
                return false;

            if (!isLogin)
                return true;

            return packetID == 0x02;
        }

        private void HandleInBoundPacket(int packetID, byte[] packetData, bool isLogin)
        {
            Queue<byte> p = new(packetData);
            PacketTypesIn pType = _packetType.GetIncomingTypeById(packetID);

            if (isLogin && packetID == 0x02)
            {
                if (_protocolVersion < Protocol18Handler.MC_1_16_Version)
                {
                    if (Guid.TryParse(_dataTypes.ReadNextString(p), out Guid uuid))
                    {
                        SetClientPlayerUUID(uuid);
                        WriteDebugLog("User UUID: " + uuid);
                    }
                }
                else
                {
                    Guid uuid = _dataTypes.ReadNextUUID(p);
                    SetClientPlayerUUID(uuid);
                    WriteDebugLog("User UUID: " + uuid);
                }
                return;
            }

            if (!isLogin && pType == PacketTypesIn.JoinGame)
            {
                SetClientEntityID(_dataTypes.ReadNextInt(p));
                return;
            }

            if (!isLogin && pType == PacketTypesIn.SpawnPlayer)
            {
                _dataTypes.ReadNextVarInt(p);
                OnPlayerSpawn(_dataTypes.ReadNextUUID(p));
                return;
            }

            if (pType == PacketTypesIn.PlayerPositionAndLook)
            {
                double x = _dataTypes.ReadNextDouble(p);
                double y = _dataTypes.ReadNextDouble(p);
                double z = _dataTypes.ReadNextDouble(p);
                float yaw = _dataTypes.ReadNextFloat(p);
                float pitch = _dataTypes.ReadNextFloat(p);
                byte locMask = _dataTypes.ReadNextByte(p);

                _playerLastPitch = pitch;
                _playerLastYaw = yaw;
                if (_protocolVersion >= Protocol18Handler.MC_1_8_Version)
                {
                    _playerLastPosition.X = (locMask & 1 << 0) != 0 ? _playerLastPosition.X + x : x;
                    _playerLastPosition.Y = (locMask & 1 << 1) != 0 ? _playerLastPosition.Y + y : y;
                    _playerLastPosition.Z = (locMask & 1 << 2) != 0 ? _playerLastPosition.Z + z : z;
                }
                else
                {
                    _playerLastPosition.X = x;
                    _playerLastPosition.Y = y;
                    _playerLastPosition.Z = z;
                }
            }
        }

        private void WriteReplayArchiveUnsafe(string replayFilePath, bool readFromActiveStream)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(replayFilePath) ?? ".");

            MetaData.duration = GetCurrentDurationMillisecondsUnsafe();
            if (_playerEntityId >= 0)
                MetaData.selfId = _playerEntityId;
            if (_playerUuid != Guid.Empty)
                MetaData.AddPlayerUUID(_playerUuid);

            MetaData.SaveToFile();

            using FileStream replayArchiveFile = new(replayFilePath, FileMode.Create, FileAccess.Write);
            using ZipArchive replayArchive = new(replayArchiveFile, ZipArchiveMode.Create);

            ZipArchiveEntry recordingEntry = replayArchive.CreateEntry(RecordingEntryName);
            using (Stream recordingEntryStream = recordingEntry.Open())
            {
                if (readFromActiveStream)
                    CopyActiveRecordingUnsafe(recordingEntryStream);
                else
                    using (FileStream recordingFile = new(_recordingFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        recordingFile.CopyTo(recordingEntryStream);
            }

            ZipArchiveEntry metadataEntry = replayArchive.CreateEntry(MetaData.MetaDataFileName);
            using Stream metadataEntryStream = metadataEntry.Open();
            using FileStream metadataFile = new(Path.Combine(_workingDirectory, MetaData.MetaDataFileName), FileMode.Open, FileAccess.Read, FileShare.Read);
            metadataFile.CopyTo(metadataEntryStream);
        }

        private void CopyActiveRecordingUnsafe(Stream destination)
        {
            _recordStream.Flush();
            long position = _recordStream.Position;
            try
            {
                _recordStream.Position = 0;
                _recordStream.CopyTo(destination);
            }
            finally
            {
                _recordStream.Position = position;
            }
        }

        private int GetCurrentDurationMillisecondsUnsafe() =>
            Math.Max(0, Convert.ToInt32((_lastPacketTime - _recordStartTime).TotalMilliseconds));

        private bool HasCapturedPacketsUnsafe() => _lastPacketTime > _recordStartTime;

        private void CloseRecordStreamUnsafe()
        {
            if (_recordStreamClosed)
                return;

            _recordStream.Flush();
            _recordStream.Dispose();
            _recordStreamClosed = true;
        }

        private void CleanupWorkingFilesUnsafe()
        {
            DeleteFileIfExists(_backupReplayPath);
            DeleteFileIfExists(_recordingFilePath);
            DeleteFileIfExists(Path.Combine(_workingDirectory, MetaData.MetaDataFileName));

            if (Directory.Exists(_workingDirectory) && Directory.GetFileSystemEntries(_workingDirectory).Length == 0)
                Directory.Delete(_workingDirectory);
        }

        private void FinalizeOnProcessExit()
        {
            lock (_sync)
            {
                if (_disposed || _state != ReplayRecordingState.Recording)
                    return;

                try
                {
                    _state = ReplayRecordingState.Finalizing;
                    CloseRecordStreamUnsafe();

                    if (HasCapturedPacketsUnsafe())
                    {
                        string replayFileName = GetReplayDefaultName();
                        WriteDebugLog("Process exit detected, finalizing replay file.");
                        WriteReplayArchiveUnsafe(ResolveReplayPath(replayFileName), readFromActiveStream: false);
                        ReplayFileName = replayFileName;
                    }

                    _state = ReplayRecordingState.Stopped;
                    CleanupWorkingFilesUnsafe();
                }
                catch (Exception e)
                {
                    _state = ReplayRecordingState.Stopped;
                    WriteDebugLog("Exception while finalizing replay on process exit: " + e.Message + "\n" + e.StackTrace);
                }
            }
        }

        private string ResolveReplayPath(string replayFileName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(replayFileName);

            if (Path.IsPathRooted(replayFileName) || !string.IsNullOrEmpty(Path.GetDirectoryName(replayFileName)))
                return replayFileName;

            return Path.Combine(ReplayFileDirectory, replayFileName);
        }

        private void EnsureNotDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

        private static void DeleteFileIfExists(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        private static void WriteLog(string t)
        {
            if (logOutput)
                ConsoleIO.WriteLogLine("[Replay] " + t);
        }

        private static void WriteDebugLog(string t)
        {
            if (Settings.Config.Logging.DebugMessages && logOutput)
                WriteLog(t);
        }

        private enum ReplayRecordingState
        {
            Recording,
            Finalizing,
            Stopped
        }
    }

    /// <summary>
    /// Metadata used by Replay Mod.
    /// </summary>
    public class MetaDataHandler
    {
        private static readonly JsonSerializerOptions s_jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly HashSet<string> _players = new(StringComparer.OrdinalIgnoreCase);

        public string MetaDataFileName { get; } = "metaData.json";
        public string temporaryCache { get; }

        public bool singlePlayer = false;
        public string? serverName;
        public string? customServerName;
        public int duration;
        public long date;
        public string mcversion = "0.0";
        public string fileFormat = "MCPR";
        public int fileFormatVersion = 14;
        public int protocol;
        public string generator = "MCC";
        public int selfId = -1;

        public MetaDataHandler(string temporaryCache)
        {
            this.temporaryCache = temporaryCache;
        }

        public void AddPlayerUUID(Guid uuid)
        {
            _players.Add(uuid.ToString());
        }

        public string ToJson()
        {
            ReplayMetaDataModel metaData = new()
            {
                Singleplayer = singlePlayer,
                ServerName = serverName,
                CustomServerName = customServerName,
                Duration = duration,
                Date = date,
                Mcversion = mcversion,
                FileFormat = fileFormat,
                FileFormatVersion = fileFormatVersion,
                Protocol = protocol,
                Generator = generator,
                SelfId = selfId,
                Players = [.. _players]
            };

            return JsonSerializer.Serialize(metaData, s_jsonOptions);
        }

        public void SaveToFile()
        {
            Directory.CreateDirectory(temporaryCache);
            File.WriteAllText(Path.Combine(temporaryCache, MetaDataFileName), ToJson());
        }

        private sealed class ReplayMetaDataModel
        {
            public bool Singleplayer { get; init; }
            public string? ServerName { get; init; }
            public string? CustomServerName { get; init; }
            public int Duration { get; init; }
            public long Date { get; init; }

            [JsonPropertyName("mcversion")]
            public string Mcversion { get; init; } = "0.0";

            public string FileFormat { get; init; } = "MCPR";
            public int FileFormatVersion { get; init; }
            public int Protocol { get; init; }
            public string Generator { get; init; } = "MCC";
            public int SelfId { get; init; } = -1;
            public string[] Players { get; init; } = [];
        }
    }
}
