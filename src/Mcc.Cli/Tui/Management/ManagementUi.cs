using Mcc.Cli.Localization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Mcc.Cli.Tui.Management;

internal enum ManagementButtonKind
{
    Primary,
    Secondary,
    Danger,
    Quiet,
}

/// <summary>The visual vocabulary shared by all management browsers.</summary>
internal static class ManagementUi
{
    internal static readonly IBrush Canvas = new SolidColorBrush(Color.FromRgb(15, 15, 25));
    internal static readonly IBrush Panel = new SolidColorBrush(Color.FromRgb(24, 24, 38));
    internal static readonly IBrush Raised = new SolidColorBrush(Color.FromRgb(31, 42, 48));
    internal static readonly IBrush Cyan = new SolidColorBrush(Color.FromRgb(0, 229, 229));
    internal static readonly IBrush CyanDark = new SolidColorBrush(Color.FromRgb(0, 139, 139));
    internal static readonly IBrush Gold = new SolidColorBrush(Color.FromRgb(255, 215, 94));
    internal static readonly IBrush Muted = new SolidColorBrush(Color.FromRgb(119, 119, 119));
    internal static readonly IBrush Soft = new SolidColorBrush(Color.FromRgb(170, 170, 170));
    internal static readonly IBrush Red = new SolidColorBrush(Color.FromRgb(255, 85, 85));
    internal static readonly IBrush RedPanel = new SolidColorBrush(Color.FromRgb(58, 25, 32));
    internal static readonly IBrush Green = new SolidColorBrush(Color.FromRgb(85, 255, 85));

    internal static Button Button(
        string label,
        Action action,
        ManagementButtonKind kind = ManagementButtonKind.Secondary,
        bool isDefault = false)
    {
        var button = new Button
        {
            Content = label,
            Padding = new Thickness(1, 0),
            BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            IsDefault = isDefault,
        };

        (button.Background, button.Foreground, button.BorderBrush) = kind switch
        {
            ManagementButtonKind.Primary => (Cyan, Brushes.Black, Cyan),
            ManagementButtonKind.Danger => (RedPanel, Red, Red),
            ManagementButtonKind.Quiet => (Canvas, Soft, Muted),
            _ => (Raised, Cyan, CyanDark),
        };

        IBrush resting = button.Foreground;
        void Restyle() => button.Foreground = !button.IsEnabled
            ? Muted
            : button.IsFocused ? Brushes.White : resting;
        button.GotFocus += (_, _) => Restyle();
        button.LostFocus += (_, _) => Restyle();
        button.PropertyChanged += (_, e) =>
        {
            if (e.Property == InputElement.IsEnabledProperty)
                Restyle();
        };
        button.Click += (_, _) => action();
        return button;
    }

    internal static Button HeaderButton(
        string label,
        Action action,
        ManagementButtonKind kind)
    {
        Button button = Button(label, action, kind);
        button.Height = 3;
        button.MinHeight = 3;
        button.Padding = new Thickness(1, 0, 1, 0);
        return button;
    }

    internal static void SetResponsiveBody(
        ContentControl host,
        bool compact,
        bool showingDetail,
        Control list,
        Control detail,
        string wideColumns)
    {
        if (host.Content is Grid existingGrid)
            existingGrid.Children.Clear();
        host.Content = null;

        if (compact)
        {
            host.Content = showingDetail ? detail : list;
            return;
        }

        var panes = new Grid { ColumnDefinitions = new ColumnDefinitions(wideColumns) };
        panes.Children.Add(list);
        Grid.SetColumn(detail, 1);
        panes.Children.Add(detail);
        host.Content = panes;
    }

    internal static TextBlock Section(string text) => new()
    {
        Text = text,
        Foreground = Gold,
        FontWeight = FontWeight.Bold,
        Margin = new Thickness(0, 1, 0, 0),
        TextWrapping = TextWrapping.Wrap,
    };

    internal static Border Card(Control child, bool danger = false, bool compact = false) => new()
    {
        Child = child,
        Background = danger ? RedPanel : Panel,
        BorderBrush = danger ? Red : CyanDark,
        BorderThickness = new Thickness(1),
        Padding = new Thickness(1),
        HorizontalAlignment = compact ? HorizontalAlignment.Center : HorizontalAlignment.Stretch,
        VerticalAlignment = compact ? VerticalAlignment.Center : VerticalAlignment.Stretch,
        MaxWidth = compact ? 64 : double.PositiveInfinity,
    };

    internal static TextBlock Body(string text, IBrush? foreground = null) => new()
    {
        Text = text,
        Foreground = foreground ?? Brushes.White,
        TextWrapping = TextWrapping.Wrap,
    };

    internal static TextBlock VirtualizedItemText<T>(
        T? item,
        Func<T, string> label,
        TextWrapping wrapping = TextWrapping.Wrap)
        where T : class
        => new()
        {
            // Avalonia rebuilds recycled presenters once with an unset data item.
            // Rendering an empty row during that transition keeps list scrolling and source replacement safe.
            Text = item is null ? string.Empty : label(item),
            TextWrapping = wrapping,
            HorizontalAlignment = HorizontalAlignment.Left,
        };

    internal static TextBox SearchBox() => new()
    {
        Watermark = Strings.MgmtSearch,
        Height = 3,
        MinHeight = 3,
        MinWidth = 12,
        Padding = new Thickness(1, 0),
        HorizontalAlignment = HorizontalAlignment.Stretch,
        HorizontalContentAlignment = HorizontalAlignment.Stretch,
        VerticalContentAlignment = VerticalAlignment.Center,
        Margin = new Thickness(0, 0, 0, 1),
    };

    internal static Grid FilterSearchToolbar(
        Control filters,
        Control refresh,
        TextBox search,
        Control? searchLeading = null)
    {
        refresh.HorizontalAlignment = HorizontalAlignment.Right;
        refresh.Margin = new Thickness(1, 1, 0, 1);
        var toolbar = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
        };
        toolbar.Children.Add(filters);
        Grid.SetColumn(refresh, 1);
        toolbar.Children.Add(refresh);

        Control searchRow = search;
        if (searchLeading is not null)
        {
            searchLeading.Margin = new Thickness(0, 0, 1, 1);
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
            row.Children.Add(searchLeading);
            Grid.SetColumn(search, 1);
            row.Children.Add(search);
            searchRow = row;
        }

        Grid.SetRow(searchRow, 1);
        Grid.SetColumnSpan(searchRow, 2);
        toolbar.Children.Add(searchRow);
        return toolbar;
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
}
