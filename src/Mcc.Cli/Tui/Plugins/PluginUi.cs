using Mcc.Cli.Localization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Mcc.Cli.Tui.Plugins;

/// <summary>Visual language shared by every page and modal in the plugin manager.</summary>
internal static class PluginUi
{
    internal static readonly IBrush Canvas = new SolidColorBrush(Color.FromRgb(15, 15, 25));
    internal static readonly IBrush Panel = new SolidColorBrush(Color.FromRgb(24, 24, 38));
    internal static readonly IBrush Raised = new SolidColorBrush(Color.FromRgb(31, 42, 48));
    internal static readonly IBrush Cyan = new SolidColorBrush(Color.FromRgb(0, 229, 229));
    internal static readonly IBrush CyanDark = new SolidColorBrush(Color.FromRgb(0, 139, 139));
    internal static readonly IBrush Gold = new SolidColorBrush(Color.FromRgb(255, 215, 94));
    internal static readonly IBrush Muted = new SolidColorBrush(Color.FromRgb(119, 119, 119));
    internal static readonly IBrush Soft = new SolidColorBrush(Color.FromRgb(170, 170, 170));
    internal static readonly IBrush White = Brushes.White;
    internal static readonly IBrush Red = new SolidColorBrush(Color.FromRgb(255, 85, 85));
    internal static readonly IBrush RedPanel = new SolidColorBrush(Color.FromRgb(58, 25, 32));
    internal static readonly IBrush Green = new SolidColorBrush(Color.FromRgb(85, 255, 85));

    internal static Control PageHeader(string title, string subtitle)
    {
        var text = new StackPanel { Spacing = 0 };
        text.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = Cyan,
            FontWeight = FontWeight.Bold,
        });
        text.Children.Add(new TextBlock
        {
            Text = subtitle,
            Foreground = Soft,
            TextWrapping = TextWrapping.Wrap,
        });
        return text;
    }

    internal static TextBlock SectionTitle(string text)
        => new()
        {
            Text = text,
            Foreground = Gold,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 1, 0, 0),
        };

    internal static Border Card(Control child, bool danger = false)
        => new()
        {
            Child = child,
            Background = danger ? RedPanel : Panel,
            BorderBrush = danger ? Red : CyanDark,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

    internal static Border EmptyState(string title, string detail)
    {
        var content = new StackPanel { Spacing = 0 };
        content.Children.Add(new TextBlock { Text = title, Foreground = Gold, FontWeight = FontWeight.Bold });
        content.Children.Add(new TextBlock { Text = detail, Foreground = Soft, TextWrapping = TextWrapping.Wrap });
        return Card(content);
    }

    internal static WrapPanel Actions(params Control[] controls)
    {
        var panel = new WrapPanel { Margin = new Thickness(0, 1, 0, 0) };
        foreach (Control control in controls)
        {
            control.Margin = new Thickness(0, 0, 1, 1);
            panel.Children.Add(control);
        }

        return panel;
    }

    internal static Button Button(
        string label,
        Action action,
        PluginButtonKind kind = PluginButtonKind.Secondary,
        bool isDefault = false)
    {
        ArgumentNullException.ThrowIfNull(action);
        var button = new Button
        {
            Content = label,
            Padding = new Thickness(1, 0),
            BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Left,
            IsDefault = isDefault,
        };

        switch (kind)
        {
            case PluginButtonKind.Primary:
                button.Background = Cyan;
                button.Foreground = Brushes.Black;
                button.BorderBrush = Cyan;
                break;
            case PluginButtonKind.Danger:
                button.Background = RedPanel;
                button.Foreground = Red;
                button.BorderBrush = Red;
                break;
            case PluginButtonKind.Quiet:
                button.Background = Canvas;
                button.Foreground = Soft;
                button.BorderBrush = Muted;
                break;
            default:
                button.Background = Raised;
                button.Foreground = Cyan;
                button.BorderBrush = CyanDark;
                break;
        }

        IBrush restingForeground = button.Foreground;
        void UpdateForeground()
            => button.Foreground = !button.IsEnabled
                ? Muted
                : button.IsFocused ? White : restingForeground;

        // ModernTheme paints the focused button's internal panel dark blue.
        // Explicit button colours otherwise keep their local foreground, which made the primary button black on dark blue and muted disabled actions look enabled.
        // Keep the palette and retain the theme's high-contrast focus state.
        button.GotFocus += (_, _) => UpdateForeground();
        button.LostFocus += (_, _) => UpdateForeground();
        button.PropertyChanged += (_, e) =>
        {
            if (e.Property == InputElement.IsEnabledProperty)
                UpdateForeground();
        };

        button.Click += (_, _) => action();
        return button;
    }

    internal static Button RowButton(string text, Action action)
    {
        Button button = Button(text, action, PluginButtonKind.Quiet);
        button.HorizontalAlignment = HorizontalAlignment.Stretch;
        button.HorizontalContentAlignment = HorizontalAlignment.Left;
        button.Background = Canvas;
        button.BorderBrush = Panel;
        return button;
    }

    internal static TextBox SearchBox() => new()
    {
        Watermark = Strings.PmSearch,
        Height = 3,
        MinHeight = 3,
        MinWidth = 12,
        MaxWidth = 72,
        Padding = new Thickness(1, 0),
        HorizontalAlignment = HorizontalAlignment.Stretch,
        HorizontalContentAlignment = HorizontalAlignment.Stretch,
        VerticalContentAlignment = VerticalAlignment.Center,
    };

    internal static Control FormField(string label, Control editor, string? help = null, double width = 62)
    {
        var field = new StackPanel { Spacing = 0, HorizontalAlignment = HorizontalAlignment.Stretch };
        field.Children.Add(new TextBlock { Text = label, Foreground = Soft });

        var editorHost = new Border
        {
            Child = editor,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        if (editor is Button)
        {
            editor.MinWidth = 0;
            editor.MaxWidth = double.PositiveInfinity;
            editor.HorizontalAlignment = HorizontalAlignment.Left;
        }
        else
        {
            editorHost.Width = width;
            editor.MinWidth = 0;
            editor.MaxWidth = double.PositiveInfinity;
            editor.HorizontalAlignment = HorizontalAlignment.Stretch;
        }

        field.Children.Add(editorHost);
        if (!string.IsNullOrWhiteSpace(help))
        {
            field.Children.Add(new TextBlock
            {
                Text = help,
                Foreground = Muted,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(1, 0, 0, 0),
            });
        }

        return field;
    }

    internal static Grid KeyValue(string label, string value, IBrush? valueBrush = null)
    {
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("18,*") };
        row.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = Soft,
            TextWrapping = TextWrapping.Wrap,
        });
        var rendered = new TextBlock
        {
            Text = value,
            Foreground = valueBrush ?? White,
            TextWrapping = TextWrapping.Wrap,
        };
        Grid.SetColumn(rendered, 1);
        row.Children.Add(rendered);
        return row;
    }
}

internal enum PluginButtonKind
{
    Primary,
    Secondary,
    Danger,
    Quiet,
}
