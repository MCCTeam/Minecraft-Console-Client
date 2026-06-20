using System.Linq;
using System.Text;
using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;

namespace MinecraftClient.Commands
{
    public class EffectsCommand : Command
    {
        public override string CmdName { get { return "effects"; } }
        public override string CmdUsage { get { return "effects"; } }
        public override string CmdDesc { get { return Translations.cmd_effects_desc; } }

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Executes(r => ShowEffects(r.Source))
                .Then(l => l.Literal("_help")
                    .Executes(r => GetUsage(r.Source, string.Empty))
                    .Redirect(dispatcher.GetRoot().GetChild("help").GetChild(CmdName)))
            );
        }

        private int GetUsage(CmdResult r, string? cmd)
        {
            return r.SetAndReturn(cmd switch
            {
#pragma warning disable format // @formatter:off
                _           =>  GetCmdDescTranslated(),
#pragma warning restore format // @formatter:on
            });
        }

        private int ShowEffects(CmdResult r)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetEntityHandlingEnabled())
                return r.SetAndReturn(CmdResult.Status.FailNeedEntity);

            var effects = handler.GetPlayerEffects()
                .Values
                .Where(effectData => !effectData.IsExpired)
                .OrderBy(effectData => effectData.Effect)
                .ToArray();

            if (effects.Length == 0)
                return r.SetAndReturn(CmdResult.Status.Done, Translations.cmd_effects_none);

            StringBuilder response = new();
            response.AppendLine(Translations.cmd_effects_header);
            foreach (var effectData in effects)
            {
                response.AppendLine(string.Format(Translations.cmd_effects_entry,
                    effectData.GetDisplayName(), effectData.GetRemainingDurationText()));
            }

            return r.SetAndReturn(CmdResult.Status.Done, response.ToString().TrimEnd());
        }
    }
}
