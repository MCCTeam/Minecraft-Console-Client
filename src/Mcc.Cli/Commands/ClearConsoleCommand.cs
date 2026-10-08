using Mcc.Cli.Localization;
using Mcc.Cli.Presentation;
using DMCBK.Core.Commands;
using Umpk.Commands;

namespace Mcc.Cli.Commands;

/// <summary>The host <c>clear-console</c> command (alias <c>cc</c>): clears the console screen.</summary>
internal sealed class ClearConsoleCommand : CommandBase
{
    public override string CmdName => "clear-console";

    public override string CmdDesc => Strings.ClearConsoleDesc;

    public override string CmdUsage => "clear-console";

    /// <inheritdoc/>
    public override CommandCategory Category => CommandCategory.Client;

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases => ["cc"];

    /// <inheritdoc/>
    public override IReadOnlyList<UsageLine> UsageLines =>
    [
        new(string.Empty, "clear the console screen"),
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
        // Captured in the configure callback because CommandBuilder.Literal returns the parent builder for chaining, not the node it just created; this is the same capture RegisterHelp uses.
        CommandNodeBuilder<CommandContext>? clear = null;
        builder.Literal(CmdName, l =>
        {
            clear = l;
            l.Executes(ctx => Clear(ctx.Source))
                .ThenLiteral("_help", h => h.Executes(ctx => ShowUsage(ctx.Source)).RedirectTo(help));
        });
        // The alias redirects onto the same node instead of only re-binding Executes, so "cc _help" works like "clear-console _help", matching MinecraftClient/Commands/ClearConsole.cs:28-31.
        builder.Literal("cc", l =>
        {
            l.Executes(ctx => Clear(ctx.Source));
            if (clear is not null)
                l.RedirectTo(clear);
        });
    }

    // Clears and says nothing.
    // A confirmation line used to be passed in here, and it was not decoration: a write through ConsoleWriter is the only public trigger for RedrawInputArea(RedrawAll: true), which is what puts "> " and the caret back after the wipe, so SOMETHING had to be written.
    // It does not have to be text.
    // RichConsole.ClearScreen writes the empty string when handed null, which repaints the prompt and leaves the cleared screen actually clear.
    private int Clear(CommandContext ctx)
    {
        HostConsole.ClearScreen();
        return ctx.Result.Ok(null);
    }
}
