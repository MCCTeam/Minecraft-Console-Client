using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Mapping
{
    /// <summary>
    /// Represents a Minecraft Block
    /// </summary>
    public struct Block
    {
        /// <summary>
        /// Storage for block ID and metadata
        /// </summary>
        private ushort blockIdAndMeta;

        /// <summary>
        /// Id of the block
        /// </summary>
        public short BlockId
        {
            get
            {
                return (short)(blockIdAndMeta >> 4);
            }
            set
            {
                blockIdAndMeta = (ushort)(value << 4 | BlockMeta);
            }
        }

        /// <summary>
        /// Metadata of the block
        /// </summary>
        public byte BlockMeta
        {
            get
            {
                return (byte)(blockIdAndMeta & 0x0F);
            }
            set
            {
                blockIdAndMeta = (ushort)((blockIdAndMeta & ~0x0F) | (value & 0x0F));
            }
        }

        /// <summary>
        /// Check if the block can be passed through or not
        /// </summary>
        public bool Solid
        {
            get
            {
                return BlockId != 0;
            }
        }

        /// <summary>
        /// Get a block of the specified type and metadata
        /// </summary>
        /// <param name="type">Block type</param>
        /// <param name="metadata">Block metadata</param>
        public Block(short type, byte metadata = 0)
        {
            this.blockIdAndMeta = 0;
            this.BlockId = type;
            this.BlockMeta = metadata;
        }

        /// <summary>
        /// Get a block of the specified type and metadata
        /// </summary>
        /// <param name="typeAndMeta"></param>
        public Block(ushort typeAndMeta)
        {
            this.blockIdAndMeta = typeAndMeta;
        }

        /// <summary>
        /// Represents an empty block
        /// </summary>
        public static Block Air
        {
            get
            {
                return new Block(0);
            }
        }

        /// <summary>
        /// String representation of the block
        /// </summary>
        public override string ToString()
        {
            return BlockId.ToString() + (BlockMeta != 0 ? ":" + BlockMeta.ToString() : "");
        }
    }
}
