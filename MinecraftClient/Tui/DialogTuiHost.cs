using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using MinecraftClient.Dialogs;

namespace MinecraftClient.Tui;

internal interface IOverlayCloseHandler
{
    bool TryCloseByUser();
}

public static class DialogTuiHost
{
    public static bool TryOpen(McClient handler, DialogInstance instance, bool force)
    {
        if (ConsoleIO.Backend is not TuiConsoleBackend)
            return false;

        Dispatcher.UIThread.Post(() =>
        {
            var view = TuiConsoleBackend.Instance?.GetView();
            if (view is null)
                return;

            if (view.HasOverlay && view.OverlayContent is not DialogView && !force)
            {
                handler.Log.Info(Translations.dialog_tui_pending);
                return;
            }

            view.ShowOverlay(new DialogView(handler, instance));
        });

        return true;
    }

    public static void CloseCurrent()
    {
        if (ConsoleIO.Backend is not TuiConsoleBackend)
            return;

        Dispatcher.UIThread.Post(() =>
        {
            var view = TuiConsoleBackend.Instance?.GetView();
            if (view?.OverlayContent is DialogView)
                view.HideOverlay();
        });
    }
}

internal sealed class DialogView : Border, IOverlayCloseHandler
{
    private readonly McClient _handler;
    private readonly DialogInstance _instance;
    private readonly TextBlock _status;
    private readonly Dictionary<string, Control> _inputControls = new(StringComparer.Ordinal);

