using DMCBK.Core.Commands;
using System.Collections.Immutable;
using System.Text;
using DMCBK.Core.Localization;

namespace Mcc.Cli.Presentation;

/// <summary>
/// Builds the two things <c>/help</c> prints: the grouped index, and one command's page.
/// <para>
/// Both used to come straight out of the Brigadier tree.
/// The index was every command's raw grammar, sorted alphabetically, with no descriptions; the page was every branch of the subtree, longest first, with no glosses and two lines of <c>_help</c> plumbing at the end.
/// Both are here now so the shape of a page is decided once rather than falling out of whatever the dispatcher happens to enumerate.
/// </para>
/// </summary>
internal static class TerminalHelpRenderer
{
    public static string RenderPage(CommandHelpPresentation request, DMCBK.Core.Presentation.GlyphSet glyphs)
        => Page(request.Command, request.DerivedUsage, request.Prefix, request.NoPrefix,
            new GateStates(request.Terrain, request.Inventory, request.Entities, request.Physics, request.Pathfinding),
            new GlyphContext(glyphs.Ok, glyphs.Fail));

    private const string Head = "§e";      // section headings
    private const string Name = "§f";      // command names and syntax
    private const string Accent = "§b";    // cross-references
    private const string Reset = "§r";

    // Descriptions, glosses and hints: text the reader is meant to READ, just not first. §7 is the vanilla light grey (170,170,170).
    // This used to be §8 (85,85,85), which is a dark grey sitting on a dark terminal background: the whole right-hand column of the index was close to unreadable, and that column is where the index says what each command is for.
    private const string Body = "§7";

    // Reserved for marks that are not text: the separator between a name and its description. §8 is dark grey, which is correct for a piece of punctuation and wrong for a sentence.
    private const string Faint = "§8";

    /// <summary>
    /// The grouped index: one section per <see cref="CommandCategory"/>, one row per command, aliases inline, hidden commands omitted.
    /// </summary>
    public static string Index(IReadOnlyList<CommandBase> commands, string prefix, GlyphContext glyphs)
    {
        ArgumentNullException.ThrowIfNull(commands);

        List<CommandBase> visible = [.. commands.Where(c => c.ShowInIndex)];
        var sb = new StringBuilder();
        sb.Append(Head).Append(McStrings.Format("cmd.help.index_header", visible.Count)).Append(Reset);

        foreach (CommandCategory category in CommandCategoryNames.Order)
        {
            List<CommandBase> inCategory =
                [.. visible.Where(c => c.Category == category).OrderBy(c => c.CmdName, StringComparer.Ordinal)];
            if (inCategory.Count == 0)
                continue;

            // Column width is per SECTION, not global: one long name in a section nobody is reading should not indent every other section's glosses off to the right.
            int width = inCategory.Max(c => NameCell(c).Length);

            sb.Append('\n').Append('\n').Append(Head).Append(CommandCategoryNames.Of(category)).Append(Reset);
            foreach (CommandBase command in inCategory)
            {
                sb.Append('\n').Append("  ").Append(Name).Append(NameCell(command).PadRight(width))
                  .Append("  ").Append(Body).Append(OneLine(command.CmdDesc)).Append(Reset);
            }
        }

        sb.Append('\n').Append('\n')
          .Append(Body).Append(McStrings.Format("cmd.help.index_footer_command", prefix)).Append(Reset)
          .Append('\n').Append(Body).Append(McStrings.Format("cmd.help.index_footer_man", prefix)).Append(Reset)
          .Append('\n').Append(Body).Append(McStrings.Format("cmd.help.index_footer_server", prefix)).Append(Reset);

        _ = glyphs;
        return sb.ToString();

        static string NameCell(CommandBase c)
            => c.Aliases.Count == 0 ? c.CmdName : $"{c.CmdName} ({string.Join(", ", c.Aliases)})";
    }

