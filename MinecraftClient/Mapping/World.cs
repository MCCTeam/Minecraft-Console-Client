using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Mapping
{
    /// <summary>
    /// Represents a Minecraft World
    /// </summary>
    public class World
    {
        /// <summary>
        /// The chunks contained into the Minecraft world
        /// </summary>
        private Dictionary<int, Dictionary<int, ChunkColumn>> chunks = new Dictionary<int, Dictionary<int, ChunkColumn>>();

        /// <summary>
        /// Read, set or unload the specified chunk column
        /// </summary>
        /// <param name="chunkX">ChunkColumn X</param>
        /// <param name="chunkY">ChunkColumn Y</param>
        /// <returns>chunk at the given location</returns>
        public ChunkColumn this[int chunkX, int chunkZ]
        {
            get
            {
                //Read a chunk
                if (chunks.ContainsKey(chunkX))
                    if (chunks[chunkX].ContainsKey(chunkZ))
                        return chunks[chunkX][chunkZ];
                return null;
            }
            set
            {
                if (value != null)
                {
                    //Update a chunk column
                    if (!chunks.ContainsKey(chunkX))
                        chunks[chunkX] = new Dictionary<int, ChunkColumn>();
                    chunks[chunkX][chunkZ] = value;
                }
                else
                {
                    //Unload a chunk column
                    if (chunks.ContainsKey(chunkX))
                    {
                        if (chunks[chunkX].ContainsKey(chunkZ))
                        {
                            chunks[chunkX].Remove(chunkZ);
                            if (chunks[chunkX].Count == 0)
                                chunks.Remove(chunkX);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get chunk column at the specified location
        /// </summary>
        /// <param name="location">Location to retrieve chunk column</param>
        /// <returns>The chunk column</returns>
        public ChunkColumn GetChunkColumn(Location location)
        {
            return this[location.ChunkX, location.ChunkZ];
        }

        /// <summary>
        /// Get block at the specified location
        /// </summary>
        /// <param name="location">Location to retrieve block from</param>
        /// <returns>Block at specified location or Air if the location is not loaded</returns>
        public Block GetBlock(Location location)
        {
            ChunkColumn column = GetChunkColumn(location);
            if (column != null)
            {
                Chunk chunk = column.GetChunk(location);
                if (chunk != null)
                    return chunk.GetBlock(location);
            }
            return new Block(0); //Air
        }

        /// <summary>
        /// Set block at the specified location
        /// </summary>
        /// <param name="location">Location to set block to</param>
        /// <param name="block">Block to set</param>
        public void SetBlock(Location location, Block block)
        {
            ChunkColumn column = this[location.ChunkX, location.ChunkZ];
            if (column != null)
            {
                Chunk chunk = column[location.ChunkY];
                if (chunk == null)
                    column[location.ChunkY] = chunk = new Chunk();
                chunk[location.ChunkBlockX, location.ChunkBlockY, location.ChunkBlockZ] = block;
            }
        }

        /// <summary>
        /// Clear all terrain data from the world
        /// </summary>
        public void Clear()
        {
            chunks = new Dictionary<int, Dictionary<int, ChunkColumn>>();
        }
    }
}
