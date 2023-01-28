using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Brigadier.NET;
using Brigadier.NET.ArgumentTypes;
using Brigadier.NET.Context;
using Brigadier.NET.Suggestion;

namespace MinecraftClient.CommandHandler.ArgumentType
{
    public class ScriptNameArgumentType : ArgumentType<string>
    {
        public override string Parse(IStringReader reader)
        {
            string remaining = reader.Remaining;
            reader.Cursor += reader.RemainingLength;
            return remaining;
        }

        public override Task<Suggestions> ListSuggestions<TSource>(CommandContext<TSource> context, SuggestionsBuilder builder)
        {
            try
            {
                string? dir = Path.GetDirectoryName(builder.Remaining);
                if (!string.IsNullOrEmpty(dir) && Path.Exists(dir))
                {
                    foreach (string fileName in Directory.GetFiles(dir, "*.cs"))
                        builder.Suggest(fileName);
                    foreach (string fileName in Directory.GetFiles(dir, "*.txt"))
                        builder.Suggest(fileName);
                }
            }
            catch (IOException) { }
            catch (ArgumentException) { }
            catch (UnauthorizedAccessException) { }

            try
            {
                foreach (string fileName in Directory.GetFiles("." + Path.DirectorySeparatorChar, "*.cs"))
                    builder.Suggest(fileName);
                foreach (string fileName in Directory.GetFiles("." + Path.DirectorySeparatorChar, "*.txt"))
                    builder.Suggest(fileName);
            }
            catch (IOException) { }
            catch (ArgumentException) { }
            catch (UnauthorizedAccessException) { }

            return builder.BuildFuture();
        }
    }
}
