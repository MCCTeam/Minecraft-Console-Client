using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using DiscordRPC.IO;
using DiscordRPC.Logging;

namespace MinecraftClient.ChatBots
{
    /// <summary>
    /// Flatpak Discord exposes the RPC socket under app/com.discordapp.Discord,
    /// but the currently published DiscordRichPresence package does not probe that path.
    /// </summary>
    internal sealed class DiscordRpcPipeClient : INamedPipeClient
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
        private readonly object _frameQueueLock = new();
        private readonly object _streamLock = new();

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
