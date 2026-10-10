using Mcc.Cli.Localization;
using Mcc.Cli.Tui.Hosting;
using Mcc.Cli.Tui.Presentation;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using DMCBK.Core;
using DMCBK.Core.Localization;
using Umpk.Game.Inventory;
using Umpk.Text;

namespace Mcc.Cli.Tui.Container;

/// <summary>
/// The container/inventory overlay behind <see cref="DMCBK.Core.Commands.IHostUi.TryOpenContainerView"/>.
/// It renders the open window's slots (fed by <see cref="InventoryApi.GetOpenContainerAsync"/>) via a typed layout chosen from the container's <c>MenuTypeId</c> (furnace, brewing, hopper, crafting, enchanting, grindstone, or a generic grid), refreshing on a timer, and routes slot clicks through the existing window-action APIs (<see cref="InventoryApi.PickupAsync"/> / <see cref="InventoryApi.QuickMoveAsync"/>).
/// Ported behavior from the legacy container views; fed only by the core snapshot.
/// </summary>
internal sealed class ContainerOverlay : Border
{
    /// <summary>Vanilla's player inventory is always window 0.</summary>
    private const int PlayerWindowId = 0;

    private static readonly IReadOnlyDictionary<int, int> EmptyProperties =
        new Dictionary<int, int>();

    private readonly MainTuiView _view;
    private readonly GameApi _game;
    private readonly TuiBackend _backend;
    private readonly ITranslationSource _translations;
    private readonly int _windowId;
    private readonly Action? _openRecipes;
    private readonly TextBlock _title;
    private readonly TextBlock _cursor;
    private readonly Panel _body;
    private readonly DispatcherTimer _timer;
    private readonly List<SlotCell> _cells = new();
    private ContainerLayout? _layout;

    // the window slot the pointer is currently over, or null; Q/Ctrl+Q drop from this slot.
    private int? _hoveredSlot;

    // The semantic key the current layout was built from.
    // Two different kinds can share a slot count (a grindstone and a furnace are both three slots), so the slot count alone is not enough to decide whether the built layout is still the right one.
    private string? _layoutKey;

    // Set when the window this view shows is known to be gone already: the server closed it, the snapshot stopped matching, or the session hopped to another server.
    // See MarkWindowGone.
    private bool _windowGone;
    private bool _preserveWindowOnClose;

