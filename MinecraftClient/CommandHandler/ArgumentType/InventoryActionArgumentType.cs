using System.Threading.Tasks;
using Brigadier.NET;
using Brigadier.NET.ArgumentTypes;
using Brigadier.NET.Context;
using Brigadier.NET.Exceptions;
using Brigadier.NET.Suggestion;
using MinecraftClient.Inventory;

namespace MinecraftClient.CommandHandler.ArgumentType
{
    public class InventoryActionArgumentType : ArgumentType<WindowActionType>
    {
        private WindowActionType[] SupportActions = new WindowActionType[]
        {
            WindowActionType.LeftClick,
            WindowActionType.RightClick,
            WindowActionType.MiddleClick,
            WindowActionType.ShiftClick,
        };

        public override WindowActionType Parse(IStringReader reader)
        {
            reader.SkipWhitespace();
            string inputStr = reader.ReadString();
            foreach (var action in SupportActions)
            {
                string actionStr = action.ToString();
                if (string.Compare(inputStr, actionStr, true) == 0)
                    return action;
            }
            throw CommandSyntaxException.BuiltInExceptions.LiteralIncorrect().CreateWithContext(reader, inputStr);
        }

        public override Task<Suggestions> ListSuggestions<TSource>(CommandContext<TSource> context, SuggestionsBuilder builder)
        {
            foreach (var action in SupportActions)
                builder.Suggest(action.ToString());
            return builder.BuildFuture();
        }
    }
}
