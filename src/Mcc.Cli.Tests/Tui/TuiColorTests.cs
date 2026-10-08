using Mcc.Cli.Localization;
using Mcc.Cli.Presentation;
using Mcc.Cli.Tui.Hosting;
using Mcc.Cli.Tui.Presentation;
using Mcc.Cli.Tui.Terminal;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Media;
using Mcc.Cli;
using Mcc.Cli.Tui;
using DMCBK.Core;
using DMCBK.Core.Configuration;
using DMCBK.Core.Localization;
using Umpk.Client.Events;
using Umpk.Text;
using Xunit;

namespace Mcc.Cli.Tests.Tui;

/// <summary>
/// The TUI emitted colour unconditionally.
/// <c>TuiHost</c> constructed its chat standing marker with <c>color: true</c> as a literal and the inline renderer wrote each component's RGB with no gate above it, so <c>NO_COLOR=1</c> and <c>console.toml</c>'s <c>ConsoleColorMode = "disable"</c> both had no effect at all in the very host the setting that selects the TUI lives beside.
/// With both switches set, a red <c>tellraw</c> rendered the same as with colour forced on.
/// <para>
/// What "no colour" can mean here is not the same as for the classic host, and these tests pin the answer.
/// A Consolonia application paints a grid of cells and always writes an attribute per cell, so it cannot stop emitting ANSI the way a line-oriented writer can; the achievable and useful meaning is MONOCHROME CELL ATTRIBUTES.
/// <see cref="MonochromeConsoleColorMode"/> is installed at Consolonia's own colour-mapping seam, which is what makes the switch complete rather than partial: MCC's own brushes, Consolonia's theme, the minimap and the server's chat colours all pass through it.
/// </para>
/// </summary>
public sealed class TuiColorTests
{
    // --------------------------------------------------------------------------------------------------- The whole-surface switch ---------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(0xFF, 0x55, 0x55)] // vanilla red
    [InlineData(0x00, 0xAA, 0x00)] // vanilla dark green
    [InlineData(0x1E, 0x90, 0xFF)] // an arbitrary hex colour
    public void Monochrome_FlattensEveryHue(byte r, byte g, byte b)
    {
        Color flat = MonochromeConsoleColorMode.Flatten(Color.FromRgb(r, g, b));

        Assert.Equal(flat.R, flat.G);
        Assert.Equal(flat.G, flat.B);
    }

    [Fact]
    public void Monochrome_MapsBothCellAttributes_NotJustTheForeground()
    {
        var mode = new MonochromeConsoleColorMode();
        (object foreground, object background) = mode.MapColors(Colors.Red, Colors.Blue, null);

        var fg = Assert.IsType<Color>(foreground);
        var bg = Assert.IsType<Color>(background);
        Assert.Equal(fg.R, fg.B);
        Assert.Equal(bg.R, bg.B);

        // And the two are still distinguishable, so a coloured-on-coloured cell does not collapse into an unreadable one.
        // Red and blue have very different luma.
        Assert.NotEqual(fg.R, bg.R);
    }

    [Fact]
    public void Monochrome_PreservesContrastOrdering()
    {
        // The layout leans on relative brightness (the selected suggestion is inverted, panels are darker than the text on them).
        // Flattening must not reorder any of that.
        Color white = MonochromeConsoleColorMode.Flatten(Colors.White);
        Color gray = MonochromeConsoleColorMode.Flatten(Colors.Gray);
        Color black = MonochromeConsoleColorMode.Flatten(Colors.Black);

        Assert.True(white.R > gray.R);
        Assert.True(gray.R > black.R);
        Assert.Equal(255, white.R);
        Assert.Equal(0, black.R);
    }

    [Fact]
    public void Monochrome_LeavesATransparentCellAlone()
    {
        // A transparent cell is the terminal's own background showing through, which is the least coloured thing available; repainting it grey would ADD colour where there was none.
        Color flat = MonochromeConsoleColorMode.Flatten(Colors.Transparent);
        Assert.Equal(Colors.Transparent, flat);
    }

    [Fact]
    public void Monochrome_IsIdempotent()
    {
        Color once = MonochromeConsoleColorMode.Flatten(Colors.Red);
        Assert.Equal(once, MonochromeConsoleColorMode.Flatten(once));
    }

    // --------------------------------------------------------------------------------------------------- The rendered output differs with the switch ---------------------------------------------------------------------------------------------------

    [Fact]
    public void InlineRenderer_EmitsTheComponentColour_WhenColourIsOn()
    {
        Run run = SingleRun(color: true, TextColor.Red);

        var brush = Assert.IsType<SolidColorBrush>(run.Foreground);
        Assert.Equal(Color.FromRgb(0xFF, 0x55, 0x55), brush.Color);
    }

    [Fact]
    public void InlineRenderer_DropsTheComponentColour_WhenColourIsOff()
    {
        Run run = SingleRun(color: false, TextColor.Red);

        // Not "the foreground is null": an Avalonia Run inherits a default brush, so the assertion that distinguishes the fixed behavior is that nothing was set on it.
        // Without the fix the property carries the component's own red.
        Assert.False(run.IsSet(TextElement.ForegroundProperty));
        Assert.Equal("T5ONLYRED", run.Text);
    }

