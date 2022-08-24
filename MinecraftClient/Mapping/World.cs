using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

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
        private Dictionary<int, Dictionary<int, ChunkColumn>> chunks = new();

        /// <summary>
        /// The dimension info of the world
        /// </summary>
        private static Dimension dimension = new();

        /// <summary>
        /// Lock for thread safety
        /// </summary>
        private readonly ReaderWriterLockSlim chunksLock = new();

        /// <summary>
        /// Chunk data parsing progress
        /// </summary>
        public int chunkCnt = 0;
        public int chunkLoadNotCompleted = 0;

        /// <summary>
        /// Read, set or unload the specified chunk column
        /// </summary>
        /// <param name="chunkX">ChunkColumn X</param>
        /// <param name="chunkZ">ChunkColumn Z</param>
        /// <returns>chunk at the given location</returns>
        public ChunkColumn? this[int chunkX, int chunkZ]
        {
            get
            {
                chunksLock.EnterReadLock();
                try
                {
                    //Read a chunk
                    if (chunks.ContainsKey(chunkX))
                        if (chunks[chunkX].ContainsKey(chunkZ))
                            return chunks[chunkX][chunkZ];
                    return null;
                }
                finally
                {
                    chunksLock.ExitReadLock();
                }
            }
            set
            {
                chunksLock.EnterWriteLock();
                try
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
                finally
                {
                    chunksLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Set dimension type
        /// </summary>
        /// <param name="name">	The name of the dimension type</param>
        /// <param name="nbt">The dimension type (NBT Tag Compound)</param>
        public static void SetDimension(string name, Dictionary<string, object> nbt)
        {
            // will change in 1.19 and above
            dimension = new Dimension(name, nbt);
        }

        /// <summary>
        /// Get current dimension
        /// </summary>
        /// <returns>Current dimension</returns>
        public static Dimension GetDimension()
        {
            return dimension;
        }

        /// <summary>
        /// Set chunk column at the specified location
        /// </summary>
        /// <param name="chunkX">ChunkColumn X</param>
        /// <param name="chunkY">ChunkColumn Y</param>
        /// <param name="chunkZ">ChunkColumn Z</param>
        /// <param name="chunkColumnSize">ChunkColumn size</param>
        /// <param name="chunk">Chunk data</param>
        /// <param name="loadCompleted">Whether the ChunkColumn has been fully loaded</param>
        public void StoreChunk(int chunkX, int chunkY, int chunkZ, int chunkColumnSize, Chunk chunk, bool loadCompleted)
        {
            ChunkColumn? chunkColumn = null;

            chunksLock.EnterUpgradeableReadLock();
            try
            {
                //Read a chunk
                if (chunks.ContainsKey(chunkX))
                    if (chunks[chunkX].ContainsKey(chunkZ))
                        chunkColumn = chunks[chunkX][chunkZ];

                if (chunkColumn == null)
                {
                    chunkColumn = new ChunkColumn(chunkColumnSize);
                    chunksLock.EnterWriteLock();
                    try
                    {
                        //Update a chunk column
                        if (!chunks.ContainsKey(chunkX))
                            chunks[chunkX] = new Dictionary<int, ChunkColumn>();
                        chunks[chunkX][chunkZ] = chunkColumn;
                    }
                    finally
                    {
                        chunksLock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                chunksLock.ExitUpgradeableReadLock();
            }

            chunkColumn[chunkY] = chunk;
            if (loadCompleted)
                chunkColumn.FullyLoaded = true;
        }

        /// <summary>
        /// Get chunk column at the specified location
        /// </summary>
        /// <param name="location">Location to retrieve chunk column</param>
        /// <returns>The chunk column</returns>
        public ChunkColumn? GetChunkColumn(Location location)
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
            ChunkColumn? column = GetChunkColumn(location);
            if (column != null)
            {
                Chunk? chunk = column.GetChunk(location);
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
                        Material blockType = doneblock.Type;
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
            ChunkColumn? column = this[location.ChunkX, location.ChunkZ];
            if (column != null && column.ColumnSize >= location.ChunkY)
            {
                Chunk? chunk = column.GetChunk(location);
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
            chunksLock.EnterWriteLock();
            try
            {
                chunks = new Dictionary<int, Dictionary<int, ChunkColumn>>();
                chunkCnt = 0;
                chunkLoadNotCompleted = 0;
            }
            finally
            {
                chunksLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Get the location of block of the entity is looking
        /// </summary>
        /// <param name="location">Location of the entity</param>
        /// <param name="yaw">Yaw of the entity</param>
        /// <param name="pitch">Pitch of the entity</param>
        /// <returns>Location of the block or empty Location if no block was found</returns>
        public Location GetLookingBlockLocation(Location location, double yaw, double pitch)
        {
            double rotX = (Math.PI / 180) * yaw;
            double rotY = (Math.PI / 180) * pitch;
            double x = -Math.Cos(rotY) * Math.Sin(rotX);
            double y = -Math.Sin(rotY);
            double z = Math.Cos(rotY) * Math.Cos(rotX);
            Location vector = new Location(x, y, z);
            for (int i = 0; i < 5; i++)
            {
                Location newVector = vector * i;
                Location blockLocation = location.EyesLocation() + new Location(newVector.X, newVector.Y, newVector.Z);
                blockLocation.X = Math.Floor(blockLocation.X);
                blockLocation.Y = Math.Floor(blockLocation.Y);
                blockLocation.Z = Math.Floor(blockLocation.Z);
                Block b = GetBlock(blockLocation);
                if (b.Type != Material.Air)
                {
                    return blockLocation;
                }
            }
            return new Location();
        }
    }
}
