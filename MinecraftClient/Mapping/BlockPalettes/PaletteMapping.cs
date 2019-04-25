using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Mapping.BlockPalettes
{
    public abstract class PaletteMapping
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
            Dictionary<int, Material> materials = GetDict();
            if (materials.ContainsKey(id))
                return materials[id];
            return Material.Air;
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
