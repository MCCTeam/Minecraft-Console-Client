using Mcc.Cli.Localization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using DMCBK.Core.Plugins;

namespace Mcc.Cli.Tui.Plugins;

/// <summary>Shared chrome for the manager's inline modals (confirms, short forms).</summary>
internal static class PluginModal
{
    /// <summary>
    /// Builds a centered gold-bordered modal with Escape wired to hide it.
    /// The caller fills <paramref name="content"/> (rows, buttons) and shows it with <c>Owner.ShowModal</c>.
    /// </summary>
    /// <param name="maxWidth">
    /// Content width cap in character cells.
    /// The centered frame sizes to its content, so without a cap a wide child (a TextBox with a large MinWidth) stretches the dialog to fullscreen and pushes focused text out of the visible viewport.
    /// 64 cells fits a long URL with room to spare.
    /// </param>
    internal static Border Frame(
        PluginManagerContext ctx,
        string title,
        out StackPanel content,
        double maxWidth = 72,
        string? dismissLabel = null,
        bool showDismiss = true)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(title);
        content = new StackPanel { Spacing = 1, MaxWidth = maxWidth };

        var heading = new TextBlock
        {
            Text = title,
            Foreground = PluginUi.Cyan,
            FontWeight = FontWeight.Bold,
            TextWrapping = TextWrapping.Wrap,
        };
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        header.Children.Add(heading);
        if (showDismiss)
        {
            Button dismiss = PluginUi.Button(
                dismissLabel ?? Strings.PmBack,
                ctx.Owner.HideModal,
                PluginButtonKind.Quiet);
            Grid.SetColumn(dismiss, 1);
            header.Children.Add(dismiss);
        }
        content.Children.Add(header);

        var frame = new Border
        {
            BorderBrush = PluginUi.Gold,
            BorderThickness = new Thickness(1),
            Background = PluginUi.Canvas,
            Padding = new Thickness(2, 1),
            MaxHeight = 34,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new ScrollViewer
            {
                Content = content,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            },
            Focusable = true,
        };
        frame.KeyDown += (_, e) =>
        {
            if (e.Key is Key.Escape)
            {
                ctx.Owner.HideModal();
                e.Handled = true;
            }
        };
        return frame;
    }

    /// <summary>A label-above-editor field for modal and page forms.</summary>
    /// <remarks>
    /// Stacked, never side-by-side: a DockPanel/StackPanel row measured its children in character cells (a MinWidth of 320 is 320 CELLS, not pixels), so side-by-side rows blew the dialog to fullscreen and scrolled pasted text out of view.
    /// Labels above full-width fields fit narrow terminals, survive pastes, and read the same in every form.
    /// Widths here are cells: 56 fits a long git URL with the caret visible at the end.
    /// </remarks>
    internal static Control LabeledField(string label, Control editor, double fieldWidth = 62)
        => PluginUi.FormField(label, editor, width: fieldWidth);

    /// <summary>
    /// Closes the open modal on Escape from an editor.
    /// Modal frames already handle bubbled Escape, but a focused TextBox may consume the key first; wiring the editor directly makes one press sufficient everywhere.
    /// </summary>
    internal static void EscToHide(PluginManagerContext ctx, params Control[] editors)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        foreach (Control editor in editors)
        {
            editor.KeyDown += (_, e) =>
            {
                if (e.Key is Key.Escape)
                {
                    ctx.Owner.HideModal();
                    e.Handled = true;
                }
            };
        }
    }

    /// <summary>A button row for modal and page footers.</summary>
    internal static WrapPanel ButtonRow(params Button[] buttons) => PluginUi.Actions(buttons);

    /// <summary>
    /// A wrapping button row for footers that may overflow narrow terminals.
    /// WrapPanel has no spacing of its own, so buttons carry a right margin instead.
    /// </summary>
    internal static WrapPanel ButtonWrap(params Control[] controls)
    {
        return PluginUi.Actions(controls);
    }

    /// <summary>Makes a button: hides the modal first when one is open, then runs the action.</summary>
    internal static Button ActionButton(
        PluginManagerContext ctx,
        string label,
        Action onClick,
        bool danger = false,
        bool primary = false,
        bool closeModal = true)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        return PluginUi.Button(label, () =>
        {
            if (closeModal)
                ctx.Owner.HideModal();

            onClick();
        }, danger ? PluginButtonKind.Danger : primary ? PluginButtonKind.Primary : PluginButtonKind.Secondary,
            isDefault: primary);
    }
}
