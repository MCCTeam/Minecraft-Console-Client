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
                        (int markChunkX, int markChunkZ) = (markedChunkPos == null) ? new(current.ChunkX, current.ChunkZ) : markedChunkPos;

                        StringBuilder sb = new();
                        sb.Append("Current position：");
                        sb.Append(current.ToString());
                        sb.Append(", chunk: ");
                        sb.Append(String.Format("({0}, {1})", current.ChunkX, current.ChunkZ));
                        sb.Append(".\n");

                        sb.Append(getChunkLoadingStatus(handler.GetWorld()));
                        sb.Append('\n');

                        int startX = current.ChunkX - 16;
                        int startZ = current.ChunkZ - 16;
                        for (int dz = 0; dz < 32; dz++)
                        {
                            for (int dx = 0; dx < 32; ++dx)
                            {
                                ChunkColumn? chunkColumn = world[startX + dx, startZ + dz];
                                if ((dz == 16 && dx == 16) || (startZ + dz == markChunkZ && startX + dx == markChunkX))
                                    sb.Append("§w"); // Player Location: background red

                                if (chunkColumn == null)
                                    sb.Append("🔳"); // empty
                                else if (chunkColumn.FullyLoaded)
                                    sb.Append("🟩"); // green
                                else
                                    sb.Append("🟨"); // yellow

                                if ((dz == 16 && dx == 16) || (startZ + dz == markChunkZ && startX + dx == markChunkX))
                                    sb.Append("§r");
                            }
                            sb.Append('\n');
                        }

                        sb.Append("Player or marked chunk:§w  §r, NotReceived:🔳, Loading:🟨, Loaded:🟩");
                        return sb.ToString();
                    }
                    else if (args[0] == "setloading") // Debug only!
                    {
                        Tuple<int, int>? chunkPos = ParseChunkPos(args);
                        if (chunkPos != null)
                        {
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
                    else if (args[0] == "setloaded") // Debug only!
                    {
                        Tuple<int, int>? chunkPos = ParseChunkPos(args);
                        if (chunkPos != null)
                        {
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
                    else if (args[0] == "delete") // Debug only!
                    {
                        Tuple<int, int>? chunkPos = ParseChunkPos(args);
                        if (chunkPos != null)
                        {
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
