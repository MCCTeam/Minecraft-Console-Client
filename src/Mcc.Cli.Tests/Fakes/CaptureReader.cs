using System.Buffers.Binary;
using Umpk.Protocol.Java.Capture;

namespace Mcc.Cli.Tests.Fakes;

// Test-only reader of the published UMPK capture format. No protocol codec or TestKit package is required.
internal enum CorpusDirection : byte { Clientbound = 0, Serverbound = 1 }
internal enum CorpusPhase : byte { Handshake = 0, Status = 1, Login = 2, Configuration = 3, Play = 4 }
internal sealed record RecordedFrame(long Sequence, CorpusDirection Direction, CorpusPhase Phase, int WireId, byte[] Body);
internal sealed record LoadedCorpus(int Protocol, IReadOnlyList<RecordedFrame> Frames);
internal static class CorpusLoader
{
    public static async Task<LoadedCorpus> LoadFileAsync(string path)
    {
        byte[] bytes = await File.ReadAllBytesAsync(path);
        if (bytes.Length < UmpkCapFormat.HeaderLength || !bytes.AsSpan(0, 6).SequenceEqual(UmpkCapFormat.Magic))
            throw new InvalidDataException("Invalid capture header.");
        int protocol = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(15));
        int count = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(19));
        var frames = new List<RecordedFrame>();
        int offset = UmpkCapFormat.HeaderLength;
        for (int i = 0; i < count; i++)
        {
            if (bytes.Length - offset < UmpkCapFormat.RecordPrefixLength) throw new InvalidDataException("Truncated capture.");
            int length = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset + 22));
            int bodyOffset = offset + UmpkCapFormat.RecordPrefixLength;
            if (length < 0 || length > bytes.Length - bodyOffset) throw new InvalidDataException("Invalid body length.");
            frames.Add(new(BinaryPrimitives.ReadInt64LittleEndian(bytes.AsSpan(offset + 2)),
                (CorpusDirection)bytes[offset], (CorpusPhase)bytes[offset + 1],
                BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset + 18)), bytes.AsSpan(bodyOffset, length).ToArray()));
            offset = bodyOffset + length;
        }
        if (count < 0 || offset != bytes.Length) throw new InvalidDataException("Invalid capture length.");
        return new(protocol, frames);
    }
}
