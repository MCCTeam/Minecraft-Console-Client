using System;
using System.Runtime.CompilerServices;
using MinecraftClient.Mapping.BlockPalettes;
using MinecraftClient.Protocol.Message;

namespace MinecraftClient.Mapping
{
    /// <summary>
    /// Represents a Minecraft Block
    /// </summary>
    public struct Block
    {
        public static readonly Block Air = new(0);

        /// <summary>
        /// Get or set global block ID to Material mapping
        /// The global Palette is a concept introduced with Minecraft 1.13
        /// </summary>
        public static BlockPalette Palette { get; set; } = new Palette112();

        /// <summary>
        /// Storage for block ID and metadata, as ushort for compatibility, performance and lower memory footprint
        /// For Minecraft 1.12 and lower, first 12 bits contain block ID (0-4095), last 4 bits contain metadata (0-15)
        /// For Minecraft 1.13 and greater, all 16 bits are used to store block state ID (0-65535)
        /// </summary>
        private ushort blockIdAndMeta;

        /// <summary>
        /// Id of the block
        /// </summary>
        public int BlockId
        {
            get
            {
                if (Palette.IdHasMetadata)
                {
                    return blockIdAndMeta >> 4;
                }
                return blockIdAndMeta;
            }
            set
            {
                if (Palette.IdHasMetadata)
                {
                    if (value > (ushort.MaxValue >> 4) || value < 0)
                        throw new ArgumentOutOfRangeException(nameof(value), "Invalid block ID. Accepted range: 0-4095");
                    blockIdAndMeta = (ushort)(value << 4 | BlockMeta);
                }
                else
                {
                    if (value > ushort.MaxValue || value < 0)
                        throw new ArgumentOutOfRangeException(nameof(value), "Invalid block ID. Accepted range: 0-65535");
                    blockIdAndMeta = (ushort)value;
                }
            }
        }

        /// <summary>
        /// Metadata of the block.
        /// This field has no effect starting with Minecraft 1.13.
        /// </summary>
        public byte BlockMeta
        {
            get
            {
                if (Palette.IdHasMetadata)
                {
                    return (byte)(blockIdAndMeta & 0x0F);
                }
                return 0;
            }
            set
            {
                if (Palette.IdHasMetadata)
                {
                    blockIdAndMeta = (ushort)((blockIdAndMeta & ~0x0F) | (value & 0x0F));
                }
            }
        }

        /// <summary>
        /// Material of the block
        /// </summary>
        public Material Type
        {
            get
            {
                return Palette.FromId(BlockId);
            }
        }

        /// <summary>
        /// Get a block of the specified type and metadata
        /// </summary>
        /// <param name="type">Block type</param>
        /// <param name="metadata">Block metadata</param>
        public Block(short type, byte metadata)
        {
            if (!Palette.IdHasMetadata)
                throw new InvalidOperationException("Current global Palette does not support block Metadata");
            blockIdAndMeta = 0;
            BlockId = type;
            BlockMeta = metadata;
        }

        /// <summary>
        /// Get a block of the specified type and metadata OR block state
        /// </summary>
        /// <param name="typeAndMeta">Type and metadata packed in the same value OR block state</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public Block(ushort typeAndMeta)
        {
            blockIdAndMeta = typeAndMeta;
        }

        public string GetTypeString()
        {
            string typeStr = Type.ToString();
            string? trans = ChatParser.TranslateString("block.minecraft." + typeStr.ToUnderscoreCase());
            return string.IsNullOrEmpty(trans) ? typeStr : trans;
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
