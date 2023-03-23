using System.Collections.Generic;

namespace MinecraftClient.Mapping;

public abstract class EntityMetadataPalette
{
    public abstract Dictionary<int, EntityMetaDataType> GetEntityMetadataMappingsList();
}