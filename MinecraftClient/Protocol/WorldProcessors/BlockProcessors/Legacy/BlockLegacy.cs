using MinecraftClient.Mapping;

namespace MinecraftClient.Protocol.WorldProcessors.BlockProcessors.Legacy
{
    /// <summary>
    /// Represents a Minecraft Block
    /// </summary>
    internal struct BlockLegacy : IBlock
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
            get { return (short) (blockIdAndMeta >> 4); }
            set { blockIdAndMeta = (ushort) (value << 4 | BlockMeta); }
        }

        /// <summary>
        /// Metadata of the block
        /// </summary>
        public byte BlockMeta
        {
            get { return (byte) (blockIdAndMeta & 0x0F); }
            set { blockIdAndMeta = (ushort) ((blockIdAndMeta & ~0x0F) | (value & 0x0F)); }
        }

        /// <summary>
        /// Material of the block
        /// </summary>
        public Material Type
        {
            get { return (Material) BlockId; }
        }

        /// <summary>
        /// Get a block of the specified type and metadata
        /// </summary>
        /// <param name="type">Block type</param>
        /// <param name="metadata">Block metadata</param>
        public BlockLegacy(short type, byte metadata = 0)
        {
            this.blockIdAndMeta = 0;
            this.BlockId = type;
            this.BlockMeta = metadata;
        }

        /// <summary>
        /// Get a block of the specified type and metadata
        /// </summary>
        /// <param name="typeAndMeta">Type and metadata packed in the same value</param>
        public BlockLegacy(ushort typeAndMeta)
        {
            this.blockIdAndMeta = typeAndMeta;
        }

        /// <summary>
        /// Get a block of the specified type and metadata
        /// </summary>
        /// <param name="type">Block type</param>
        public BlockLegacy(Material type, byte metadata = 0)
            : this((short) type, metadata)
        {
        }

        /// <summary>
        /// String representation of the block
        /// </summary>
        public override string ToString()
        {
            return BlockId + (BlockMeta != 0 ? ":" + BlockMeta : "");
        }

        public bool CanHarmPlayers()
        {
            return Type.CanHarmPlayers();
        }

        public bool IsSolid()
        {
            return Type.IsSolid();
        }

        public bool IsLiquid()
        {
            return Type.IsLiquid();
        }
    }
}