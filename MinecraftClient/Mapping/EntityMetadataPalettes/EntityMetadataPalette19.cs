using System.Collections.Generic;

namespace MinecraftClient.Mapping.EntityMetadataPalettes;

public class EntityMetadataPalette19 : EntityMetadataPalette
{
    // 1.8 : https://wiki.vg/index.php?title=Entity_metadata&oldid=6220 (Requires a different algorithm)
    // 1.9 : https://wiki.vg/index.php?title=Entity_metadata&oldid=7416
    private readonly Dictionary<int, EntityMetaDataType> entityMetadataMappings = new()
    {
        { 0, EntityMetaDataType.Byte },
        { 1, EntityMetaDataType.VarInt },
        { 2, EntityMetaDataType.Float },
        { 3, EntityMetaDataType.String },
        { 4, EntityMetaDataType.Chat },
        { 5, EntityMetaDataType.Slot },
        { 6, EntityMetaDataType.Boolean },
        { 7, EntityMetaDataType.Vector3 },
        { 8, EntityMetaDataType.Position },
        { 9, EntityMetaDataType.OptionalPosition },
        { 10, EntityMetaDataType.Direction },
        { 11, EntityMetaDataType.OptionalUuid },
        { 12, EntityMetaDataType.OptionalBlockId }
    };
        
    public override Dictionary<int, EntityMetaDataType> GetEntityMetadataMappingsList()
    {
        return entityMetadataMappings;
    }
}