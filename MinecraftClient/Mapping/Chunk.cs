using System;
using System.Runtime.CompilerServices;

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
        private readonly Block[] blocks = new Block[SizeY * SizeZ * SizeX];


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
                    throw new ArgumentOutOfRangeException(nameof(blockX), "Must be between 0 and " + (SizeX - 1) + " (inclusive)");
                if (blockY < 0 || blockY >= SizeY)
                    throw new ArgumentOutOfRangeException(nameof(blockY), "Must be between 0 and " + (SizeY - 1) + " (inclusive)");
                if (blockZ < 0 || blockZ >= SizeZ)
                    throw new ArgumentOutOfRangeException(nameof(blockZ), "Must be between 0 and " + (SizeZ - 1) + " (inclusive)");

                return blocks[(blockY << 8) | (blockZ << 4) | blockX];
            }
            set
            {
                if (blockX < 0 || blockX >= SizeX)
                    throw new ArgumentOutOfRangeException(nameof(blockX), "Must be between 0 and " + (SizeX - 1) + " (inclusive)");
                if (blockY < 0 || blockY >= SizeY)
                    throw new ArgumentOutOfRangeException(nameof(blockY), "Must be between 0 and " + (SizeY - 1) + " (inclusive)");
                if (blockZ < 0 || blockZ >= SizeZ)
                    throw new ArgumentOutOfRangeException(nameof(blockZ), "Must be between 0 and " + (SizeZ - 1) + " (inclusive)");

                blocks[(blockY << 8) | (blockZ << 4) | blockX] = value;
            }
        }

        /// <summary>
        /// Used when parsing chunks
        /// </summary>
        /// <param name="blockX">Block X</param>
        /// <param name="blockY">Block Y</param>
        /// <param name="blockZ">Block Z</param>
        /// <param name="block">Block</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void SetWithoutCheck(int blockX, int blockY, int blockZ, Block block)
        {
            blocks[(blockY << 8) | (blockZ << 4) | blockX] = block;
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
