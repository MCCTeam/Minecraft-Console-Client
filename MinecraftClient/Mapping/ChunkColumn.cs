using System;

namespace MinecraftClient.Mapping
{
    /// <summary>
    /// Represent a column of chunks of terrain in a Minecraft world
    /// </summary>
    public class ChunkColumn
    {
        public int ColumnSize;

        public bool FullyLoaded = false;

        /// <summary>
        /// Blocks contained into the chunk
        /// </summary>
        private readonly Chunk?[] chunks;

        /// <summary>
        /// Create a new ChunkColumn
        /// </summary>
        public ChunkColumn(int size = 16)
        {
            ColumnSize = size;
            chunks = new Chunk?[size];
        }

        /// <summary>
        /// Get or set the specified chunk column
        /// </summary>
        /// <param name="chunkX">ChunkColumn X</param>
        /// <param name="chunkY">ChunkColumn Y</param>
        /// <returns>chunk at the given location</returns>
        public Chunk? this[int chunkY]
        {
            get
            {
                return chunks[chunkY];
            }
            set
            {
                chunks[chunkY] = value;
            }
        }

        /// <summary>
        /// Get chunk at the specified location
        /// </summary>
        /// <param name="location">Location, a modulo will be applied</param>
        /// <returns>The chunk, or null if not loaded</returns>
        public Chunk? GetChunk(Location location)
        {
            try
            {
                return this[location.ChunkY];
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            }
        }
    }
}
