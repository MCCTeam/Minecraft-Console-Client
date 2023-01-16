using System.Threading.Tasks;
using Brigadier.NET;
using Brigadier.NET.ArgumentTypes;
using Brigadier.NET.Context;
using Brigadier.NET.Suggestion;

namespace MinecraftClient.CommandHandler.ArgumentType
{
    public class AutoCraftRecipeNameArgumentType : ArgumentType<string>
    {
        public override string Parse(IStringReader reader)
        {
            reader.SkipWhitespace();
            return reader.ReadString();
        }

        public override Task<Suggestions> ListSuggestions<TSource>(CommandContext<TSource> context, SuggestionsBuilder builder)
        {
            var recipeList = Settings.Config.ChatBot.AutoCraft.Recipes;
            foreach (var recipe in recipeList)
                builder.Suggest(recipe.Name);
            return builder.BuildFuture();
        }
    }
}
