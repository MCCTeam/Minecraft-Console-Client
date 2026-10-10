using System.Diagnostics;
using Mcc.Cli.Diagnostics;
using Umpk.Protocol.Java;
using Mcc.Cli.Tests.Fakes;
using Xunit;

namespace Mcc.Cli.Tests.Diagnostics;

/// <summary>Compatibility and retention coverage for the diagnostics packet capture.</summary>
public sealed class PacketCaptureWriterTests
{
    [Fact]
    public async Task Write_ProducesCaptureAcceptedByUmpkCorpusLoader()
    {
        string folder = CreateTempFolder();
        try
        {
            using (PacketCaptureWriter writer = PacketCaptureWriter.TryOpen(
                       folder,
                       maxTotalBytes: 0,
                       TimeSpan.FromMinutes(20),
                       Stopwatch.StartNew()))
            {
                writer.Write(774, PacketFlow.Clientbound, ProtocolPhase.Configuration, 0x07, [1, 2, 3]);
                writer.Write(774, PacketFlow.Serverbound, ProtocolPhase.Play, 0x12, [0xAA, 0xBB]);
            }

            string path = Assert.Single(Directory.GetFiles(folder, "*.umpkcap"));
            LoadedCorpus loaded = await CorpusLoader.LoadFileAsync(path);

            Assert.Equal(774, loaded.Protocol);
            Assert.Collection(
                loaded.Frames,
                frame =>
                {
                    Assert.Equal(0, frame.Sequence);
                    Assert.Equal(CorpusDirection.Clientbound, frame.Direction);
                    Assert.Equal(CorpusPhase.Configuration, frame.Phase);
                    Assert.Equal(0x07, frame.WireId);
                    Assert.Equal([1, 2, 3], frame.Body);
                },
                frame =>
                {
                    Assert.Equal(1, frame.Sequence);
                    Assert.Equal(CorpusDirection.Serverbound, frame.Direction);
                    Assert.Equal(CorpusPhase.Play, frame.Phase);
                    Assert.Equal(0x12, frame.WireId);
                    Assert.Equal([0xAA, 0xBB], frame.Body);
                });
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public async Task Write_OmitsLoginAndHandshakeTrafficEntirely()
    {
        string folder = CreateTempFolder();
        try
        {
            using (PacketCaptureWriter writer = PacketCaptureWriter.TryOpen(
                       folder,
                       maxTotalBytes: 0,
                       TimeSpan.FromMinutes(20),
                       Stopwatch.StartNew()))
            {
                writer.Write(774, PacketFlow.Serverbound, ProtocolPhase.Handshake, 0x00, [1]);
                writer.Write(774, PacketFlow.Clientbound, ProtocolPhase.Login, 0x01, [2]);
                writer.Write(774, PacketFlow.Clientbound, ProtocolPhase.Configuration, 0x02, [3]);
            }

            string path = Assert.Single(Directory.GetFiles(folder, "*.umpkcap"));
            LoadedCorpus loaded = await CorpusLoader.LoadFileAsync(path);
            RecordedFrame frame = Assert.Single(loaded.Frames);
            Assert.Equal(0, frame.Sequence);
            Assert.Equal(CorpusPhase.Configuration, frame.Phase);
            Assert.Equal([3], frame.Body);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public async Task Write_StopsAtTwentyMinuteDurationLimit()
    {
        string folder = CreateTempFolder();
        try
        {
            TimeSpan elapsed = TimeSpan.Zero;
            using (PacketCaptureWriter writer = PacketCaptureWriter.TryOpen(
                       folder,
                       maxTotalBytes: 0,
                       DiagnosticsSettings.MaxCaptureDuration,
                       Stopwatch.StartNew(),
                       () => elapsed))
            {
                writer.Write(774, PacketFlow.Clientbound, ProtocolPhase.Play, 0x01, [1]);
                elapsed = TimeSpan.FromMinutes(20);
                writer.Write(774, PacketFlow.Clientbound, ProtocolPhase.Play, 0x02, [2]);
            }

            string path = Assert.Single(Directory.GetFiles(folder, "*.umpkcap"));
            LoadedCorpus loaded = await CorpusLoader.LoadFileAsync(path);
            RecordedFrame frame = Assert.Single(loaded.Frames);
            Assert.Equal(0x01, frame.WireId);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void Finish_PrunesCompletedBundlesToFiveSessions()
    {
        string logsRoot = CreateTempFolder();
        try
        {
            for (int i = 0; i < 5; i++)
            {
                string path = Path.Combine(logsRoot, $"old-{i}.zip");
                File.WriteAllBytes(path, []);
                File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(-10 + i));
            }

            var settings = new DiagnosticsSettings(
                Enabled: true,
                CapturePackets: false,
                MaxCaptureMegabytes: 0,
                KeepSessions: 5);
            SessionDiagnostics diagnostics = Assert.IsType<SessionDiagnostics>(
                SessionDiagnostics.TryOpen(logsRoot, settings, "localhost"));

            string? newest = diagnostics.Finish();

            Assert.NotNull(newest);
            Assert.Equal(5, Directory.GetFiles(logsRoot, "*.zip").Length);
            Assert.False(File.Exists(Path.Combine(logsRoot, "old-0.zip")));
        }
        finally
        {
            Directory.Delete(logsRoot, recursive: true);
        }
    }

    private static string CreateTempFolder()
    {
        string folder = Path.Combine(Path.GetTempPath(), "mcc-packet-capture-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return folder;
    }
}
