using System.Collections.Generic;

namespace MinecraftClient.Mapping.EntityMetadataPalettes;

/// <summary>
/// For 1.20.6+
/// Added PARTICLES (id 18), WOLF_VARIANT (id 23), ARMADILLO_STATE (id 28)
/// compared to 1.19.4 palette.
/// </summary>
public class EntityMetadataPalette1206 : EntityMetadataPalette
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
        { 18, EntityMetaDataType.Particles },
        { 19, EntityMetaDataType.VillagerData },
        { 20, EntityMetaDataType.OptionalVarInt },
        { 21, EntityMetaDataType.Pose },
        { 22, EntityMetaDataType.CatVariant },
        { 23, EntityMetaDataType.WolfVariant },
        { 24, EntityMetaDataType.FrogVariant },
        { 25, EntityMetaDataType.OptionalGlobalPosition },
        { 26, EntityMetaDataType.PaintingVariant },
        { 27, EntityMetaDataType.SnifferState },
        { 28, EntityMetaDataType.ArmadilloState },
        { 29, EntityMetaDataType.Vector3 },
        { 30, EntityMetaDataType.Quaternion },
    };

    public override Dictionary<int, EntityMetaDataType> GetEntityMetadataMappingsList()
    {
        return entityMetadataMappings;
    }
}
