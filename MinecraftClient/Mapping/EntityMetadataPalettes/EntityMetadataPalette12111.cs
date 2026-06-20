using System.Collections.Generic;

namespace MinecraftClient.Mapping.EntityMetadataPalettes;

public class EntityMetadataPalette12111 : EntityMetadataPalette
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
        { 22, EntityMetaDataType.CowVariant },
        { 23, EntityMetaDataType.WolfVariant },
        { 24, EntityMetaDataType.WolfSoundVariant },
        { 25, EntityMetaDataType.FrogVariant },
        { 26, EntityMetaDataType.PigVariant },
        { 27, EntityMetaDataType.ChickenVariant },
        { 28, EntityMetaDataType.ZombieNautilusVariant },
        { 29, EntityMetaDataType.OptionalGlobalPosition },
        { 30, EntityMetaDataType.PaintingVariant },
        { 31, EntityMetaDataType.SnifferState },
        { 32, EntityMetaDataType.ArmadilloState },
        { 33, EntityMetaDataType.CopperGolemState },
        { 34, EntityMetaDataType.WeatheringCopperState },
        { 35, EntityMetaDataType.Vector3 },
        { 36, EntityMetaDataType.Quaternion },
        { 37, EntityMetaDataType.ResolvableProfile },
        { 38, EntityMetaDataType.HumanoidArm },
    };

    public override Dictionary<int, EntityMetaDataType> GetEntityMetadataMappingsList()
    {
        return entityMetadataMappings;
    }
}
