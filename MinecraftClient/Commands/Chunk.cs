using System;
using System.Collections.Generic;
using System.Text;
using MinecraftClient.Mapping;

namespace MinecraftClient.Commands
{
    public class Chunk : Command
    {
        public override string CmdName { get { return "chunk"; } }
        public override string CmdUsage { get { return "chunk status [chunkX chunkZ|locationX locationY locationZ]"; } }
        public override string CmdDesc { get { return "cmd.chunk.desc"; } }

        public override string Run(McClient handler, string command, Dictionary<string, object>? localVars)
        {
            if (HasArg(command))
            {
                string[] args = GetArgs(command);
                if (args.Length > 0)
                {
                    if (args[0] == "status")
                    {
                        World world = handler.GetWorld();
                        Location current = handler.GetCurrentLocation();

                        Tuple<int, int>? markedChunkPos = ParseChunkPos(args);
                        (int markChunkX, int markChunkZ) = markedChunkPos ?? (new(current.ChunkX, current.ChunkZ));

                        StringBuilder sb = new();

                        sb.Append(World.GetChunkLoadingStatus(handler.GetWorld()));
                        sb.Append('\n');

                        sb.Append(String.Format("Current location：{0}, chunk: ({1}, {2}).\n", current, current.ChunkX, current.ChunkZ));
                        if (markedChunkPos != null)
                        {
                            sb.Append("Marked location: ");
                            if (args.Length == 1 + 3)
                                sb.Append(String.Format("X:{0:0.00} Y:{1:0.00} Z:{2:0.00}, ", double.Parse(args[1]), double.Parse(args[2]), double.Parse(args[3])));
                            sb.Append(String.Format("chunk: ({0}, {1}).\n", markChunkX, markChunkZ));
                        }

                        int consoleHeight = Math.Max(Console.BufferHeight - 2, 25);
                        if (consoleHeight % 2 == 0)
                            --consoleHeight;

                        int consoleWidth = Math.Max(Console.BufferWidth / 2, 17);
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
                            sb.Append("§x§0Since the marked chunk is outside the graph, it will not be displayed!§r\n");
                        else
                        {
                            topMost = Math.Min(topMost, markChunkZ);
                            bottomMost = Math.Max(bottomMost, markChunkZ);
                            leftMost = Math.Min(leftMost, markChunkX);
                            rightMost = Math.Max(rightMost, markChunkX);
                        }


                        // \ud83d\udd33: 🔳, \ud83d\udfe8: 🟨, \ud83d\udfe9: 🟩, \u25A1: □, \u25A3: ▣, \u25A0: ■
                        string[] chunkStatusStr = Settings.EnableEmoji ?
                            new string[] { "\ud83d\udd33", "\ud83d\udfe8", "\ud83d\udfe9" } : new string[] { "\u25A1", "\u25A3", "\u25A0" };

                        // Output
                        for (int z = topMost; z <= bottomMost; ++z)
                        {
                            for (int x = leftMost; x <= rightMost; ++x)
                            {
                                if (z == current.ChunkZ && x == current.ChunkX)
                                    sb.Append("§z");           // Player Location: background gray
                                else if (z == markChunkZ && x == markChunkX)
                                    sb.Append("§w");           // Marked chunk: background red

                                ChunkColumn? chunkColumn = world[x, z];
                                if (chunkColumn == null)
                                    sb.Append(chunkStatusStr[0]);
                                else if (chunkColumn.FullyLoaded)
                                    sb.Append(chunkStatusStr[2]);
                                else
                                    sb.Append(chunkStatusStr[1]);

                                if ((z == current.ChunkZ && x == current.ChunkX) || (z == markChunkZ && x == markChunkX))
                                    sb.Append("§r");           // Reset background color
                            }
                            sb.Append('\n');
                        }

                        sb.Append("Player:§z  §r, MarkedChunk:§w  §r, ");
                        sb.Append(string.Format("NotReceived:{0}, Loading:{1}, Loaded:{2}", chunkStatusStr[0], chunkStatusStr[1], chunkStatusStr[2]));
                        return sb.ToString();
                    }
                    else if (args[0] == "setloading") // For debugging
                    {
                        Tuple<int, int>? chunkPos = ParseChunkPos(args);
                        if (chunkPos != null)
                        {
                            handler.Log.Info("§x§0This command is used for debugging, make sure you know what you are doing.§r");
                            World world = handler.GetWorld();
                            (int chunkX, int chunkZ) = chunkPos;
                            ChunkColumn? chunkColumn = world[chunkX, chunkZ];
                            if (chunkColumn != null)
                                chunkColumn.FullyLoaded = false;
                            return (chunkColumn == null) ? "Fail: chunk dosen't exist!" :
                                String.Format("Successfully marked chunk ({0}, {1}) as loading.", chunkX, chunkZ);
                        }
                        else
                            return GetCmdDescTranslated();
                    }
                    else if (args[0] == "setloaded") // For debugging
                    {
                        Tuple<int, int>? chunkPos = ParseChunkPos(args);
                        if (chunkPos != null)
                        {
                            handler.Log.Info("§x§0This command is used for debugging, make sure you know what you are doing.§r");
                            World world = handler.GetWorld();
                            (int chunkX, int chunkZ) = chunkPos;
                            ChunkColumn? chunkColumn = world[chunkX, chunkZ];
                            if (chunkColumn != null)
                                chunkColumn.FullyLoaded = true;
                            return (chunkColumn == null) ? "Fail: chunk dosen't exist!" :
                                String.Format("Successfully marked chunk ({0}, {1}) as loaded.", chunkX, chunkZ);
                        }
                        else
                            return GetCmdDescTranslated();
                    }
                    else if (args[0] == "delete") // For debugging
                    {
                        Tuple<int, int>? chunkPos = ParseChunkPos(args);
                        if (chunkPos != null)
                        {
                            handler.Log.Info("§x§0This command is used for debugging, make sure you know what you are doing.§r");
                            World world = handler.GetWorld();
                            (int chunkX, int chunkZ) = chunkPos;
                            world[chunkX, chunkZ] = null;
                            return String.Format("Successfully deleted chunk ({0}, {1}).", chunkX, chunkZ);
                        }
                        else
                            return GetCmdDescTranslated();
                    }
                    else
                        return GetCmdDescTranslated();
                }
                else
                    return GetCmdDescTranslated();
            }
            else
                return GetCmdDescTranslated();
        }

        private static Tuple<int, int>? ParseChunkPos(string[] args)
        {
            try
            {
                int chunkX, chunkZ;
                if (args.Length == 1 + 3)
                {
                    Location pos = new(double.Parse(args[1]), double.Parse(args[2]), double.Parse(args[3]));
                    chunkX = pos.ChunkX;
                    chunkZ = pos.ChunkZ;
                }
                else if (args.Length == 1 + 2)
                {
                    chunkX = int.Parse(args[1]);
                    chunkZ = int.Parse(args[2]);
                }
                else
                    return null;
                return new(chunkX, chunkZ);
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
