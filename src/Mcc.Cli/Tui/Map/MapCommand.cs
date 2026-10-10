using Mcc.Cli.Localization;
using Mcc.Cli.Tui.Minimap;
using DMCBK.Core;
using DMCBK.Core.Commands;
using Umpk.Commands;

namespace Mcc.Cli.Tui.Map;

/// <summary>
/// The host-registered <c>map</c> command: opens the fullscreen map overlay for the most recently received filled map (see <see cref="MapController"/>).
/// Registered directly in Mcc.Cli exactly like <c>MinimapCommand</c>; only meaningful in TUI mode.
/// </summary>
internal sealed class MapCommand : CommandBase
{
    private readonly MapController _map;
    private readonly GameApi _game;

    public MapCommand(MapController map, GameApi game)
    {
        _map = map;
        _game = game;
    }

    public override string CmdName => "map";

    public override string CmdDesc => Strings.MapDesc;

    public override string CmdUsage => "map";

    /// <inheritdoc/>
    public override CommandCategory Category => CommandCategory.Client;

    /// <inheritdoc/>
    public override IReadOnlyList<UsageLine> UsageLines =>
    [
        new(string.Empty, "show the held map item"),
    ];

    /// <inheritdoc/>
    public override IReadOnlyList<string> Examples => ["map"];

    /// <inheritdoc/>
    public override string? ManTopic => "tui";

    public override void Register(CommandBuilder<CommandContext> builder)
    {
        CommandNodeBuilder<CommandContext> help = RegisterHelp(builder, CmdName, ShowUsage);
        builder.Literal(CmdName, l => l
            .Executes(ctx => ctx.Source.Result.Ok(_map.Open(_game)))
            .ThenLiteral("_help", h => h.Executes(ctx => ShowUsage(ctx.Source)).RedirectTo(help)));
    }
}
