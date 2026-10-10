using Mcc.Cli.Diagnostics;
using Xunit;

namespace Mcc.Cli.Tests.Diagnostics;

/// <summary>Privacy and plain-text coverage for the diagnostics console transcript.</summary>
public sealed class SessionDiagnosticsTests
{
    private const string Esc = "\u001b";

    [Fact]
    public void WriteConsole_StripsAllConsoleColorFormatting()
    {
        string logsRoot = CreateTempFolder();
        try
        {
            using SessionDiagnostics diagnostics = Open(logsRoot);

            diagnostics.WriteConsole(
                $"{Esc}[38;2;255;85;85mred{Esc}[0m §agreen§r §§4background§§r plain");

            string transcript = ReadTranscript(diagnostics);
            Assert.DoesNotContain(Esc, transcript, StringComparison.Ordinal);
            Assert.DoesNotContain('§', transcript);
            Assert.Contains("red green background plain", transcript, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(logsRoot, recursive: true);
        }
    }

    [Fact]
    public void WriteConsole_OmitsLoginAndRegisterLines()
    {
        string logsRoot = CreateTempFolder();
        try
        {
            using SessionDiagnostics diagnostics = Open(logsRoot);

            diagnostics.WriteConsole("ordinary line");
            diagnostics.WriteConsole($"{Esc}[37m> /login secret-password{Esc}[0m");
            diagnostics.WriteConsole("§7> §f/REGISTER secret-password secret-password§r");
            diagnostics.WriteConsole("Server says: use /login secret-password");
            diagnostics.WriteConsole("login secret-password<--[HERE]");
            diagnostics.WriteConsole("register secret-password secret-password<--[HERE]");

            string transcript = ReadTranscript(diagnostics);
            Assert.Contains("ordinary line", transcript, StringComparison.Ordinal);
            Assert.DoesNotContain("/login", transcript, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("/register", transcript, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("secret-password", transcript, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(logsRoot, recursive: true);
        }
    }

    private static string ReadTranscript(SessionDiagnostics diagnostics)
    {
        // The session writer remains open. Windows readers must allow its write access.
        using var stream = new FileStream(
            Path.Combine(diagnostics.Folder, "console.log"),
            FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static SessionDiagnostics Open(string logsRoot)
    {
        var settings = new DiagnosticsSettings(
            Enabled: true,
            CapturePackets: false,
            MaxCaptureMegabytes: 20,
            KeepSessions: 5);
        return Assert.IsType<SessionDiagnostics>(SessionDiagnostics.TryOpen(logsRoot, settings, "localhost"));
    }

    private static string CreateTempFolder()
    {
        string folder = Path.Combine(Path.GetTempPath(), "mcc-session-diagnostics-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return folder;
    }
}
