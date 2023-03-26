using System.Collections.Generic;

namespace MinecraftClient.Mapping.EntityMetadataPalettes;

/// <summary>
/// For 1.19.3
/// </summary>
public class EntityMetadataPalette1193 : EntityMetadataPalette
{
    private readonly Dictionary<int, EntityMetaDataType> entityMetadataMappings = new()
    {
        { 0, EntityMetaDataType.Byte },
        { 1, EntityMetaDataType.VarInt },
        { 2, EntityMetaDataType.VarLong },
        { 3, EntityMetaDataType.Float },
        { 4, EntityMetaDataType.String },
        { 5, EntityMetaDataType.Chat },
        { 6, EntityMetaDataType.OptionalChat },
        { 7, EntityMetaDataType.Slot },
        { 8, EntityMetaDataType.Boolean },
        { 9, EntityMetaDataType.Rotation },
        { 10, EntityMetaDataType.Position },
        { 11, EntityMetaDataType.OptionalPosition },
        { 12, EntityMetaDataType.Direction },
        { 13, EntityMetaDataType.OptionalUuid },
        { 14, EntityMetaDataType.OptionalBlockId },
        { 15, EntityMetaDataType.Nbt },
        { 16, EntityMetaDataType.Particle },
        { 17, EntityMetaDataType.VillagerData },
        { 18, EntityMetaDataType.OptionalVarInt },
        { 19, EntityMetaDataType.Pose },
        { 20, EntityMetaDataType.CatVariant },
        { 21, EntityMetaDataType.FrogVariant },
        { 22, EntityMetaDataType.OptionalGlobalPosition },
        { 23, EntityMetaDataType.PaintingVariant }
    };
        
    public override Dictionary<int, EntityMetaDataType> GetEntityMetadataMappingsList()
    {
        return entityMetadataMappings;
    }
}