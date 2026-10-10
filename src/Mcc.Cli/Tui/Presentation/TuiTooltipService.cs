using Mcc.Cli.Tui.Hosting;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace Mcc.Cli.Tui.Presentation;

/// <summary>One line of a <see cref="TuiTooltipService"/> tooltip.</summary>
/// <param name="Text">The line text.</param>
/// <param name="Foreground">The line's foreground brush.</param>
public sealed record TuiTooltipLine(string Text, IBrush Foreground);

/// <summary>
/// A global floating tooltip layer, one per <see cref="MainTuiView"/> (see <see cref="MainTuiView.GetTooltipService"/>), used by the minimap hover and any future hover source.
/// A borderless hit-test-transparent <see cref="Canvas"/> moves into Avalonia's overlay layer when available, keeping it above managed windows, and <see cref="Show"/> positions a bordered text block at the given point, auto-flipping left/right so it never runs off the screen.
/// </summary>
internal sealed class TuiTooltipService
{
    private readonly Panel _rootPanel;
    private readonly Visual _overlayAnchor;
    private readonly Canvas _canvas;
    private readonly Border _border;
    private readonly StackPanel _content;
    private Panel _layer;

    internal TuiTooltipService(Panel rootPanel, Visual? overlayAnchor = null)
    {
        ArgumentNullException.ThrowIfNull(rootPanel);
        _rootPanel = rootPanel;
        _overlayAnchor = overlayAnchor ?? rootPanel;

        _content = new StackPanel { Orientation = Orientation.Vertical };
        _border = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(230, 20, 20, 20)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
            BorderThickness = new Avalonia.Thickness(1),
            Padding = new Avalonia.Thickness(1),
            Child = _content,
            IsVisible = false,
        };

        _canvas = new Canvas
        {
            IsHitTestVisible = false,
            Children = { _border },
        };
        _canvas.ZIndex = int.MaxValue;
        rootPanel.Children.Add(_canvas);
        _layer = rootPanel;
    }

    /// <summary>Whether the tooltip is currently shown.</summary>
    public bool IsVisible => _border.IsVisible;

    /// <summary>
    /// Shows (or repositions) the tooltip at (<paramref name="x"/>, <paramref name="y"/>) in root-panel coordinates.
    /// Empty <paramref name="lines"/> hides it instead.
    /// <paramref name="preferRight"/> tries placing the tooltip to the right of the point first, flipping to the left when it would overflow the root panel's width (and vice versa when false).
    /// </summary>
    public void Show(double x, double y, IReadOnlyList<TuiTooltipLine> lines, bool preferRight = true)
    {
        ArgumentNullException.ThrowIfNull(lines);
        AttachAboveManagedWindows();
        _content.Children.Clear();

        if (lines.Count == 0)
        {
            _border.IsVisible = false;
            return;
        }

        int maxChars = 0;
        foreach (TuiTooltipLine line in lines)
        {
            _content.Children.Add(new TextBlock
            {
                Text = line.Text,
                Foreground = line.Foreground,
                TextWrapping = TextWrapping.NoWrap,
                Padding = new Avalonia.Thickness(0),
                Margin = new Avalonia.Thickness(0),
            });

            if (line.Text.Length > maxChars)
                maxChars = line.Text.Length;
        }

        double tipW = maxChars + 4;
        double screenW = _rootPanel.Bounds.Width;

        const double gap = 1;
        double gx = preferRight ? x + gap : x - tipW - gap;
        if (preferRight && gx + tipW > screenW)
            gx = x - tipW - gap;
        else if (!preferRight && gx < 0)
            gx = x + gap;

        Canvas.SetLeft(_border, Math.Max(0, gx));
        Canvas.SetTop(_border, Math.Max(0, y));
        _border.IsVisible = true;
    }

    private void AttachAboveManagedWindows()
    {
        Panel target = OverlayLayer.GetOverlayLayer(_overlayAnchor) ?? _rootPanel;
        if (!ReferenceEquals(target, _layer))
        {
            _layer.Children.Remove(_canvas);
            target.Children.Add(_canvas);
            _layer = target;
        }

        // Managed windows use the overlay layer too.
        // Keep hover content above their chrome and body, regardless of which window was most recently activated.
        _canvas.ZIndex = int.MaxValue;
    }

    /// <summary>Hides the tooltip and clears its content.</summary>
    public void Hide()
    {
        _border.IsVisible = false;
        _content.Children.Clear();
    }
}
