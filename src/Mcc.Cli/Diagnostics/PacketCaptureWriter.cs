using System.Diagnostics;
using System.Globalization;
using Umpk.Protocol.Java;
using Umpk.Protocol.Java.Capture;

namespace Mcc.Cli.Diagnostics;

/// <summary>
/// Streams the wire frames of a session into numbered <c>.umpkcap</c> files.
/// Every chunk is a complete, independently replayable capture using the same binary format as UMPK's conformance corpus.
/// </summary>
/// <remarks>
/// Captures are chunked so a busy server does not produce one unwieldy file.
/// The 20-minute duration limit applies to the whole diagnostics session, while the byte limit applies across all chunks.
/// A protocol change on reconnect also starts a fresh chunk because an <c>.umpkcap</c> header identifies one protocol.
/// </remarks>
internal sealed class PacketCaptureWriter : IDisposable
{
    /// <summary>Maximum bytes per independently replayable capture chunk.</summary>
    private const long ChunkBytes = 32L * 1024 * 1024;

    private readonly string _folder;
    private readonly Stopwatch _clock;
    private readonly Func<TimeSpan> _elapsed;
    private readonly long _maxTotalBytes;
    private readonly TimeSpan _maxDuration;
    private readonly object _gate = new();

    private FileStream? _stream;
    private int _protocol;
    private int _chunk;
    private int _chunkFrameCount;
    private long _chunkWritten;
    private long _totalWritten;
    private long _sequence;
    private bool _capped;
    private bool _disposed;

    private PacketCaptureWriter(
        string folder,
        long maxTotalBytes,
        TimeSpan maxDuration,
        Stopwatch clock,
        Func<TimeSpan>? elapsed)
    {
        _folder = folder;
        _maxTotalBytes = maxTotalBytes;
        _maxDuration = maxDuration;
        _clock = clock;
        _elapsed = elapsed ?? (() => clock.Elapsed);
    }

    /// <summary>How many frames have been written across all chunks.</summary>
    public long FrameCount
    {
        get { lock (_gate) return _sequence; }
    }

    /// <summary>How many capture chunks exist.</summary>
    public int ChunkCount
    {
        get { lock (_gate) return _chunk; }
    }

    /// <summary>Creates a lazy capture writer. The first file is opened when the first safe frame arrives.</summary>
    public static PacketCaptureWriter TryOpen(
        string folder,
        long maxTotalBytes,
        TimeSpan maxDuration,
        Stopwatch clock,
        Func<TimeSpan>? elapsed = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        ArgumentNullException.ThrowIfNull(clock);
        return new PacketCaptureWriter(folder, maxTotalBytes, maxDuration, clock, elapsed);
    }

    /// <summary>Records one decrypted and decompressed frame body.</summary>
    /// <remarks>
    /// Login and handshake traffic is omitted entirely because it can carry authentication material.
    /// The remaining records match UMPK's corpus contract byte for byte: flow, phase, global sequence, monotonic timestamp, wire id, body length, then the raw body.
    /// </remarks>
    public void Write(
        int protocol,
        PacketFlow flow,
        ProtocolPhase phase,
        int wireId,
        ReadOnlySpan<byte> payload)
    {
        if (phase is ProtocolPhase.Login or ProtocolPhase.Handshake)
            return;

        lock (_gate)
        {
            if (_disposed || _capped)
                return;

            if (_maxDuration > TimeSpan.Zero && _elapsed() >= _maxDuration)
            {
                StopCapture();
                return;
            }

            long recordBytes = UmpkCapFormat.RecordPrefixLength + payload.Length;
            bool needsChunk = _stream is null || _protocol != protocol
                || (_chunkFrameCount > 0 && _chunkWritten + recordBytes > ChunkBytes);
            long additionalBytes = recordBytes + (needsChunk ? UmpkCapFormat.HeaderLength : 0);
            if (_maxTotalBytes > 0 && _totalWritten + additionalBytes > _maxTotalBytes)
            {
                StopCapture();
                return;
            }

            if (needsChunk)
            {
                if (!Roll(protocol))
                {
                    StopCapture();
                    return;
                }
            }

            Span<byte> prefix = stackalloc byte[UmpkCapFormat.RecordPrefixLength];
            UmpkCapFormat.WriteRecordPrefix(
                prefix,
                UmpkCapFormat.DirectionByte(flow),
                UmpkCapFormat.PhaseByte(phase),
                _sequence,
                _clock.ElapsedTicks,
                wireId,
                payload.Length);

            try
            {
                _stream!.Write(prefix);
                _stream.Write(payload);
                _sequence++;
                _chunkFrameCount++;
                _chunkWritten += recordBytes;
                _totalWritten += recordBytes;
            }
            catch (IOException)
            {
                StopCapture();
            }
        }
    }

    /// <summary>Closes the current chunk and opens the next. Called under the lock.</summary>
    private bool Roll(int protocol)
    {
        CloseCurrent();

        int nextChunk = _chunk + 1;
        string path = Path.Combine(
            _folder, string.Create(CultureInfo.InvariantCulture, $"packets.{nextChunk:0000}.umpkcap"));

        try
        {
            var stream = new FileStream(
                path,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.Read,
                bufferSize: 64 * 1024,
                FileOptions.SequentialScan);

            Span<byte> header = stackalloc byte[UmpkCapFormat.HeaderLength];
            UmpkCapFormat.WriteHeader(header, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), protocol, frameCount: 0);
            stream.Write(header);

            _stream = stream;
            _protocol = protocol;
            _chunk = nextChunk;
            _chunkFrameCount = 0;
            _chunkWritten = UmpkCapFormat.HeaderLength;
            _totalWritten += UmpkCapFormat.HeaderLength;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _stream = null;
            return false;
        }
    }

    /// <summary>Patches the chunk's frame count and closes it. Called under the lock.</summary>
    private void CloseCurrent()
    {
        if (_stream is null)
            return;

        try
        {
            Span<byte> header = stackalloc byte[UmpkCapFormat.HeaderLength];
            UmpkCapFormat.WriteHeader(
                header,
                recordedAtUnixMs: 0,
                _protocol,
                _chunkFrameCount);

            // Preserve the original recorded-at timestamp while patching the count known only at close.
            _stream.Position = UmpkCapFormat.FrameCountOffset;
            _stream.Write(header[UmpkCapFormat.FrameCountOffset..]);
            _stream.Flush();
            _stream.Dispose();
        }
        catch (IOException)
        {
            try
            {
                _stream.Dispose();
            }
            catch (IOException)
            {
            }
        }
        finally
        {
            _stream = null;
        }
    }

    private void StopCapture()
    {
        _capped = true;
        CloseCurrent();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
                return;

            _disposed = true;
            CloseCurrent();
        }
    }
}
