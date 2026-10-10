using DMCBK.Core.Commands;
using DMCBK.Core.Localization;
using Umpk.Commands;

namespace Mcc.Cli.Tui.Minimap;

/// <summary>
/// The host-registered <c>minimap</c> command (on/off/zoom/names/cave/position), registered into the dispatcher from Mcc.Cli exactly like <c>clear-console</c>.
/// It is a host-owned TUI command that mutates the TUI <see cref="MinimapController"/>; it runs only in TUI mode and reports that in classic mode.
/// <para>
/// The grammar: the bare command toggles the minimap, every bare subcommand reports the current setting instead of toggling, and <c>zoom</c> accepts <c>in</c>/<c>out</c> as well as an explicit level.
/// </para>
/// <para>
/// Every message comes from the legacy string corpus (<c>cmd.minimap.*</c>), like the legacy command; the controller is a pure state mutator and does not phrase anything.
/// </para>
/// </summary>
internal sealed class MinimapCommand : CommandBase
{
    private readonly MinimapController _minimap;

    public MinimapCommand(MinimapController minimap) => _minimap = minimap;

    public override string CmdName => "minimap";

    public override string CmdDesc => Mcc.Cli.Localization.MccStrings.Get("cmd.minimap.desc");

    /// <summary>The legacy usage line, which is the spec for the grammar below.</summary>
    public override string CmdUsage =>
        "minimap [on|off] | minimap zoom [in|out|<1-16>] | minimap names [players|hostile|neutral|passive] [on|off] "
        + "| minimap names [all_on|all_off] | minimap position [top_left|top_right|center|bottom_left|bottom_right] "
        + "| minimap cave [auto|on|off]";

    /// <inheritdoc/>
    public override CommandCategory Category => CommandCategory.Client;

    /// <inheritdoc/>
    public override IReadOnlyList<UsageLine> UsageLines =>
    [
        new(string.Empty, "toggle the movable minimap window"),
    ];

    /// <inheritdoc/>
    public override IReadOnlyList<string> Examples => ["minimap"];

    /// <inheritdoc/>
    public override string? ManTopic => "tui";

    public override void Register(CommandBuilder<CommandContext> builder)
    {
        CommandNodeBuilder<CommandContext> help = RegisterHelp(builder, CmdName, ShowUsage);
        builder.Literal(CmdName, l => l
            .Executes(ctx => Toggle(ctx.Source))
            .ThenLiteral("on", h => h.Executes(ctx => SetEnabled(ctx.Source, true)))
            .ThenLiteral("off", h => h.Executes(ctx => SetEnabled(ctx.Source, false)))
            .ThenLiteral("zoom", h => h
                // Legacy: a bare "zoom" REPORTS the current level; "in" lowers the blocks-per-pixel ratio (closer), "out" raises it.
                // Both clamp to MinZoom/MaxZoom instead of failing.
                .Executes(ctx => ZoomInfo(ctx.Source))
                .ThenLiteral("in", a => a.Executes(ctx => ZoomStep(ctx.Source, -1)))
                .ThenLiteral("out", a => a.Executes(ctx => ZoomStep(ctx.Source, +1)))
                .ThenArgument("level", Arguments.Integer(MinimapControl.MinZoom, MinimapControl.MaxZoom), a => a
                    .Executes(ctx => SetZoom(ctx.Source, ctx.GetArgument<int>("level")))))
            .ThenLiteral("names", h =>
            {
                h.Executes(ctx => NamesInfo(ctx.Source))
                    .ThenLiteral("all_on", a => a.Executes(ctx => NamesAll(ctx.Source, true)))
                    .ThenLiteral("all_off", a => a.Executes(ctx => NamesAll(ctx.Source, false)));
                NamesCategory(h, "players", MobCategory.Player);
                NamesCategory(h, "hostile", MobCategory.Hostile);
                NamesCategory(h, "neutral", MobCategory.Neutral);
                NamesCategory(h, "passive", MobCategory.Passive);
            })
            .ThenLiteral("position", h => h
                .Executes(ctx => PositionInfo(ctx.Source))
                .ThenLiteral("top_left", a => a.Executes(ctx => PositionSet(ctx.Source, MinimapPosition.TopLeft)))
                .ThenLiteral("top_right", a => a.Executes(ctx => PositionSet(ctx.Source, MinimapPosition.TopRight)))
                .ThenLiteral("center", a => a.Executes(ctx => PositionSet(ctx.Source, MinimapPosition.Center)))
                .ThenLiteral("bottom_left", a => a.Executes(ctx => PositionSet(ctx.Source, MinimapPosition.BottomLeft)))
                .ThenLiteral("bottom_right", a => a.Executes(ctx => PositionSet(ctx.Source, MinimapPosition.BottomRight))))
            .ThenLiteral("cave", h => h
                .Executes(ctx => CaveInfo(ctx.Source))
                .ThenLiteral("auto", a => a.Executes(ctx => CaveSet(ctx.Source, MinimapCaveMode.Auto)))
                .ThenLiteral("on", a => a.Executes(ctx => CaveSet(ctx.Source, MinimapCaveMode.On)))
                .ThenLiteral("off", a => a.Executes(ctx => CaveSet(ctx.Source, MinimapCaveMode.Off))))
            .ThenLiteral("_help", h => h.Executes(ctx => ShowUsage(ctx.Source)).RedirectTo(help)));
    }

