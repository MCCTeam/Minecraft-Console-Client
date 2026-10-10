using Mcc.Cli.Localization;
using Mcc.Cli.Tui.Hosting;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using DMCBK.Core;
using DMCBK.Core.Commands;
using Umpk.Client.Actions;

namespace Mcc.Cli.Tui.Overlays;

/// <summary>
/// The dialog overlay behind <see cref="IHostUi.TryShowDialog"/>.
/// It renders the live server dialog the core hands it (<see cref="DialogViewRequest"/>): title, type, body, one editor per input seeded with the value that would be submitted, and one button per dialog button.
/// Pressing a button submits through <see cref="DialogApi.ClickAsync"/> with the edited input values; the cancel button triggers the dialog's exit action through <see cref="DialogApi.CancelAsync"/>.
/// Escape only closes the overlay, so leaving the view never sends anything the user did not ask for.
/// A dialog the server sent as a registry reference has no renderable body, and the overlay says exactly that.
/// Ported in structure from the legacy <c>DialogTuiHost</c>.
/// </summary>
internal sealed class DialogOverlay : Border
{
    private readonly MainTuiView _view;
    private readonly GameApi _game;
    private readonly TuiBackend _backend;
    private readonly DialogSnapshot _dialog;
    private readonly Dictionary<string, TextBox> _inputs = new(StringComparer.Ordinal);
    private readonly TextBlock _status;

    private DialogOverlay(MainTuiView view, GameApi game, TuiBackend backend, DialogViewRequest request)
    {
        _view = view;
        _game = game;
        _backend = backend;
        _dialog = request.Dialog;
        _status = new TextBlock { Foreground = Brushes.DarkGray, Text = Strings.TuiDialogControls };

        var stack = new StackPanel();
        if (_dialog.RegistryId is { } registryId)
        {
            stack.Children.Add(new TextBlock
            {
                Text = Strings.TuiDialogRegistryOnly(registryId),
                Foreground = Brushes.Gold,
            });
        }
        else
            BuildBody(stack, request);

        stack.Children.Add(_status);

        BorderBrush = Brushes.DarkCyan;
        BorderThickness = new Avalonia.Thickness(1);
        Background = new SolidColorBrush(Color.FromRgb(15, 15, 25));
        Padding = new Avalonia.Thickness(1);
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;
        Child = new ScrollViewer
        {
            Content = stack,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };
        Focusable = true;
        KeyDown += OnKeyDown;
    }

    public static void Open(MainTuiView view, GameApi game, TuiBackend backend, DialogViewRequest request)
    {
        var overlay = new DialogOverlay(view, game, backend, request);
        view.ShowOverlay(overlay);
    }

    private void BuildBody(StackPanel stack, DialogViewRequest request)
    {
        stack.Children.Add(new TextBlock
        {
            Text = _dialog.Title,
            Foreground = Brushes.Cyan,
            FontWeight = FontWeight.Bold,
        });
        stack.Children.Add(new TextBlock { Text = _dialog.TypeId, Foreground = Brushes.Gray });

        foreach (string line in _dialog.Body)
            stack.Children.Add(new TextBlock { Text = line, Foreground = Brushes.White });

        if (_dialog.Inputs.Count > 0)
        {
            stack.Children.Add(new TextBlock { Text = Strings.TuiDialogInputs, Foreground = Brushes.Gold });
            foreach (DialogInputInfo input in _dialog.Inputs)
            {
                string seed = request.Values.TryGetValue(input.Key, out string? staged) ? staged : input.InitialValue;
                var editor = new TextBox { Text = seed };
                _inputs[input.Key] = editor;

                var row = new DockPanel();
                var label = new TextBlock
                {
                    Text = Strings.TuiDialogInputLabel(input.Key, input.Label ?? string.Empty, input.Kind),
                    Foreground = Brushes.Gray,
                };
                DockPanel.SetDock(label, Dock.Left);
                row.Children.Add(label);
                row.Children.Add(editor);
                stack.Children.Add(row);
            }
        }

        if (_dialog.Buttons.Count == 0)
            stack.Children.Add(new TextBlock { Text = Strings.TuiDialogNoButtons, Foreground = Brushes.Gray });

        for (int i = 0; i < _dialog.Buttons.Count; i++)
        {
            int index = i;
            DialogButtonInfo info = _dialog.Buttons[i];
            var button = new Button { Content = Strings.TuiDialogButton(index, info.Label) };
            button.Click += (_, _) => _ = ClickAsync(index);
            stack.Children.Add(button);
        }

        var cancel = new Button { Content = Strings.TuiDialogCancel };
        cancel.Click += (_, _) => _ = CancelAsync();
        stack.Children.Add(cancel);
    }

    private Dictionary<string, string> CollectValues()
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, TextBox> entry in _inputs)
            values[entry.Key] = entry.Value.Text ?? string.Empty;

        return values;
    }

    private async Task ClickAsync(int index)
    {
        try
        {
            // The outcome says what the press did; only the custom-click kinds reach the server, so a flat "Dialog response sent." would be a false success for the rest.
            // The line goes to the transcript rather than the overlay's own status, because the press closes the overlay and a status the user cannot read is no better than no status.
            DialogClickOutcome outcome = await _game.Dialogs.ClickAsync(index, CollectValues()).ConfigureAwait(true);
            string report = outcome switch
            {
                DialogClickOutcome.ActionSent => Strings.TuiDialogSent,
                DialogClickOutcome.CommandSent => Strings.TuiDialogCommandRun(
                    _dialog.Buttons[index].ActionValue ?? string.Empty),
                DialogClickOutcome.ClosedOnly => Strings.TuiDialogClosedOnly,
                _ => Strings.TuiDialogNotPerformed,
            };

            _status.Text = report;
            _backend.WriteLine(report);
            _view.HideOverlay();
        }
        catch (Exception ex) when (ex is DmcbkNotInSessionException or DmcbkFeatureDisabledException)
        {
            _view.HideOverlay();
        }
    }

    private async Task CancelAsync()
    {
        try
        {
            await _game.Dialogs.CancelAsync().ConfigureAwait(true);
            _status.Text = Strings.TuiDialogSent;
            _view.HideOverlay();
        }
        catch (Exception ex) when (ex is DmcbkNotInSessionException or DmcbkFeatureDisabledException)
        {
            _view.HideOverlay();
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Escape)
        {
            _view.HideOverlay();
            e.Handled = true;
        }
    }
}
