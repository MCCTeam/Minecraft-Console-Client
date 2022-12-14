using System;
using System.Text;
using Brigadier.NET;
using Brigadier.NET.Builder;
using MinecraftClient.CommandHandler;
using MinecraftClient.Mapping;
using static MinecraftClient.CommandHandler.CmdResult;

namespace MinecraftClient.Commands
{
    public class Chunk : Command
    {
        public override string CmdName { get { return "chunk"; } }
        public override string CmdUsage { get { return "chunk status [chunkX chunkZ|locationX locationY locationZ]"; } }
        public override string CmdDesc { get { return Translations.cmd_chunk_desc; } }

        public override void RegisterCommand(CommandDispatcher<CmdResult> dispatcher)
        {
            dispatcher.Register(l => l.Literal("help")
                .Then(l => l.Literal(CmdName)
                    .Executes(r => GetUsage(r.Source, string.Empty))
                    .Then(l => l.Literal("status")
                        .Executes(r => GetUsage(r.Source, "status")))
                    .Then(l => l.Literal("_setloading")
                        .Executes(r => GetUsage(r.Source, "_setloading")))
                    .Then(l => l.Literal("_setloaded")
                        .Executes(r => GetUsage(r.Source, "_setloaded")))
                    .Then(l => l.Literal("_delete")
                        .Executes(r => GetUsage(r.Source, "_delete")))
                )
            );

            dispatcher.Register(l => l.Literal(CmdName)
                .Then(l => l.Literal("status")
                    .Executes(r => LogChunkStatus(r.Source))
                    .Then(l => l.Argument("Location", MccArguments.Location())
                        .Executes(r => LogChunkStatus(r.Source, pos: MccArguments.GetLocation(r, "Location"))))
                    .Then(l => l.Argument("Chunk", MccArguments.Tuple())
                        .Executes(r => LogChunkStatus(r.Source, markedChunkPos: MccArguments.GetTuple(r, "Chunk")))))
                .Then(l => l.Literal("_setloading")
                    .Then(l => l.Argument("Location", MccArguments.Location())
                        .Executes(r => DebugSetLoading(r.Source, pos: MccArguments.GetLocation(r, "Location"))))
                    .Then(l => l.Argument("Chunk", MccArguments.Tuple())
                        .Executes(r => DebugSetLoading(r.Source, markedChunkPos: MccArguments.GetTuple(r, "Chunk")))))
                .Then(l => l.Literal("_setloaded")
                    .Then(l => l.Argument("Location", MccArguments.Location())
                        .Executes(r => DebugSetLoaded(r.Source, pos: MccArguments.GetLocation(r, "Location"))))
                    .Then(l => l.Argument("Chunk", MccArguments.Tuple())
                        .Executes(r => DebugSetLoaded(r.Source, markedChunkPos: MccArguments.GetTuple(r, "Chunk")))))
                .Then(l => l.Literal("_delete")
                    .Then(l => l.Argument("Location", MccArguments.Location())
                        .Executes(r => DebugDelete(r.Source, pos: MccArguments.GetLocation(r, "Location"))))
                    .Then(l => l.Argument("Chunk", MccArguments.Tuple())
                        .Executes(r => DebugDelete(r.Source, markedChunkPos: MccArguments.GetTuple(r, "Chunk")))))
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
                "status"       =>  GetCmdDescTranslated(),
                "_setloading"  =>  GetCmdDescTranslated(),
                "_setloaded"   => GetCmdDescTranslated(),
                "_delete"      => GetCmdDescTranslated(),
                _              =>  GetCmdDescTranslated(),
#pragma warning restore format // @formatter:on
            });
        }