    /// <summary>Registers one <c>names &lt;category&gt; [on|off]</c> subtree (bare form reports).</summary>
    private void NamesCategory(CommandNodeBuilder<CommandContext> parent, string literal, MobCategory category) =>
        parent.ThenLiteral(literal, c => c
            .Executes(ctx => NamesCategoryInfo(ctx.Source, category))
            .ThenLiteral("on", a => a.Executes(ctx => NamesCategorySet(ctx.Source, category, true)))
            .ThenLiteral("off", a => a.Executes(ctx => NamesCategorySet(ctx.Source, category, false))));

    private int Toggle(CommandContext ctx) => SetEnabled(ctx, !_minimap.Enabled);

    private int SetEnabled(CommandContext ctx, bool on)
    {
        _minimap.SetEnabled(on);
        return ctx.Result.Ok(Mcc.Cli.Localization.MccStrings.Get(on ? "cmd.minimap.enabled" : "cmd.minimap.disabled"));
    }

    private int ZoomInfo(CommandContext ctx) =>
        ctx.Result.Ok(Mcc.Cli.Localization.MccStrings.Format("cmd.minimap.zoom_current", _minimap.Zoom, MinimapControl.MaxZoom));

    private int ZoomStep(CommandContext ctx, int delta) => SetZoom(ctx, _minimap.Zoom + delta);

    private int SetZoom(CommandContext ctx, int level)
    {
        _minimap.SetZoom(level);
        // Report what was actually applied: the controller clamps to MinZoom/MaxZoom, which is what makes "zoom in" at level 1 (and "zoom out" at the maximum) a no-op report rather than an error.
        return ctx.Result.Ok(Mcc.Cli.Localization.MccStrings.Format("cmd.minimap.zoom_set", _minimap.Zoom));
    }

    private int NamesInfo(CommandContext ctx) =>
        ctx.Result.Ok(Mcc.Cli.Localization.MccStrings.Format(
            "cmd.minimap.names_status",
            BoolStr(_minimap.GetNameCategory(MobCategory.Player)),
            BoolStr(_minimap.GetNameCategory(MobCategory.Hostile)),
            BoolStr(_minimap.GetNameCategory(MobCategory.Neutral)),
            BoolStr(_minimap.GetNameCategory(MobCategory.Passive))));

    private int NamesAll(CommandContext ctx, bool on)
    {
        _minimap.SetAllNameCategories(on);
        return ctx.Result.Ok(Mcc.Cli.Localization.MccStrings.Get(on ? "cmd.minimap.names_all_on" : "cmd.minimap.names_all_off"));
    }

    private int NamesCategoryInfo(CommandContext ctx, MobCategory category) =>
        ctx.Result.Ok(Mcc.Cli.Localization.MccStrings.Format(
            "cmd.minimap.names_cat", category, BoolStr(_minimap.GetNameCategory(category))));

    private int NamesCategorySet(CommandContext ctx, MobCategory category, bool on)
    {
        _minimap.SetNameCategory(category, on);
        return ctx.Result.Ok(Mcc.Cli.Localization.MccStrings.Format("cmd.minimap.names_cat_set", category, BoolStr(on)));
    }

    private int PositionInfo(CommandContext ctx) =>
        ctx.Result.Ok(Mcc.Cli.Localization.MccStrings.Format("cmd.minimap.position_current", PositionToken(_minimap.Position)));

    private int PositionSet(CommandContext ctx, MinimapPosition position)
    {
        _minimap.SetPosition(position);
        return ctx.Result.Ok(Mcc.Cli.Localization.MccStrings.Format("cmd.minimap.position_set", PositionToken(position)));
    }

    private int CaveInfo(CommandContext ctx) =>
        ctx.Result.Ok(Mcc.Cli.Localization.MccStrings.Format("cmd.minimap.cave_current", CaveToken(_minimap.CaveMode)));

    private int CaveSet(CommandContext ctx, MinimapCaveMode mode)
    {
        _minimap.SetCaveMode(mode);
        return ctx.Result.Ok(Mcc.Cli.Localization.MccStrings.Format("cmd.minimap.cave_set", CaveToken(mode)));
    }

    /// <summary>
    /// The legacy <c>BoolStr</c> helper.
    /// ON/OFF are literal state tokens with no corpus key of their own; they are formatted into the localized <c>cmd.minimap.names_*</c> sentences.
    /// </summary>
    private static string BoolStr(bool value) => value ? "ON" : "OFF";

    /// <summary>
    /// The command-grammar token for a position, so the report reads back exactly what the command accepts (<c>top_left</c>, not <c>TopLeft</c>).
    /// Legacy got this for free: its enum members were snake_case.
    /// </summary>
    private static string PositionToken(MinimapPosition position) => position switch
    {
        MinimapPosition.TopLeft => "top_left",
        MinimapPosition.BottomLeft => "bottom_left",
        MinimapPosition.BottomRight => "bottom_right",
        MinimapPosition.Center => "center",
        _ => "top_right",
    };

    /// <summary>The command-grammar token for a cave mode, for the same reason as <see cref="PositionToken"/>.</summary>
    private static string CaveToken(MinimapCaveMode mode) => mode switch
    {
        MinimapCaveMode.On => "on",
        MinimapCaveMode.Off => "off",
        _ => "auto",
    };
}
