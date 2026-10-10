using Mcc.Cli.Localization;
using Mcc.Cli.Tui.Hosting;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using DMCBK.Core;
using DMCBK.Core.Commands;

namespace Mcc.Cli.Tui.Overlays;

/// <summary>
/// The book editor overlay behind <see cref="IHostUi.TryOpenBookEditor"/>.
/// It renders the current pages in a multiline editor with page navigation and a title field; on save it calls the existing <see cref="InventoryApi.EditBookAsync"/> path (the command owns the action; the hook only supplies the edited pages/title).
/// Signed books are read-only.
/// Ported behavior from the legacy <c>BookTuiHost</c>.
/// </summary>
internal sealed class BookOverlay : Border
{
    private readonly MainTuiView _view;
    private readonly GameApi _game;
    private readonly TuiBackend _backend;
    private readonly bool _signed;
    private readonly List<string> _pages;
    private readonly TextBox _pageEditor;
    private readonly TextBox _titleEditor;
    private readonly TextBlock _header;
    private readonly TextBlock _message;
    private readonly TextBlock _status;
    private int _page;

    // the protocol-derived page/title-length caps, re-fetched once from the live session (MaxPages is flat 100 in every era, so the PageDown cap below is correct even before the fetch resolves; only the per-page and title lengths, used at save time, actually depend on it).
    private BookLimits _limits = BookLimits.ForProtocol(0);

    // tracks unsaved edits for the Escape-confirm flow.
    // Suppressed while ShowPage() sets the editor text programmatically, so paging alone never counts as an edit.
    private bool _suppressChangeTracking;
    private bool _dirty;
    private bool _closeConfirmPending;

    private BookOverlay(MainTuiView view, GameApi game, TuiBackend backend, BookEditorRequest request)
    {
        _view = view;
        _game = game;
        _backend = backend;
        _signed = request.Signed;
        _pages = request.Pages.Count > 0 ? [.. request.Pages] : [string.Empty];

        _header = new TextBlock { Foreground = Brushes.Cyan, FontWeight = FontWeight.Bold };
        _message = new TextBlock { Foreground = Brushes.OrangeRed };
        _status = new TextBlock { Foreground = Brushes.DarkGray, Text = Strings.TuiBookControls };

        _titleEditor = new TextBox
        {
            Text = request.Title ?? string.Empty,
            IsEnabled = !_signed,
            Watermark = Strings.TuiBookSignPrompt,
        };
        _titleEditor.TextChanged += (_, _) =>
        {
            if (!_suppressChangeTracking)
                _dirty = true;
        };

        _pageEditor = new TextBox
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            IsReadOnly = _signed,
            MinHeight = 8,
        };
        _pageEditor.TextChanged += (_, _) =>
        {
            _pages[_page] = _pageEditor.Text ?? string.Empty;
            if (!_suppressChangeTracking)
                _dirty = true;
        };

        var titleRow = new DockPanel();
        var titleLabel = new TextBlock { Text = Strings.TuiBookTitleLabel, Foreground = Brushes.Gray };
        DockPanel.SetDock(titleLabel, Dock.Left);
        titleRow.Children.Add(titleLabel);
        titleRow.Children.Add(_titleEditor);

        var stack = new StackPanel();
        stack.Children.Add(_header);
        if (_signed)
            stack.Children.Add(new TextBlock { Text = Strings.TuiBookSigned, Foreground = Brushes.Gold });

        stack.Children.Add(_pageEditor);
        stack.Children.Add(titleRow);
        stack.Children.Add(_message);
        stack.Children.Add(_status);

        BorderBrush = Brushes.DarkCyan;
        BorderThickness = new Avalonia.Thickness(1);
        Background = new SolidColorBrush(Color.FromRgb(20, 18, 12));
        Padding = new Avalonia.Thickness(1);
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;
        Width = 60;
        Child = stack;
        Focusable = true;
        KeyDown += OnKeyDown;

