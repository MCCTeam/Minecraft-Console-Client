using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.Pathing.Execution;
using static MinecraftClient.CommandHandler.CmdResult;

namespace MinecraftClient.Commands
{
    /// <summary>
    /// Toggles Info-level pathing diagnostics. When enabled, <see cref="PathSegmentManager"/>
    /// emits the full waypoint dump of every planned/replanned path, the recent per-tick
    /// trace at segment-failure time, and the failing segment's target. Used for
    /// reporting pathing bugs without permanently changing the debug log level.
    /// </summary>
    public class PathDiag : Command
    {
        public override string CmdName => "pathdiag";
        public override string CmdUsage => "pathdiag [on|off]";
        public override string CmdDesc => Translations.cmd_pathdiag_desc;

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source)))
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Executes(r => Toggle(r.Source))
                .Then(l => l.Literal("on")
                    .Executes(r => SetDiagnostics(r.Source, true)))
                .Then(l => l.Literal("off")
                    .Executes(r => SetDiagnostics(r.Source, false)))
                .Then(l => l.Literal("_help")
                    .Executes(r => GetUsage(r.Source))
                    .Redirect(dispatcher.GetRoot().GetChild("help").GetChild(CmdName)))
            );
        }

        private int GetUsage(CmdResult r)
        {
            return r.SetAndReturn(GetCmdDescTranslated());
        }

        private static int Toggle(CmdResult r)
        {
            return SetDiagnostics(r, !PathSegmentManager.DiagnosticsEnabled);
        }

        private static int SetDiagnostics(CmdResult r, bool enabled)
        {
            PathSegmentManager.DiagnosticsEnabled = enabled;
            return r.SetAndReturn(Status.Done,
                enabled ? Translations.cmd_pathdiag_enabled : Translations.cmd_pathdiag_disabled);
        }
    }
}