        private static int LogChunkStatus(CmdResult r, Location? pos = null, Tuple<int, int>? markedChunkPos = null)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetTerrainEnabled())
                return r.SetAndReturn(Status.FailNeedTerrain);

            World world = handler.GetWorld();
            Location current = handler.GetCurrentLocation();
            if (pos.HasValue)
                pos.Value.ToAbsolute(current);

            (int markChunkX, int markChunkZ) = markedChunkPos ??
                (pos.HasValue ? new(pos.Value.ChunkX, pos.Value.ChunkZ) : new(current.ChunkX, current.ChunkZ));

            StringBuilder sb = new();

            sb.Append(World.GetChunkLoadingStatus(handler.GetWorld()));
            sb.Append('\n');

            sb.AppendLine(string.Format(Translations.cmd_chunk_current, current, current.ChunkX, current.ChunkZ));
            if (markedChunkPos != null)
            {
                sb.Append(Translations.cmd_chunk_marked);
                if (pos.HasValue)
                    sb.Append(string.Format("X:{0:0.00} Y:{1:0.00} Z:{2:0.00}, ", pos.Value.X, pos.Value.Y, pos.Value.Z));
                sb.AppendLine(string.Format(Translations.cmd_chunk_chunk_pos, markChunkX, markChunkZ)); ;
            }

            int consoleHeight = Math.Max(Math.Max(Console.BufferHeight, Settings.Config.Main.Advanced.MinTerminalHeight) - 2, 25);
            if (consoleHeight % 2 == 0)
                --consoleHeight;

            int consoleWidth = Math.Max(Math.Max(Console.BufferWidth, Settings.Config.Main.Advanced.MinTerminalWidth) / 2, 17);
            if (consoleWidth % 2 == 0)
                --consoleWidth;

            int startZ = current.ChunkZ - consoleHeight, endZ = current.ChunkZ + consoleHeight;
            int startX = current.ChunkX - consoleWidth, endX = current.ChunkX + consoleWidth;

            int leftMost = endX, rightMost = startX, topMost = endZ, bottomMost = startZ;
            for (int z = startZ; z <= endZ; z++)
            {
                for (int x = startX; x <= endX; ++x)
                {
                    if (world[x, z] != null)
                    {
                        leftMost = Math.Min(leftMost, x);
                        rightMost = Math.Max(rightMost, x);
                        topMost = Math.Min(topMost, z);
                        bottomMost = Math.Max(bottomMost, z);
                    }
                }
            }

            // Include the player's location
            topMost = Math.Min(topMost, current.ChunkZ);
            bottomMost = Math.Max(bottomMost, current.ChunkZ);
            leftMost = Math.Min(leftMost, current.ChunkX);
            rightMost = Math.Max(rightMost, current.ChunkX);

            // Empty one row and one column each
            --leftMost; ++rightMost; --topMost; ++bottomMost;

            // Resize according to limitations
            if ((bottomMost - topMost + 1) > consoleHeight)
            {
                int delta = (bottomMost - topMost + 1) - consoleHeight;
                if (bottomMost - (delta + 1) / 2 < current.ChunkZ + 1)
                {
                    int bottomReduce = bottomMost - (current.ChunkZ + 1);
                    bottomMost -= bottomReduce;
                    topMost += delta - bottomReduce;
                }
                else if (topMost + delta / 2 > current.ChunkZ - 1)
                {
                    int topAdd = topMost - (current.ChunkZ - 1);
                    topMost += topAdd;
                    bottomMost -= delta - topAdd;
                }
                else
                {
                    topMost += delta / 2;
                    bottomMost -= (delta + 1) / 2;
                }
            }
            if ((rightMost - leftMost + 1) > consoleWidth)
            {
                int delta = (rightMost - leftMost + 1) - consoleWidth;
                if (rightMost - (delta + 1) / 2 < current.ChunkX + 1)
                {
                    int rightReduce = rightMost - (current.ChunkX + 1);
                    rightMost -= rightReduce;
                    leftMost += delta - rightReduce;
                }
                else if (leftMost + delta / 2 > current.ChunkX - 1)
                {
                    int leftAdd = leftMost - (current.ChunkX - 1);
                    leftMost += leftAdd;
                    rightMost -= delta - leftAdd;
                }
                else
                {
                    leftMost += delta / 2;
                    rightMost -= (delta + 1) / 2;
                }
            }

            // Try to include the marker chunk
            if (markedChunkPos != null &&
                (((Math.Max(bottomMost, markChunkZ) - Math.Min(topMost, markChunkZ) + 1) > consoleHeight) ||
                ((Math.Max(rightMost, markChunkX) - Math.Min(leftMost, markChunkX) + 1) > consoleWidth)))
                sb.AppendLine(Translations.cmd_chunk_outside);
            else
            {
                topMost = Math.Min(topMost, markChunkZ);
                bottomMost = Math.Max(bottomMost, markChunkZ);
                leftMost = Math.Min(leftMost, markChunkX);
                rightMost = Math.Max(rightMost, markChunkX);
            }


            // \ud83d\udd33: 🔳, \ud83d\udfe8: 🟨, \ud83d\udfe9: 🟩, \u25A1: □, \u25A3: ▣, \u25A0: ■
            string[] chunkStatusStr = Settings.Config.Main.Advanced.EnableEmoji ?
                new string[] { "\ud83d\udd33", "\ud83d\udfe8", "\ud83d\udfe9" } : new string[] { "\u25A1", "\u25A3", "\u25A0" };

            // Output
            for (int z = topMost; z <= bottomMost; ++z)
            {
                for (int x = leftMost; x <= rightMost; ++x)
                {
                    if (z == current.ChunkZ && x == current.ChunkX)
                        sb.Append("§§7");           // Player Location: background gray
                    else if (z == markChunkZ && x == markChunkX)
                        sb.Append("§§4");           // Marked chunk: background red

                    ChunkColumn? chunkColumn = world[x, z];
                    if (chunkColumn == null)
                        sb.Append(chunkStatusStr[0]);
                    else if (chunkColumn.FullyLoaded)
                        sb.Append(chunkStatusStr[2]);
                    else
                        sb.Append(chunkStatusStr[1]);

                    if ((z == current.ChunkZ && x == current.ChunkX) || (z == markChunkZ && x == markChunkX))
                        sb.Append("§§r");           // Reset background color
                }
                sb.Append('\n');
            }

            sb.Append(string.Format(Translations.cmd_chunk_icon, "§§7  §§r", "§§4  §§r", chunkStatusStr[0], chunkStatusStr[1], chunkStatusStr[2]));
            handler.Log.Info(sb.ToString());

            return r.SetAndReturn(Status.Done);
        }

        private static int DebugSetLoading(CmdResult r, Location? pos = null, Tuple<int, int>? markedChunkPos = null)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetTerrainEnabled())
                return r.SetAndReturn(Status.FailNeedTerrain);

            if (pos.HasValue)
                pos.Value.ToAbsolute(handler.GetCurrentLocation());
            handler.Log.Info(Translations.cmd_chunk_for_debug);
            (int chunkX, int chunkZ) = markedChunkPos ?? new(pos!.Value.ChunkX, pos!.Value.ChunkZ);
            ChunkColumn? chunkColumn = handler.GetWorld()[chunkX, chunkZ];
            if (chunkColumn != null)
                chunkColumn.FullyLoaded = false;

            if (chunkColumn == null)
                return r.SetAndReturn(Status.Fail, "Fail: chunk dosen't exist!");
            else
                return r.SetAndReturn(Status.Done, string.Format("Successfully marked chunk ({0}, {1}) as loading.", chunkX, chunkZ));
        }

        private static int DebugSetLoaded(CmdResult r, Location? pos = null, Tuple<int, int>? markedChunkPos = null)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetTerrainEnabled())
                return r.SetAndReturn(Status.FailNeedTerrain);

            if (pos.HasValue)
                pos.Value.ToAbsolute(handler.GetCurrentLocation());
            handler.Log.Info(Translations.cmd_chunk_for_debug);
            (int chunkX, int chunkZ) = markedChunkPos ?? new(pos!.Value.ChunkX, pos!.Value.ChunkZ);
            ChunkColumn? chunkColumn = handler.GetWorld()[chunkX, chunkZ];
            if (chunkColumn != null)
                chunkColumn.FullyLoaded = false;

            if (chunkColumn == null)
                return r.SetAndReturn(Status.Fail, "Fail: chunk dosen't exist!");
            else
                return r.SetAndReturn(Status.Done, string.Format("Successfully marked chunk ({0}, {1}) as loaded.", chunkX, chunkZ));
        }

        private static int DebugDelete(CmdResult r, Location? pos = null, Tuple<int, int>? markedChunkPos = null)
        {
            McClient handler = CmdResult.currentHandler!;
            if (!handler.GetTerrainEnabled())
                return r.SetAndReturn(Status.FailNeedTerrain);

            if (pos.HasValue)
                pos.Value.ToAbsolute(handler.GetCurrentLocation());
            handler.Log.Info(Translations.cmd_chunk_for_debug);
            (int chunkX, int chunkZ) = markedChunkPos ?? new(pos!.Value.ChunkX, pos!.Value.ChunkZ);
            handler.GetWorld()[chunkX, chunkZ] = null;

            return r.SetAndReturn(Status.Done, string.Format("Successfully deleted chunk ({0}, {1}).", chunkX, chunkZ));
        }
    }
}
