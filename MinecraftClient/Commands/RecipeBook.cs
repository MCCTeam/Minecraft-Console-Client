using System.Text;
using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;

namespace MinecraftClient.Commands
{
    public class RecipeBook : Command
    {
        public override string CmdName => "recipebook";
        public override string CmdUsage => "recipebook <list|craft|craftall> [recipe id]";
        public override string CmdDesc => Translations.cmd_recipebook_desc;

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                    .Then(l => l.Literal("list")
                        .Executes(r => GetUsage(r.Source, "list")))
                    .Then(l => l.Literal("craft")
                        .Executes(r => GetUsage(r.Source, "craft")))
                    .Then(l => l.Literal("craftall")
                        .Executes(r => GetUsage(r.Source, "craftall")))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Then(l => l.Literal("list")
                    .Executes(r => ListRecipes(r.Source)))
                .Then(l => l.Literal("craft")
                    .Then(l => l.Argument("RecipeId", Arguments.String())
                        .Executes(r => CraftRecipe(r.Source, Arguments.GetString(r, "RecipeId"), makeAll: false))))
                .Then(l => l.Literal("craftall")
                    .Then(l => l.Argument("RecipeId", Arguments.String())
                        .Executes(r => CraftRecipe(r.Source, Arguments.GetString(r, "RecipeId"), makeAll: true))))
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
                "list"      => GetCmdDescTranslated(),
                "craft"     => GetCmdDescTranslated(),
                "craftall"  => GetCmdDescTranslated(),
                _           => GetCmdDescTranslated(),
#pragma warning restore format // @formatter:on
            });
        }

        private int ListRecipes(CmdResult r)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetInventoryEnabled())
                return r.SetAndReturn(CmdResult.Status.FailNeedInventory);

            string[] recipeIds = handler.GetUnlockedRecipes();
            if (recipeIds.Length == 0)
                return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_recipebook_no_recipes);

            StringBuilder response = new();
            response.AppendLine(Translations.cmd_recipebook_list);
            foreach (string recipeId in recipeIds)
                response.AppendLine("- " + recipeId);

            handler.Log.Info(response.ToString().TrimEnd());
            return r.SetAndReturn(CmdResult.Status.Done);
        }

        private int CraftRecipe(CmdResult r, string recipeId, bool makeAll)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetInventoryEnabled())
                return r.SetAndReturn(CmdResult.Status.FailNeedInventory);

            if (handler.GetProtocolVersion() < Protocol.Handlers.Protocol18Handler.MC_1_13_Version)
                return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_recipebook_unsupported);

            if (handler.GetActiveRecipeBookInventory() is null)
                return r.SetAndReturn(CmdResult.Status.Fail, Translations.cmd_recipebook_no_active_inventory);

            return handler.SendPlaceRecipe(recipeId, makeAll)
                ? r.SetAndReturn(CmdResult.Status.Done, string.Format(Translations.cmd_recipebook_craft_sent, recipeId, makeAll))
                : r.SetAndReturn(CmdResult.Status.Fail, string.Format(Translations.cmd_recipebook_craft_failed, recipeId));
        }
    }
}
