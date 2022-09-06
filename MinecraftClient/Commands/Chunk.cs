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

                        int startX = current.ChunkX - 16;
                        int startZ = current.ChunkZ - 16;
                        for (int dz = 0; dz < 32; dz++)
                        {
                            for (int dx = 0; dx < 32; ++dx)
                            {
                                ChunkColumn? chunkColumn = world[startX + dx, startZ + dz];
                                if (dz == 16 && dx == 16)
                                    sb.Append("§z"); // Player Location: background gray
                                else if (startZ + dz == markChunkZ && startX + dx == markChunkX)
                                    sb.Append("§w"); // Marked chunk: background red

                                if (chunkColumn == null)
                                    sb.Append("\ud83d\udd33"); // "🔳" white hollow square
                                else if (chunkColumn.FullyLoaded)
                                    sb.Append("\ud83d\udfe9"); // "🟩" green
                                else
                                    sb.Append("\ud83d\udfe8"); // "🟨" yellow

                                if ((dz == 16 && dx == 16) || (startZ + dz == markChunkZ && startX + dx == markChunkX))
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
