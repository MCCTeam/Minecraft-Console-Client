using System.Globalization;
using System.Text;

namespace Mcc.Cli.Tui.Plugins;

/// <summary>The editor's reading of one TOML value. Public so tests can name the cases.</summary>
public enum SettingValueKind
{
    /// <summary>A section header (<c>[Name]</c>); never editable, only a label.</summary>
    Section,
    Bool,
    Integer,
    Float,
    Text,
    /// <summary>A single-line array of strings; anything else array-shaped is <see cref="Unsupported"/>.</summary>
    StringList,
    /// <summary>Shown read-only and preserved byte-identical: dotted keys, multiline arrays, tables.</summary>
    Unsupported,
}

/// <summary>One editor row: a section header or a key under a section, with its documentation.</summary>
/// <param name="LineIndex">The zero-based line holding the value (or the header).</param>
/// <param name="Section">The enclosing <c>[table]</c>, or empty for the root.</param>
/// <param name="Key">The key, or empty for a section header.</param>
/// <param name="Comment">Preceding comment lines plus the inline comment, joined.</param>
/// <param name="Kind">How the row edits.</param>
/// <param name="Value">The display text (unescaped, unquoted; comma-joined for lists).</param>
internal sealed record SettingRow(
    int LineIndex,
    string Section,
    string Key,
    string Comment,
    SettingValueKind Kind,
    string Value);

/// <summary>A validated replacement value for one row's line.</summary>
/// <param name="LineIndex">The row's line.</param>
/// <param name="RenderedValue">The TOML source for the value (already rendered).</param>
internal sealed record SettingEdit(int LineIndex, string RenderedValue);

/// <summary>
/// The settings editor's TOML layer, pure so it is unit-testable: parse a <c>settings.toml</c> into editor rows, render edited display text back to TOML, and merge replacements line by line.
/// <para>
/// Anything the editor cannot model (dotted keys, multiline arrays, nested tables, unknown shapes) is not a row: <see cref="ApplyEdits"/> only touches the lines it is given, so unmodeled content survives a save byte-identical.
/// Comments survive the same way, which is what makes an editor save equivalent to hand-editing the values.
/// </para>
/// </summary>
internal static class PluginSettingsModel
{
    /// <summary>Parses editor rows out of TOML source. Never throws on shape; unknowns are skipped.</summary>
    public static IReadOnlyList<SettingRow> ParseRows(string toml)
    {
        ArgumentNullException.ThrowIfNull(toml);
        var rows = new List<SettingRow>();
        string[] lines = toml.ReplaceLineEndings("\n").Split('\n');
        string section = string.Empty;
        var pendingComments = new List<string>();

        int i = 0;
        while (i < lines.Length)
        {
            string line = lines[i];
            string trimmed = line.Trim();

            if (trimmed.Length == 0)
            {
                pendingComments.Clear();
                i++;
                continue;
            }

            if (trimmed.StartsWith('#'))
            {
                pendingComments.Add(trimmed[1..].Trim());
                i++;
                continue;
            }

            if (trimmed.StartsWith('[') && trimmed.EndsWith(']') && !trimmed.StartsWith("[[", StringComparison.Ordinal))
            {
                section = trimmed[1..^1].Trim();
                rows.Add(new SettingRow(i, section, string.Empty, Join(pendingComments), SettingValueKind.Section, string.Empty));
                pendingComments.Clear();
                i++;
                continue;
            }

            int equals = IndexOfKeyEquals(line);
            if (equals < 0)
            {
                // Dotted keys, array-of-tables, continuations: preserved on save, not edited.
                pendingComments.Clear();
                i++;
                continue;
            }

            string key = line[..equals].Trim();
            (string valuePart, string inline) = SplitComment(line[(equals + 1)..]);
            string comment = Join(pendingComments, inline);

            // A value that runs past its line (multiline array) is display-only: joining it here would reformat what the save promises to preserve.
            if (valuePart.TrimStart().StartsWith('[') && !BracketsBalanced(valuePart))
            {
                var joined = new StringBuilder(valuePart.Trim());
                int j = i + 1;
                while (j < lines.Length && !BracketsBalanced(joined.ToString()))
                {
                    joined.Append(' ').Append(lines[j].Trim());
                    j++;
                }

                rows.Add(new SettingRow(i, section, key, comment, SettingValueKind.Unsupported, joined.ToString()));
                pendingComments.Clear();
                i = j;
                continue;
            }

            (SettingValueKind kind, string value) = Classify(valuePart.Trim());
            rows.Add(new SettingRow(i, section, key, comment, kind, value));
            pendingComments.Clear();
            i++;
        }

        return rows;
    }

