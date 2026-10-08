using Mcc.Cli.Hosting;
using Mcc.Cli.Localization;
using DMCBK.Core.Commands;
using Umpk.Commands;

namespace Mcc.Cli.Commands;

/// <summary>The host <c>exit</c> command (alias <c>quit</c>): requests process exit with an optional code.</summary>
internal sealed class ExitCommand : CommandBase
{
    private readonly HostControl _control;

    public ExitCommand(HostControl control) => _control = control;

    public override string CmdName => "exit";

    public override string CmdDesc => Strings.ExitDesc;

    public override string CmdUsage => "exit [code]";

    /// <inheritdoc/>
    public override CommandCategory Category => CommandCategory.Session;

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases => ["quit"];

    /// <inheritdoc/>
    public override IReadOnlyList<UsageLine> UsageLines =>
    [
        new(string.Empty, "quit MCC"),
        new("<code>", "quit with an exit code"),
    ];

    /// <inheritdoc/>
    public override IReadOnlyList<string> Examples => ["exit", "exit 1"];

    public override void Register(CommandBuilder<CommandContext> builder)
    {
        CommandNodeBuilder<CommandContext> help = RegisterHelp(builder, CmdName, ShowUsage);
        builder.Literal(CmdName, l => l
            .Executes(ctx => DoExit(ctx.Source, 0, explicitCode: false))
            .ThenArgument("code", Arguments.Integer(), h => h
                .Executes(ctx => DoExit(ctx.Source, ctx.GetArgument<int>("code"), explicitCode: true)))
            .ThenLiteral("_help", h => h.Executes(ctx => ShowUsage(ctx.Source)).RedirectTo(help)));
        builder.Literal("quit", l => l
            .Executes(ctx => DoExit(ctx.Source, 0, explicitCode: false))
            .ThenArgument("code", Arguments.Integer(), h => h
                .Executes(ctx => DoExit(ctx.Source, ctx.GetArgument<int>("code"), explicitCode: true))));
    }

    private int DoExit(CommandContext ctx, int code, bool explicitCode)
    {
        _control.RequestExit(code, explicitCode);
        return ctx.Result.Ok(null);
    }
}
