using System.Collections.Generic;

namespace MinecraftClient.Mapping.EntityMetadataPalettes;

/// <summary>
/// For 1.19.4
/// </summary>
public class EntityMetadataPalette1194 : EntityMetadataPalette
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
        { 14, EntityMetaDataType.BlockId },
        { 15, EntityMetaDataType.OptionalBlockId },
        { 16, EntityMetaDataType.Nbt },
        { 17, EntityMetaDataType.Particle },
        { 18, EntityMetaDataType.VillagerData },
        { 19, EntityMetaDataType.OptionalVarInt },
        { 20, EntityMetaDataType.Pose },
        { 21, EntityMetaDataType.CatVariant },
        { 22, EntityMetaDataType.FrogVariant },
        { 23, EntityMetaDataType.OptionalGlobalPosition },
        { 24, EntityMetaDataType.PaintingVariant },
        { 25, EntityMetaDataType.SnifferState },
        { 26, EntityMetaDataType.Vector3 },
        { 27, EntityMetaDataType.Quaternion },
    };
        
    public override Dictionary<int, EntityMetaDataType> GetEntityMetadataMappingsList()
    {
        return entityMetadataMappings;
    }
}