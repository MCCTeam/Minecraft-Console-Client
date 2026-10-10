using Mcc.Cli.Localization;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Iciclecreek.Avalonia.WindowManager;
using DMCBK.Core;

namespace Mcc.Cli.Tui.Scoreboard;

/// <summary>
/// A movable, resizable live scoreboard.
/// MCC's public snapshot exposes every objective but not the server-selected display slot, so the window shows the complete objective set without guessing which one vanilla would place in its sidebar.
/// </summary>
internal sealed class ScoreboardWindow : ManagedWindow
{
    private const int EdgeMargin = 1;
    private const int VisibleScoreLimit = 15;
    private readonly GameApi _game;
    private readonly Control _windowSurface;
    private readonly StackPanel _objectives = new() { Spacing = 1 };
    private readonly TextBlock _summary;
    private readonly DispatcherTimer _timer;
    private readonly CancellationTokenSource _lifetime = new();
    private int _refreshing;
    private bool _closed;

    internal ScoreboardWindow(GameApi game, Control windowSurface)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(windowSurface);

        _game = game;
        _windowSurface = windowSurface;
        _summary = new TextBlock
        {
            Foreground = Brushes.Cyan,
            FontWeight = FontWeight.Bold,
        };

        var scroll = new ScrollViewer
        {
            Content = _objectives,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };

        var body = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            Margin = new Thickness(1, 0),
        };
        body.Children.Add(_summary);
        Grid.SetRow(scroll, 1);
        body.Children.Add(scroll);
        var hint = new TextBlock
        {
            Text = Strings.ScoreboardWindowHint,
            Foreground = Brushes.Gray,
            TextWrapping = TextWrapping.Wrap,
        };
        Grid.SetRow(hint, 2);
        body.Children.Add(hint);

        Title = Strings.ScoreboardWindowTitle;
        Content = body;
        Width = 42;
        Height = 24;
        MinWidth = 24;
        MinHeight = 10;
        SizeToContent = SizeToContent.Manual;
        WindowStartupLocation = WindowStartupLocation.Manual;
        CanResize = true;
        ShowActivated = false;
        AnimateWindow = false;

        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _timer.Tick += (_, _) => _ = RefreshAsync();
        Opened += (_, _) =>
        {
            Dispatcher.UIThread.Post(ApplyInitialPosition, DispatcherPriority.Loaded);
            _timer.Start();
            _ = RefreshAsync();
        };
        Closed += (_, _) => Stop();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;

        Close();
        e.Handled = true;
    }

    private async Task RefreshAsync()
    {
        if (_closed || Interlocked.Exchange(ref _refreshing, 1) != 0)
            return;

        try
        {
            ScoreboardSnapshot board = await _game.Player
                .GetScoreboardAsync(_lifetime.Token)
                .ConfigureAwait(false);
            Dispatcher.UIThread.Post(() =>
            {
                if (!_closed)
                    Render(board);
            });
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex) when (ex is DmcbkNotInSessionException or DmcbkFeatureDisabledException)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!_closed)
                    RenderUnavailable();
            });
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!_closed)
                    RenderError(ex.Message);
            });
        }
        finally
        {
            Interlocked.Exchange(ref _refreshing, 0);
        }
    }

    private void Render(ScoreboardSnapshot board)
    {
        _summary.Text = Strings.ScoreboardWindowObjectives(board.Objectives.Count);
        _objectives.Children.Clear();
        if (board.Objectives.Count == 0)
        {
            _objectives.Children.Add(Message(Strings.ScoreboardWindowEmpty));
            return;
        }

        foreach (ObjectiveInfo objective in board.Objectives.OrderBy(static item => item.Name, StringComparer.Ordinal))
        {
            string heading = string.IsNullOrWhiteSpace(objective.DisplayName)
                || string.Equals(objective.DisplayName, objective.Name, StringComparison.Ordinal)
                ? objective.Name
                : $"{objective.DisplayName} ({objective.Name})";
            _objectives.Children.Add(new TextBlock
            {
                Text = heading,
                Foreground = Brushes.Gold,
                FontWeight = FontWeight.Bold,
                TextWrapping = TextWrapping.Wrap,
            });

            List<KeyValuePair<string, int>> scores =
            [
                .. objective.Scores
                    .OrderByDescending(static item => item.Value)
                    .ThenBy(static item => item.Key, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(static item => item.Key, StringComparer.Ordinal),
            ];
            if (scores.Count == 0)
            {
                _objectives.Children.Add(Message(Strings.ScoreboardWindowNoScores));
                continue;
            }

            int shown = Math.Min(scores.Count, VisibleScoreLimit);
            int rankWidth = shown.ToString(CultureInfo.InvariantCulture).Length;
            for (int index = 0; index < shown; index++)
            {
                string rank = (index + 1).ToString(CultureInfo.InvariantCulture).PadLeft(rankWidth);
                _objectives.Children.Add(new TextBlock
                {
                    Text = $"  {rank}. {scores[index].Key}  {scores[index].Value.ToString(CultureInfo.InvariantCulture)}",
                    TextWrapping = TextWrapping.Wrap,
                });
            }

            if (shown < scores.Count)
                _objectives.Children.Add(Message(Strings.ScoreboardWindowMore(scores.Count - shown)));
        }
    }

    private void RenderUnavailable()
    {
        _summary.Text = string.Empty;
        _objectives.Children.Clear();
        _objectives.Children.Add(Message(Strings.ScoreboardWindowDisconnected));
    }

    private void RenderError(string reason)
    {
        _summary.Text = string.Empty;
        _objectives.Children.Clear();
        _objectives.Children.Add(Message(Strings.ScoreboardWindowError(reason)));
    }

    private static TextBlock Message(string text) => new()
    {
        Text = text,
        Foreground = Brushes.Gray,
        TextWrapping = TextWrapping.Wrap,
    };

    private void ApplyInitialPosition()
    {
        Size surfaceSize = _windowSurface.Bounds.Size;
        Size windowSize = Bounds.Size;
        if (surfaceSize.Width > 0 && surfaceSize.Height > 0
            && windowSize.Width > 0 && windowSize.Height > 0)
            Position = ResolveRightCenter(surfaceSize, windowSize);
    }

    private void Stop()
    {
        if (_closed)
            return;

        _closed = true;
        _timer.Stop();
        _lifetime.Cancel();
        _lifetime.Dispose();
    }

    /// <summary>Returns a right-edge, vertically centered position clamped to the available surface.</summary>
    internal static PixelPoint ResolveRightCenter(Size surfaceSize, Size windowSize)
    {
        int maxX = Math.Max(0, (int)Math.Floor(surfaceSize.Width - windowSize.Width));
        int maxY = Math.Max(0, (int)Math.Floor(surfaceSize.Height - windowSize.Height));
        return new PixelPoint(Math.Max(0, maxX - Math.Min(EdgeMargin, maxX)), maxY / 2);
    }
}
