using Mcc.Cli.Localization;
using Mcc.Cli.Tui.Hosting;
using Mcc.Cli.Tui.Terminal;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Mcc.Cli.Tui.Plugins;

internal enum PluginManagerStartPage
{
    Installed,
    Marketplaces,
}

/// <summary>
/// A page inside the plugin manager.
/// <see cref="OnEscapeKey"/> lets a page consume Escape itself (a confirm page answers "no" instead of navigating back); false takes the default Back.
/// </summary>
internal interface IOverlayPage
{
    /// <summary>Whether the manager should scroll the whole page instead of giving it a fixed viewport.</summary>
    bool UseWorkspaceScroll => true;

    /// <summary>Handles Escape; true when the page consumed it.</summary>
    bool OnEscapeKey() => false;
}

/// <summary>A page that can rebuild itself when navigated back to (lists over live state).</summary>
internal interface IRefreshablePage
{
    /// <summary>Re-reads the host/market and rebuilds the rows.</summary>
    Task RefreshAsync();
}

/// <summary>
/// The visual plugin manager behind <c>plugins ui</c>: a fullscreen overlay with a page stack (list, detail, settings, install, updates, marketplaces, search, small tools), an inline modal layer for confirms and short forms, and a status + hints bar.
/// Escape walks back, closing at the root.
/// <para>
/// Pages rebuild on navigation (forward always builds fresh; Back rebuilds the list), so no page ever shows a stale state and the settings editor's abandon-on-leave rule holds without bookkeeping.
/// </para>
/// </summary>
internal sealed class PluginManagerOverlay : Border
{
    private readonly PluginManagerContext _ctx;
    private readonly ContentControl _pageHost;
    private readonly ContentControl _pageViewport;
    private readonly ScrollViewer _pageScroll;
    private readonly TextBlock _status;
    private readonly TextBlock _hints;
    private readonly Border _modalLayer;
    private readonly Button _backButton;
    private readonly Stack<Control> _pages = new();

    public PluginManagerOverlay(
        MainTuiView view,
        TuiBackend backend,
        DMCBK.Core.Client client,
        PluginManagerStartPage startPage = PluginManagerStartPage.Installed)
    {
        _ctx = new PluginManagerContext(this, view, backend, client);

        _pageHost = new ContentControl
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
        };
        _pageScroll = new ScrollViewer
        {
            Content = _pageHost,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            // Disabled, not Auto: with horizontal scrolling on, pages measure infinitely wide, TextWrapping never kicks in, and long lines run off the edge (the install confirm's source path).
            // Constrained width wraps them like every other surface.
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        _pageViewport = new ContentControl
        {
            Content = _pageScroll,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
        };

        _status = new TextBlock
        {
            Foreground = PluginUi.Gold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(1, 0),
        };
        _hints = new TextBlock
        {
            Foreground = PluginUi.Muted,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(1, 0),
        };

        _backButton = PluginUi.Button(Strings.PmBack, RequestBack, PluginButtonKind.Secondary);
        var close = PluginUi.Button(Strings.PmClose, _ctx.Close, PluginButtonKind.Quiet);
        var identity = new StackPanel
        {
            Spacing = 1,
            Margin = new Thickness(1, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        identity.Children.Add(new TextBlock
        {
            Text = Strings.PmManagerKicker,
            Foreground = PluginUi.Cyan,
            FontWeight = FontWeight.Bold,
        });
        identity.Children.Add(new TextBlock
        {
            Text = Strings.PmManagerSubtitle,
            Foreground = PluginUi.Soft,
            TextWrapping = TextWrapping.Wrap,
        });
        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            Background = PluginUi.Panel,
            Margin = new Thickness(0, 0, 0, 1),
        };
        header.Children.Add(_backButton);
        Grid.SetColumn(identity, 1);
        header.Children.Add(identity);
        Grid.SetColumn(close, 2);
        header.Children.Add(close);

        // A DockPanel, not a StackPanel: a vertical StackPanel measures children with infinite height, so the ScrollViewer sized to its full content, never scrolled, and anything past the viewport (the confirm's buttons) was unreachable.
        // Docked status/hints leave the scroller the finite remainder, which is what makes it scroll.
        var content = new DockPanel();
        DockPanel.SetDock(header, Dock.Top);
        DockPanel.SetDock(_status, Dock.Bottom);
        DockPanel.SetDock(_hints, Dock.Bottom);
        content.Children.Add(header);
        content.Children.Add(_status);
        content.Children.Add(_hints);
        content.Children.Add(_pageViewport);

        var grid = new Grid();
        grid.Children.Add(content);
        _modalLayer = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(140, 0, 0, 0)),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            IsVisible = false,
        };
        grid.Children.Add(_modalLayer);

