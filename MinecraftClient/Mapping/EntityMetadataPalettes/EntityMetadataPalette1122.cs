using System.Collections.Generic;

namespace MinecraftClient.Mapping.EntityMetadataPalettes;

public class EntityMetadataPalette1122 : EntityMetadataPalette
{
    // 1.9 - 1.12.2
    private readonly Dictionary<int, EntityMetaDataType> entityMetadataMappings = new()
    {
        { 0, EntityMetaDataType.Byte },
        { 1, EntityMetaDataType.VarInt },
        { 2, EntityMetaDataType.Float },
        { 3, EntityMetaDataType.String },
        { 4, EntityMetaDataType.Chat },
        { 5, EntityMetaDataType.Slot },
        { 6, EntityMetaDataType.Boolean },
        { 7, EntityMetaDataType.Rotation },
        { 8, EntityMetaDataType.Position },
        { 9, EntityMetaDataType.OptionalPosition },
        { 10, EntityMetaDataType.Direction },
        { 11, EntityMetaDataType.OptionalUuid },
        { 12, EntityMetaDataType.OptionalBlockId },
        { 13, EntityMetaDataType.Nbt },
    };
        
    public override Dictionary<int, EntityMetaDataType> GetEntityMetadataMappingsList()
    {
        return entityMetadataMappings;
    }
}