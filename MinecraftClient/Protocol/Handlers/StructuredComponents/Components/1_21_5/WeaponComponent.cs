using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_5;

public class WeaponComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int ItemDamagePerAttack { get; set; }
    public float DisableBlockingForSeconds { get; set; }

    public override void Parse(Queue<byte> data)
    {
        ItemDamagePerAttack = dataTypes.ReadNextVarInt(data);
        DisableBlockingForSeconds = dataTypes.ReadNextFloat(data);
    }

    public override Queue<byte> Serialize()
    {
        var bytes = new List<byte>();
        bytes.AddRange(DataTypes.GetVarInt(ItemDamagePerAttack));
        bytes.AddRange(DataTypes.GetFloat(DisableBlockingForSeconds));
        return new Queue<byte>(bytes);
    }
}
