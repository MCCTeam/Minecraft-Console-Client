using Mcc.Cli.Presentation;
using Mcc.Cli;
using DMCBK.Core.Localization;
using Umpk.Text;
using Xunit;

namespace Mcc.Cli.Tests.Presentation;

/// <summary>
/// The classic host's Component -> ANSI renderer.
/// Proves named and hex colors and decorations map to the expected SGR escape sequences, that colors-off yields clean plain text (and strips embedded legacy section codes rather than leaking them), that legacy section-sign strings render through the same ANSI path, and that translation keys resolve through the core translation source with styled argument substitution.
/// </summary>
public sealed class AnsiComponentRendererTests
{
    private const char Section = '\u00a7';
    private const string Esc = "\u001b";

    private static ITranslationSource Translations(params (string Key, string Template)[] entries)
    {
        var overrides = new Dictionary<string, string>();
        foreach ((string key, string template) in entries)
            overrides[key] = template;

        return new HostTranslations(overrides);
    }

    [Fact]
    public void NamedColor_MapsTo24BitForeground()
    {
        var renderer = new AnsiComponentRenderer(Translations(), depth: ConsoleColorDepth.Vt10024Bit);
        var component = new Component(new TextContent("hi"), new Style { Color = TextColor.Red });

        // red = 0xFF5555 -> 255;85;85
        Assert.Equal($"{Esc}[38;2;255;85;85mhi{Esc}[0m", renderer.Render(component));
    }

    [Fact]
    public void HexColorAndDecorations_MapToSeparateSgrSequences()
    {
        // Separate sequences, not the semicolon-joined "[38;2;30;144;255;1;4m".
        // Both paint identically; only the split form survives the suggestion popup, whose restore buffer classifies each escape with anchored colour-only regexes and discards anything else.
        // See SuggestionPopupRestoreTests.
        var renderer = new AnsiComponentRenderer(Translations(), depth: ConsoleColorDepth.Vt10024Bit);
        var style = new Style { Color = TextColor.FromRgb(0x1E90FF), Bold = true, Underlined = true };
        var component = new Component(new TextContent("x"), style);

        Assert.Equal($"{Esc}[38;2;30;144;255m{Esc}[1m{Esc}[4mx{Esc}[0m", renderer.Render(component));
    }

    [Fact]
    public void PlainText_HasNoEscapes_WhenStyleEmpty()
    {
        var renderer = new AnsiComponentRenderer(Translations(), depth: ConsoleColorDepth.Vt10024Bit);
        Assert.Equal("plain", renderer.Render(Component.Text("plain")));
    }

    [Fact]
    public void ColorDisabled_YieldsPlainText()
    {
        var renderer = new AnsiComponentRenderer(Translations(), depth: ConsoleColorDepth.Disable);
        var component = new Component(new TextContent("hi"), new Style { Color = TextColor.Green, Bold = true });

        Assert.Equal("hi", renderer.Render(component));
    }

    [Fact]
    public void LegacySectionString_RendersThroughAnsiPath()
    {
        var renderer = new AnsiComponentRenderer(Translations(), depth: ConsoleColorDepth.Vt10024Bit);
        var component = Component.Text($"{Section}cred");

        Assert.Equal($"{Esc}[38;2;255;85;85mred{Esc}[0m", renderer.Render(component));
    }

    [Fact]
    public void LegacySectionString_StrippedInPlainMode()
    {
        var renderer = new AnsiComponentRenderer(Translations(), depth: ConsoleColorDepth.Disable);
        var component = Component.Text($"{Section}agreen{Section}rtail");

        Assert.Equal("greentail", renderer.Render(component));
    }

    [Fact]
    public void ChildrenInheritParentStyle()
    {
        var renderer = new AnsiComponentRenderer(Translations(), depth: ConsoleColorDepth.Vt10024Bit);
        var child = new Component(new TextContent("b"), Style.Empty);
        var parent = new Component(new TextContent("a"), new Style { Bold = true }, [child]);

        // Both runs are bold: parent's own text, then the child that inherits bold.
        Assert.Equal($"{Esc}[1ma{Esc}[0m{Esc}[1mb{Esc}[0m", renderer.Render(parent));
    }

    [Fact]
    public void TranslatableKey_ResolvesWithStyledArgs()
    {
        var renderer = new AnsiComponentRenderer(Translations(("test.chat", "<%s> %s")), depth: ConsoleColorDepth.Disable);
        var component = new Component(new TranslatableContent(
            "test.chat", null, [Component.Text("Steve"), Component.Text("hello")]));

        Assert.Equal("<Steve> hello", renderer.Render(component));
    }

    [Fact]
    public void TranslatableKey_UnknownUsesFallback()
    {
        var renderer = new AnsiComponentRenderer(Translations(), depth: ConsoleColorDepth.Disable);
        var component = new Component(new TranslatableContent(
            "missing.key", "fallback %s", [Component.Text("arg")]));

        Assert.Equal("fallback arg", renderer.Render(component));
    }

    [Fact]
    public void TranslatableArg_KeepsItsOwnColor()
    {
        var renderer = new AnsiComponentRenderer(Translations(("test.one", "hi %s")), depth: ConsoleColorDepth.Vt10024Bit);
        var arg = new Component(new TextContent("Steve"), new Style { Color = TextColor.Yellow });
        var component = new Component(new TranslatableContent("test.one", null, [arg]));

        // yellow = 0xFFFF55 -> 255;255;85.
        // The literal "hi " has no style, the arg is colored.
        Assert.Equal($"hi {Esc}[38;2;255;255;85mSteve{Esc}[0m", renderer.Render(component));
    }

    [Fact]
    public void LegacySectionString_UnderABoldParent_ClearsBold_PerVanilla()
    {
        // Vanilla applyLegacyFormat (1.20.4-decompiled/net/minecraft/network/chat/Style.java:316-321): a colour code resets bold/italic/underline/strikethrough/obfuscated to explicit false, it does not merely leave them unset for an enclosing style to keep showing through.
        // A bold PARENT component whose own text carries a legacy colour code must therefore render that text NOT bold, which the old Component.Append/AppendText walk got wrong: LegacyText.Parse's per-run style left decorations null rather than false, so ApplyTo's null-coalescing re-inherited the parent's bold.
        var renderer = new AnsiComponentRenderer(Translations(), depth: ConsoleColorDepth.Vt10024Bit);
        var component = new Component(new TextContent($"{Section}cred"), new Style { Bold = true });

        Assert.Equal($"{Esc}[38;2;255;85;85mred{Esc}[0m", renderer.Render(component));
    }

    [Fact]
    public void LegacySectionReset_UnderAColouredParent_RestoresTheParentColour()
    {
        // Vanilla StringDecomposer resets the running style to the BASE style on section-r, not to an empty style; the base here is the colour the enclosing component itself carries.
        // A colour code earlier in the same legacy-coded string must not permanently clobber the parent's colour for text that follows a reset.
        var renderer = new AnsiComponentRenderer(Translations(), depth: ConsoleColorDepth.Vt10024Bit);
        var component = new Component(
            new TextContent($"{Section}chi {Section}rrestored"), new Style { Color = TextColor.Blue });

        // red = 0xFF5555 -> 255;85;85, blue = 0x5555FF -> 85;85;255.
        Assert.Equal(
            $"{Esc}[38;2;255;85;85mhi {Esc}[0m{Esc}[38;2;85;85;255mrestored{Esc}[0m",
            renderer.Render(component));
    }
}
