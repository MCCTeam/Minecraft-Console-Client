using DMCBK.Core;
using Mcc.Cli.Hosting;
using Mcc.Cli.Localization;
using Mcc.Cli.Presentation;
using Mcc.Cli.Startup;
using Mcc.Cli;
using DMCBK.Core.Presentation;
using Xunit;

namespace Mcc.Cli.Tests.Startup;

/// <summary>
/// The <c>--help</c> / <c>--help-short</c> one-shots and the friendly page itself.
/// The page's examples are <c>Mcc.Cli</c> invocations, so the content assertions pin the exact contract the parser implements: a stale example here is a lie the user can copy.
/// </summary>
public sealed class CliHelpTests
{
    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("-?")]
    [InlineData("/?")]
    [InlineData("--help-short")]
    public void HelpFlags_AreHandled(string flag)
    {
        using var stdout = new StringWriter();
        TextWriter saved = Console.Out;
        Console.SetOut(stdout);
        try
        {
            Assert.True(CliHelp.TryHandle([flag], out int exitCode));
            Assert.Equal(HostExit.Clean, exitCode);
        }
        finally
        {
            Console.SetOut(saved);
        }
    }

    [Fact]
    public void OrdinaryArgs_AreNotHandled()
    {
        Assert.False(CliHelp.TryHandle(["Steve", "-", "play.example.com"], out _));
        Assert.False(CliHelp.TryHandle([], out _));
    }

    [Fact]
    public void HelpShort_PrintsTheCompactReference()
    {
        using var stdout = new StringWriter();
        TextWriter saved = Console.Out;
        Console.SetOut(stdout);
        try
        {
            Assert.True(CliHelp.TryHandle(["--help-short"], out _));
        }
        finally
        {
            Console.SetOut(saved);
        }

        Assert.Contains("Usage: Mcc.Cli [<username>]", stdout.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Every example on the page must survive the parser it documents: each one is re-parsed here with the slots it claims, so a reordered contract breaks this test, not a user.
    /// </summary>
    [Fact]
    public void HelpExamples_ParseIntoTheClaimedSlots()
    {
        Assert.True(CliArguments.TryParse(
            ["Steve", "-", "play.example.com"], out CliArguments basic, out _));
        Assert.Equal("Steve", basic.Overrides.Username);
        Assert.Equal("-", basic.Overrides.Password);
        Assert.Equal("play.example.com", basic.Overrides.Address);

        Assert.True(CliArguments.TryParse(
            ["--configurations", "./my-config", "Steve", "-", "play.example.com:25565"],
            out CliArguments configured, out _));
        Assert.Equal("./my-config", configured.ConfigArg);
        Assert.Equal("Steve", configured.Overrides.Username);
        Assert.Equal("play.example.com:25565", configured.Overrides.Address);

        Assert.True(CliArguments.TryParse(
            ["--auth", "microsoft", "Steve", "-", "play.example.com"],
            out CliArguments authed, out _));
        Assert.Equal("microsoft", authed.Overrides.AuthMode);

        Assert.True(CliArguments.TryParse(
            ["-v", "1.21.5", "Steve", "-", "play.example.com"],
            out CliArguments pinned, out _));
        Assert.Equal("1.21.5", pinned.Overrides.Version);

        Assert.True(CliArguments.TryParse(
            ["--exercise", "smoke", "Steve", "-", "play.example.com"],
            out CliArguments exercised, out _));
        Assert.Equal("smoke", exercised.Exercise);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HelpMarkdown_RendersInBothGlyphModes(bool emoji)
    {
        var renderer = new MarkdownConsoleRenderer(
            ConsoleColorDepth.Disable, emoji ? MccGlyphs.Emoji : GlyphSet.Ascii);
        IReadOnlyList<string> lines = renderer.RenderLines(Strings.HelpMarkdown(emoji), 76);

        Assert.NotEmpty(lines);
        Assert.Contains(lines, l => l.Contains("Minecraft Console Client", StringComparison.Ordinal));
        Assert.Contains(lines, l => l.Contains("--configurations", StringComparison.Ordinal));
    }

    [Fact]
    public void HelpMarkdown_KeepsTheHouseStyle()
    {
        foreach (bool emoji in new[] { true, false })
        {
            string page = Strings.HelpMarkdown(emoji);
            Assert.DoesNotContain('—', page); // em dash
        }
    }
}
