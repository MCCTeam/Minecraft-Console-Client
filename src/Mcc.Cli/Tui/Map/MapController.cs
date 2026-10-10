using Mcc.Cli.Localization;
using Mcc.Cli.Tui.Hosting;
using Mcc.Cli.Tui.Minimap;
using DMCBK.Core;
using Umpk.Client.Events;

namespace Mcc.Cli.Tui.Map;

/// <summary>
/// Owns the <c>/map</c> overlay's data source: tracks the most recently received filled-map id for the live session (subscribing to UMPK's <see cref="MapDataReceived"/> on every join/rejoin, mirroring <c>MinimapController.Attach</c>) and opens/refreshes <see cref="MapOverlay"/> from it.
/// "Held" map support is not implemented (DMCBK.Core's item-stack surface does not decode the map-id data component yet); "last received" covers the documented live-test path (walk with a map in hand so the server sends map data).
/// </summary>
internal sealed class MapController
{
    private readonly TuiBackend _backend;
    private volatile int _lastMapId = -1;
    private IDisposable? _subscription;
    private MapOverlay? _openOverlay;

    public MapController(TuiBackend backend) => _backend = backend;

    /// <summary>(Re)subscribes to the current session's map-data events. Called on every Playing transition.</summary>
    public void Attach(GameApi game)
    {
        _subscription?.Dispose();
        _subscription = game.Events.Subscribe<MapDataReceived>(e => OnMapDataReceived(game, e.MapId));
    }

    /// <summary>
    /// Opens the overlay for the last-received map, or reports there is none yet.
    /// Runs on a background thread (command execution); the actual UI mutation is posted to the UI thread.
    /// </summary>
    public string Open(GameApi game)
    {
        int mapId = _lastMapId;
        if (mapId < 0)
            return Strings.TuiMapNoData;

        _ = OpenOrRefreshAsync(game, mapId);
        return Strings.TuiMapOpening(mapId);
    }

    private void OnMapDataReceived(GameApi game, int mapId)
    {
        _lastMapId = mapId;

        // Live-update the overlay only when it is already open and showing THIS map id; otherwise leave it alone (the player may be reviewing a different map than the one that just updated).
        _backend.Post(() =>
        {
            if (_openOverlay is { } overlay && overlay.MapId == mapId)
                _ = RefreshAsync(game, overlay, mapId);
        });
    }

    private async Task OpenOrRefreshAsync(GameApi game, int mapId)
    {
        MapSnapshotInfo? map;
        try
        {
            map = await game.Maps.GetMapAsync(mapId).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is DmcbkNotInSessionException or DmcbkFeatureDisabledException)
        {
            return;
        }

        if (map is null)
            return;

        _backend.Post(() =>
        {
            MainTuiView? view = _backend.View;
            if (view is null)
                return;

            if (_openOverlay is { } existing && !view.HasOverlay)
                // The overlay was dismissed since we started fetching; drop the stale reference.
                _openOverlay = null;

            if (_openOverlay is { } open)
                open.UpdateMap(map);
            else
            {
                MapOverlay.Open(view, map);
                _openOverlay = FindOpenOverlay(view);
            }
        });
    }

    private async Task RefreshAsync(GameApi game, MapOverlay overlay, int mapId)
    {
        MapSnapshotInfo? map;
        try
        {
            map = await game.Maps.GetMapAsync(mapId).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is DmcbkNotInSessionException or DmcbkFeatureDisabledException)
        {
            return;
        }

        if (map is not null)
            _backend.Post(() => overlay.UpdateMap(map));
    }

    private static MapOverlay? FindOpenOverlay(MainTuiView view) => view.CurrentOverlay as MapOverlay;
}