    public DialogView(McClient handler, DialogInstance instance)
    {
        _handler = handler;
        _instance = instance;

        BorderBrush = Brushes.White;
        BorderThickness = new Thickness(1);
        Background = Brushes.Black;
        Padding = new Thickness(1);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        Focusable = true;

        _status = new TextBlock
        {
            Foreground = Brushes.Gray,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(1, 0)
        };

        Child = BuildContent();

        AttachedToVisualTree += (_, _) =>
        {
            AddHandler(KeyDownEvent, OnTunnelKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
            Focus();
        };
        DetachedFromVisualTree += (_, _) => RemoveHandler(KeyDownEvent, OnTunnelKeyDown);
    }

    public bool TryCloseByUser()
    {
        if (!_instance.Definition.CanCloseWithEscape && _instance.Definition.CancelAction is null)
        {
            SetStatus(Translations.dialog_cannot_cancel);
            return false;
        }

        var result = _handler.Dialogs.Cancel();
        SetStatus(result.Message);
        if (result.Success)
            CloseIfInactive();

        return false;
    }

    private Control BuildContent()
    {
        var main = new StackPanel
        {
            Spacing = 1,
            Margin = new Thickness(1)
        };

        main.Children.Add(new TextBlock
        {
            Text = _instance.Definition.DisplayTitle(),
            Foreground = Brushes.Yellow,
            FontWeight = FontWeight.Bold,
            TextWrapping = TextWrapping.Wrap
        });

        foreach (var body in _instance.Definition.Body)
        {
            if (string.IsNullOrWhiteSpace(body.Text))
                continue;

            main.Children.Add(new TextBlock
            {
                Text = body.Text,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            });
        }

        foreach (var input in _instance.Definition.Inputs)
            main.Children.Add(BuildInput(input));

        var buttons = new WrapPanel
        {
            Orientation = Orientation.Horizontal
        };

        foreach (var action in _instance.Definition.Actions)
        {
            var button = new Button
            {
                Content = action.Label,
                Margin = new Thickness(0, 0, 1, 1),
                MinWidth = Math.Max(8, Math.Min(action.Label.Length + 4, 32))
            };
            button.Click += (_, _) => Click(action.Index);
            buttons.Children.Add(button);
        }

        if (_instance.Definition.CancelAction is not null || _instance.Definition.CanCloseWithEscape)
        {
            var cancel = new Button
            {
                Content = Translations.tui_dialog_cancel,
                Margin = new Thickness(0, 0, 1, 1)
            };
            cancel.Click += (_, _) => TryCloseByUser();
            buttons.Children.Add(cancel);
        }

        if (buttons.Children.Count > 0)
            main.Children.Add(buttons);

        main.Children.Add(_status);

        return new ScrollViewer
        {
            Content = main,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };
    }

    private Control BuildInput(DialogInput input)
    {
        _instance.Values.TryGetValue(input.Key, out var value);
        value ??= input.InitialValue;

        var panel = new StackPanel
        {
            Spacing = 0,
            Margin = new Thickness(0, 1)
        };

        if (input.LabelVisible && !string.IsNullOrWhiteSpace(input.Label))
        {
            panel.Children.Add(new TextBlock
            {
                Text = input.Label,
                Foreground = Brushes.LightGray,
                TextWrapping = TextWrapping.Wrap
            });
        }

        Control control = input.Kind switch
        {
            DialogInputKind.Boolean => BuildBooleanInput(value),
            DialogInputKind.SingleOption => BuildOptionInput(input, value),
            DialogInputKind.NumberRange => BuildNumberInput(input, value),
            _ => BuildTextInput(input, value)
        };

        _inputControls[input.Key] = control;
        panel.Children.Add(control);
        return panel;
    }

    private static Control BuildTextInput(DialogInput input, string value)
    {
        return new TextBox
        {
            Text = value,
            AcceptsReturn = input.Multiline,
            TextWrapping = TextWrapping.Wrap,
            MaxLength = input.MaxLength,
            Foreground = Brushes.White,
            Background = Brushes.Black,
            BorderBrush = Brushes.Gray
        };
    }

    private static Control BuildBooleanInput(string value)
    {
        return new CheckBox
        {
            IsChecked = value.Equals("true", StringComparison.OrdinalIgnoreCase),
            Foreground = Brushes.White
        };
    }

    private static Control BuildOptionInput(DialogInput input, string value)
    {
        var combo = new ComboBox
        {
            ItemsSource = input.Options ?? [],
            Foreground = Brushes.White
        };
        combo.SelectionBoxItemTemplate = null;
        combo.SelectedItem = input.Options?.FirstOrDefault(option => option.Id.Equals(value, StringComparison.Ordinal))
            ?? input.Options?.FirstOrDefault();
        return combo;
    }

    private static Control BuildNumberInput(DialogInput input, string value)
    {
        var slider = new Slider
        {
            Minimum = Math.Min(input.Start, input.End),
            Maximum = Math.Max(input.Start, input.End),
            Value = double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                ? number
                : input.InitialNumber ?? input.Start,
            TickFrequency = input.Step ?? 1,
            IsSnapToTickEnabled = input.Step is not null
        };
        return slider;
    }

    private void Click(int index)
    {
        if (!StoreInputs())
            return;

        var result = _handler.Dialogs.Click(index);
        SetStatus(result.Message);
        if (result.Success)
            CloseIfInactive();
    }

    private bool StoreInputs()
    {
        foreach (var input in _instance.Definition.Inputs)
        {
            if (!_inputControls.TryGetValue(input.Key, out var control))
                continue;

            var value = control switch
            {
                TextBox textBox => textBox.Text ?? string.Empty,
                CheckBox checkBox => checkBox.IsChecked == true ? "true" : "false",
                ComboBox comboBox when comboBox.SelectedItem is DialogOption option => option.Id,
                Slider slider => NumberToString((float)slider.Value),
                _ => input.InitialValue
            };

            var result = _handler.Dialogs.SetInput(input.Key, value);
            if (!result.Success)
            {
                SetStatus(result.Message);
                return false;
            }
        }

        return true;
    }

    private void CloseIfInactive()
    {
        var current = _handler.Dialogs.Current;
        if (current is null)
        {
            DialogTuiHost.CloseCurrent();
            return;
        }

        if (current.Revision != _instance.Revision)
            DialogTuiHost.TryOpen(_handler, current, force: true);
    }

    private void SetStatus(string text)
    {
        _status.Text = text;
    }

    private void OnTunnelKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;

        TryCloseByUser();
        e.Handled = true;
    }

    private static string NumberToString(float value)
    {
        var integer = (int)value;
        return integer == value
            ? integer.ToString(CultureInfo.InvariantCulture)
            : value.ToString(CultureInfo.InvariantCulture);
    }
}
