using Mcc.Cli.Tui.Hosting;
using Mcc.Cli.Configuration;
using DMCBK.Core;

namespace Mcc.Cli.Tui.Minimap;

/// <summary>
/// Owns the minimap's mutable state (enabled, zoom, names, cave mode, position, refresh interval) seeded from console.toml, and installs / drives the <see cref="MinimapControl"/> in a movable managed window.
/// The <c>minimap</c> host command mutates it.
/// The control samples the world through the core surface-region feed (<see cref="WorldApi.GetSurfaceRegionAsync"/>) and entities through <see cref="EntitiesApi.AllAsync"/>.
/// <para>
/// The four name-category toggles (player/hostile/neutral/passive) replace the previous single dead <c>ShowNames</c> flag, matching console.toml's <c>Minimap.ShowPlayerNames</c>/<c>ShowHostileNames</c>/ <c>ShowNeutralNames</c>/<c>ShowPassiveNames</c>.
/// They are wired through to <see cref="MinimapControl.SetNameFlags"/>, so each seeded/toggled flag actually bakes that category's name labels onto the map (<see cref="MinimapControl"/>'s own doc covers the rendering).
/// Each category is now addressable on its own (<c>minimap names hostile off</c>), matching the legacy grammar.
/// </para>
/// <para>
/// The operations here are pure state mutators: <see cref="MinimapCommand"/> phrases every message from the legacy string corpus, exactly as the legacy command did.
/// </para>
/// </summary>
internal sealed class MinimapController
{
    private readonly TuiBackend _backend;
    private MainTuiView? _view;
    private GameApi? _game;
    private MinimapControl? _control;
    private MinimapWindow? _window;

    public MinimapController(TuiBackend backend, ConsoleHostConfig console)
    {
        _backend = backend;
        Enabled = console.MinimapEnabled;
        Zoom = Math.Clamp(console.MinimapZoom, MinimapControl.MinZoom, MinimapControl.MaxZoom);
        Width = console.MinimapWidth;
        Height = console.MinimapHeight;
        Position = MinimapControl.ParsePosition(console.MinimapPosition);
        RefreshIntervalMs = Math.Clamp(console.MinimapRefreshIntervalMs, MinimapControl.MinRefreshMs, MinimapControl.MaxRefreshMs);
        ShowPlayerNames = console.MinimapShowPlayerNames;
        ShowHostileNames = console.MinimapShowHostileNames;
        ShowNeutralNames = console.MinimapShowNeutralNames;
        ShowPassiveNames = console.MinimapShowPassiveNames;
        // console.toml carries a bool, so only on/off are reachable at startup; auto is command-only.
        CaveMode = console.MinimapCaveMode ? MinimapCaveMode.On : MinimapCaveMode.Off;
    }

    public bool Enabled { get; private set; }

    public int Zoom { get; private set; }

    public int Width { get; }

    public int Height { get; }

    public MinimapPosition Position { get; private set; }

    public int RefreshIntervalMs { get; }

    public bool ShowPlayerNames { get; private set; }

    public bool ShowHostileNames { get; private set; }

    public bool ShowNeutralNames { get; private set; }

    public bool ShowPassiveNames { get; private set; }

    /// <summary>Whether any name category is currently shown (the aggregate signal <see cref="MinimapControl"/> consumes).</summary>
    public bool AnyNamesShown => ShowPlayerNames || ShowHostileNames || ShowNeutralNames || ShowPassiveNames;

    /// <summary>
    /// The requested cave mode (<c>minimap cave auto|on|off</c>).
    /// Tri-state like legacy's <c>CaveModeOption</c>, so the command can report back what was asked for.
    /// </summary>
    public MinimapCaveMode CaveMode { get; private set; }

    /// <summary>
    /// Whether the map is actually drawn cave-style.
    /// Legacy resolved <c>auto</c> per frame from the dimension's ceiling flag and the player's column (MinimapControl.ResolveCaveMode); the new <see cref="MinimapControl"/> has no such detection and the core exposes no ceiling data, so <see cref="MinimapCaveMode.Auto"/> currently renders as surface, like <c>off</c>.
    /// </summary>
    public bool CaveActive => CaveMode == MinimapCaveMode.On;