    [Fact]
    public void InlineRenderer_DefaultsUncolouredChatToWhite()
    {
        var renderer = new ComponentInlineRenderer(HostTranslations.CreateDefault());

        TextBlock block = renderer.Render(Component.Text("<boban> this is a test"));

        Assert.IsType<SelectableTextBlock>(block);
        var brush = Assert.IsAssignableFrom<ISolidColorBrush>(block.Foreground);
        Assert.Equal(Colors.White, brush.Color);
    }

    [Theory]
    [InlineData(KeyModifiers.Control, true)]
    [InlineData(KeyModifiers.Control | KeyModifiers.Shift, true)]
    [InlineData(KeyModifiers.None, false)]
    [InlineData(KeyModifiers.Alt, false)]
    public void MainMenuShortcut_RequiresControlM(KeyModifiers modifiers, bool expected)
        => Assert.Equal(expected, MainTuiView.IsMainMenuGesture(Key.M, modifiers));

    [Fact]
    public void MainMenuShortcut_DoesNotCaptureOtherControlKeys()
        => Assert.False(MainTuiView.IsMainMenuGesture(Key.N, KeyModifiers.Control));

    [Fact]
    public void MainMenuShortcut_AcceptsTerminalNormalizedControlM()
        => Assert.True(MainTuiView.IsMainMenuGesture(Key.Enter, KeyModifiers.Control));

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    public void MainMenuShortcut_AcceptsPlainCarriageReturnOnlyAtAnEmptyMainPrompt(
        bool hasOverlay,
        bool inputIsEmpty,
        bool expected)
        => Assert.Equal(
            expected,
            MainTuiView.ShouldOpenMainMenu(Key.Enter, KeyModifiers.None, hasOverlay, inputIsEmpty));

    [Fact]
    public void MainMenuShortcut_PreservesOverlayControlEnter()
        => Assert.False(MainTuiView.ShouldOpenMainMenu(
            Key.Enter, KeyModifiers.Control, hasOverlay: true, inputIsEmpty: true));

    [Fact]
    public void StatusBar_RendersExperienceAfterFood()
    {
        var block = new TextBlock();
        block.Inlines ??= [];

        StatusBarBuilder.Build(
            block.Inlines,
            health: 20,
            food: 18,
            experienceLevel: 7,
            experienceProgress: 0.4f,
            totalExperience: 123,
            effects: [],
            showEffectNames: false,
            HostTranslations.CreateDefault());

        string text = string.Concat(block.Inlines.OfType<Run>().Select(static run => run.Text));
        int foodIndex = text.IndexOf("18", StringComparison.Ordinal);
        int experienceIndex = text.IndexOf("[████░░░░░░]", StringComparison.Ordinal);
        Assert.True(foodIndex >= 0);
        Assert.True(experienceIndex > foodIndex);
        Assert.Contains("Lv 7 (123 XP)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void InlineRenderer_KeepsDecorations_WhenColourIsOff()
    {
        // Bold and italic are not colour, and a monochrome pane needs them MORE, not less: they are the only emphasis left once hue is gone.
        var style = new Style { Color = TextColor.Red, Bold = true, Italic = true };
        var component = new Component(new TextContent("T5ONLYRED"), style);
        var renderer = new ComponentInlineRenderer(HostTranslations.CreateDefault(), color: false);

        Run run = Assert.IsType<Run>(Assert.Single(renderer.Render(component).Inlines!));
        Assert.False(run.IsSet(TextElement.ForegroundProperty));
        Assert.Equal(FontWeight.Bold, run.FontWeight);
        Assert.Equal(FontStyle.Italic, run.FontStyle);
    }

    // --------------------------------------------------------------------------------------------------- The standing marker ---------------------------------------------------------------------------------------------------

    [Fact]
    public void StandingMark_WithoutColour_IsAWordTag_AndOnlyForWhatMatters()
    {
        // MarkInsecureMsg is off by default (an offline-mode server signs nothing, so it would mark every line); turn it on so both hosts' handling of the same standing is comparable here.
        var config = new SignatureConfig { MarkInsecureMsg = true };
        var coloured = new ChatStandingMarker(config, color: true);
        var plain = new ChatStandingMarker(config, color: false);

        // With colour, every marked standing is the same bar and the colour carries the meaning.
        Assert.Equal(Strings.ChatStandingBar, coloured.Mark(Chat(ChatVerification.Verified))?.Text);
        Assert.Equal(Strings.ChatStandingBar, coloured.Mark(Chat(ChatVerification.Insecure))?.Text);

        // Without it, a grey bar would say nothing about WHICH standing it is, so the normal case is silent and the abnormal ones are named.
        // This is the classic host's existing rule; the TUI now shares it instead of asserting that colour is always available.
        Assert.Null(plain.Mark(Chat(ChatVerification.Verified)));
        Assert.Equal(Strings.ChatStandingInsecure, plain.Mark(Chat(ChatVerification.Insecure))?.Text);
        Assert.Equal(Strings.ChatStandingRejected, plain.Mark(Chat(ChatVerification.Failed))?.Text);
    }

    private static Run SingleRun(bool color, TextColor textColor)
    {
        var component = new Component(new TextContent("T5ONLYRED"), new Style { Color = textColor });
        var renderer = new ComponentInlineRenderer(HostTranslations.CreateDefault(), color);
        TextBlock block = renderer.Render(component);
        return Assert.IsType<Run>(Assert.Single(block.Inlines!));
    }

    private static ChatMessageReceived Chat(ChatVerification verification)
        => new(Component.Text("hello"), ChatCategory.Player, Component.Text("Tester"), Guid.Empty, false, verification);
}
