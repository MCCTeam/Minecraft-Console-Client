using Mcc.Cli.Presentation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Umpk.Text;

namespace Mcc.Cli.Tui.Presentation;

/// <summary>
/// Renders a UMPK <see cref="Component"/> tree into an Avalonia <see cref="SelectableTextBlock"/> of styled <see cref="Run"/> inlines for the Consolonia TUI.
/// This is the TUI counterpart of <see cref="AnsiComponentRenderer"/> (which emits ANSI for the classic host): both are thin sinks over <see cref="ComponentFlattener"/>'s walk (style inheritance, translation-key resolution, legacy section-sign decoding); only the sink differs (Consolonia brushes/decorations instead of SGR codes).
/// It replaces the legacy TUI <c>McColorParser</c>, which parsed section codes rather than the Component model.
/// </summary>
internal sealed class ComponentInlineRenderer
{
    private readonly ITranslationSource _translations;
    private readonly bool _color;

    /// <summary>
    /// Builds a renderer.
    /// <paramref name="color"/> is the host's resolved colour capability (<see cref="TerminalCapability.ResolveColor"/>): with colour off, a component's own <c>color</c> style is dropped rather than turned into a brush.
    /// The monochrome console colour mode would flatten it anyway, but not emitting it at all is what makes "the server sent red" observable as an absence in the rendered inline tree rather than only in the terminal bytes.
    /// </summary>
    public ComponentInlineRenderer(ITranslationSource translations, bool color = true)
    {
        ArgumentNullException.ThrowIfNull(translations);
        _translations = translations;
        _color = color;
    }

    /// <summary>Builds a wrapping, selectable text control rendering the component.</summary>
    public SelectableTextBlock Render(Component component, TextWrapping wrapping = TextWrapping.Wrap)
    {
        ArgumentNullException.ThrowIfNull(component);
        var block = new SelectableTextBlock
        {
            TextWrapping = wrapping,
            // Uncoloured Minecraft components are ordinary chat text.
            // Set the foreground explicitly so they do not inherit Consolonia's dim theme foreground against the black chat pane.
            Foreground = Brushes.White
        };
        ComponentFlattener.Flatten(
            component, new InlineSink(block.Inlines!, _color), ComponentFlattenOptions.For(_translations));
        if (block.Inlines!.Count == 0)
            block.Text = string.Empty;

        return block;
    }

    /// <summary>Flattens a component to plain text (no styling), for tooltips and single-line labels.</summary>
    public string PlainText(Component component)
    {
        ArgumentNullException.ThrowIfNull(component);
        return component.ToPlainText(_translations);
    }

    // Turns one resolved run into a Consolonia inline.
    // Reference-semantic (wraps the InlineCollection), so copying the struct through ComponentFlattener's recursive walk is safe.
    private readonly struct InlineSink(InlineCollection sink, bool color) : IStyledRunSink
    {
        public void Accept(in StyledRun run)
        {
            if (run.Text.Length == 0)
                return;

            var inline = new Run(run.Text);
            if (color && run.Style.Color is TextColor textColor)
            {
                int rgb = textColor.Rgb;
                inline.Foreground = new SolidColorBrush(
                    Color.FromRgb((byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF)));
            }

            if (run.Style.Bold == true)
                inline.FontWeight = FontWeight.Bold;

            if (run.Style.Italic == true)
                inline.FontStyle = FontStyle.Italic;

            var decorations = new TextDecorationCollection();
            if (run.Style.Underlined == true)
                decorations.Add(new TextDecoration { Location = TextDecorationLocation.Underline });

            if (run.Style.Strikethrough == true)
                decorations.Add(new TextDecoration { Location = TextDecorationLocation.Strikethrough });

            if (decorations.Count > 0)
                inline.TextDecorations = decorations;

            sink.Add(inline);
        }
    }
}
