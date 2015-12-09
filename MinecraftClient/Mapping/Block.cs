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
        /// Material of the block
        /// </summary>
        public Material Type
        {
            get
            {
                return (Material)BlockId;
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
        /// <param name="typeAndMeta">Type and metadata packed in the same value</param>
        public Block(ushort typeAndMeta)
        {
            this.blockIdAndMeta = typeAndMeta;
        }

        /// <summary>
        /// Get a block of the specified type and metadata
        /// </summary>
        /// <param name="type">Block type</param>
        public Block(Material type, byte metadata = 0)
            : this((short)type, metadata) { }

        /// <summary>
        /// String representation of the block
        /// </summary>
        public override string ToString()
        {
            return BlockId.ToString() + (BlockMeta != 0 ? ":" + BlockMeta.ToString() : "");
        }
    }
}
