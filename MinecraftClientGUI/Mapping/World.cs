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
        /// Look for a block around the specified location
        /// </summary>
        /// <param name="from">Start location</param>
        /// <param name="block">Block type</param>
        /// <param name="radius">Search radius - larger is slower: O^3 complexity</param>
        /// <returns>Block matching the specified block type</returns>
        public List<Location> FindBlock(Location from, Material block, int radius)
        {
            return FindBlock(from, block, radius, radius, radius);
        }

        /// <summary>
        /// Look for a block around the specified location
        /// </summary>
        /// <param name="from">Start location</param>
        /// <param name="block">Block type</param>
        /// <param name="radiusx">Search radius on the X axis</param>
        /// <param name="radiusy">Search radius on the Y axis</param>
        /// <param name="radiusz">Search radius on the Z axis</param>
        /// <returns>Block matching the specified block type</returns>
        public List<Location> FindBlock(Location from, Material block, int radiusx, int radiusy, int radiusz)
        {
            Location minPoint = new Location(from.X - radiusx, from.Y - radiusy, from.Z - radiusz);
            Location maxPoint = new Location(from.X + radiusx, from.Y + radiusy, from.Z + radiusz);
            List<Location> list = new List<Location> { };
            for (double x = minPoint.X; x <= maxPoint.X; x++)
            {
                for (double y = minPoint.Y; y <= maxPoint.Y; y++)
                {
                    for (double z = minPoint.Z; z <= maxPoint.Z; z++)
                    {
                        Location doneloc = new Location(x, y, z);
                        Block doneblock = GetBlock(doneloc);
                        Material blockType = GetBlock(doneloc).Type;
                        if (blockType == block)
                        {
                            list.Add(doneloc);
                        }
                    }
                }
            }
            return list;
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
