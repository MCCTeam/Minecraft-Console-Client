using Mcc.Cli.Localization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Iciclecreek.Avalonia.WindowManager;

namespace Mcc.Cli.Tui.Minimap;

/// <summary>
/// Non-modal Consolonia managed window for the live minimap.
/// Its title bar supplies native pointer dragging, focus and close behavior; the configured position remains useful as an initial or command-selected anchor.
/// </summary>
internal sealed class MinimapWindow : ManagedWindow
{
    private const int EdgeMargin = 1;

    private readonly Control _windowSurface;
    private MinimapPosition _anchor;
    private bool _initialSizeCaptured;

    internal MinimapWindow(MinimapControl content, Control windowSurface, MinimapPosition anchor)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(windowSurface);

        _windowSurface = windowSurface;
        _anchor = anchor;

        Title = Strings.MinimapWindowTitle;
        Content = content;
        Padding = new Thickness(0);
        SizeToContent = SizeToContent.WidthAndHeight;
        WindowStartupLocation = WindowStartupLocation.Manual;
        CanResize = true;
        MinWidth = 12;
        // Eight map rows, two footer rows, plus the managed frame/title.
        MinHeight = 12;
        ShowActivated = false;
        AnimateWindow = false;

        Opened += (_, _) => PostInitialLayout();
    }

    /// <summary>Moves the window back to one of the command/configuration anchor presets.</summary>
    internal void MoveTo(MinimapPosition anchor)
    {
        _anchor = anchor;
        PostAnchorPosition();
    }

    private void PostAnchorPosition()
        => Dispatcher.UIThread.Post(ApplyAnchorPosition, DispatcherPriority.Loaded);

    private void PostInitialLayout()
        => Dispatcher.UIThread.Post(() =>
        {
            if (!_initialSizeCaptured)
            {
                // Let SizeToContent establish the configured starting size once.
                // Manual sizing is required after that for drag-resize and the managed window's maximize/restore commands.
                Size initial = Bounds.Size;
                SizeToContent = SizeToContent.Manual;
                Width = Math.Max(MinWidth, initial.Width);
                Height = Math.Max(MinHeight, initial.Height);
                _initialSizeCaptured = true;
            }

            ApplyAnchorPosition();
        }, DispatcherPriority.Loaded);

    private void ApplyAnchorPosition()
    {
        Size surfaceSize = _windowSurface.Bounds.Size;
        Size windowSize = Bounds.Size;
        if (surfaceSize.Width <= 0 || surfaceSize.Height <= 0
            || windowSize.Width <= 0 || windowSize.Height <= 0)
            return;

        Position = ResolveAnchor(_anchor, surfaceSize, windowSize);
    }

    /// <summary>Pure anchor calculation, kept separate so edge clamping is regression-testable.</summary>
    internal static PixelPoint ResolveAnchor(MinimapPosition anchor, Size surfaceSize, Size windowSize)
    {
        int maxX = Math.Max(0, (int)Math.Floor(surfaceSize.Width - windowSize.Width));
        int maxY = Math.Max(0, (int)Math.Floor(surfaceSize.Height - windowSize.Height));
        int left = Math.Min(EdgeMargin, maxX);
        int top = Math.Min(EdgeMargin, maxY);
        int right = Math.Max(0, maxX - EdgeMargin);
        int bottom = Math.Max(0, maxY - EdgeMargin);

        return anchor switch
        {
            MinimapPosition.TopLeft => new PixelPoint(left, top),
            MinimapPosition.BottomLeft => new PixelPoint(left, bottom),
            MinimapPosition.BottomRight => new PixelPoint(right, bottom),
            MinimapPosition.Center => new PixelPoint(maxX / 2, maxY / 2),
            _ => new PixelPoint(right, top),
        };
    }
}
