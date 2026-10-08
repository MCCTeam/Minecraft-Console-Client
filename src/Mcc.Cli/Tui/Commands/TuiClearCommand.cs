using Mcc.Cli.Commands;
using Mcc.Cli.Localization;
using Mcc.Cli.Tui.Hosting;
using DMCBK.Core.Commands;
using Umpk.Commands;

namespace Mcc.Cli.Tui.Commands;

/// <summary>
/// The TUI <c>clear-console</c> command (alias <c>cc</c>): clears the chat log in the TUI view instead of the raw console (the classic host's <see cref="ClearConsoleCommand"/> calls <c>Console.Clear</c>, which would corrupt the Consolonia frame).
/// Registered the same way as the classic host command.
/// </summary>
internal sealed class TuiClearCommand : CommandBase
{
    private readonly TuiBackend _backend;

    public TuiClearCommand(TuiBackend backend) => _backend = backend;

    public override string CmdName => "clear-console";

    public override IReadOnlyList<string> Aliases => ["cc"];

    public override string CmdDesc => Strings.ClearConsoleDesc;

    public override string CmdUsage => "clear-console";

    /// <inheritdoc/>
    public override CommandCategory Category => CommandCategory.Client;

    /// <inheritdoc/>
    public override IReadOnlyList<UsageLine> UsageLines =>
    [
        new(string.Empty, "clear the TUI log pane"),
    ];

    /// <inheritdoc/>
    public override IReadOnlyList<string> Examples => ["clear-console"];

    /// <inheritdoc/>
    /// <remarks>
    /// Not echoed.
    /// Printing "> /cc" straight after wiping the console leaves one stray line at the top of a display the user just asked to be empty.
    /// </remarks>
    public override bool EchoWhenRun => false;

    public override void Register(CommandBuilder<CommandContext> builder)
    {
        CommandNodeBuilder<CommandContext> help = RegisterHelp(builder, CmdName, ShowUsage);
        builder.Literal(CmdName, l => l
            .Executes(ctx => Clear(ctx.Source))
            .ThenLiteral("_help", h => h.Executes(ctx => ShowUsage(ctx.Source)).RedirectTo(help)));
        builder.Literal("cc", l => l.Executes(ctx => Clear(ctx.Source)));
    }

    private int Clear(CommandContext ctx)
    {
        _backend.Post(() => _backend.View?.ClearLog());
        return ctx.Result.Ok(null);
    }
}
