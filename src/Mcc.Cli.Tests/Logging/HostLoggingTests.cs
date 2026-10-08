using Mcc.Cli.Logging;
using Mcc.Cli.Presentation;
using Mcc.Cli;
using DMCBK.Core.Configuration;
using Xunit;

namespace Mcc.Cli.Tests.Logging;

/// <summary>
/// Logging seam: the classic host's ANSI stripper, the configurable log filter (FilterMode), and the log file sink (PrependTimestamp + SaveColorCodes color stripping).
/// </summary>
public sealed class HostLoggingTests
{
    private const string Esc = "\u001b";

    [Fact]
    public void AnsiStrip_RemovesSgrSequences()
    {
        string colored = $"{Esc}[38;2;255;85;85mred{Esc}[0m plain {Esc}[1mbold{Esc}[0m";
        Assert.Equal("red plain bold", Ansi.Strip(colored));
    }

    [Fact]
    public void AnsiStrip_LeavesPlainTextUntouched()
    {
        Assert.Equal("nothing here", Ansi.Strip("nothing here"));
    }

    [Fact]
    public void LogFilter_Disable_ShowsEverything()
    {
        var filter = new LogFilter("secret", LogFilterMode.Disable);
        Assert.True(filter.ShouldShow("a secret line"));
        Assert.True(filter.ShouldShow("anything"));
    }

    [Fact]
    public void LogFilter_Blacklist_HidesMatches()
    {
        var filter = new LogFilter("secret", LogFilterMode.Blacklist);
        Assert.False(filter.ShouldShow("a secret line"));
        Assert.True(filter.ShouldShow("a public line"));
    }

    [Fact]
    public void LogFilter_Whitelist_ShowsOnlyMatches()
    {
        var filter = new LogFilter("keep", LogFilterMode.Whitelist);
        Assert.True(filter.ShouldShow("please keep this"));
        Assert.False(filter.ShouldShow("drop this"));
    }

    [Fact]
    public void LogFilter_InvalidRegex_FailsOpen()
    {
        var filter = new LogFilter("(unclosed", LogFilterMode.Whitelist);
        Assert.True(filter.ShouldShow("anything at all"));
    }

    [Fact]
    public void LogFileSink_StripsColor_WhenSaveColorCodesFalse()
    {
        string path = Path.Combine(Path.GetTempPath(), $"mcc-log-{Guid.NewGuid():N}.txt");
        try
        {
            using (var sink = new LogFileSink(path, prependTimestamp: false, saveColorCodes: false))
                sink.Write($"{Esc}[1mhello{Esc}[0m");

            Assert.Equal("hello", File.ReadAllText(path).TrimEnd('\r', '\n'));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LogFileSink_KeepsColor_WhenSaveColorCodesTrue()
    {
        string path = Path.Combine(Path.GetTempPath(), $"mcc-log-{Guid.NewGuid():N}.txt");
        try
        {
            using (var sink = new LogFileSink(path, prependTimestamp: false, saveColorCodes: true))
                sink.Write($"{Esc}[1mhello{Esc}[0m");

            Assert.Equal($"{Esc}[1mhello{Esc}[0m", File.ReadAllText(path).TrimEnd('\r', '\n'));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LogFileSink_PrependsTimestamp_WhenConfigured()
    {
        string path = Path.Combine(Path.GetTempPath(), $"mcc-log-{Guid.NewGuid():N}.txt");
        try
        {
            using (var sink = new LogFileSink(path, prependTimestamp: true, saveColorCodes: true))
                sink.Write("line");

            string written = File.ReadAllText(path).TrimEnd('\r', '\n');
            Assert.Matches(@"^\[\d{2}:\d{2}:\d{2}\] line$", written);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
