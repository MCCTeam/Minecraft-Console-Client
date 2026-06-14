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
    private static readonly Color AccentColor = Color.FromRgb(80, 180, 255);
    private static readonly Color BorderColor = Color.FromRgb(70, 70, 70);
    private static readonly Color SectionBg = Color.FromRgb(20, 20, 20);
    private static readonly Color InputBg = Color.FromRgb(35, 35, 35);

    private readonly McClient _handler;
    private readonly DialogInstance _instance;
    private readonly TextBlock _status;
    private readonly Dictionary<string, Control> _inputControls = new(StringComparer.Ordinal);
    private readonly StackPanel _inputsPanel;
    private readonly WrapPanel _buttonsPanel;

    public DialogView(McClient handler, DialogInstance instance)
    {
        _handler = handler;
        _instance = instance;

        BorderBrush = new SolidColorBrush(BorderColor);
        BorderThickness = new Thickness(1);
        Background = new SolidColorBrush(Color.FromRgb(12, 12, 12));
        Padding = new Thickness(2);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        Focusable = true;

        _status = new TextBlock
        {
            Foreground = Brushes.Gray,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 1, 0, 0)
        };

        _inputsPanel = new StackPanel { Spacing = 0, Margin = new Thickness(0) };
        _buttonsPanel = new WrapPanel { Orientation = Orientation.Horizontal };

        Child = BuildContent();

        AttachedToVisualTree += (_, _) =>
        {
            AddHandler(KeyDownEvent, OnTunnelKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
            FocusFirstInput();
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
        var root = new DockPanel { Margin = new Thickness(0) };

        var scroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        var main = new StackPanel { Spacing = 0, Margin = new Thickness(0) };

        // Title
        main.Children.Add(McColorParser.CreateColoredTextBlock(_instance.Definition.DisplayTitle()));

        // Separator
        main.Children.Add(new Border
        {
            Height = 1,
            Background = new SolidColorBrush(BorderColor),
            Margin = new Thickness(0, 1, 0, 1)
        });

        // Body
        foreach (var body in _instance.Definition.Body)
        {
            if (string.IsNullOrWhiteSpace(body.Text))
                continue;
            main.Children.Add(McColorParser.CreateColoredTextBlock(body.Text));
        }

        // Build action buttons
        EnsureActionButtons();

        // Inputs
        foreach (var input in _instance.Definition.Inputs)
            main.Children.Add(BuildInput(input));

        // Action buttons
        if (_buttonsPanel.Children.Count > 0)
            main.Children.Add(_buttonsPanel);

        // Cancel hint
        if (_instance.Definition.CancelAction is not null || _instance.Definition.CanCloseWithEscape)
        {
            main.Children.Add(new TextBlock
            {
                Text = Translations.dialog_render_cancel_hint,
                Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                TextWrapping = TextWrapping.Wrap
            });
        }

        main.Children.Add(_status);
        scroll.Content = main;
        root.Children.Add(scroll);

        return root;
    }

    private void EnsureActionButtons()
    {
        if (_buttonsPanel.Children.Count > 0)
            return;

        foreach (var action in _instance.Definition.Actions)
        {
            var btn = new Button
            {
                Content = McColorParser.CreateColoredTextBlock(action.Label),
                Padding = new Thickness(1),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Background = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
                Margin = new Thickness(0, 0, 1, 1)
            };
            btn.Click += (_, _) => Click(action.Index);
            _buttonsPanel.Children.Add(btn);
        }

        if (_instance.Definition.CancelAction is not null || _instance.Definition.CanCloseWithEscape)
        {
            var cancel = new Button
            {
                Content = McColorParser.CreateColoredTextBlock(Translations.tui_dialog_cancel),
                Padding = new Thickness(1),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(80, 40, 40)),
                Background = new SolidColorBrush(Color.FromRgb(50, 25, 25))
            };
            cancel.Click += (_, _) => TryCloseByUser();
            _buttonsPanel.Children.Add(cancel);
        }
    }

    private Control BuildInput(DialogInput input)
    {
        _instance.Values.TryGetValue(input.Key, out var value);
        value ??= input.InitialValue;

        var panel = new StackPanel { Spacing = 0, Margin = new Thickness(0) };

        if (input.LabelVisible && !string.IsNullOrWhiteSpace(input.Label))
            panel.Children.Add(McColorParser.CreateColoredTextBlock(input.Label));

        Control inner = input.Kind switch
        {
            DialogInputKind.Boolean => BuildBooleanInput(value, input),
            DialogInputKind.SingleOption => BuildOptionInput(input, value),
            DialogInputKind.NumberRange => BuildNumberInput(input, value),
            _ => BuildTextInput(input, value)
        };

        _inputControls[input.Key] = inner;

        if (input.Kind == DialogInputKind.Boolean)
        {
            panel.Children.Add(inner);
        }
        else
        {
            panel.Children.Add(new Border
            {
                Background = new SolidColorBrush(InputBg),
                BorderBrush = new SolidColorBrush(Color.FromRgb(55, 55, 55)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(1),
                Child = inner
            });
        }

        return panel;
    }

    private Control BuildTextInput(DialogInput input, string value)
    {
        var tb = new TextBox
        {
            Text = value,
            AcceptsReturn = input.Multiline,
            TextWrapping = input.Multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
            MaxLength = input.MaxLength,
            Foreground = Brushes.White,
            Background = new SolidColorBrush(InputBg),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0)
        };

        return tb;
    }

    private Control BuildBooleanInput(string value, DialogInput input)
    {
        return new CheckBox
        {
            IsChecked = value.Equals("true", StringComparison.OrdinalIgnoreCase),
            Foreground = Brushes.White,
            Padding = new Thickness(0)
        };
    }

    private Control BuildOptionInput(DialogInput input, string value)
    {
        var combo = new ComboBox
        {
            ItemsSource = input.Options ?? [],
            Foreground = Brushes.White,
            Background = new SolidColorBrush(InputBg),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(1, 0)
        };

        combo.SelectedItem = input.Options?.FirstOrDefault(option => option.Id.Equals(value, StringComparison.Ordinal))
            ?? input.Options?.FirstOrDefault();

        return combo;
    }

    private Control BuildNumberInput(DialogInput input, string value)
    {
        double numValue = double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : input.InitialNumber ?? input.Start;

        double min = Math.Min(input.Start, input.End);
        double max = Math.Max(input.Start, input.End);

        var panel = new DockPanel { LastChildFill = true };

        var slider = new Slider
        {
            Minimum = min,
            Maximum = max,
            Value = numValue,
            TickFrequency = input.Step ?? 1,
            IsSnapToTickEnabled = input.Step is not null,
            Foreground = new SolidColorBrush(AccentColor)
        };

        var label = new TextBlock
        {
            Text = numValue.ToString(CultureInfo.InvariantCulture),
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2, 0, 0, 0),
            MinWidth = 16
        };

        slider.PropertyChanged += (_, e) =>
        {
            if (e.Property == Slider.ValueProperty)
                label.Text = ((float)slider.Value).ToString(CultureInfo.InvariantCulture);
        };

        DockPanel.SetDock(label, Dock.Right);
        panel.Children.Add(slider);
        panel.Children.Add(label);

        return panel;
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

    private void FocusFirstInput()
    {
        var first = _inputControls.Values.FirstOrDefault();
        if (first is TextBox tb)
        {
            tb.Focus();
            tb.SelectAll();
        }
        else
        {
            first?.Focus();
        }
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
        if (e.Key == Key.Escape)
        {
            TryCloseByUser();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
            if (focused is TextBox && _instance.Definition.Actions.Count > 0)
            {
                Click(_instance.Definition.Actions[0].Index);
                e.Handled = true;
            }
        }
    }

    private static string NumberToString(float value)
    {
        var integer = (int)value;
        return integer == value
            ? integer.ToString(CultureInfo.InvariantCulture)
            : value.ToString(CultureInfo.InvariantCulture);
    }
}
