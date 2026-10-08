using Mcc.Cli.Presentation;
using Mcc.Cli;
using DMCBK.Core;
using DMCBK.Core.Localization;
using Umpk.Protocol.Java;
using Xunit;

namespace Mcc.Cli.Tests.Presentation;

/// <summary>
/// The connect-time status panel's greens are vanilla §a bright green, matching legacy's <c>ServerStatusDisplay</c> (which colors these rows " §a") and the old TUI palette.
/// Every other color on the panel stays exactly where legacy put it.
/// </summary>
public sealed class ServerStatusPanelTests
{
    private const string StatusJson = """
        {"version":{"name":"1.21.11","protocol":774},
         "players":{"max":20,"online":1,"sample":[{"name":"Steve","id":"00000000-0000-0000-0000-000000000000"}]},
         "description":{"text":"A Minecraft Server"}}
        """;

    private static IReadOnlyList<string> Build(ConsoleColorDepth depth)
    {
        ServerStatus status = ServerStatus.Parse(StatusJson, TimeSpan.FromMilliseconds(2));
        var args = new ServerStatusReceivedEventArgs(status, "localhost", 25565, resolvedVersion: null);
        var renderer = new AnsiComponentRenderer(new HostTranslations(), depth);
        return ServerStatusPanel.BuildLines(args, renderer, depth);
    }

    [Fact]
    public void PanelGreens_AreVanillaBrightGreen()
    {
        string text = string.Join('\n', Build(ConsoleColorDepth.Vt10024Bit));

        // Bright green §a on the ping, the player count and the sample name.
        Assert.Contains("38;2;85;255;85", text, StringComparison.Ordinal);
    }

    [Fact]
    public void OtherPanelColors_AreUntouched()
    {
        string text = string.Join('\n', Build(ConsoleColorDepth.Vt10024Bit));

        Assert.Contains("38;2;85;255;255", text, StringComparison.Ordinal); // aqua host/version
        Assert.Contains("38;2;255;255;85", text, StringComparison.Ordinal); // yellow protocol
        Assert.Contains("A Minecraft Server", text, StringComparison.Ordinal); // MOTD survives
    }

    [Fact]
    public void ColorDisabled_EmitsNoEscapes()
    {
        string text = string.Join('\n', Build(ConsoleColorDepth.Disable));

        Assert.DoesNotContain("\u001b[", text, StringComparison.Ordinal);
        Assert.Contains("1/20", text, StringComparison.Ordinal);
    }
}
