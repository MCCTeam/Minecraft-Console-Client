using Mcc.Cli.Tui.Presentation;
using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using DMCBK.Core;
using DMCBK.Core.Commands;
using Umpk.Text;

namespace Mcc.Cli.Tui.Container;

/// <summary>
/// One inventory slot cell: a checkerboard-backed border showing the item's display name and stack count, with pointer routing to the container's click handler.
/// The visual/interaction model is ported from the legacy container views; the data is the core <see cref="ItemStackInfo"/>.
/// </summary>
internal sealed class SlotCell : Border
{
    /// <summary>The cell's outer width in terminal columns.</summary>
    private const int CellWidth = 14;

    /// <summary>
    /// The columns a cell can actually print in: <see cref="CellWidth"/> less the one-column border on each side.
    /// The cell is a single row, so a name longer than this has to be cut rather than wrapped.
    /// </summary>
    private const int NameWidth = CellWidth - 2;

    private readonly ITranslationSource _translations;
    private readonly TextBlock _name;
    private readonly TextBlock _count;

    /// <summary>
    /// Builds a cell.
    /// <paramref name="translations"/> is the session's translation chain, needed because the name a slot shows is a resolved vanilla lang entry, not the registry id.
    /// </summary>
    public SlotCell(
        int slotIndex,
        int checker,
        ITranslationSource translations,
        Action<int, MouseButtonKind> onClick,
        Action<int, bool> onHover)
    {
        ArgumentNullException.ThrowIfNull(translations);
        _translations = translations;
        SlotIndex = slotIndex;
        Width = CellWidth;
        Height = 3;
        BorderBrush = Brushes.DimGray;
        BorderThickness = new Thickness(1);
        Background = checker == 0 ? new SolidColorBrush(Color.FromRgb(28, 28, 34)) : new SolidColorBrush(Color.FromRgb(22, 22, 28));

        _name = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.White, FontSize = 1 };
        _count = new TextBlock
        {
            Foreground = Brushes.Gold,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
        };
        Child = new Panel { Children = { _name, _count } };

        PointerPressed += (_, e) =>
        {
            PointerPoint point = e.GetCurrentPoint(this);
            MouseButtonKind kind = point.Properties.IsRightButtonPressed ? MouseButtonKind.Right : MouseButtonKind.Left;
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                kind = MouseButtonKind.Shift;

            onClick(SlotIndex, kind);
        };

        // tracks which slot the pointer is over, so the container's Q/Ctrl+Q drop shortcuts (which carry no slot argument of their own) know what to drop.
        PointerEntered += (_, _) => onHover(SlotIndex, true);
        PointerExited += (_, _) => onHover(SlotIndex, false);
    }

    public int SlotIndex { get; }

    public void Update(ItemStackInfo stack)
    {
        ArgumentNullException.ThrowIfNull(stack);
        if (stack.IsEmpty)
        {
            _name.Text = string.Empty;
            _count.Text = string.Empty;
            return;
        }

        string count = stack.Count > 1 ? stack.Count.ToString(CultureInfo.InvariantCulture) : string.Empty;
        _count.Text = count;

        // The count is drawn over the same single row as the name (both live in one Panel), so the columns it occupies, plus one of separation, are not columns the name may use.
        int room = count.Length == 0 ? NameWidth : NameWidth - count.Length - 1;
        _name.Text = Fit(DisplayName(stack, _translations), room);
    }

    /// <summary>
    /// The name a slot shows, untruncated: the stack's custom name when the server gave it one, else the item's real display name.
    /// <para>
    /// The display name is resolved by <see cref="InventoryRendering.TypeName"/>, the same call the text listing (<c>/inventory list</c>) uses, so a slot reads "Cobblestone" in both places rather than "cobblestone" in one and "Cobblestone" in the other.
    /// Trimming the registry id was the bug: it showed "red_stained_" and "netherite_sw", which are ids, not names.
    /// </para>
    /// <para>
    /// Preferring the custom name is a deliberate step past the legacy cell, which always printed the type name (Tui/SlotViewModel.cs:69-71) and carried the custom name only in the hover tooltip's <c>ToFullString</c>.
    /// This view has no tooltip, and vanilla itself labels a renamed stack by its given name, so a stack the server renamed shows that name here.
    /// </para>
    /// </summary>
    internal static string DisplayName(ItemStackInfo stack, ITranslationSource translations)
    {
        ArgumentNullException.ThrowIfNull(stack);
        ArgumentNullException.ThrowIfNull(translations);

        if (!string.IsNullOrEmpty(stack.CustomName))
        {
            string custom = StripLegacyCodes(stack.CustomName).Trim();
            if (custom.Length > 0)
                return custom;
        }

        return InventoryRendering.TypeName(translations, stack.ItemId);
    }

    /// <summary>
    /// Cuts <paramref name="name"/> to <paramref name="width"/> columns, as legacy's single-line cell did (Tui/SlotViewModel.cs:104-105).
    /// The cut is applied to the RESOLVED name, so what survives is the head of a real name ("Netherite Sw") rather than the head of an id.
    /// </summary>
    internal static string Fit(string name, int width)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (width <= 0)
            return string.Empty;

        return name.Length <= width ? name : name[..width];
    }

    // ItemStackInfo.CustomName comes from Component.ToPlainText, which deliberately leaves legacy section-sign codes verbatim (umpk/src/Umpk.Text/Component.cs:90-97).
    // A one-row cell cannot render them as colour, and printed literally each would eat two of its twelve columns, so they are dropped.
    private static string StripLegacyCodes(string text)
    {
        if (!LegacyTextInlines.HasCodes(text))
            return text;

        var sb = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '§' && i + 1 < text.Length)
            {
                i++;
                continue;
            }

            sb.Append(text[i]);
        }

        return sb.ToString();
    }
}

/// <summary>The kind of slot click the TUI maps to a window action.</summary>
internal enum MouseButtonKind
{
    Left,
    Right,
    Shift,
}