        ShowPage();
        _ = RefreshLimitsAsync();
    }

    public static void Open(MainTuiView view, GameApi game, TuiBackend backend, BookEditorRequest request)
    {
        var overlay = new BookOverlay(view, game, backend, request);
        view.ShowOverlay(overlay);
        overlay._pageEditor.Focus();
    }

    private void ShowPage()
    {
        _suppressChangeTracking = true;
        _header.Text = Strings.TuiBookPageHeader(_page + 1, _pages.Count);
        _pageEditor.Text = _pages[_page];
        _suppressChangeTracking = false;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        bool ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        switch (e.Key)
        {
            case Key.Escape:
                // Unsaved edits ask once (a confirm hint).
                // A second Escape discards and closes even if further edits occurred after the first Escape.
                if (_dirty && !_closeConfirmPending)
                {
                    _closeConfirmPending = true;
                    _message.Text = Strings.TuiBookUnsavedConfirm;
                    e.Handled = true;
                    break;
                }

                _view.HideOverlay();
                e.Handled = true;
                break;

            case Key.PageUp:
                if (_page > 0)
                {
                    _page--;
                    ShowPage();
                }

                e.Handled = true;
                break;

            case Key.PageDown:
                if (_page == _pages.Count - 1)
                {
                    // cap page creation at the protocol's max-pages limit rather than letting the editor grow a book the server will reject outright at save time.
                    if (_pages.Count >= _limits.MaxPages)
                    {
                        _message.Text = Strings.TuiBookMaxPagesReached(_limits.MaxPages);
                        e.Handled = true;
                        break;
                    }

                    _pages.Add(string.Empty);
                    _dirty = true;
                }

                _page++;
                ShowPage();
                e.Handled = true;
                break;

            case Key.S when ctrl && !_signed:
                _ = SaveAsync(null);
                e.Handled = true;
                break;

            case Key.G when ctrl && !_signed:
                _ = SaveAsync(string.IsNullOrWhiteSpace(_titleEditor.Text) ? null : _titleEditor.Text);
                e.Handled = true;
                break;
        }
    }

    /// <summary>Re-reads the negotiated protocol's book limits from the live session.</summary>
    private async Task RefreshLimitsAsync()
    {
        try
        {
            SessionInfoSnapshot info = await _game.Session.GetInfoAsync().ConfigureAwait(true);
            _limits = BookLimits.ForProtocol(info.Protocol);
        }
        catch (Exception ex) when (ex is DmcbkNotInSessionException or DmcbkFeatureDisabledException)
        {
            // No live session (rare: the overlay only opens from a session-driven hook); keep the conservative pre-1.17 default until one exists.
        }
    }

    /// <summary>
    /// Validates the book against <see cref="_limits"/> before it is sent: page count, every page's length, and (only when signing, i.e. <paramref name="title"/> is not null) the title length.
    /// Ported from legacy's <c>BookView.Validate</c> (MinecraftClient/Tui/BookTuiHost.cs:307-334).
    /// </summary>
    private bool Validate(string? title, out string error)
    {
        error = string.Empty;

        if (_pages.Count > _limits.MaxPages)
        {
            error = Strings.TuiBookTooManyPages(_pages.Count, _limits.MaxPages);
            return false;
        }

        for (int i = 0; i < _pages.Count; i++)
        {
            if (_pages[i].Length > _limits.MaxPageLength)
            {
                error = Strings.TuiBookPageTooLong(i + 1, _pages[i].Length, _limits.MaxPageLength);
                return false;
            }
        }

        if (title is not null && (title.Length == 0 || title.Length > _limits.MaxTitleLength))
        {
            error = Strings.TuiBookTitleTooLong(_limits.MaxTitleLength);
            return false;
        }

        return true;
    }

    private async Task SaveAsync(string? title)
    {
        if (!Validate(title, out string error))
        {
            _message.Text = error;
            return;
        }

        try
        {
            int slot = await _game.Inventory.GetHeldSlotAsync().ConfigureAwait(true);

            // EditBookAsync reports whether the negotiated version can carry the edit at all; on one that cannot, UMPK drops it with a log warning, so announcing "Book saved." would be a false success.
            if (!await _game.Inventory.EditBookAsync(slot, _pages, title).ConfigureAwait(true))
            {
                _backend.WriteLine(Strings.TuiBookEditUnsupported);
                return;
            }

            _dirty = false;
            _closeConfirmPending = false;
            _message.Text = string.Empty;
            _backend.WriteLine(Strings.TuiBookSaved);
            _backend.Post(_view.HideOverlay);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _backend.WriteLine(Strings.SendFailed(ex.Message));
        }
    }
}