    /// <summary>Binds the live view/game and starts the minimap when enabled (called once joined).</summary>
    public void Attach(MainTuiView view, GameApi game)
    {
        _view = view;
        _game = game;
        if (Enabled)
            EnsureControl();
    }

    public void SetEnabled(bool on)
    {
        Enabled = on;
        _backend.Post(() =>
        {
            if (on)
            {
                EnsureControl();
                _control?.Start();
            }
            else
            {
                _control?.Stop();
                _window?.Close();
            }
        });
    }

    /// <summary>Sets the zoom level, clamped to the control's supported range.</summary>
    public void SetZoom(int zoom)
    {
        Zoom = Math.Clamp(zoom, MinimapControl.MinZoom, MinimapControl.MaxZoom);
        _backend.Post(() => _control?.SetZoom(Zoom));
    }

    /// <summary>Reads one category's name flag (the <c>minimap names &lt;category&gt;</c> report).</summary>
    public bool GetNameCategory(MobCategory category) => category switch
    {
        MobCategory.Player => ShowPlayerNames,
        MobCategory.Hostile => ShowHostileNames,
        MobCategory.Neutral => ShowNeutralNames,
        MobCategory.Passive => ShowPassiveNames,
        _ => false,
    };

    /// <summary>Sets one category's name flag (<c>minimap names &lt;category&gt; on|off</c>).</summary>
    public void SetNameCategory(MobCategory category, bool on)
    {
        switch (category)
        {
            case MobCategory.Player: ShowPlayerNames = on; break;
            case MobCategory.Hostile: ShowHostileNames = on; break;
            case MobCategory.Neutral: ShowNeutralNames = on; break;
            case MobCategory.Passive: ShowPassiveNames = on; break;
            default: return;
        }

        PostNameFlags();
    }

    /// <summary>Sets all four name categories at once (<c>minimap names all_on|all_off</c>).</summary>
    public void SetAllNameCategories(bool on)
    {
        ShowPlayerNames = on;
        ShowHostileNames = on;
        ShowNeutralNames = on;
        ShowPassiveNames = on;
        PostNameFlags();
    }

    /// <summary>Sets the cave mode (<c>minimap cave auto|on|off</c>); see <see cref="CaveActive"/> for auto.</summary>
    public void SetCaveMode(MinimapCaveMode mode)
    {
        CaveMode = mode;
        bool active = CaveActive;
        _backend.Post(() => _control?.SetCaveMode(active));
    }

    public void SetPosition(MinimapPosition position)
    {
        Position = position;
        _backend.Post(() =>
        {
            _window?.MoveTo(position);
        });
    }

    private void PostNameFlags() =>
        _backend.Post(() => _control?.SetNameFlags(ShowPlayerNames, ShowHostileNames, ShowNeutralNames, ShowPassiveNames));

    private void EnsureControl()
    {
        if (_view is null || _game is null)
            return;

        if (_control is null)
        {
            _control = new MinimapControl(_game, Width, Height, RefreshIntervalMs)
            {
                Zoom = Zoom,
                CaveMode = CaveActive,
                TooltipService = _view.GetTooltipService(),
            };
            _control.SetNameFlags(ShowPlayerNames, ShowHostileNames, ShowNeutralNames, ShowPassiveNames);
        }

        if (_window is null)
        {
            var window = new MinimapWindow(_control, _view.WindowSurface, Position);
            _window = window;
            window.Closed += (_, _) =>
            {
                _control?.Stop();
                window.Content = null;
                if (ReferenceEquals(_window, window))
                {
                    _window = null;
                    Enabled = false;
                }
            };
            _view.ShowWindow(window);
        }

        _control.Start();
    }
}

/// <summary>
/// The cave-rendering choice, the port of legacy's <c>CaveModeOption</c> (MinimapControl.cs:16) that the <c>minimap cave [auto|on|off]</c> grammar needs.
/// Lives here rather than in MinimapEnums.cs because it is controller state, not rendering vocabulary.
/// </summary>
internal enum MinimapCaveMode
{
    /// <summary>Let the client decide (see <see cref="MinimapController.CaveActive"/> for the current limit).</summary>
    Auto,

    /// <summary>Always draw the map cave-style, clamped to the player's level.</summary>
    On,

    /// <summary>Always draw the surface.</summary>
    Off,
}
