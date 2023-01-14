using System;
using Brigadier.NET;
using Brigadier.NET.ArgumentTypes;

namespace MinecraftClient.CommandHandler.ArgumentType
{
    public class TupleArgumentType : ArgumentType<Tuple<int, int>>
    {
        public override Tuple<int, int> Parse(IStringReader reader)
        {
            reader.SkipWhitespace();
            int int1 = reader.ReadInt();
            reader.SkipWhitespace();
            int int2 = reader.ReadInt();
            return new(int1, int2);
        }
    }
}
