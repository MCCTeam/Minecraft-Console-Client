using DMCBK.Core;
using Mcc.Cli.Hosting.Classic;
using Mcc.Cli.Localization;
using Mcc.Cli.Logging;
using Mcc.Cli.Presentation;
using Mcc.Cli;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Mcc.Cli.Tests.Hosting;

/// <summary>
/// MCC's own words carry <c>[MCC]</c> so they scan apart from typed input and server output, and the two notices a fresh user must act on carry color.
/// Plain text when color is off, so redirected logs and <c>NO_COLOR</c> runs stay clean.
/// </summary>
[Collection("console-io")]
public sealed class CliHostPrefixTests
{
    [Fact]
    public void Host_PrefixesEveryLine()
    {
        Assert.Equal("[MCC] a\n[MCC]\n[MCC] b", Strings.Host("a\n\nb"));
    }

    [Fact]
    public void Host_KeepsSameLinePromptSpacing()
    {
        Assert.Equal("[MCC] Paste: ", Strings.Host("Paste: "));
    }

    [Fact]
    public void IdleNoServer_Disabled_IsPlainWithPrefix()
    {
        Assert.Equal(
            "[MCC] Not connected: no server is configured. "
            + "Type '/connect <host[:port]>', or add one to servers.toml.",
            Strings.IdleNoServer(ConsoleColorDepth.Disable));
    }

    [Fact]
    public void IdleNoServer_FullColor_IsRedWithAYellowCommand()
    {
        string colored = Strings.IdleNoServer(ConsoleColorDepth.Vt10024Bit);

        Assert.StartsWith("[MCC] ", colored, StringComparison.Ordinal); // the stamp itself stays plain
        Assert.Contains("38;2;255;85;85", colored, StringComparison.Ordinal); // red
        Assert.Contains("38;2;255;255;85", colored, StringComparison.Ordinal); // yellow
        Assert.Equal(Strings.IdleNoServer(ConsoleColorDepth.Disable), Ansi.Strip(colored));
    }

    [Fact]
    public void IdleNoServer_FourBit_StillStripsToPlain()
    {
        string colored = Strings.IdleNoServer(ConsoleColorDepth.Vt1004Bit);

        Assert.Contains("\u001b[", colored, StringComparison.Ordinal); // a real SGR escape, not a bracket
        Assert.Equal(Strings.IdleNoServer(ConsoleColorDepth.Disable), Ansi.Strip(colored));
    }

    [Fact]
    public void IdleAutoConnectOff_NamesTheAddress()
    {
        string plain = Strings.IdleAutoConnectOff("play.example.com:25565", ConsoleColorDepth.Disable);

        Assert.StartsWith("[MCC] ", plain, StringComparison.Ordinal);
        Assert.Contains("play.example.com:25565", plain, StringComparison.Ordinal);
        Assert.Contains("'/reco'", plain, StringComparison.Ordinal);
    }

    [Fact]
    public void GuidedOnlineDeferred_Disabled_IsPlainWithPrefix()
    {
        Assert.Equal(
            "[MCC] Sign-in will be requested when you connect to a server. "
            + "Type '/connect <host[:port]>', or add one to servers.toml.",
            Strings.GuidedOnlineDeferred(ConsoleColorDepth.Disable));
    }

    [Fact]
    public void GuidedOnlineDeferred_FullColor_IsLightBlue()
    {
        string colored = Strings.GuidedOnlineDeferred(ConsoleColorDepth.Vt10024Bit);

        Assert.StartsWith("[MCC] ", colored, StringComparison.Ordinal); // the stamp itself stays plain
        Assert.Contains("38;2;85;255;255", colored, StringComparison.Ordinal); // bright vanilla aqua
        Assert.Equal(Strings.GuidedOnlineDeferred(ConsoleColorDepth.Disable), Ansi.Strip(colored));
    }

    [Fact]
    public void PluginLines_CarryMccInsteadOfPlugins()
    {
        Assert.Equal("[MCC] Loaded 0/0 plugin(s).", Strings.PluginsLoaded("Loaded 0/0 plugin(s)."));
        Assert.StartsWith("[MCC] ", Strings.PluginsLoadError("boom"), StringComparison.Ordinal);
        Assert.DoesNotContain("[plugins]", Strings.PluginsLoaded("x"), StringComparison.Ordinal);
    }

