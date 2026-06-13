using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.Dialogs;
using MinecraftClient.Tui;

namespace MinecraftClient.Commands;

public class Dialog : Command
{
    public override string CmdName => "dialog";
    public override string CmdUsage => Translations.cmd_dialog_usage;
    public override string CmdDesc => Translations.cmd_dialog_desc;

    public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
    {
        dispatcher.Register(l => l.Literal("help")
            .Then(l => l.Literal(CmdName)
                .Executes(r => GetUsage(r.Source))));

        dispatcher.Register(l => l.Literal(CmdName)
            .Executes(r => Show(r.Source))
            .Then(l => l.Literal("show")
                .Executes(r => Show(r.Source)))
            .Then(l => l.Literal("open")
                .Executes(r => Open(r.Source)))
            .Then(l => l.Literal("set")
                .Then(l => l.Argument("Input", Arguments.String())
                    .Then(l => l.Argument("Value", Arguments.GreedyString())
                        .Executes(r => SetInput(r.Source, Arguments.GetString(r, "Input"), Arguments.GetString(r, "Value"))))))
            .Then(l => l.Literal("input")
                .Then(l => l.Argument("Input", Arguments.String())
                    .Then(l => l.Argument("Value", Arguments.GreedyString())
                        .Executes(r => SetInput(r.Source, Arguments.GetString(r, "Input"), Arguments.GetString(r, "Value"))))))
            .Then(l => l.Literal("click")
                .Then(l => l.Argument("Index", Arguments.Integer(min: 1))
                    .Executes(r => Click(r.Source, Arguments.GetInteger(r, "Index")))))
            .Then(l => l.Literal("click-label")
                .Then(l => l.Argument("Label", Arguments.GreedyString())
                    .Executes(r => ClickLabel(r.Source, Arguments.GetString(r, "Label")))))
            .Then(l => l.Literal("cancel")
                .Executes(r => Cancel(r.Source)))
            .Then(l => l.Literal("dismiss")
                .Executes(r => Dismiss(r.Source)))
            .Then(l => l.Literal("_help")
                .Executes(r => GetUsage(r.Source))
                .Redirect(dispatcher.GetRoot().GetChild("help").GetChild(CmdName))));
    }

    private int GetUsage(CmdResult r) => r.SetAndReturn(GetCmdDescTranslated());

    private static int Show(CmdResult r)
    {
        var handler = CmdResult.currentHandler!;
        var current = handler.Dialogs.Current;
        return current is null
            ? r.SetAndReturn(CmdResult.Status.Fail, Translations.dialog_none)
            : r.SetAndReturn(CmdResult.Status.Done, DialogFormatter.Render(current));
    }

    private static int Open(CmdResult r)
    {
        var handler = CmdResult.currentHandler!;
        var current = handler.Dialogs.Current;
        if (current is null)
            return r.SetAndReturn(CmdResult.Status.Fail, Translations.dialog_none);

        if (ConsoleIO.Backend is not TuiConsoleBackend)
            return r.SetAndReturn(CmdResult.Status.Fail, Translations.dialog_tui_unavailable);

        return DialogTuiHost.TryOpen(handler, current, force: true)
            ? r.SetAndReturn(CmdResult.Status.Done, Translations.dialog_tui_opened)
            : r.SetAndReturn(CmdResult.Status.Fail, Translations.dialog_tui_unavailable);
    }

    private static int SetInput(CmdResult r, string key, string value)
    {
        var result = CmdResult.currentHandler!.Dialogs.SetInput(key, value);
        return r.SetAndReturn(result.Success ? CmdResult.Status.Done : CmdResult.Status.Fail, result.Message);
    }

    private static int Click(CmdResult r, int index)
    {
        var result = CmdResult.currentHandler!.Dialogs.Click(index);
        return r.SetAndReturn(result.Success ? CmdResult.Status.Done : CmdResult.Status.Fail, result.Message);
    }

    private static int ClickLabel(CmdResult r, string label)
    {
        var result = CmdResult.currentHandler!.Dialogs.ClickLabel(label);
        return r.SetAndReturn(result.Success ? CmdResult.Status.Done : CmdResult.Status.Fail, result.Message);
    }

    private static int Cancel(CmdResult r)
    {
        var result = CmdResult.currentHandler!.Dialogs.Cancel();
        return r.SetAndReturn(result.Success ? CmdResult.Status.Done : CmdResult.Status.Fail, result.Message);
    }

    private static int Dismiss(CmdResult r)
    {
        var result = CmdResult.currentHandler!.Dialogs.Dismiss();
        DialogTuiHost.CloseCurrent();
        return r.SetAndReturn(result.Success ? CmdResult.Status.Done : CmdResult.Status.Fail, result.Message);
    }
}
