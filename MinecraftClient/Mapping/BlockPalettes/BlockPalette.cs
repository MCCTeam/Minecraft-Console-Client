using System;
using System.Collections.Generic;

namespace MinecraftClient.Mapping.BlockPalettes
{
    public abstract class BlockPalette
    {
        /// <summary>
        /// Get mapping dictionary. Must be overriden with proper implementation.
        /// </summary>
        /// <returns>Palette dictionary</returns>
        protected abstract Dictionary<int, Material> GetDict();

        /// <summary>
        /// Get material from block ID or block state ID
        /// </summary>
        /// <param name="id">Block ID (up to MC 1.12) or block state (MC 1.13+)</param>
        /// <returns>Material corresponding to the specified ID</returns>
        public Material FromId(int id)
        {
            return GetDict().TryGetValue(id, out Material material) ? material : Material.Air;
        }

        /// <summary>
        /// Get block-state properties for a modern block state ID.
        /// </summary>
        /// <param name="stateId">Raw block state ID.</param>
        /// <returns>Block-state property names and values, or an empty map when unavailable.</returns>
        public IReadOnlyDictionary<string, string> GetStateProperties(int stateId)
        {
            BlockStateDefinition[] definitions = GetStateDefinitions();
            int low = 0;
            int high = definitions.Length - 1;

            while (low <= high)
            {
                int middle = low + ((high - low) / 2);
                BlockStateDefinition definition = definitions[middle];
                if (stateId < definition.FirstStateId)
                {
                    high = middle - 1;
                }
                else if (stateId > definition.LastStateId)
                {
                    low = middle + 1;
                }
                else
                {
                    return definition.GetProperties(stateId);
                }
            }

            return BlockStateDefinition.EmptyProperties;
        }

        /// <summary>
        /// Get compact block-state definitions sorted by their first state ID.
        /// </summary>
        protected virtual BlockStateDefinition[] GetStateDefinitions()
        {
            return Array.Empty<BlockStateDefinition>();
        }

        /// <summary>
        /// Returns TRUE if block ID uses old metadata encoding with ID and Meta inside one ushort
        /// Only Palette112 should override this.
        /// </summary>
        public virtual bool IdHasMetadata
        {
            get
            {
                return false;
            }
        }
    }
}
