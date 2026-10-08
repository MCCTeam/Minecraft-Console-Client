using System.Globalization;
using System.Text;
using DMCBK.Core.Commands;
using DMCBK.Core.Localization;
using DMCBK.Core.Presentation;
using Umpk.Client.Snapshots;
using Umpk.Geometry;

namespace Mcc.Cli.Presentation;

/// <summary>Terminal chunk map layout, colors and legend.</summary>
internal static class ChunkMapRenderer
{
    public static IReadOnlyList<string> Render(ChunkMapPresentation request, GlyphSet glyphSet)
    {
        var lines = new List<string>();
        ChunkStatusGrid grid = request.Grid;
        Vec3d? markedLocation = request.MarkedLocation;
        ChunkPos? markedChunk = request.MarkedChunk;
        int consoleWidth = request.Columns, consoleHeight = request.Rows;
        string[] glyphs = glyphSet.ChunkStatus;
        int currentChunkX = grid.CenterChunkX, currentChunkZ = grid.CenterChunkZ;
        bool marked = markedChunk is not null || markedLocation is not null;

        // Legacy Chunk.cs:86-87: the marker is the chunk pair when one was given, the chunk holding the given location when that form was used, and the player's own chunk otherwise.
        int markChunkX, markChunkZ;
        if (markedChunk is { } chunk)
        {
            markChunkX = chunk.X;
            markChunkZ = chunk.Z;
        }
        else if (markedLocation is { } marker)
        {
            ChunkPos markerChunk = ChunkPos.Containing(BlockPos.Containing(marker));
            markChunkX = markerChunk.X;
            markChunkZ = markerChunk.Z;
        }
        else
        {
            markChunkX = currentChunkX;
            markChunkZ = currentChunkZ;
        }

        lines.Add(LoadingStatus(grid, request));
        lines.Add(McStrings.Format(
            "cmd.chunk.current", request.PlayerPositionText, currentChunkX, currentChunkZ));

        if (marked)
        {
            // Legacy Chunk.cs:95-101 printed this only for the chunk-pair form, so the coordinate form marked a chunk red on the map and never said which one; both forms report it here.
            StringBuilder line = new(McStrings.Get("cmd.chunk.marked"));
            if (markedLocation is { } ml)
                line.Append(CultureInfo.CurrentCulture, $"X:{ml.X:0.00} Y:{ml.Y:0.00} Z:{ml.Z:0.00}, ");

            line.Append(McStrings.Format("cmd.chunk.chunk_pos", markChunkX, markChunkZ));
            lines.Add(line.ToString());
        }

        // Legacy Chunk.cs:115-137: the drawn region is the bounding box of every column the client holds within the scan range, widened to include the player, then padded by one empty ring.
        int startZ = currentChunkZ - consoleHeight, endZ = currentChunkZ + consoleHeight;
        int startX = currentChunkX - consoleWidth, endX = currentChunkX + consoleWidth;

        int leftMost = endX, rightMost = startX, topMost = endZ, bottomMost = startZ;
        for (int z = startZ; z <= endZ; z++)
        {
            for (int x = startX; x <= endX; ++x)
            {
                if (IsPresent(grid, x, z))
                {
                    leftMost = Math.Min(leftMost, x);
                    rightMost = Math.Max(rightMost, x);
                    topMost = Math.Min(topMost, z);
                    bottomMost = Math.Max(bottomMost, z);
                }
            }
        }

        topMost = Math.Min(topMost, currentChunkZ);
        bottomMost = Math.Max(bottomMost, currentChunkZ);
        leftMost = Math.Min(leftMost, currentChunkX);
        rightMost = Math.Max(rightMost, currentChunkX);

        --leftMost;
        ++rightMost;
        --topMost;
        ++bottomMost;

        // Legacy Chunk.cs:143-184: shrink to the terminal, keeping the player's row/column inside.
        if (bottomMost - topMost + 1 > consoleHeight)
        {
            int delta = bottomMost - topMost + 1 - consoleHeight;
            if (bottomMost - ((delta + 1) / 2) < currentChunkZ + 1)
            {
                int bottomReduce = bottomMost - (currentChunkZ + 1);
                bottomMost -= bottomReduce;
                topMost += delta - bottomReduce;
            }
            else if (topMost + (delta / 2) > currentChunkZ - 1)
            {
                int topAdd = topMost - (currentChunkZ - 1);
                topMost += topAdd;
                bottomMost -= delta - topAdd;
            }
            else
            {
                topMost += delta / 2;
                bottomMost -= (delta + 1) / 2;
            }
        }

        if (rightMost - leftMost + 1 > consoleWidth)
        {
            int delta = rightMost - leftMost + 1 - consoleWidth;
            if (rightMost - ((delta + 1) / 2) < currentChunkX + 1)
            {
                int rightReduce = rightMost - (currentChunkX + 1);
                rightMost -= rightReduce;
                leftMost += delta - rightReduce;
            }
            else if (leftMost + (delta / 2) > currentChunkX - 1)
            {
                int leftAdd = leftMost - (currentChunkX - 1);
                leftMost += leftAdd;
                rightMost -= delta - leftAdd;
            }
            else
            {
                leftMost += delta / 2;
                rightMost -= (delta + 1) / 2;
            }
        }

        // Legacy Chunk.cs:186-197: pull the marked chunk into view when it fits, say so when it does not.
        if (marked &&
            (Math.Max(bottomMost, markChunkZ) - Math.Min(topMost, markChunkZ) + 1 > consoleHeight ||
             Math.Max(rightMost, markChunkX) - Math.Min(leftMost, markChunkX) + 1 > consoleWidth))
            lines.Add(McStrings.Get("cmd.chunk.outside"));
        else
        {
            topMost = Math.Min(topMost, markChunkZ);
            bottomMost = Math.Max(bottomMost, markChunkZ);
            leftMost = Math.Min(leftMost, markChunkX);
            rightMost = Math.Max(rightMost, markChunkX);
        }

        StringBuilder row = new();
        for (int z = topMost; z <= bottomMost; ++z)
        {
            row.Clear();
            for (int x = leftMost; x <= rightMost; ++x)
            {
                bool isPlayer = z == currentChunkZ && x == currentChunkX;
                bool isMarked = z == markChunkZ && x == markChunkX;
                if (isPlayer)
                    row.Append("§§7");          // Player Location: background gray
                else if (isMarked)
                    row.Append("§§4");          // Marked chunk: background red

                row.Append(Glyph(grid, x, z, glyphs, request));

                if (isPlayer || isMarked)
                    row.Append("§§r");          // Reset background color
            }

            lines.Add(row.ToString());
        }

        lines.Add(McStrings.Format(
            "cmd.chunk.icon", "§§7  §§r", "§§4  §§r", glyphs[0], glyphs[1], glyphs[2]));

        return lines;
    }