    /// <summary>
    /// Renders edited display text to TOML source.
    /// False with a reason when the text is not that kind (the editor shows the reason inline and does not save).
    /// </summary>
    public static bool TryRender(SettingRow row, string display, out string rendered, out string? error)
    {
        ArgumentNullException.ThrowIfNull(row);
        ArgumentNullException.ThrowIfNull(display);
        rendered = string.Empty;
        error = null;
        string trimmed = display.Trim();

        switch (row.Kind)
        {
            case SettingValueKind.Bool:
                if (bool.TryParse(trimmed, out bool flag))
                {
                    rendered = flag ? "true" : "false";
                    return true;
                }

                error = "Enter true or false.";
                return false;

            case SettingValueKind.Integer:
                if (long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                {
                    rendered = trimmed;
                    return true;
                }

                error = "Enter a whole number.";
                return false;

            case SettingValueKind.Float:
                if (double.TryParse(
                    trimmed,
                    NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture,
                    out _))
                {
                    rendered = trimmed;
                    return true;
                }

                error = "Enter a number.";
                return false;

            case SettingValueKind.Text:
                rendered = $"\"{trimmed.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
                return true;

            case SettingValueKind.StringList:
                var items = trimmed.Split(',')
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 0)
                    .Select(s => $"\"{s.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"");
                rendered = "[ " + string.Join(", ", items) + " ]";
                return true;

            default:
                error = "This value cannot be edited here; change it in the file.";
                return false;
        }
    }

    /// <summary>
    /// Merges rendered replacements into the original text, touching only the given lines: every other line (comments, sections, unmodeled shapes) is copied byte-identical.
    /// </summary>
    public static string ApplyEdits(string original, IReadOnlyList<SettingEdit> edits)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(edits);
        if (edits.Count == 0)
            return original;

        bool endsWithNewline = original.EndsWith('\n');
        string[] lines = original.ReplaceLineEndings("\n").Split('\n');
        foreach (SettingEdit edit in edits)
        {
            if (edit.LineIndex < 0 || edit.LineIndex >= lines.Length)
                continue;

            string line = lines[edit.LineIndex];
            int equals = IndexOfKeyEquals(line);
            if (equals < 0)
                continue;

            // Preserve the head (key, spacing) and the inline comment; only the value is replaced.
            // The comment is located by index: the line may end in spaces the trimmed comment text no longer accounts for, so length arithmetic would land mid-comment.
            int valueStart = equals + 1;
            while (valueStart < line.Length && line[valueStart] is ' ' or '\t')
                valueStart++;

            int commentAt = CommentStart(line, equals + 1);
            if (commentAt < 0)
            {
                lines[edit.LineIndex] = line[..valueStart] + edit.RenderedValue;
                continue;
            }

            int gap = commentAt;
            while (gap > valueStart && line[gap - 1] is ' ' or '\t')
                gap--;

            lines[edit.LineIndex] = line[..valueStart] + edit.RenderedValue + line[gap..commentAt] + line[commentAt..];
        }

        string merged = string.Join('\n', lines);
        if (endsWithNewline && !merged.EndsWith('\n'))
            merged += '\n';

        return merged;
    }

    private static string Join(List<string> pending, string inline = "")
    {
        string text = string.Join(' ', pending);
        if (inline.Length > 0)
            text = text.Length == 0 ? inline : text + " " + inline;

        return text;
    }

