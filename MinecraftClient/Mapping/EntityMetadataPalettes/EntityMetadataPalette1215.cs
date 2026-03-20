using System.Collections.Generic;

namespace MinecraftClient.Mapping.EntityMetadataPalettes;

public class EntityMetadataPalette1215 : EntityMetadataPalette
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
        { 13, EntityMetaDataType.OptionalLivingEntityReference },
        { 14, EntityMetaDataType.BlockId },
        { 15, EntityMetaDataType.OptionalBlockId },
        { 16, EntityMetaDataType.Nbt },
        { 17, EntityMetaDataType.Particle },
        { 18, EntityMetaDataType.Particles },
        { 19, EntityMetaDataType.VillagerData },
        { 20, EntityMetaDataType.OptionalVarInt },
        { 21, EntityMetaDataType.Pose },
        { 22, EntityMetaDataType.CatVariant },
        { 23, EntityMetaDataType.CowVariant },
        { 24, EntityMetaDataType.WolfVariant },
        { 25, EntityMetaDataType.WolfSoundVariant },
        { 26, EntityMetaDataType.FrogVariant },
        { 27, EntityMetaDataType.PigVariant },
        { 28, EntityMetaDataType.ChickenVariant },
        { 29, EntityMetaDataType.OptionalGlobalPosition },
        { 30, EntityMetaDataType.PaintingVariant },
        { 31, EntityMetaDataType.SnifferState },
        { 32, EntityMetaDataType.ArmadilloState },
        { 33, EntityMetaDataType.Vector3 },
        { 34, EntityMetaDataType.Quaternion },
    };

    public override Dictionary<int, EntityMetaDataType> GetEntityMetadataMappingsList()
    {
        return entityMetadataMappings;
    }
}
