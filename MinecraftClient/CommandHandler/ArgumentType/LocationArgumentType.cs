using System;
using System.Threading.Tasks;
using Brigadier.NET;
using Brigadier.NET.ArgumentTypes;
using Brigadier.NET.Context;
using Brigadier.NET.Suggestion;
using MinecraftClient.Mapping;

namespace MinecraftClient.CommandHandler.ArgumentType
{
    public class LocationArgumentType : ArgumentType<Location>
    {
        public override Location Parse(IStringReader reader)
        {
            int[] status = new int[3];
            double[] coords = new double[3];
            for (int i = 0; i < 3; ++i)
            {
                reader.SkipWhitespace();
                if (reader.Peek() == '~' || reader.Peek() == '～')
                {
                    status[i] = 1;
                    reader.Next();
                    if (reader.CanRead())
                    {
                        char next = reader.Peek();
                        if (char.IsDigit(next) || next == '.' || next == '-')
                            coords[i] = reader.ReadDouble();
                        else if (next == '+')
                        {
                            reader.Next();
                            coords[i] = reader.ReadDouble();
                        }
                        else coords[i] = 0;
                    }
                    else coords[i] = 0;
                }
                else
                {
                    status[i] = 0;
                    coords[i] = reader.ReadDouble();
                }
            }

            return new Location(coords[0], coords[1], coords[2], (byte)(status[0] | status[1] << 1 | status[2] << 2));
        }

        public override Task<Suggestions> ListSuggestions<TSource>(CommandContext<TSource> context, SuggestionsBuilder builder)
        {
            McClient? client = CmdResult.currentHandler;
            string[] args = builder.Remaining.Split(' ', StringSplitOptions.TrimEntries);
            if (args.Length == 0 || (args.Length == 1 && string.IsNullOrWhiteSpace(args[0])))
            {
                if (client != null)
                {
                    Location current = client.GetCurrentLocation();
                    builder.Suggest(string.Format("{0:0.00}", current.X));
                    builder.Suggest(string.Format("{0:0.00} {1:0.00}", current.X, current.Y));
                    builder.Suggest(string.Format("{0:0.00} {1:0.00} {2:0.00}", current.X, current.Y, current.Z));
                }
                else
                {
                    builder.Suggest("~");
                    builder.Suggest("~ ~");
                    builder.Suggest("~ ~ ~");
                }
            }
            else if (args.Length == 1 || (args.Length == 2 && string.IsNullOrWhiteSpace(args[1])))
            {
                string add = args.Length == 1 ? " " : string.Empty;
                if (client != null)
                {
                    Location current = client.GetCurrentLocation();
                    builder.Suggest(string.Format("{0}{2}{1:0.00}", builder.Remaining, current.Y, add));
                    builder.Suggest(string.Format("{0}{3}{1:0.00} {2:0.00}", builder.Remaining, current.Y, current.Z, add));
                }
                else
                {
                    builder.Suggest(builder.Remaining + add + "~");
                    builder.Suggest(builder.Remaining + add + "~ ~");
                }
            }
            else if (args.Length == 2 || (args.Length == 3 && string.IsNullOrWhiteSpace(args[2])))
            {
                string add = args.Length == 2 ? " " : string.Empty;
                if (client != null)
                {
                    Location current = client.GetCurrentLocation();
                    builder.Suggest(string.Format("{0}{2}{1:0.00}", builder.Remaining, current.Z, add));
                }
                else
                {
                    builder.Suggest(builder.Remaining + add + "~");
                }
            }
            return builder.BuildFuture();
        }
    }
}