    /// <summary>The <c>=</c> ending a bare key (<c>word =</c>), or -1 for dotted keys and the rest.</summary>
    private static int IndexOfKeyEquals(string line)
    {
        int equals = line.IndexOf('=');
        if (equals <= 0)
            return -1;

        string head = line[..equals].Trim();
        if (head.Length == 0)
            return -1;

        foreach (char c in head)
        {
            if (!char.IsLetterOrDigit(c) && c is not '_' and not '-')
                return -1;
        }

        return equals;
    }

    /// <summary>Splits a value tail into its value and its inline comment, honouring quotes.</summary>
    private static (string Value, string Comment) SplitComment(string rest)
    {
        int at = CommentStart(rest, 0);
        return at < 0
            ? (rest.TrimEnd(), string.Empty)
            : (rest[..at].TrimEnd(), rest[(at + 1)..].Trim());
    }

    /// <summary>The index where an inline comment starts, or -1. A <c>#</c> inside quotes is text.</summary>
    private static int CommentStart(string text, int start)
    {
        bool inString = false;
        bool escaped = false;
        for (int i = start; i < text.Length; i++)
        {
            char c = text[i];
            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (c == '\\' && inString)
            {
                escaped = true;
                continue;
            }

            if (c == '"')
                inString = !inString;
            else if (c == '#' && !inString)
                return i;
        }

        return -1;
    }

    private static bool BracketsBalanced(string text)
    {
        bool inString = false;
        bool escaped = false;
        int depth = 0;
        foreach (char c in text)
        {
            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (c == '\\' && inString)
            {
                escaped = true;
                continue;
            }

            if (c == '"')
                inString = !inString;
            else if (!inString && c == '[')
                depth++;
            else if (!inString && c == ']')
                depth--;
        }

        return depth <= 0;
    }

    private static (SettingValueKind Kind, string Value) Classify(string value)
    {
        if (value is "true" or "false")
            return (SettingValueKind.Bool, value);

        if (value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"'))
            return (SettingValueKind.Text, Unescape(value[1..^1]));

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            return (SettingValueKind.Integer, value);

        if (double.TryParse(
            value,
            NumberStyles.Float | NumberStyles.AllowThousands,
            CultureInfo.InvariantCulture,
            out _))
            return (SettingValueKind.Float, value);

        if (value.StartsWith('[') && value.EndsWith(']'))
        {
            var items = new List<string>();
            foreach (string part in SplitTopLevel(value[1..^1]))
            {
                string item = part.Trim();
                if (item.Length >= 2 && item.StartsWith('"') && item.EndsWith('"'))
                {
                    items.Add(Unescape(item[1..^1]));
                    continue;
                }

                if (item.Length == 0)
                    continue;

                return (SettingValueKind.Unsupported, value);
            }

            return (SettingValueKind.StringList, string.Join(", ", items));
        }

        return (SettingValueKind.Unsupported, value);
    }

    private static IEnumerable<string> SplitTopLevel(string text)
    {
        bool inString = false;
        bool escaped = false;
        int depth = 0;
        int start = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (c == '\\' && inString)
            {
                escaped = true;
                continue;
            }

            if (c == '"')
                inString = !inString;
            else if (!inString && c == '[')
                depth++;
            else if (!inString && c == ']')
                depth--;
            else if (!inString && c == ',' && depth == 0)
            {
                yield return text[start..i];
                start = i + 1;
            }
        }

        yield return text[start..];
    }

    /// <summary>
    /// Unescapes a quoted value for display.
    /// Handles <c>\"</c> and <c>\\</c> (the only escapes plugin settings carry); anything else passes through untouched, and only an edited row is ever re-rendered, so exotic values survive a save unless the user edits that very row.
    /// </summary>
    private static string Unescape(string text)
    {
        var sb = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\\' && i + 1 < text.Length && text[i + 1] is '"' or '\\')
            {
                sb.Append(text[i + 1]);
                i++;
                continue;
            }

            sb.Append(text[i]);
        }

        return sb.ToString();
    }
}
