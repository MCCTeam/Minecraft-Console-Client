using System.Collections.Generic;

namespace MinecraftClient.Mapping.EntityMetadataPalettes;

public class EntityMetadataPalette18 : EntityMetadataPalette
{
    // 1.8 : https://wiki.vg/index.php?title=Entity_metadata&oldid=6220
    private readonly Dictionary<int, EntityMetaDataType> entityMetadataMappings = new()
    {
        { 0, EntityMetaDataType.Byte },
        { 1, EntityMetaDataType.Short },
        { 2, EntityMetaDataType.Int },
        { 3, EntityMetaDataType.Float },
        { 4, EntityMetaDataType.String },
        { 5, EntityMetaDataType.Slot },
        { 6, EntityMetaDataType.Vector3Int },
        { 7, EntityMetaDataType.Rotation }
    };
        
    public override Dictionary<int, EntityMetaDataType> GetEntityMetadataMappingsList()
    {
        return entityMetadataMappings;
    }
}