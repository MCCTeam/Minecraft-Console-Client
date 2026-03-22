using System.Collections.Generic;

namespace MinecraftClient.Mapping.EntityMetadataPalettes;

public class EntityMetadataPalette261 : EntityMetadataPalette
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
        { 16, EntityMetaDataType.Particle },
        { 17, EntityMetaDataType.Particles },
        { 18, EntityMetaDataType.VillagerData },
        { 19, EntityMetaDataType.OptionalVarInt },
        { 20, EntityMetaDataType.Pose },
        { 21, EntityMetaDataType.CatVariant },
        { 22, EntityMetaDataType.CatSoundVariant },
        { 23, EntityMetaDataType.CowVariant },
        { 24, EntityMetaDataType.CowSoundVariant },
        { 25, EntityMetaDataType.WolfVariant },
        { 26, EntityMetaDataType.WolfSoundVariant },
        { 27, EntityMetaDataType.FrogVariant },
        { 28, EntityMetaDataType.PigVariant },
        { 29, EntityMetaDataType.PigSoundVariant },
        { 30, EntityMetaDataType.ChickenVariant },
        { 31, EntityMetaDataType.ChickenSoundVariant },
        { 32, EntityMetaDataType.ZombieNautilusVariant },
        { 33, EntityMetaDataType.OptionalGlobalPosition },
        { 34, EntityMetaDataType.PaintingVariant },
        { 35, EntityMetaDataType.SnifferState },
        { 36, EntityMetaDataType.ArmadilloState },
        { 37, EntityMetaDataType.CopperGolemState },
        { 38, EntityMetaDataType.WeatheringCopperState },
        { 39, EntityMetaDataType.Vector3 },
        { 40, EntityMetaDataType.Quaternion },
        { 41, EntityMetaDataType.ResolvableProfile },
        { 42, EntityMetaDataType.HumanoidArm },
    };

    public override Dictionary<int, EntityMetaDataType> GetEntityMetadataMappingsList()
    {
        return entityMetadataMappings;
    }
}
