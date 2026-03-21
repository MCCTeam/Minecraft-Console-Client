using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_11;

/// <summary>
/// EitherHolder backed by holderRegistry (VarInt = raw registry ID, 0 is valid).
/// Used for DamageType and ZombieNautilusVariant where the holder codec is holderRegistry(),
/// unlike the holder() codec used in SoundEvent (where 0 means inline).
/// </summary>
public class RegistryEitherHolderComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public bool IsHolder { get; set; }
    public int HolderId { get; set; }
    public string? ResourceKey { get; set; }

    public override void Parse(Queue<byte> data)
    {
        IsHolder = dataTypes.ReadNextBool(data);
        if (IsHolder)
            HolderId = dataTypes.ReadNextVarInt(data);
        else
            ResourceKey = dataTypes.ReadNextString(data);
    }

    public override Queue<byte> Serialize()
    {
        var bytes = new List<byte>();
        bytes.AddRange(DataTypes.GetBool(IsHolder));
        if (IsHolder)
            bytes.AddRange(DataTypes.GetVarInt(HolderId));
        else
            bytes.AddRange(DataTypes.GetString(ResourceKey ?? ""));
        return new Queue<byte>(bytes);
    }
}
