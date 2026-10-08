using Mcc.Cli.Localization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Mcc.Cli.Tui.Minimap;
using DMCBK.Core;
using DMCBK.Core.Commands;
using Umpk.Client;
using Umpk.Client.Actions;

namespace Mcc.Cli.Tui.Management;

internal sealed class EntityBrowserPage : DockPanel, IManagementWorkspacePage, IDisposable
{
    private readonly ManagementWorkspace _workspace;
    private readonly Client _client;
    private readonly TextBox _search;
    private readonly Button _sort;
    private readonly ListBox _list;
    private readonly StackPanel _detail;
    private readonly ScrollViewer _detailScroll;
    private readonly ContentControl _body;
    private readonly Button _use;
    private readonly Button _attack;
    private readonly Control _entityActions;
    private readonly DispatcherTimer _timer;
    private IReadOnlyList<EntitySnapshot> _entities = [];
    private EntitySnapshot? _retained;
    private EntityFilter _filter;
    private EntitySort _sortMode;
    private int? _selectedId;
    private bool _selectedGone;
    private bool _compact;
    private bool _showingDetail;
    private int _refreshing;
    private bool _disposed;
    private Umpk.Geometry.Vec3d _selfPosition;

    public EntityBrowserPage(ManagementWorkspace workspace, Client client)
    {
        _workspace = workspace;
        _client = client;
        _search = ManagementUi.SearchBox();
        _search.TextChanged += (_, _) => Filter();
        _sort = ManagementUi.Button(Strings.EntityUiSortDistance, CycleSort);
        Control filters = ManagementUi.Actions(
            ManagementUi.Button(Strings.EntityUiAll, () => SetFilter(EntityFilter.All)),
            ManagementUi.Button(Strings.EntityUiPlayers, () => SetFilter(EntityFilter.Player)),
            ManagementUi.Button(Strings.EntityUiHostile, () => SetFilter(EntityFilter.Hostile)),
            ManagementUi.Button(Strings.EntityUiNeutral, () => SetFilter(EntityFilter.Neutral)),
            ManagementUi.Button(Strings.EntityUiPassive, () => SetFilter(EntityFilter.Passive)),
            ManagementUi.Button(Strings.EntityUiOther, () => SetFilter(EntityFilter.Other)),
            _sort);
        Grid toolbar = ManagementUi.FilterSearchToolbar(
            filters,
            ManagementUi.Button(Strings.MgmtRefresh, Refresh),
            _search);
        SetDock(toolbar, Dock.Top);
        Children.Add(toolbar);

        _list = new ListBox
        {
            MinWidth = 36,
            MinHeight = 18,
            Background = ManagementUi.Canvas,
            Foreground = Brushes.White,
            BorderBrush = ManagementUi.CyanDark,
            BorderThickness = new Thickness(1),
        };
        _list.SelectionChanged += (_, _) => SelectCurrent(open: false);
        ScrollViewer.SetVerticalScrollBarVisibility(_list, ScrollBarVisibility.Visible);
        ScrollViewer.SetHorizontalScrollBarVisibility(_list, ScrollBarVisibility.Disabled);
        _list.DoubleTapped += (_, _) => SelectCurrent(open: true);
        _list.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                SelectCurrent(open: true);
                e.Handled = true;
            }
        };

        _detail = new StackPanel { Spacing = 0, MinWidth = 42, Margin = new Thickness(1, 0, 0, 0) };
        _detailScroll = new ScrollViewer
        {
            Content = _detail,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
        };
        _use = ManagementUi.Button(Strings.EntityUiUse, () => _ = UseAsync(), ManagementButtonKind.Primary);
        _attack = ManagementUi.Button(Strings.EntityUiAttack, () => _ = AttackAsync(), ManagementButtonKind.Danger);
        _entityActions = ManagementUi.Actions(_use, _attack);
        _body = new ContentControl
        {
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
        };
        Children.Add(_body);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _timer.Tick += (_, _) => Refresh();
        _timer.Start();
        _client.StatusChanged += OnStatusChanged;
        Refresh();
    }

    public bool UseWorkspaceScroll => false;

    public bool HasLocalBackNavigation => _compact && _showingDetail;

    public void FocusSearch() => _search.Focus();

    public void Refresh() => _ = RefreshAsync();

    public bool HandleBack()
    {
        if (!_compact || !_showingDetail)
            return false;
        _showingDetail = false;
        BuildLayout();
        _list.Focus();
        return true;
    }

    public void SetCompact(bool compact)
    {
        _compact = compact;
        if (!compact)
            _showingDetail = false;
        BuildLayout();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _timer.Stop();
        _client.StatusChanged -= OnStatusChanged;
    }

    private async Task RefreshAsync()
    {
        if (_disposed || Interlocked.Exchange(ref _refreshing, 1) != 0)
            return;
        try
        {
            _entities = await _client.Game.Entities.AllAsync(_workspace.Closed);
            _selfPosition = (await _client.Game.Movement.GetPoseAsync(_workspace.Closed)).Position;
            Filter(_selectedId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _workspace.SetStatus(Strings.ScriptsUiFailed(ex.Message));
        }
        finally
        {
            Interlocked.Exchange(ref _refreshing, 0);
        }
    }

    private void SetFilter(EntityFilter filter)
    {
        _filter = filter;
        _showingDetail = false;
        Filter(_selectedId);
    }

    private void CycleSort()
    {
        _sortMode = _sortMode switch
        {
            EntitySort.Distance => EntitySort.Name,
            EntitySort.Name => EntitySort.Type,
            _ => EntitySort.Distance,
        };
        _sort.Content = _sortMode switch
        {
            EntitySort.Name => Strings.EntityUiSortName,
            EntitySort.Type => Strings.EntityUiSortType,
            _ => Strings.EntityUiSortDistance,
        };
        Filter(_selectedId);
    }

    private void Filter(int? preserve = null)
    {
        string query = (_search.Text ?? string.Empty).Trim();
        IEnumerable<EntityRow> queryable = _entities.Select(entity => new EntityRow(
            entity,
            DisplayName(entity),
            EntityPresentation.TypeName(_client.Translations, entity.TypeId),
            entity.Position.Subtract(_selfPosition).Length(),
            Category(entity)));
        queryable = queryable.Where(row => MatchesFilter(row.Category)
            && (query.Length == 0
                || row.Entity.Id.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)
                || row.Type.Contains(query, StringComparison.OrdinalIgnoreCase)
                || row.Entity.TypeId.Contains(query, StringComparison.OrdinalIgnoreCase)
                || row.Name.Contains(query, StringComparison.OrdinalIgnoreCase)));
        queryable = _sortMode switch
        {
            EntitySort.Name => queryable.OrderBy(row => row.Name, StringComparer.CurrentCultureIgnoreCase),
            EntitySort.Type => queryable.OrderBy(row => row.Type, StringComparer.CurrentCultureIgnoreCase),
            _ => queryable.OrderBy(row => row.Distance),
        };
        List<EntityRow> rows = queryable.ToList();
        _list.ItemsSource = rows;
        EntityRow? restored = rows.FirstOrDefault(row => row.Entity.Id == preserve);
        if (restored is not null)
        {
            _list.SelectedItem = restored;
            _selectedId = restored.Entity.Id;
            _retained = restored.Entity;
            _selectedGone = false;
        }
        else if (preserve is not null && _retained is not null)
        {
            _list.SelectedItem = null;
            _selectedGone = true;
        }
        else
        {
            _list.SelectedItem = rows.FirstOrDefault();
            _selectedId = rows.FirstOrDefault()?.Entity.Id;
            _retained = rows.FirstOrDefault()?.Entity;
            _selectedGone = false;
        }
        _workspace.SetStatus(rows.Count == 0 ? Strings.EntityUiNoEntities : Strings.MgmtShowing(rows.Count, _entities.Count));
        RenderDetail();
        BuildLayout();
    }

    private void SelectCurrent(bool open)
    {
        if (_list.SelectedItem is EntityRow row)
        {
            _selectedId = row.Entity.Id;
            _retained = row.Entity;
            _selectedGone = false;
        }
        RenderDetail();
        if (open && _compact && _retained is not null)
        {
            _showingDetail = true;
            BuildLayout();
        }
    }

    private void RenderDetail()
    {
        _detail.Children.Clear();
        if (_retained is not { } entity)
        {
            _detail.Children.Add(ManagementUi.Body(Strings.EntityUiNoEntities, ManagementUi.Muted));
            return;
        }
        string type = EntityPresentation.TypeName(_client.Translations, entity.TypeId);
        _detail.Children.Add(ManagementUi.Section(DisplayName(entity)));
        if (_selectedGone)
            _detail.Children.Add(ManagementUi.Body(Strings.EntityUiGone, ManagementUi.Red));
        _detail.Children.Add(ManagementUi.Section(Strings.EntityUiIdentity));
        AddLine(Strings.EntityUiType, type + " · " + entity.TypeId);
        AddLine(Strings.EntityUiName, DisplayName(entity));
        _detail.Children.Add(ManagementUi.Body(Strings.EntityUiId(entity.Id)));
        _detail.Children.Add(ManagementUi.Body(Strings.EntityUiUuid(entity.Uuid)));

        _detail.Children.Add(ManagementUi.Section(Strings.EntityUiPosition));
        AddLine(Strings.EntityUiCoordinates, entity.Position.ToString());
        AddLine(Strings.EntityUiPose, entity.Pose);
        AddLine(Strings.EntityUiRotation, $"{entity.Yaw:0.0} / {entity.Pitch:0.0} / {entity.HeadYaw:0.0}");
        AddLine(Strings.EntityUiVelocity, entity.Velocity.ToString());
        if (_list.SelectedItem is EntityRow row)
            _detail.Children.Add(ManagementUi.Body(Strings.EntityUiDistance(row.Distance)));

        _detail.Children.Add(ManagementUi.Section(Strings.EntityUiEquipment));
        foreach ((string slot, ItemStackInfo stack) in entity.Equipment)
            AddLine(slot, InventoryRendering.TypeName(_client.Translations, stack.ItemId) + " ×" + stack.Count);
        AddLine(Strings.EntityUiEffects, string.Join(", ", entity.Effects.Select(effect => effect.EffectId + " " + effect.Level)));
        AddLine(Strings.EntityUiPassengers, string.Join(", ", entity.PassengerIds));
        AddLine(Strings.EntityUiVehicle, entity.VehicleId?.ToString() ?? Strings.BrowserNone);
        if (entity.CarriedItem is { } carried)
            AddLine(Strings.EntityUiCarried, InventoryRendering.TypeName(_client.Translations, carried.ItemId) + " ×" + carried.Count);
        _use.IsEnabled = !_selectedGone;
        _attack.IsEnabled = !_selectedGone;
        _detail.Children.Add(_entityActions);
    }

    private async Task UseAsync()
    {
        if (_selectedId is not { } id || _selectedGone)
            return;
        try
        {
            await _client.Game.Entities.InteractAsync(id, Hand.Main, _workspace.Closed);
            _workspace.SetStatus(Strings.EntityUiUsed);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _workspace.SetStatus(Strings.ScriptsUiFailed(ex.Message));
        }
    }

    private async Task AttackAsync()
    {
        if (_selectedId is not { } id || _selectedGone)
            return;
        try
        {
            EntitySnapshot? entity = await _client.Game.Entities.ByIdAsync(id, _workspace.Closed);
            if (entity is null)
            {
                _selectedGone = true;
                RenderDetail();
                return;
            }
            PlayerPose pose = await _client.Game.Movement.GetPoseAsync(_workspace.Closed);
            bool visible = EntityTargeting.IsInFront(pose, entity.Position)
                && await _client.Game.World.HasLineOfSightAsync(entity.Position, _workspace.Closed);
            if (!visible)
            {
                _workspace.SetStatus(Strings.EntityUiNotVisible);
                return;
            }
            await _client.Game.Entities.AttackAsync(id, _workspace.Closed);
            _workspace.SetStatus(Strings.EntityUiAttacked);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _workspace.SetStatus(Strings.ScriptsUiFailed(ex.Message));
        }
    }

    private void AddLine(string label, string value)
        => _detail.Children.Add(ManagementUi.Body(label + ": " + (value.Length == 0 ? Strings.BrowserNone : value)));

    private bool MatchesFilter(EntityFilter category) => _filter == EntityFilter.All || _filter == category;

    private static EntityFilter Category(EntitySnapshot entity) => MinimapEntityClassifier.Classify(
        entity.TypeId,
        entity.PlayerName is not null || string.Equals(entity.TypeId, "minecraft:player", StringComparison.Ordinal)) switch
    {
        MobCategory.Player => EntityFilter.Player,
        MobCategory.Hostile => EntityFilter.Hostile,
        MobCategory.Neutral => EntityFilter.Neutral,
        MobCategory.Passive => EntityFilter.Passive,
        _ => EntityFilter.Other,
    };

    private static string DisplayName(EntitySnapshot entity)
        => entity.PlayerName ?? entity.CustomName ?? entity.TypeId;

    private void BuildLayout()
    {
        ManagementUi.SetResponsiveBody(
            _body, _compact, _showingDetail, _list, _detailScroll, "38,*");
        _workspace.RefreshNavigation();
    }

    private void OnStatusChanged(object? sender, ClientStatusChangedEventArgs args)
    {
        if (args.Current != ClientStatus.Playing)
            _timer.Stop();
    }

    private enum EntityFilter { All, Player, Hostile, Neutral, Passive, Other }
    private enum EntitySort { Distance, Name, Type }

    private sealed record EntityRow(EntitySnapshot Entity, string Name, string Type, double Distance, EntityFilter Category)
    {
        public override string ToString() => $"{Distance,6:0.0}  {Name}  ·  {Type}  #{Entity.Id}";
    }
}