    private ContainerOverlay(MainTuiView view, GameApi game, TuiBackend backend, int windowId, Action? openRecipes)
    {
        _view = view;
        _game = game;
        _backend = backend;
        _translations = ResolveTranslations(backend);
        _windowId = windowId;
        _openRecipes = openRecipes;

        _title = new TextBlock
        {
            Foreground = Brushes.Cyan,
            FontWeight = FontWeight.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };
        _cursor = new TextBlock { Foreground = Brushes.Gold };
        _body = new StackPanel();

        var closeButton = new Button
        {
            Content = Strings.TuiContainerClose,
            Padding = new Thickness(1, 0),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brushes.Cyan,
            Background = new SolidColorBrush(Color.FromRgb(31, 42, 48)),
            BorderBrush = Brushes.DarkCyan,
            BorderThickness = new Thickness(1)
        };
        closeButton.Click += (_, _) => _view.HideOverlay();

        var recipesButton = new Button
        {
            Content = Strings.RecipeUiInventoryLink,
            Padding = new Thickness(1, 0),
            Foreground = Brushes.Cyan,
            Background = new SolidColorBrush(Color.FromRgb(31, 42, 48)),
            BorderBrush = Brushes.DarkCyan,
            BorderThickness = new Thickness(1),
            IsVisible = _openRecipes is not null,
        };
        recipesButton.Click += (_, _) =>
        {
            _preserveWindowOnClose = true;
            _view.HideOverlay();
            _openRecipes?.Invoke();
        };

        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto") };
        header.Children.Add(_title);
        Grid.SetColumn(recipesButton, 1);
        header.Children.Add(recipesButton);
        Grid.SetColumn(closeButton, 2);
        header.Children.Add(closeButton);

        var footer = new StackPanel();
        footer.Children.Add(_cursor);
        footer.Children.Add(new TextBlock
        {
            Text = Strings.TuiContainerControls,
            Foreground = Brushes.DarkGray,
            TextWrapping = TextWrapping.Wrap
        });

        var scrollViewer = new ScrollViewer
        {
            Content = _body,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var content = new DockPanel();
        DockPanel.SetDock(header, Dock.Top);
        DockPanel.SetDock(footer, Dock.Bottom);
        content.Children.Add(header);
        content.Children.Add(footer);
        content.Children.Add(scrollViewer);

        BorderBrush = Brushes.DarkCyan;
        BorderThickness = new Thickness(1);
        Background = new SolidColorBrush(Color.FromRgb(15, 15, 22));
        Padding = new Thickness(1);
        Margin = new Thickness(1);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        Child = content;
        Focusable = true;
        KeyDown += OnKeyDown;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        // Same reason as MainTuiView's status timer: a tick runs on the UI thread and the synchronous prefix of RefreshAsync reaches into the session loop.
        _timer.Tick += (_, _) => _ = Task.Run(RefreshAsync);
    }

    /// <summary>The window this overlay is showing, so a host can close the right one.</summary>
    public int WindowId => _windowId;

    /// <summary>
    /// Records that the window is already gone, so dismissing this view does not tell the server to close a container it has no record of.
    /// </summary>
    /// <remarks>
    /// Dismissing a container view normally means "I am done with this chest", and sending <c>container_close</c> is the whole point.
    /// When the window went away on its own it is the opposite: after a proxy's backend switch the frame would reach a DIFFERENT server, addressing a window id that server never issued.
    /// </remarks>
    public void MarkWindowGone() => _windowGone = true;

    public static void Open(
        MainTuiView view,
        GameApi game,
        TuiBackend backend,
        int windowId,
        Action? openRecipes = null)
    {
        var overlay = new ContainerOverlay(view, game, backend, windowId, openRecipes);
        view.ShowOverlay(overlay, overlay.OnClosed);
        overlay._timer.Start();
        _ = Task.Run(overlay.RefreshAsync);
    }

    private void OnClosed()
    {
        _timer.Stop();

        // Task.Run, not a bare fire-and-forget call.
        // HideOverlay invokes this INLINE on the UI thread, and an async method runs synchronously up to its first await: evaluating Inventory.CloseAsync() hands work to the session loop and can block there, which held the UI thread and made Esc take about a second to visibly close the overlay.
        // The overlay is already detached by the time this runs, so nothing here needs to be ordered against the redraw.
        _ = Task.Run(CloseWindowAsync);
    }

    private async Task CloseWindowAsync()
    {
        // Dismissing the view of your OWN inventory is a local action; sending a close-window for window 0 would be telling the server you shut a container you never opened.
        // A window that is already gone is the same story for a different reason (see MarkWindowGone).
        if (_windowId == PlayerWindowId || _windowGone || _preserveWindowOnClose)
            return;

        try
        {
            await _game.Inventory.CloseAsync().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is DmcbkNotInSessionException or DmcbkFeatureDisabledException)
        {
            // Session gone; nothing to close.
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Escape or Key.E)
        {
            _view.HideOverlay();
            e.Handled = true;
        }
        else if (e.Key is Key.R)
        {
            _ = RefreshAsync();
            e.Handled = true;
        }
        else if (e.Key == Key.Q && _hoveredSlot is { } slot)
        {
            // Q drops one from the hovered slot, Ctrl+Q drops the whole stack.
            bool wholeStack = e.KeyModifiers.HasFlag(KeyModifiers.Control);
            _ = DropAsync(slot, wholeStack);
            e.Handled = true;
        }
    }

    private async Task DropAsync(int slot, bool wholeStack)
    {
        try
        {
            await _game.Inventory.DropAsync(slot, wholeStack).ConfigureAwait(true);
            await RefreshAsync().ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _backend.WriteLine(Strings.SendFailed(ex.Message));
        }
    }

    private async Task RefreshAsync()
    {
        try
        {
            OpenContainerSnapshot? snapshot = await ReadWindowAsync().ConfigureAwait(true);
            if (snapshot is null || snapshot.WindowId != _windowId)
            {
                // Whatever this view was showing is not what the session holds any more, so closing it must not send a close-window for it.
                CloseBecauseWindowGone();
                return;
            }

            Dispatcher.UIThread.Post(() => Render(snapshot));
        }
        catch (Exception ex) when (ex is DmcbkNotInSessionException or DmcbkFeatureDisabledException)
        {
            CloseBecauseWindowGone();
        }
    }

    private void CloseBecauseWindowGone()
    {
        _windowGone = true;
        Dispatcher.UIThread.Post(() => _view.HideOverlay());
    }

    /// <summary>
    /// The window this overlay is showing.
    /// Window 0 is the player's OWN inventory, which UMPK models separately from an open container, so it is projected into the same snapshot shape here.
    /// Without this the overlay asked GetOpenContainerAsync for window 0, got null because no chest was open, and hid itself immediately: "/inventory 0 open" reported success and nothing appeared.
    /// Cells bind by their own slot index, so the projection needs no index fixing.
    /// </summary>
    private async Task<OpenContainerSnapshot?> ReadWindowAsync()
    {
        if (_windowId != PlayerWindowId)
            return await _game.Inventory.GetOpenContainerAsync().ConfigureAwait(true);

        PlayerInventorySnapshot player = await _game.Inventory.GetPlayerInventoryAsync().ConfigureAwait(true);
        return new OpenContainerSnapshot(
            PlayerWindowId,
            player.Slots,
            EmptyProperties,
            player.Cursor,
            player.StateId,
            Trades: null,
            MenuTypeId: -1,
            Title: Strings.TuiPlayerInventoryTitle);
    }

    private void Render(OpenContainerSnapshot snapshot)
    {
        if (_layout is null
            || _layout.SlotCount != snapshot.Slots.Count
            || !string.Equals(_layoutKey, LayoutKeyOf(snapshot), StringComparison.Ordinal))
            BuildLayout(snapshot);

        _title.Text = Strings.TuiContainerTitle(snapshot.Title ?? _layout!.KindName, snapshot.WindowId);

        // Bind by the cell's own window slot index, not by creation order: the typed rows do not create cells in slot order (the brewing row runs fuel, ingredient, then the three bottles), so a positional bind showed the wrong stack in every such window.
        foreach (SlotCell cell in _cells)
        {
            if (cell.SlotIndex >= 0 && cell.SlotIndex < snapshot.Slots.Count)
                cell.Update(snapshot.Slots[cell.SlotIndex]);
        }

        // The held stack is named the same way a cell is; it used to print the raw registry id, which is the same "minecraft:cobblestone instead of Cobblestone" complaint one line lower down.
        _cursor.Text = snapshot.Cursor.IsEmpty
            ? $"{Strings.TuiCursorLabel} {Strings.TuiContainerEmpty}"
            : $"{Strings.TuiCursorLabel} {SlotCell.DisplayName(snapshot.Cursor, _translations)} x{snapshot.Cursor.Count}";
    }

    private void BuildLayout(OpenContainerSnapshot snapshot)
    {
        _body.Children.Clear();
        _cells.Clear();
        // The semantic menu type decides the layout; the pre-1.14 window string is the fallback, and only with neither does the old slot-count/property heuristic run.
        _layout = ContainerLayout.Classify(
            snapshot.Slots.Count, snapshot.Properties, snapshot.MenuType, snapshot.LegacyWindowType);
        _layoutKey = LayoutKeyOf(snapshot);
        Control content = _layout.Build(MakeCell);
        _body.Children.Add(content);
    }

    /// <summary>The identity a built layout depends on: the semantic type, else the legacy wire string.</summary>
    private static string LayoutKeyOf(OpenContainerSnapshot snapshot)
        => snapshot.MenuType ?? snapshot.LegacyWindowType ?? string.Empty;

    private SlotCell MakeCell(int slotIndex, int checker)
    {
        var cell = new SlotCell(slotIndex, checker, _translations, OnSlotClick, OnSlotHover);
        _cells.Add(cell);
        return cell;
    }

    /// <summary>
    /// The translation chain the slot names resolve against.
    /// The overlay is handed a view, a <see cref="GameApi"/> and the backend, and none of the three carries the client's chain directly; the backend's component renderer does, because <see cref="TuiBackend.UseRenderer"/> is built from <c>client.Translations</c> at startup (Tui/TuiHost.cs:40).
    /// Going through it keeps the names on the session's negotiated protocol table, which is what makes the grid agree with the text listing on an old server too.
    /// With no renderer installed (a backend that never rendered a component), the default host chain still resolves modern names rather than leaving raw ids on screen.
    /// </summary>
    private static ITranslationSource ResolveTranslations(TuiBackend backend)
        => backend.Renderer is { } renderer
            ? new RendererTranslations(renderer)
            : HostTranslations.CreateDefault();

    private void OnSlotClick(int slot, MouseButtonKind kind)
    {
        _ = ClickAsync(slot, kind);
    }

    // only clear on exit if this cell is still the one recorded as hovered, so a pointer move that enters the next cell before this one's exit fires (or vice versa) can never clobber the new hover.
    private void OnSlotHover(int slot, bool entered)
        => _hoveredSlot = entered ? slot : (_hoveredSlot == slot ? null : _hoveredSlot);

    private async Task ClickAsync(int slot, MouseButtonKind kind)
    {
        try
        {
            switch (kind)
            {
                case MouseButtonKind.Right:
                    await _game.Inventory.PickupAsync(slot, Umpk.Game.Inventory.MouseButton.Right).ConfigureAwait(true);
                    break;

                case MouseButtonKind.Shift:
                    await _game.Inventory.QuickMoveAsync(slot).ConfigureAwait(true);
                    break;

                default:
                    await _game.Inventory.PickupAsync(slot, Umpk.Game.Inventory.MouseButton.Left).ConfigureAwait(true);
                    break;
            }

            await RefreshAsync().ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _backend.WriteLine(Strings.SendFailed(ex.Message));
        }
    }

    /// <summary>
    /// Reads a translation template back out of a <see cref="ComponentInlineRenderer"/>.
    /// The renderer holds the chain but does not expose it, so a key is resolved by flattening a one-node translatable through it: a key the chain knows comes back as its template, and one it does not comes back as the key itself, which is the miss <see cref="ITranslationSource.TryResolve"/> reports as false.
    /// <para>
    /// The flatten is why this bridge is for argument-free keys only, which is all the item and block names it serves are: a template carrying <c>%s</c> placeholders would come back with them already substituted (against no arguments) instead of verbatim.
    /// </para>
    /// </summary>
    internal sealed class RendererTranslations(ComponentInlineRenderer renderer) : ITranslationSource
    {
        public bool TryResolve(string key, [NotNullWhen(true)] out string? template)
        {
            ArgumentNullException.ThrowIfNull(key);
            string resolved = renderer.PlainText(Component.Translatable(key));
            if (resolved.Length == 0 || string.Equals(resolved, key, StringComparison.Ordinal))
            {
                template = null;
                return false;
            }

            template = resolved;
            return true;
        }
    }
}
