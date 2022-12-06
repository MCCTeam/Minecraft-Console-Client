using System.Text;
using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.Mapping;
using static MinecraftClient.CommandHandler.CmdResult;

namespace MinecraftClient.Commands
{
    public class BlockInfo : Command
    {
        public override string CmdName { get { return "blockinfo"; } }
        public override string CmdUsage { get { return "blockinfo <x> <y> <z> [-s]"; } }
        public override string CmdDesc { get { return Translations.cmd_blockinfo_desc; } }

        public override void RegisterCommand(McClient handler, CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                    .Then(l => l.Literal("-s")
                        .Executes(r => GetUsage(r.Source, "-s")))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Executes(r => LogBlockInfo(r.Source, handler, handler.GetCurrentLocation(), false))
                .Then(l => l.Literal("-s")
                    .Executes(r => LogBlockInfo(r.Source, handler, handler.GetCurrentLocation(), true)))
                .Then(l => l.Argument("Location", MccArguments.Location())
                    .Executes(r => LogBlockInfo(r.Source, handler, MccArguments.GetLocation(r, "Location"), false))
                    .Then(l => l.Literal("-s")
                        .Executes(r => LogBlockInfo(r.Source, handler, MccArguments.GetLocation(r, "Location"), true))))
                .Then(l => l.Literal("_help")
                    .Redirect(dispatcher.GetRoot().GetChild("help").GetChild(CmdName)))
            );
        }

        private int GetUsage(CmdResult r, string? cmd)
        {
            return r.SetAndReturn(cmd switch
            {
#pragma warning disable format // @formatter:off
                "-s"        =>  GetCmdDescTranslated(),
                _           =>  GetCmdDescTranslated(),
#pragma warning restore format // @formatter:on
            });
        }

        private static int LogBlockInfo(CmdResult r, McClient handler, Location targetBlock, bool reportSurrounding)
        {
            if (!handler.GetTerrainEnabled())
                return r.SetAndReturn(Status.FailNeedTerrain);

            targetBlock.ToAbsolute(handler.GetCurrentLocation());
            Block block = handler.GetWorld().GetBlock(targetBlock);

            handler.Log.Info($"{Translations.cmd_blockinfo_BlockType}: {block.GetTypeString()}");
            if (reportSurrounding)
            {
                StringBuilder sb = new();
                sb.AppendLine($"{Translations.cmd_blockinfo_BlocksAround}:");

                Block blockXPositive = handler.GetWorld().GetBlock(new Location(targetBlock.X + 1, targetBlock.Y, targetBlock.Z));
                Block blockXNegative = handler.GetWorld().GetBlock(new Location(targetBlock.X - 1, targetBlock.Y, targetBlock.Z));
                Block blockYPositive = handler.GetWorld().GetBlock(new Location(targetBlock.X, targetBlock.Y + 1, targetBlock.Z));
                Block blockYNegative = handler.GetWorld().GetBlock(new Location(targetBlock.X, targetBlock.Y - 1, targetBlock.Z));
                Block blockZPositive = handler.GetWorld().GetBlock(new Location(targetBlock.X, targetBlock.Y, targetBlock.Z + 1));
                Block blockZNegative = handler.GetWorld().GetBlock(new Location(targetBlock.X, targetBlock.Y, targetBlock.Z - 1));

                sb.AppendLine($"[X {Translations.cmd_blockinfo_Positive}] {Translations.cmd_blockinfo_BlockType}: {blockXPositive.GetTypeString()}");
                sb.AppendLine($"[X {Translations.cmd_blockinfo_Negative}] {Translations.cmd_blockinfo_BlockType}: {blockXNegative.GetTypeString()}");

                sb.AppendLine(" ");

                sb.AppendLine($"[Y {Translations.cmd_blockinfo_Positive}] {Translations.cmd_blockinfo_BlockType}: {blockYPositive.GetTypeString()}");
                sb.AppendLine($"[Y {Translations.cmd_blockinfo_Negative}] {Translations.cmd_blockinfo_BlockType}: {blockYNegative.GetTypeString()}");

                sb.AppendLine(" ");

                sb.AppendLine($"[Z {Translations.cmd_blockinfo_Positive}] {Translations.cmd_blockinfo_BlockType}: {blockZPositive.GetTypeString()}");
                sb.AppendLine($"[Z {Translations.cmd_blockinfo_Negative}] {Translations.cmd_blockinfo_BlockType}: {blockZNegative.GetTypeString()}");

                handler.Log.Info(sb.ToString());
            }
            return r.SetAndReturn(Status.Done);
        }
    }
}
