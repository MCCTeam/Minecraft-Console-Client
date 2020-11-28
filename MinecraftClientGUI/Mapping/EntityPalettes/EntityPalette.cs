using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Mapping.EntityPalettes
{
    public abstract class EntityPalette
    {
        /// <summary>
        /// Get mapping dictionary. Must be overriden with proper implementation.
        /// </summary>
        /// <returns>Palette dictionary</returns>
        protected abstract Dictionary<int, EntityType> GetDict();

        /// <summary>
        /// Get mapping dictionary for pre-1.14 non-living entities.
        /// </summary>
        /// <returns>Palette dictionary for non-living entities (pre-1.14)</returns>
        protected virtual Dictionary<int, EntityType> GetDictNonLiving()
        {
            return null;
        }

        /// <summary>
        /// Get entity type from type ID
        /// </summary>
        /// <param name="id">Entity type ID</param>
        /// <returns>EntityType corresponding to the specified ID</returns>
        public EntityType FromId(int id, bool living)
        {
            Dictionary<int, EntityType> entityTypes = GetDict();
            Dictionary<int, EntityType> entityTypesNonLiving = GetDictNonLiving();

            if (entityTypesNonLiving != null && !living)
            {
                //Pre-1.14 non-living entities have a different set of IDs (entityTypesNonLiving != null)
                if (entityTypesNonLiving.ContainsKey(id))
                    return entityTypesNonLiving[id];
            }
            else
            {
                //1.14+ entities have the same set of IDs regardless of living status
                if (entityTypes.ContainsKey(id))
                    return entityTypes[id];
            }

            throw new System.IO.InvalidDataException("Unknown Entity ID " + id + ". Is Entity Palette up to date for this Minecraft version?");
        }
    }
}
