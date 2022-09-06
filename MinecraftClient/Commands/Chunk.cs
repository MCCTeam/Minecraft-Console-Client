using System;
using System.Collections.Generic;
using System.Linq;
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
            if (hasArg(command))
            {
                string[] args = getArgs(command);
                if (args.Length > 0)
                {
                    if (args[0] == "status")
                    {
                        World world = handler.GetWorld();
                        Location current = handler.GetCurrentLocation();

                        Tuple<int, int>? markedChunkPos = ParseChunkPos(args);
                        (int markChunkX, int markChunkZ) = markedChunkPos ?? (new(current.ChunkX, current.ChunkZ));

                        StringBuilder sb = new();

                        sb.Append(getChunkLoadingStatus(handler.GetWorld()));
                        sb.Append('\n');

                        sb.Append(String.Format("Current location：{0}, chunk: ({1}, {2}).\n", current, current.ChunkX, current.ChunkZ));
                        if (markedChunkPos != null)
                        {
                            sb.Append("Marked location: ");
                            if (args.Length == 1 + 3)
                                sb.Append(String.Format("X:{0:0.00} Y:{1:0.00} Z:{2:0.00}, ", double.Parse(args[1]), double.Parse(args[2]), double.Parse(args[3])));
                            sb.Append(String.Format("chunk: ({0}, {1}).\n", markChunkX, markChunkZ));
                        }

                        if (markedChunkPos != null &&
                            (markChunkX < current.ChunkX - 16 || markChunkX >= current.ChunkX + 16 || markChunkZ < current.ChunkZ - 16 || markChunkZ >= current.ChunkZ + 16))
                            sb.Append("§x§0Since the marked chunk is outside the graph, it will not be displayed!§r\n");

                        int consoleHei = Math.Max(Console.BufferHeight - 2, 21);
                        if (consoleHei % 2 == 0)
                            --consoleHei;

                        int consoleWid = Math.Max(Console.BufferWidth / 2, 21);
                        if (consoleWid % 2 == 0)
                            --consoleWid;

                        int startZ = current.ChunkZ - consoleHei / 2, endZ = current.ChunkZ + 1 + consoleHei / 2;
                        int startX = current.ChunkX - consoleWid / 2, endX = current.ChunkX + 1 + consoleWid / 2;

                        int leftMost = endX, rightMost = startX, topMost = endZ, bottomMost = startZ;

                        Dictionary<Tuple<int, int>, byte> chunkStatus = new();
                        for (int z = startZ - 1; z <= endZ + 1; z++)
                        {
                            for (int x = startX - 1; x <= endX + 1; ++x)
                            {
                                ChunkColumn? chunkColumn = world[x, z];
                                if (chunkColumn == null)
                                    chunkStatus[new(x, z)] = 0; // "🔳" white hollow square
                                else
                                {
                                    leftMost = Math.Min(leftMost, x);
                                    rightMost = Math.Max(rightMost, x);
                                    topMost = Math.Min(topMost, z);
                                    bottomMost = Math.Max(bottomMost, z);
                                    if (chunkColumn.FullyLoaded)
                                        chunkStatus[new(x, z)] = 1; // "🟩" green
                                    else
                                        chunkStatus[new(x, z)] = 2; // "🟨" yellow
                                }
                            }
                        }

                        // Add a blank line
                        if (topMost != startZ)
                            --topMost;
                        if (bottomMost != endZ)
                            ++bottomMost;
                        if (Console.BufferWidth / 2 >= ((rightMost - leftMost + 1) + 2))
                        {
                            if (leftMost != startX)
                                --leftMost;
                            if (rightMost != endX)
                                ++rightMost;
                        }
                        else
                        {
                            leftMost = Math.Max(leftMost, startX);
                            rightMost = Math.Min(rightMost, endX);
                        }

                        // Output
                        string[] chunkStatuToEmoji = new string[] { "\ud83d\udd33", "\ud83d\udfe9", "\ud83d\udfe8" };
                        for (int z = topMost; z <= bottomMost; ++z)
                        {
                            for (int x = leftMost; x <= rightMost; ++x)
                            {
                                if (z == current.ChunkZ && x == current.ChunkX)
                                    sb.Append("§z"); // Player Location: background gray
                                else if (z == markChunkZ && x == markChunkX)
                                    sb.Append("§w"); // Marked chunk: background red
                                sb.Append(chunkStatuToEmoji[chunkStatus.GetValueOrDefault<Tuple<int, int>, byte>(new(x, z), 0)]);
                                if ((z == current.ChunkZ && x == current.ChunkX) || (z == markChunkZ && x == markChunkX))
                                    sb.Append("§r");
                            }
                            sb.Append('\n');
                        }

                        sb.Append("Player:§z  §r, MarkedChunk:§w  §r, NotReceived:\ud83d\udd33, Loading:\ud83d\udfe8, Loaded:\ud83d\udfe9");
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

        private Tuple<int, int>? ParseChunkPos(string[] args)
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

        private string getChunkLoadingStatus(World world)
        {
            double chunkLoadedRatio;
            if (world.chunkCnt == 0)
                chunkLoadedRatio = 0;
            else
                chunkLoadedRatio = (world.chunkCnt - world.chunkLoadNotCompleted) / (double)world.chunkCnt;

            string status = Translations.Get("cmd.move.chunk_loading_status",
                    chunkLoadedRatio, world.chunkCnt - world.chunkLoadNotCompleted, world.chunkCnt);

            return status;
        }
    }
}
