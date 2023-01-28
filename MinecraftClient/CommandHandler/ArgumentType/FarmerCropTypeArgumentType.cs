using System;
using System.Threading.Tasks;
using Brigadier.NET;
using Brigadier.NET.ArgumentTypes;
using Brigadier.NET.Context;
using Brigadier.NET.Exceptions;
using Brigadier.NET.Suggestion;
using static MinecraftClient.ChatBots.Farmer;

namespace MinecraftClient.CommandHandler.ArgumentType
{
    public class FarmerCropTypeArgumentType : ArgumentType<CropType>
    {
        public override CropType Parse(IStringReader reader)
        {
            reader.SkipWhitespace();
            string inputStr = reader.ReadString();
            if (Enum.TryParse(inputStr, true, out CropType cropType))
                return cropType;
            else
                throw CommandSyntaxException.BuiltInExceptions.LiteralIncorrect().CreateWithContext(reader, inputStr);
        }

        public override Task<Suggestions> ListSuggestions<TSource>(CommandContext<TSource> context, SuggestionsBuilder builder)
        {
            foreach (var result in Enum.GetNames(typeof(CropType)))
                builder.Suggest(result);
            return builder.BuildFuture();
        }
    }
}
