using System.Collections.Generic;

namespace MinecraftClient.Mapping.EntityMetadataPalettes;

/// <summary>
/// 1.13 - 1.19.2
/// </summary>
public class EntityMetadataPalette1191 : EntityMetadataPalette
{
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
        { 15, EntityMetaDataType.Particle },
        { 16, EntityMetaDataType.VillagerData },
        { 17, EntityMetaDataType.OptionalVarInt },
        { 18, EntityMetaDataType.Pose },
        { 19, EntityMetaDataType.CatVariant },
        { 20, EntityMetaDataType.FrogVariant },
        { 21, EntityMetaDataType.OptionalGlobalPosition },
        { 22, EntityMetaDataType.PaintingVariant }
    };
        
    public override Dictionary<int, EntityMetaDataType> GetEntityMetadataMappingsList()
    {
        return entityMetadataMappings;
    }
}