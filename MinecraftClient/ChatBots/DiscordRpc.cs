using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using DiscordRPC;
using DiscordRPC.IO;
using DiscordRPC.Logging;
using MinecraftClient.Mapping;
using MinecraftClient.Scripting;
using Tomlet.Attributes;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// Displays a Discord Rich Presence status showing the player's
    /// current Minecraft session information (server, health, dimension, etc.).
    /// Requires a Discord Application ID from https://discord.com/developers/applications
    /// </summary>
    public class DiscordRpc : ChatBot
    {
        public static Configs Config = new();

        [TomlDoNotInlineObject]
        public class Configs
        {
            [NonSerialized]
            private const string BotName = "DiscordRpc";

            public bool Enabled = false;

            [TomlInlineComment("$ChatBot.DiscordRpc.ApplicationId$")]
            public string ApplicationId = string.Empty;

            [TomlInlineComment("$ChatBot.DiscordRpc.PresenceDetails$")]
            public string PresenceDetails = "Playing on {server_host}:{server_port}";

            [TomlInlineComment("$ChatBot.DiscordRpc.PresenceState$")]
            public string PresenceState = "{dimension} - HP: {health}/{max_health}";

            [TomlInlineComment("$ChatBot.DiscordRpc.LargeImageKey$")]
            public string LargeImageKey = "mcc_icon";

            [TomlInlineComment("$ChatBot.DiscordRpc.LargeImageText$")]
            public string LargeImageText = "Minecraft Console Client";

            [TomlInlineComment("$ChatBot.DiscordRpc.SmallImageKey$")]
            public string SmallImageKey = string.Empty;

            [TomlInlineComment("$ChatBot.DiscordRpc.SmallImageText$")]
            public string SmallImageText = string.Empty;

            [TomlInlineComment("$ChatBot.DiscordRpc.ShowServerAddress$")]
            public bool ShowServerAddress = true;

            [TomlInlineComment("$ChatBot.DiscordRpc.ShowCoordinates$")]
            public bool ShowCoordinates = true;

            [TomlInlineComment("$ChatBot.DiscordRpc.ShowHealth$")]
            public bool ShowHealth = true;

            [TomlInlineComment("$ChatBot.DiscordRpc.ShowDimension$")]
            public bool ShowDimension = true;

            [TomlInlineComment("$ChatBot.DiscordRpc.ShowGamemode$")]
            public bool ShowGamemode = true;

            [TomlInlineComment("$ChatBot.DiscordRpc.ShowElapsedTime$")]
            public bool ShowElapsedTime = true;

            [TomlInlineComment("$ChatBot.DiscordRpc.ShowPlayerCount$")]
            public bool ShowPlayerCount = true;

            [TomlInlineComment("$ChatBot.DiscordRpc.UpdateIntervalSeconds$")]
            public int UpdateIntervalSeconds = 10;

            public void OnSettingUpdate()
            {
                ApplicationId ??= string.Empty;
                PresenceDetails ??= string.Empty;
                PresenceState ??= string.Empty;
                LargeImageKey ??= string.Empty;
                LargeImageText ??= string.Empty;
                SmallImageKey ??= string.Empty;
                SmallImageText ??= string.Empty;

                if (UpdateIntervalSeconds < 1)
                {
                    UpdateIntervalSeconds = 10;
                    LogToConsole(BotName, Translations.bot_DiscordRpc_invalid_interval);
                }
            }
        }

        private DiscordRpcClient? _rpcClient;
        private int _tickCounter;
        private int _updateIntervalTicks;
        private Timestamps? _sessionTimestamps;
        private float _lastHealth;

        public override void Initialize()
        {
            if (string.IsNullOrWhiteSpace(Config.ApplicationId))
            {
                LogToConsole(Translations.bot_DiscordRpc_missing_app_id);
                UnloadBot();
                return;
            }

            try
            {
                _rpcClient = OperatingSystem.IsLinux()
                    ? new DiscordRpcClient(Config.ApplicationId.Trim(), client: new DiscordRpcPipeClient())
                    : new DiscordRpcClient(Config.ApplicationId.Trim());

                _rpcClient.Logger = Settings.Config.Logging.DebugMessages
                    ? new ConsoleLogger(LogLevel.Trace)
                    : new ConsoleLogger(LogLevel.None);

                _rpcClient.OnReady += (_, e) =>
                {
                    LogToConsole(string.Format(Translations.bot_DiscordRpc_connected, e.User.Username));
                };

                _rpcClient.OnConnectionFailed += (_, e) =>
                {
                    LogToConsole(string.Format(Translations.bot_DiscordRpc_connection_failed, e.FailedPipe));
                };

                _rpcClient.Initialize();
                _updateIntervalTicks = Settings.DoubleToTick(Config.UpdateIntervalSeconds);

                LogToConsole(Translations.bot_DiscordRpc_initialized);
            }
            catch (Exception e)
            {
                LogToConsole(string.Format(Translations.bot_DiscordRpc_init_error, e.Message));
                LogDebugToConsole(e.StackTrace ?? string.Empty);
                UnloadBot();
            }
        }

        public override void OnUnload()
        {
            if (_rpcClient is { IsDisposed: false })
            {
                _rpcClient.ClearPresence();
                _rpcClient.Dispose();
            }

            _rpcClient = null;
        }

        public override void AfterGameJoined()
        {
            if (Config.ShowElapsedTime)
                _sessionTimestamps = Timestamps.Now;

            _lastHealth = Handler.GetHealth();
            _tickCounter = 0;
            SetPresence();
        }

        public override void Update()
        {
            _tickCounter++;
            if (_tickCounter < _updateIntervalTicks)
                return;

            _tickCounter = 0;
            SetPresence();
        }

        public override void OnHealthUpdate(float health, int food)
        {
            _lastHealth = health;
        }

        public override bool OnDisconnect(DisconnectReason reason, string message)
        {
            if (_rpcClient is { IsDisposed: false })
                _rpcClient.ClearPresence();

            return false;
        }

        private void SetPresence()
        {
            if (_rpcClient is null or { IsDisposed: true })
                return;

            try
            {
                string details = ReplacePlaceholders(Config.PresenceDetails);
                string state = ReplacePlaceholders(Config.PresenceState);

                var presence = new RichPresence
                {
                    Details = TruncateForDiscord(details, 128),
                    State = TruncateForDiscord(state, 128)
                };

                // Assets (images)
                var assets = new Assets();
                bool hasAssets = false;

                if (!string.IsNullOrWhiteSpace(Config.LargeImageKey))
                {
                    assets.LargeImageKey = Config.LargeImageKey.Trim();
                    assets.LargeImageText = TruncateForDiscord(
                        ReplacePlaceholders(Config.LargeImageText), 128);
                    hasAssets = true;
                }

                if (!string.IsNullOrWhiteSpace(Config.SmallImageKey))
                {
                    assets.SmallImageKey = Config.SmallImageKey.Trim();
                    assets.SmallImageText = TruncateForDiscord(
                        ReplacePlaceholders(Config.SmallImageText), 128);
                    hasAssets = true;
                }

                if (hasAssets)
                    presence.Assets = assets;

                // Timestamps
                if (Config.ShowElapsedTime && _sessionTimestamps is not null)
                    presence.Timestamps = _sessionTimestamps;

                // Player count as party
                if (Config.ShowPlayerCount)
                {
                    string[] onlinePlayers = GetOnlinePlayers();
                    int playerCount = onlinePlayers.Length;
                    if (playerCount > 0)
                    {
                        presence.Party = new Party
                        {
                            ID = $"mcc_{GetServerHost()}_{GetServerPort()}",
                            Size = playerCount,
                            Max = playerCount
                        };
                    }
                }

                _rpcClient.SetPresence(presence);
            }
            catch (Exception e)
            {
                LogDebugToConsole(string.Format(Translations.bot_DiscordRpc_update_error, e.Message));
            }
        }

        private string ReplacePlaceholders(string template)
        {
            if (string.IsNullOrEmpty(template))
                return string.Empty;

            string serverHost = Config.ShowServerAddress ? GetServerHost() : "Hidden";
            int serverPort = GetServerPort();
            string serverPortStr = Config.ShowServerAddress ? serverPort.ToString() : "****";
            string username = GetUsername();
            float health = Handler.GetHealth();
            int foodLevel = Handler.GetSaturation();
            Location location = GetCurrentLocation();
            string[] onlinePlayers = GetOnlinePlayers();
            int gamemode = GetGamemode();
            int protocolVersion = GetProtocolVersion();

            string healthStr = Config.ShowHealth ? ((int)Math.Ceiling(health)).ToString() : "?";
            string maxHealthStr = Config.ShowHealth ? "20" : "?";
            string foodStr = Config.ShowHealth ? foodLevel.ToString() : "?";
            string xStr = Config.ShowCoordinates ? ((int)location.X).ToString() : "?";
            string yStr = Config.ShowCoordinates ? ((int)location.Y).ToString() : "?";
            string zStr = Config.ShowCoordinates ? ((int)location.Z).ToString() : "?";

            string dimensionName = Config.ShowDimension ? "Unknown" : "Hidden";
            if (Config.ShowDimension)
            {
                try
                {
                    var dim = World.GetDimension();
                    dimensionName = dim.Name ?? "Unknown";

                    // Clean up the dimension name for display
                    if (dimensionName.StartsWith("minecraft:", StringComparison.Ordinal))
                        dimensionName = dimensionName["minecraft:".Length..];

                    dimensionName = dimensionName switch
                    {
                        "overworld" => "Overworld",
                        "the_nether" => "The Nether",
                        "the_end" => "The End",
                        _ => dimensionName
                    };
                }
                catch
                {
                    // World may not be available
                }
            }

            string gamemodeStr = Config.ShowGamemode
                ? gamemode switch
                {
                    0 => "Survival",
                    1 => "Creative",
                    2 => "Adventure",
                    3 => "Spectator",
                    _ => "Unknown"
                }
                : "Hidden";

            return template
                .Replace("{server_host}", serverHost)
                .Replace("{server_port}", serverPortStr)
                .Replace("{username}", username)
                .Replace("{health}", healthStr)
                .Replace("{max_health}", maxHealthStr)
                .Replace("{food}", foodStr)
                .Replace("{dimension}", dimensionName)
                .Replace("{gamemode}", gamemodeStr)
                .Replace("{x}", xStr)
                .Replace("{y}", yStr)
                .Replace("{z}", zStr)
                .Replace("{player_count}", onlinePlayers.Length.ToString())
                .Replace("{protocol}", protocolVersion.ToString());
        }

        private static string TruncateForDiscord(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Length <= maxLength ? value : value[..(maxLength - 3)] + "...";
        }

        /// <summary>
        /// Flatpak Discord exposes the RPC socket under app/com.discordapp.Discord,
        /// but the currently published DiscordRichPresence package does not probe that path.
        /// </summary>
        private sealed class DiscordRpcPipeClient : INamedPipeClient
        {
            private const string DiscordPipePrefix = "discord-ipc-";
            private const int MaximumPipeVariations = 10;

            private static readonly string[] s_unixPackageDirectories =
            [
                // Official Discord clients
                "app/com.discordapp.Discord",
                "snap.discord",

                // Community desktop clients / wrappers
                "app/dev.vencord.Vesktop",
                ".flatpak/dev.vencord.Vesktop/xdg-run",
                "app/org.equicord.equibop",
                "app/io.github.equicord.equibop",
                "app/xyz.armcord.ArmCord",
                "app/io.github.spacingbat3.webcord"
            ];

            private readonly byte[] _buffer = new byte[PipeFrame.MAX_SIZE];
            private readonly Queue<PipeFrame> _frameQueue = new();
            private readonly Lock _frameQueueLock = new();
            private readonly Lock _streamLock = new();

            private int _connectedPipe;
            private NamedPipeClientStream? _stream;
            private volatile bool _isClosed = true;
            private volatile bool _isDisposed;

            public ILogger Logger { get; set; } = new NullLogger();

            public bool IsConnected
            {
                get
                {
                    if (_isClosed)
                        return false;

                    lock (_streamLock)
                        return _stream is { IsConnected: true };
                }
            }

            [Obsolete("The connected pipe is not neccessary information.")]
            public int ConnectedPipe => _connectedPipe;

            public bool Connect(int pipe)
            {
                Logger.Trace("DiscordRpcPipeClient.Connect({0})", pipe);

                if (_isDisposed)
                    throw new ObjectDisposedException(nameof(DiscordRpcPipeClient));

                if (pipe > 9)
                    throw new ArgumentOutOfRangeException(nameof(pipe), "Argument cannot be greater than 9");

                int startPipe = pipe >= 0 ? pipe : 0;

                foreach (string pipeName in GetPipeCandidates(startPipe))
                {
                    if (AttemptConnection(pipeName))
                    {
                        BeginReadStream();
                        return true;
                    }
                }

                return false;
            }

            public bool ReadFrame(out PipeFrame frame)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(nameof(DiscordRpcPipeClient));

                lock (_frameQueueLock)
                {
                    if (_frameQueue.Count == 0)
                    {
                        frame = default;
                        return false;
                    }

                    frame = _frameQueue.Dequeue();
                    return true;
                }
            }

            public bool WriteFrame(PipeFrame frame)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(nameof(DiscordRpcPipeClient));

                if (_isClosed || !IsConnected)
                {
                    Logger.Error("Failed to write frame because the stream is closed");
                    return false;
                }

                try
                {
                    frame.WriteStream(_stream);
                    return true;
                }
                catch (IOException io)
                {
                    Logger.Error("Failed to write frame because of a IO Exception: {0}", io.Message);
                }
                catch (ObjectDisposedException)
                {
                    Logger.Warning("Failed to write frame as the stream was already disposed");
                }
                catch (InvalidOperationException)
                {
                    Logger.Warning("Failed to write frame because of a invalid operation");
                }

                return false;
            }

            public void Close()
            {
                if (_isClosed)
                {
                    Logger.Warning("Tried to close a already closed pipe.");
                    return;
                }

                try
                {
                    lock (_streamLock)
                    {
                        if (_stream is not null)
                        {
                            try
                            {
                                _stream.Flush();
                                _stream.Dispose();
                            }
                            catch
                            {
                            }

                            _stream = null;
                            _isClosed = true;
                        }
                        else
                        {
                            Logger.Warning("Stream was closed, but no stream was available to begin with!");
                        }
                    }
                }
                catch (ObjectDisposedException)
                {
                    Logger.Warning("Tried to dispose already disposed stream");
                }
                finally
                {
                    _isClosed = true;
                }
            }

            public void Dispose()
            {
                if (_isDisposed)
                    return;

                if (!_isClosed)
                    Close();

                lock (_streamLock)
                {
                    _stream?.Dispose();
                    _stream = null;
                }

                _isDisposed = true;
            }

            private bool AttemptConnection(string pipeName)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(nameof(DiscordRpcPipeClient));

                try
                {
                    lock (_streamLock)
                    {
                        Logger.Info("Attempting to connect to {0}", pipeName);
                        _stream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                        _stream.Connect(0);

                        Logger.Trace("Waiting for connection...");
                        while (!_stream.IsConnected)
                            Thread.Sleep(10);
                    }

                    Logger.Info("Connected to {0}", pipeName);
                    _connectedPipe = int.Parse(pipeName[(pipeName.LastIndexOf('-') + 1)..], System.Globalization.CultureInfo.InvariantCulture);
                    _isClosed = false;
                }
                catch (Exception e)
                {
                    Logger.Error("Failed connection to {0}. {1}", pipeName, e.Message);
                    Close();
                }

                Logger.Trace("Done. Result: {0}", _isClosed);
                return !_isClosed;
            }

            private void BeginReadStream()
            {
                if (_isClosed)
                    return;

                try
                {
                    lock (_streamLock)
                    {
                        if (_stream is not { IsConnected: true })
                            return;

                        Logger.Trace("Beginning Read of {0} bytes", _buffer.Length);
                        _stream.BeginRead(_buffer, 0, _buffer.Length, EndReadStream, _stream.IsConnected);
                    }
                }
                catch (ObjectDisposedException)
                {
                    Logger.Warning("Attempted to start reading from a disposed pipe");
                }
                catch (InvalidOperationException)
                {
                    Logger.Warning("Attempted to start reading from a closed pipe");
                }
                catch (Exception e)
                {
                    Logger.Error("An exception occurred while starting to read a stream: {0}", e.Message);
                    Logger.Error(e.StackTrace);
                }
            }

            private void EndReadStream(IAsyncResult callback)
            {
                Logger.Trace("Ending Read");
                int bytes;

                try
                {
                    lock (_streamLock)
                    {
                        if (_stream is not { IsConnected: true })
                            return;

                        bytes = _stream.EndRead(callback);
                    }
                }
                catch (IOException)
                {
                    Logger.Warning("Attempted to end reading from a closed pipe");
                    return;
                }
                catch (NullReferenceException)
                {
                    Logger.Warning("Attempted to read from a null pipe");
                    return;
                }
                catch (ObjectDisposedException)
                {
                    Logger.Warning("Attempted to end reading from a disposed pipe");
                    return;
                }
                catch (Exception e)
                {
                    Logger.Error("An exception occurred while ending a read of a stream: {0}", e.Message);
                    Logger.Error(e.StackTrace);
                    return;
                }

                Logger.Trace("Read {0} bytes", bytes);

                if (bytes > 0)
                {
                    using MemoryStream memory = new(_buffer, 0, bytes);
                    try
                    {
                        PipeFrame frame = new();
                        if (frame.ReadStream(memory))
                        {
                            Logger.Trace("Read a frame: {0}", frame.Opcode);
                            lock (_frameQueueLock)
                                _frameQueue.Enqueue(frame);
                        }
                        else
                        {
                            Logger.Error("Pipe failed to read from the data received by the stream.");
                            Close();
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error("An exception has occurred while trying to parse the pipe data: {0}", e.Message);
                        Close();
                    }
                }
                else
                {
                    Logger.Error("Empty frame was read on {0}, aborting.", Environment.OSVersion);
                    Close();
                }

                if (!_isClosed && IsConnected)
                {
                    Logger.Trace("Starting another read");
                    BeginReadStream();
                }
            }

            private static IEnumerable<string> GetPipeCandidates(int startPipe)
            {
                if (OperatingSystem.IsWindows())
                {
                    for (int i = startPipe; i < MaximumPipeVariations; i++)
                        yield return $"{DiscordPipePrefix}{i}";

                    yield break;
                }

                foreach (string runtimeDir in GetUnixRuntimeDirectories())
                {
                    for (int index = startPipe; index < MaximumPipeVariations; index++)
                    {
                        string pipeFileName = $"{DiscordPipePrefix}{index}";

                        foreach (string packageDirectory in s_unixPackageDirectories)
                        {
                            string packagePipe = Path.Combine(runtimeDir, packageDirectory, pipeFileName);
                            if (File.Exists(packagePipe))
                                yield return packagePipe;
                        }

                        string defaultPipe = Path.Combine(runtimeDir, pipeFileName);
                        if (File.Exists(defaultPipe))
                            yield return defaultPipe;

                        foreach (string packageDirectory in s_unixPackageDirectories)
                        {
                            string packagePipe = Path.Combine(runtimeDir, packageDirectory, pipeFileName);
                            if (!File.Exists(packagePipe))
                                yield return packagePipe;
                        }

                        if (!File.Exists(defaultPipe))
                            yield return defaultPipe;
                    }
                }
            }

            private static IEnumerable<string> GetUnixRuntimeDirectories()
            {
                HashSet<string> yielded = new(StringComparer.Ordinal);

                string[] candidates =
                [
                    Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR") ?? string.Empty,
                    Environment.GetEnvironmentVariable("TMPDIR") ?? string.Empty,
                    Environment.GetEnvironmentVariable("TMP") ?? string.Empty,
                    Environment.GetEnvironmentVariable("TEMP") ?? string.Empty,
                    Path.GetTempPath(),
                    "/tmp"
                ];

                foreach (string candidate in candidates)
                {
                    if (string.IsNullOrWhiteSpace(candidate))
                        continue;

                    string normalized = candidate.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    if (yielded.Add(normalized))
                        yield return normalized;
                }
            }
        }
    }
}
