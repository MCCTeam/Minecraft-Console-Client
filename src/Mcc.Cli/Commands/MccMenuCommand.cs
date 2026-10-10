using DMCBK.Core.Commands;
using Umpk.Commands;

namespace Mcc.Cli.Commands;

/// <summary>Opens the rich host's launcher for MCC management workspaces.</summary>
public sealed class MccMenuCommand : CommandBase
{
    /// <inheritdoc/>
    public override string CmdName => "mcc-menu";

    /// <inheritdoc/>
    public override string CmdDesc => Mcc.Cli.Localization.MccStrings.Get("cli.menu.description");

    /// <inheritdoc/>
    public override string CmdUsage => "mcc-menu";

    /// <inheritdoc/>
    public override CommandCategory Category => CommandCategory.Client;

    /// <inheritdoc/>
    public override IReadOnlyList<UsageLine> UsageLines =>
    [
        new(string.Empty, Mcc.Cli.Localization.MccStrings.Get("cli.menu.usage")),
    ];

    /// <inheritdoc/>
    public override IReadOnlyList<string> Examples => ["mcc-menu"];

    /// <inheritdoc/>
    public override IReadOnlyList<string> SeeAlso =>
        ["help", "man", "scripts", "recipebook", "entity", "achievement", "plugins"];

    /// <inheritdoc/>
    public override string? ManTopic => "tui";

    /// <inheritdoc/>
    public override void Register(CommandBuilder<CommandContext> builder)
    {
        CommandNodeBuilder<CommandContext> help = RegisterHelp(builder, CmdName, ShowUsage);
        builder.Literal(CmdName, command => command
            .Executes(ctx => Open(ctx.Source))
            .ThenLiteral("_help", h => h.Executes(ctx => ShowUsage(ctx.Source)).RedirectTo(help)));
    }

    private static int Open(CommandContext ctx)
        => (ctx.Ui as IMccNavigation)?.TryOpenMccMenu() == true
            ? ctx.Result.Ok()
            : ctx.Result.Fail(Mcc.Cli.Localization.MccStrings.Get("cli.menu.unavailable"));
}