    private static string LoadingStatus(ChunkStatusGrid grid, ChunkMapPresentation request)
    {
        int total = 0, completed = 0;
        for (int row = 0; row < grid.Rows; row++)
        {
            for (int column = 0; column < grid.Columns; column++)
            {
                if (!grid.IsLoadedAt(row, column))
                    continue;

                total++;
                ChunkPos pos = grid.ChunkAt(row, column);
                if (!(request.LoadingChunks.Contains(pos)))
                    completed++;
            }
        }

        double ratio = total == 0 ? 0 : completed / (double)total;
        return McStrings.Format("cmd.move.chunk_loading_status", ratio, completed, total);
    }

    private static string Glyph(ChunkStatusGrid grid, int chunkX, int chunkZ, string[] glyphs, ChunkMapPresentation request)
    {
        if (!IsPresent(grid, chunkX, chunkZ))
            return glyphs[0];

        return request.LoadingChunks.Contains(new ChunkPos(chunkX, chunkZ)) ? glyphs[1] : glyphs[2];
    }

    private static bool IsPresent(ChunkStatusGrid grid, int chunkX, int chunkZ)
    {
        int column = chunkX - grid.CenterChunkX + ((grid.Columns - 1) / 2);
        int row = chunkZ - grid.CenterChunkZ + ((grid.Rows - 1) / 2);
        return column >= 0 && column < grid.Columns
            && row >= 0 && row < grid.Rows
            && grid.IsLoadedAt(row, column);
    }

}