    [Fact]
    public void ClassicBanner_Disabled_IsPlainWithoutTheProjectUrl()
    {
        string plain = Strings.ClassicBanner("v2.0.0", "1.8.9", "26.3", ConsoleColorDepth.Disable);

        Assert.Equal("Minecraft Console Client vv2.0.0 - for MC 1.8.9 to 26.3", plain);
        Assert.DoesNotContain("Github", plain, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ClassicBanner_FullColor_IsLightGray()
    {
        string colored = Strings.ClassicBanner("v2.0.0", "1.8.9", "26.3", ConsoleColorDepth.Vt10024Bit);

        Assert.Contains("38;2;170;170;170", colored, StringComparison.Ordinal); // light gray
        Assert.Equal(Strings.ClassicBanner("v2.0.0", "1.8.9", "26.3", ConsoleColorDepth.Disable), Ansi.Strip(colored));
    }

    [Fact]
    public void StarUs_CarriesTheLinkWithEmojiOnlyWhenSupported()
    {
        const string url = "https://github.com/MCCTeam/Minecraft-Console-Client";

        Assert.Contains(url, Strings.StarUs(emoji: true), StringComparison.Ordinal);
        Assert.StartsWith("⭐ ", Strings.StarUs(emoji: true), StringComparison.Ordinal);

        Assert.Contains(url, Strings.StarUs(emoji: false), StringComparison.Ordinal);
        Assert.DoesNotContain("⭐", Strings.StarUs(emoji: false), StringComparison.Ordinal);
    }

    [Fact]
    public void WriteErrorLine_NonRich_GoesToStderr()
    {
        // Pre-rich (redirected, file-input, one-shots) there is no ring to keep consistent, so the stderr split is preserved for the harness.
        using var stderr = new StringWriter();
        TextWriter saved = Console.Error;
        Console.SetError(stderr);
        try
        {
            HostConsole.WriteErrorLine("[MCC] boom");
        }
        finally
        {
            Console.SetError(saved);
        }

        Assert.Contains("[MCC] boom", stderr.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeviceCodeLine_HasNoInputPromptPrefix()
    {
        // The reported defect: the device-code line printed as "> [MCC] To sign in, ...", with the live input prompt leaked onto it by a raw Console write.
        // Through HostConsole the writer clears the input area first, so the line starts with the stamp.
        using var stdout = new StringWriter();
        TextWriter saved = Console.Out;
        Console.SetOut(stdout);
        try
        {
            var interaction = new ConsoleAuthInteraction(ConsoleColorDepth.Disable);
            await interaction.ShowDeviceCodeAsync(
                new Umpk.Auth.DeviceCodePrompt(
                    "TV8DPLYN",
                    new Uri("https://www.microsoft.com/link"),
                    "message",
                    DateTimeOffset.UtcNow.AddMinutes(15)),
                CancellationToken.None);
        }
        finally
        {
            Console.SetOut(saved);
        }

        string text = stdout.ToString();
        Assert.StartsWith("[MCC] To sign in, open", text, StringComparison.Ordinal);
        Assert.DoesNotContain(">", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Logger_InfoPrefix_IsPlainMcc()
    {
        using var stdout = new StringWriter();
        TextWriter savedOut = Console.Out;
        Console.SetOut(stdout);
        try
        {
            var factory = new ConsoleLoggerFactory(LogLevel.Information, ConsoleColorDepth.Vt10024Bit);
            factory.CreateLogger("test").LogInformation("hello");
        }
        finally
        {
            Console.SetOut(savedOut);
        }

        Assert.StartsWith("[MCC] hello", stdout.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Logger_WarnPrefix_KeepsItsColor()
    {
        using var stdout = new StringWriter();
        TextWriter savedOut = Console.Out;
        Console.SetOut(stdout);
        try
        {
            var factory = new ConsoleLoggerFactory(LogLevel.Warning, ConsoleColorDepth.Vt10024Bit);
            factory.CreateLogger("test").LogWarning("careful");
        }
        finally
        {
            Console.SetOut(savedOut);
        }

        string text = stdout.ToString();
        Assert.Contains("[WARN]", text, StringComparison.Ordinal);
        Assert.Contains("\u001b[", text, StringComparison.Ordinal);
    }
}