        BorderBrush = PluginUi.CyanDark;
        BorderThickness = new Thickness(1);
        Background = PluginUi.Canvas;
        Padding = new Thickness(1);
        Margin = new Thickness(0);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        Child = grid;
        Focusable = true;

        KeyDown += OnKeyDown;
        Show(new PluginListPage(_ctx));
        if (startPage == PluginManagerStartPage.Marketplaces)
            Show(new PluginMarketplacesPage(_ctx));
    }

    /// <summary>Pushes a page, replacing what is shown (the stack keeps it for Back).</summary>
    internal void Show(Control page)
    {
        ArgumentNullException.ThrowIfNull(page);
        if (_pageHost.Content is Control current)
            _pages.Push(current);

        SetPage(page);
        UpdateNavigation();
    }

    /// <summary>Returns to the previous page, rebuilding a stale list; closes at the root.</summary>
    internal void Back()
    {
        // A modal owns its own Escape (handled before this runs), so reaching here with one open means a programmatic Back: drop the modal, keep the page.
        if (HasModal)
        {
            HideModal();
            return;
        }

        if (_pages.Count == 0)
        {
            _ctx.View.HideOverlay();
            return;
        }

        Control page = _pages.Pop();
        SetPage(page);
        if (page is IRefreshablePage refreshable)
            _ = refreshable.RefreshAsync();

        UpdateNavigation();
    }

    /// <summary>Replaces the current page without touching the stack (state refresh).</summary>
    internal void ReplaceTop(Control page)
    {
        ArgumentNullException.ThrowIfNull(page);
        SetPage(page);
        UpdateNavigation();
    }

    /// <summary>Drops the whole stack and shows a fresh list (after an answered confirm).</summary>
    internal void BackToList()
    {
        HideModal();
        _pages.Clear();
        var list = new PluginListPage(_ctx);
        SetPage(list);
        _ = list.RefreshAsync();
        UpdateNavigation();
    }

    /// <summary>Shows an inline modal (confirm, short form) over the current page.</summary>
    internal void ShowModal(Control dialog)
    {
        ArgumentNullException.ThrowIfNull(dialog);
        _modalLayer.Child = dialog;
        _modalLayer.IsVisible = true;
        dialog.Focus();
    }

    /// <summary>Hides the inline modal, if one is open.</summary>
    internal void HideModal()
    {
        _modalLayer.IsVisible = false;
        _modalLayer.Child = null;
    }

    /// <summary>Whether an inline modal is currently open.</summary>
    internal bool HasModal => _modalLayer.IsVisible;

    /// <summary>Sets the status line (an action's outcome).</summary>
    internal void SetStatus(string text) => _status.Text = text ?? string.Empty;

    /// <summary>Sets the keyboard hints bar.</summary>
    internal void SetHints(string text) => _hints.Text = text ?? string.Empty;

    /// <summary>Runs page-aware Back behavior, including declining an active install confirmation.</summary>
    internal void RequestBack()
    {
        if (HasModal)
        {
            HideModal();
            return;
        }

        if (_pageHost.Content is IOverlayPage page
            && page.OnEscapeKey())
            return;

        Back();
    }

    private void UpdateNavigation()
        => _backButton.IsVisible = _pages.Count > 0;

    private void SetPage(Control page)
    {
        SetPageScrollMode(
            _pageViewport,
            _pageScroll,
            _pageHost,
            (page as IOverlayPage)?.UseWorkspaceScroll ?? true);
        _pageHost.Content = page;
    }

    internal static void SetPageScrollMode(
        ContentControl viewport,
        ScrollViewer scroll,
        ContentControl pageHost,
        bool useWorkspaceScroll)
        => TuiViewport.SetPageScrollMode(viewport, scroll, pageHost, useWorkspaceScroll);

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // A modal owns Escape while open (it marks the event handled); anything unhandled here is a page-level key or the global Back.
        if (e.Key is Key.Escape && !e.Handled)
        {
            RequestBack();
            e.Handled = true;
        }
    }
}
