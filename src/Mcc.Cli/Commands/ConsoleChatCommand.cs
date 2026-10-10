using Mcc.Cli.Hosting;
using Mcc.Cli.Localization;
using DMCBK.Core.Commands;
using Umpk.Commands;

namespace Mcc.Cli.Commands;

/// <summary>The host <c>console-chat</c> command: toggles chat visibility in the console.</summary>
internal sealed class ConsoleChatCommand : CommandBase
{
    private readonly ConsoleHostState _state;

    public ConsoleChatCommand(ConsoleHostState state) => _state = state;

    public override string CmdName => "console-chat";

    public override string CmdDesc => Strings.ConsoleChatDesc;

    public override string CmdUsage => "console-chat [on|off]";

    /// <inheritdoc/>
    public override CommandCategory Category => CommandCategory.Client;

    /// <inheritdoc/>
    public override IReadOnlyList<UsageLine> UsageLines =>
    [
        new(string.Empty, "show whether chat is visible"),
        new("on", "show incoming chat"),
        new("off", "hide it"),
    ];

    /// <inheritdoc/>
    public override IReadOnlyList<string> Examples => ["console-chat off"];

    /// <inheritdoc/>
    public override IReadOnlyList<string> SeeAlso => ["send"];

    /// <inheritdoc/>
    public override string? ManTopic => "chat";

    public override void Register(CommandBuilder<CommandContext> builder)
    {
        CommandNodeBuilder<CommandContext> help = RegisterHelp(builder, CmdName, ShowUsage);
        builder.Literal(CmdName, l => l
            .Executes(ctx => Set(ctx.Source, !_state.ChatVisible))
            .ThenLiteral("on", h => h.Executes(ctx => Set(ctx.Source, true)))
            .ThenLiteral("off", h => h.Executes(ctx => Set(ctx.Source, false)))
            .ThenLiteral("_help", h => h.Executes(ctx => ShowUsage(ctx.Source)).RedirectTo(help)));
    }

    private int Set(CommandContext ctx, bool visible)
    {
        _state.ChatVisible = visible;
        return ctx.Result.Ok(visible ? Strings.ConsoleChatOn : Strings.ConsoleChatOff);
    }
}
