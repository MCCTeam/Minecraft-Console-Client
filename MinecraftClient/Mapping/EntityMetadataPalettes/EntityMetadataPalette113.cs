using System.Collections.Generic;

namespace MinecraftClient.Mapping.EntityMetadataPalettes;

public class EntityMetadataPalette113 : EntityMetadataPalette
{
    // 1.13 : https://wiki.vg/index.php?title=Entity_metadata&oldid=14539
    private readonly Dictionary<int, EntityMetaDataType> entityMetadataMappings = new()
    {
        { 0, EntityMetaDataType.Byte },
        { 1, EntityMetaDataType.VarInt },
        { 2, EntityMetaDataType.Float },
        { 3, EntityMetaDataType.String },
        { 4, EntityMetaDataType.Chat },
        { 5, EntityMetaDataType.OptionalChat },
        { 6, EntityMetaDataType.Slot },
        { 7, EntityMetaDataType.Boolean },
        { 8, EntityMetaDataType.Rotation },
        { 9, EntityMetaDataType.Position },
        { 10, EntityMetaDataType.OptionalPosition },
        { 11, EntityMetaDataType.Direction },
        { 12, EntityMetaDataType.OptionalUuid },
        { 13, EntityMetaDataType.OptionalBlockId },
        { 14, EntityMetaDataType.Nbt },
        { 15, EntityMetaDataType.Particle }
    };
        
    public override Dictionary<int, EntityMetaDataType> GetEntityMetadataMappingsList()
    {
        return entityMetadataMappings;
    }
}