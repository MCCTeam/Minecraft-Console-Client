using Mcc.Cli.Localization;
using Mcc.Cli.Tui.Hosting;
using Mcc.Cli.Tui.Terminal;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace Mcc.Cli.Tui.Management;

internal interface IManagementWorkspacePage
{
    bool UseWorkspaceScroll => true;
    bool HasLocalBackNavigation => false;
    string? Hints => null;
    bool HandleBack() => false;
    void FocusSearch() { }
    void Refresh() { }
    void SetCompact(bool compact) { }
}

/// <summary>Shared fullscreen chrome, navigation, modal, scrolling, and cancellation for management tools.</summary>
internal sealed class ManagementWorkspace : Border, IDisposable
{
    internal const double WideThreshold = 100;

    internal static bool CompactForWidth(double width) => width < WideThreshold;

    private readonly MainTuiView _view;
    private readonly Button _backButton;
    private readonly ContentControl _pageHost;
    private readonly ContentControl _pageViewport;
    private readonly ScrollViewer _pageScroll;
    private readonly Border _modalLayer;
    private readonly TextBlock _status;
    private readonly TextBlock _hints;
    private readonly Stack<Control> _pages = new();
    private readonly CancellationTokenSource _lifetime = new();
    private bool _disposed;
    private bool _compact;

    public ManagementWorkspace(MainTuiView view, string title, string subtitle)
    {
        _view = view;

        var identity = new StackPanel
        {
            // A three-row block keeps the two text lines balanced around the exact row used by the Back/Close labels: title, centre row, subtitle.
            Spacing = 1,
            Margin = new Thickness(1, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        identity.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = ManagementUi.Cyan,
            FontWeight = FontWeight.Bold,
        });
        identity.Children.Add(new TextBlock
        {
            Text = subtitle,
            Foreground = ManagementUi.Soft,
            TextWrapping = TextWrapping.Wrap,
        });

        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            RowDefinitions = new RowDefinitions("1,3,1"),
            Background = ManagementUi.Panel,
            Margin = new Thickness(0, 0, 0, 1),
        };
        _backButton = ManagementUi.HeaderButton(
            Strings.MgmtBack, RequestBack, ManagementButtonKind.Secondary);
        _backButton.IsVisible = false;
        Grid.SetRow(_backButton, 1);
        header.Children.Add(_backButton);
        Grid.SetRow(identity, 1);
        Grid.SetColumn(identity, 1);
        header.Children.Add(identity);
        Button close = ManagementUi.HeaderButton(
            Strings.MgmtClose, Close, ManagementButtonKind.Quiet);
        Grid.SetRow(close, 1);
        Grid.SetColumn(close, 2);
        header.Children.Add(close);

        _pageHost = new ContentControl
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
        };
        _pageScroll = new ScrollViewer
        {
            Content = _pageHost,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };
        _pageViewport = new ContentControl
        {
            Content = _pageScroll,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
        };

        _status = new TextBlock
        {
            Foreground = ManagementUi.Gold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(1, 0),
        };
        _hints = new TextBlock
        {
            Text = Strings.MgmtHints,
            Foreground = ManagementUi.Muted,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(1, 0),
        };

        var content = new DockPanel();
        DockPanel.SetDock(header, Dock.Top);
        DockPanel.SetDock(_status, Dock.Bottom);
        DockPanel.SetDock(_hints, Dock.Bottom);
        content.Children.Add(header);
        content.Children.Add(_status);
        content.Children.Add(_hints);
        content.Children.Add(_pageViewport);

        var root = new Grid();
        root.Children.Add(content);
        _modalLayer = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(150, 0, 0, 0)),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            IsVisible = false,
        };
        root.Children.Add(_modalLayer);

        BorderBrush = ManagementUi.CyanDark;
        BorderThickness = new Thickness(1);
        Background = ManagementUi.Canvas;
        Padding = new Thickness(1);
        Margin = new Thickness(0);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        Child = root;
        Focusable = true;
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    public CancellationToken Closed => _lifetime.Token;

    public bool IsCompact => _compact;

    public void SetRoot(Control page)
    {
        _pages.Clear();
        SetCurrent(page);
    }

    public void Push(Control page)
    {
        if (_pageHost.Content is Control current)
            _pages.Push(current);

        SetCurrent(page);
    }

    public void SetStatus(string? text) => _status.Text = text ?? string.Empty;

    public void ShowModal(Control modal)
    {
        _modalLayer.Child = modal;
        _modalLayer.IsVisible = true;
        modal.Focus();
    }

    public void HideModal()
    {
        _modalLayer.IsVisible = false;
        _modalLayer.Child = null;
        (_pageHost.Content as Control)?.Focus();
    }

    public void RequestBack()
    {
        if (_modalLayer.IsVisible)
        {
            HideModal();
            return;
        }

        if (_pageHost.Content is IManagementWorkspacePage page && page.HandleBack())
        {
            RefreshNavigation();
            return;
        }

        if (_pages.Count == 0)
        {
            Close();
            return;
        }

        DisposeCurrent();
        SetCurrent(_pages.Pop());
    }

    public void Close()
    {
        Dispose();
        _view.HideOverlay();
    }

    internal void RefreshNavigation()
    {
        bool localBack = (_pageHost.Content as IManagementWorkspacePage)?.HasLocalBackNavigation == true;
        _backButton.IsVisible = ShouldShowBack(_pages.Count, localBack);
    }

    internal static bool ShouldShowBack(int pageDepth, bool localBack)
        => pageDepth > 0 || localBack;

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _lifetime.Cancel();
        DisposeCurrent();
        foreach (Control page in _pages)
            (page as IDisposable)?.Dispose();
        _pages.Clear();
        _lifetime.Dispose();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        bool compact = CompactForWidth(e.NewSize.Width);
        if (compact == _compact)
            return;

        _compact = compact;
        if (_pageHost.Content is IManagementWorkspacePage page)
        {
            page.SetCompact(compact);
            RefreshNavigation();
        }
    }

    private void SetCurrent(Control page)
    {
        SetPageScrollMode(
            _pageViewport,
            _pageScroll,
            _pageHost,
            (page as IManagementWorkspacePage)?.UseWorkspaceScroll ?? true);
        _pageHost.Content = page;
        if (page is IManagementWorkspacePage workspacePage)
        {
            _hints.Text = workspacePage.Hints ?? Strings.MgmtHints;
            workspacePage.SetCompact(_compact);
        }
        else
            _hints.Text = Strings.MgmtHints;
        RefreshNavigation();
        page.Focus();
    }

    internal static void SetPageScrollMode(
        ContentControl viewport,
        ScrollViewer scroll,
        ContentControl pageHost,
        bool useWorkspaceScroll)
        => TuiViewport.SetPageScrollMode(viewport, scroll, pageHost, useWorkspaceScroll);

    private void DisposeCurrent() => (_pageHost.Content as IDisposable)?.Dispose();

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_modalLayer.IsVisible && e.Key == Key.Escape)
        {
            HideModal();
            e.Handled = true;
            return;
        }

        if (_pageHost.Content is not IManagementWorkspacePage page)
            return;

        if (e.Key == Key.Escape)
        {
            RequestBack();
            e.Handled = true;
        }
        else if (e.Key == Key.F5)
        {
            page.Refresh();
            e.Handled = true;
        }
        else if (e.Key == Key.F && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            page.FocusSearch();
            e.Handled = true;
        }
    }
}
