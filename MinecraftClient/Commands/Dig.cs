using System;
using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.Mapping;
using static MinecraftClient.CommandHandler.CmdResult;

namespace MinecraftClient.Commands
{
    public class Dig : Command
    {
        public override string CmdName { get { return "dig"; } }
        public override string CmdUsage { get { return "dig <x> <y> <z>"; } }
        public override string CmdDesc { get { return Translations.cmd_dig_desc; } }

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Executes(r => DigLookAt(r.Source))
                .Then(l => l.Argument("Duration", Arguments.Double())
                    .Executes(r => DigLookAt(r.Source, Arguments.GetDouble(r, "Duration"))))
                .Then(l => l.Argument("Location", MccArguments.Location())
                    .Executes(r => DigAt(r.Source, MccArguments.GetLocation(r, "Location")))
                    .Then(l => l.Argument("Duration", Arguments.Double())
                        .Executes(r => DigAt(r.Source, MccArguments.GetLocation(r, "Location"), Arguments.GetDouble(r, "Duration")))))
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

        private int DigAt(CmdResult r, Location blockToBreak, double duration = 0)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetTerrainEnabled())
                return r.SetAndReturn(Status.FailNeedTerrain);

            Location current = handler.GetCurrentLocation();
            blockToBreak = blockToBreak.ToAbsolute(current);
            if (blockToBreak.DistanceSquared(current.EyesLocation()) > 25)
                return r.SetAndReturn(Status.Fail, Translations.cmd_dig_too_far);
            Block block = handler.GetWorld().GetBlock(blockToBreak);
            if (block.Type == Material.Air)
                return r.SetAndReturn(Status.Fail, Translations.cmd_dig_no_block);
            else if (handler.DigBlock(blockToBreak, duration: duration))
            {
                blockToBreak = blockToBreak.ToCenter();
                return r.SetAndReturn(Status.Done, string.Format(Translations.cmd_dig_dig, blockToBreak.X, blockToBreak.Y, blockToBreak.Z, block.GetTypeString()));
            }
            else
                return r.SetAndReturn(Status.Fail, Translations.cmd_dig_fail);
        }

        private int DigLookAt(CmdResult r, double duration = 0)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetTerrainEnabled())
                return r.SetAndReturn(Status.FailNeedTerrain);

            (bool hasBlock, Location blockLoc, Block block) = RaycastHelper.RaycastBlock(handler, 4.5, false);
            if (!hasBlock)
                return r.SetAndReturn(Status.Fail, Translations.cmd_dig_too_far);
            else if (block.Type == Material.Air)
                return r.SetAndReturn(Status.Fail, Translations.cmd_dig_no_block);
            else if (handler.DigBlock(blockLoc, lookAtBlock: false, duration: duration))
                return r.SetAndReturn(Status.Done, string.Format(Translations.cmd_dig_dig, blockLoc.X, blockLoc.Y, blockLoc.Z, block.GetTypeString()));
            else
                return r.SetAndReturn(Status.Fail, Translations.cmd_dig_fail);
        }
    }
}
