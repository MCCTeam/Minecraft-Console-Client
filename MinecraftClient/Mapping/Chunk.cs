using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Mapping
{
    /// <summary>
    /// Represent a chunk of terrain in a Minecraft world
    /// </summary>
    public class Chunk
    {
        public const int SizeX = 16;
        public const int SizeY = 16;
        public const int SizeZ = 16;

        /// <summary>
        /// Blocks contained into the chunk
        /// </summary>
        private readonly Block[,,] blocks = new Block[SizeX, SizeY, SizeZ];

        /// <summary>
        /// Read, or set the specified block
        /// </summary>
        /// <param name="blockX">Block X</param>
        /// <param name="blockY">Block Y</param>
        /// <param name="blockZ">Block Z</param>
        /// <returns>chunk at the given location</returns>
        public Block this[int blockX, int blockY, int blockZ]
        {
            get
            {
                if (blockX < 0 || blockX >= SizeX)
                    throw new ArgumentOutOfRangeException("blockX", "Must be between 0 and " + (SizeX - 1) + " (inclusive)");
                if (blockY < 0 || blockY >= SizeY)
                    throw new ArgumentOutOfRangeException("blockY", "Must be between 0 and " + (SizeY - 1) + " (inclusive)");
                if (blockZ < 0 || blockZ >= SizeZ)
                    throw new ArgumentOutOfRangeException("blockZ", "Must be between 0 and " + (SizeZ - 1) + " (inclusive)");
                return blocks[blockX, blockY, blockZ];
            }
            set
            {
                if (blockX < 0 || blockX >= SizeX)
                    throw new ArgumentOutOfRangeException("blockX", "Must be between 0 and " + (SizeX - 1) + " (inclusive)");
                if (blockY < 0 || blockY >= SizeY)
                    throw new ArgumentOutOfRangeException("blockY", "Must be between 0 and " + (SizeY - 1) + " (inclusive)");
                if (blockZ < 0 || blockZ >= SizeZ)
                    throw new ArgumentOutOfRangeException("blockZ", "Must be between 0 and " + (SizeZ - 1) + " (inclusive)");
                blocks[blockX, blockY, blockZ] = value;
            }
        }

        /// <summary>
        /// Get block at the specified location
        /// </summary>
        /// <param name="location">Location, a modulo will be applied</param>
        /// <returns>The block</returns>
        public Block GetBlock(Location location)
        {
            return this[location.ChunkBlockX, location.ChunkBlockY, location.ChunkBlockZ];
        }
    }
}