    /// <summary>
    /// One command's page: header, usage rows, flags, gate status, examples, cross-references.
    /// </summary>
    /// <param name="command">The command being described.</param>
    /// <param name="derived">Raw usage lines from the dispatcher, used when the command authors none.</param>
    /// <param name="prefix">The active internal-command prefix character.</param>
    /// <param name="noPrefix">True in <c>none</c> prefix mode.</param>
    /// <param name="gates">The live gate states, for the <c>REQUIRES</c> block.</param>
    /// <param name="glyphs">The resolved glyph vocabulary.</param>
    public static string Page(
        CommandBase command,
        ImmutableArray<string> derived,
        char prefix,
        bool noPrefix,
        GateStates gates,
        GlyphContext glyphs)
    {
        ArgumentNullException.ThrowIfNull(command);

        string p = noPrefix ? string.Empty : prefix.ToString();
        var sb = new StringBuilder();

        sb.Append(Name).Append(p).Append(command.CmdName).Append(Reset);
        if (!string.IsNullOrEmpty(command.CmdDesc))
            sb.Append(Faint).Append(" - ").Append(Reset).Append(OneLine(command.CmdDesc));

        // USAGE.
        // Authored rows when there are any; otherwise the dispatcher's branches, which are at least complete even when they are not pretty.
        IReadOnlyList<UsageLine> rows = command.UsageLines;
        if (rows.Count == 0)
            rows = [.. DeriveRows(derived, command.CmdUsage)];

        if (rows.Count > 0)
        {
            sb.Append('\n').Append('\n').Append(Head).Append(McStrings.Get("cmd.help.section_usage")).Append(Reset);
            int width = rows.Max(r => r.Syntax.Length);
            foreach (UsageLine row in rows)
            {
                sb.Append('\n').Append("  ").Append(Name).Append(p).Append(command.CmdName);
                if (row.Syntax.Length > 0)
                    sb.Append(' ').Append(row.Syntax);

                if (!string.IsNullOrEmpty(row.Description))
                {
                    // Pad to the widest syntax so the glosses form a column.
                    // The +1 covers the space before the syntax that an empty syntax does not print.
                    int pad = width - row.Syntax.Length + (row.Syntax.Length == 0 ? 1 : 0);
                    sb.Append(new string(' ', Math.Max(pad, 0))).Append("   ")
                      .Append(Body).Append(row.Description);
                }

                sb.Append(Reset);
            }
        }

        if (command.Flags.Count > 0)
        {
            sb.Append('\n').Append('\n').Append(Head).Append(McStrings.Get("cmd.help.section_flags")).Append(Reset);
            int width = command.Flags.Max(f => f.Name.Length);
            foreach (UsageFlag flag in command.Flags)
            {
                sb.Append('\n').Append("  ").Append(Accent).Append(flag.Name.PadRight(width)).Append(Reset)
                  .Append("   ").Append(Body).Append(flag.Description).Append(Reset);
            }
        }

        if (command.RequiredFeatures != CommandFeature.None)
        {
            sb.Append('\n').Append('\n').Append(Head).Append(McStrings.Get("cmd.help.section_requires")).Append(Reset)
              .Append('\n').Append("  ").Append(Requires(command.RequiredFeatures, gates, glyphs))
              .Append('\n').Append("  ").Append(Body).Append(McStrings.Get("cmd.help.requires_source")).Append(Reset);
        }

        if (command.Examples.Count > 0)
        {
            sb.Append('\n').Append('\n').Append(Head).Append(McStrings.Get("cmd.help.section_examples")).Append(Reset);
            foreach (string example in command.Examples)
                sb.Append('\n').Append("  ").Append(Name).Append(p).Append(example).Append(Reset);
        }

        if (command.SeeAlso.Count > 0 || command.ManTopic is { Length: > 0 })
        {
            var refs = new List<string>();
            foreach (string other in command.SeeAlso)
                refs.Add($"{p}{other}");

            if (command.ManTopic is { Length: > 0 } topic)
                refs.Add($"{p}man {topic}");

            sb.Append('\n').Append('\n').Append(Head).Append(McStrings.Get("cmd.help.section_see_also")).Append(Reset)
              .Append('\n').Append("  ").Append(Accent).Append(string.Join("   ", refs)).Append(Reset);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Turns the dispatcher's raw branch list into usage rows for a command that authors none: drops the command's own name, de-duplicates, and keeps the order the tree produced.
    /// </summary>
    private static List<UsageLine> DeriveRows(ImmutableArray<string> derived, string fallback)
    {
        if (derived.IsDefaultOrEmpty)
        {
            // The command registered nothing enumerable (a pure argument command).
            // Its own CmdUsage is the only statement of shape there is, minus the leading command name the caller re-adds.
            string trimmed = fallback;
            int space = trimmed.IndexOf(' ');
            return [new UsageLine(space < 0 ? string.Empty : trimmed[(space + 1)..])];
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var rows = new List<UsageLine>();
        foreach (string line in derived)
        {
            // `_help` is the internal redirect node; it is plumbing, not a form anyone types.
            if (line.StartsWith('_') || !seen.Add(line))
                continue;

            rows.Add(new UsageLine(line));
        }

        return rows;
    }

    /// <summary>The <c>REQUIRES</c> row: every needed gate, with a tick or a cross for its live state.</summary>
    private static string Requires(CommandFeature required, GateStates gates, GlyphContext glyphs)
    {
        var parts = new List<string>();
        Add(CommandFeature.Terrain, gates.Terrain, "Terrain");
        Add(CommandFeature.Inventory, gates.Inventory, "Inventory");
        Add(CommandFeature.Entity, gates.Entity, "Entity");
        Add(CommandFeature.Physics, gates.Physics, "Physics");
        Add(CommandFeature.Pathfinding, gates.Pathfinding, "Pathfinding");
        return string.Join("   ", parts);

        void Add(CommandFeature flag, bool on, string label)
        {
            if (!required.HasFlag(flag))
                return;

            parts.Add(on
                ? $"§a{glyphs.Ok} {label}{Reset}"
                : $"§c{glyphs.Fail} {label}{Reset}");
        }
    }

    /// <summary>Collapses a description to one line so a multi-line corpus string cannot break the index columns.</summary>
    private static string OneLine(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        int br = text.IndexOfAny(['\n', '\r']);
        return br < 0 ? text : text[..br].TrimEnd();
    }

    /// <summary>The live gameplay gate states a <c>REQUIRES</c> block reports.</summary>
    /// <param name="Terrain">Terrain handling.</param>
    /// <param name="Inventory">Inventory handling.</param>
    /// <param name="Entity">Entity handling.</param>
    /// <param name="Physics">The local physics simulation.</param>
    /// <param name="Pathfinding">Pathfinding/navigation.</param>
    public readonly record struct GateStates(
        bool Terrain, bool Inventory, bool Entity, bool Physics, bool Pathfinding);

    /// <summary>The two glyphs the help renderer needs, so it does not take a dependency on the whole set.</summary>
    /// <param name="Ok">The affirmative glyph.</param>
    /// <param name="Fail">The negative glyph.</param>
    public readonly record struct GlyphContext(string Ok, string Fail);
}
