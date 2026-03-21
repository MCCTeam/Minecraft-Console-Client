namespace MinecraftClient.Mapping;

public enum EntityMetaDataType
{
    Byte,
    Short,      // 1.8 only
    Int,        // 1.8 only
    Vector3Int, // 1.8 only (not used by the game)
    VarInt,
    VarLong,
    Float,
    String,
    Chat,
    OptionalChat,
    Slot,
    Boolean,
    /// <summary>
    /// Float x3
    /// </summary>
    Rotation,
    Position,
    OptionalPosition,
    /// <summary>
    /// VarInt
    /// </summary>
    Direction,
    OptionalUuid,
    /// <summary>
    /// Boolean + UUID (1.21.5+, replaces OptionalUuid)
    /// </summary>
    OptionalLivingEntityReference,
    /// <summary>
    /// VarInt
    /// </summary>
    BlockId,
    /// <summary>
    /// VarInt (0 for absent)
    /// </summary>
    OptionalBlockId,
    Nbt,
    Particle,
    /// <summary>
    /// List of Particle (1.20.6+)
    /// </summary>
    Particles,
    /// <summary>
    /// VarInt x3
    /// </summary>
    VillagerData,
    OptionalVarInt,
    /// <summary>
    /// VarInt
    /// </summary>
    Pose,
    /// <summary>
    /// VarInt
    /// </summary>
    CatVariant,
    /// <summary>
    /// VarInt (1.20.6+)
    /// </summary>
    CowVariant,
    /// <summary>
    /// VarInt (1.20.6+)
    /// </summary>
    WolfVariant,
    /// <summary>
    /// VarInt (1.21.5+)
    /// </summary>
    WolfSoundVariant,
    FrogVariant,
    /// <summary>
    /// VarInt (1.21.5+)
    /// </summary>
    PigVariant,
    /// <summary>
    /// VarInt (1.21.5+)
    /// </summary>
    ChickenVariant,
    /// <summary>
    /// String + Position
    /// </summary>
    GlobalPosition,
    /// <summary>
    /// Boolean + String + Position
    /// </summary>
    OptionalGlobalPosition,
    /// <summary>
    /// VarInt
    /// </summary>
    PaintingVariant,
    /// <summary>
    /// VarInt
    /// </summary>
    SnifferState,
    /// <summary>
    /// VarInt (1.20.6+)
    /// </summary>
    ArmadilloState,
    /// <summary>
    /// VarInt (1.21.9+)
    /// </summary>
    CopperGolemState,
    /// <summary>
    /// VarInt (1.21.9+)
    /// </summary>
    WeatheringCopperState,
    /// <summary>
    /// Float x3
    /// </summary>
    Vector3,
    /// <summary>
    /// Float x4
    /// </summary>
    Quaternion,
    /// <summary>
    /// Either&lt;GameProfile, Partial&gt; + PlayerSkin.Patch (1.21.9+)
    /// </summary>
    ResolvableProfile
}