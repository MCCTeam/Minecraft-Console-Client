using System.Collections.Generic;
using MinecraftClient.Inventory;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_5;

/// <summary>
/// 1.21.5+ enchantments: showInTooltip removed from wire format (moved to tooltip_display component).
/// Wire: VarInt count, then (VarInt holder_id + VarInt level) per entry. No trailing boolean.
/// </summary>
public class EnchantmentsComponent1215(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : EnchantmentsComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public override void Parse(Queue<byte> data)
    {
        NumberOfEnchantments = dataTypes.ReadNextVarInt(data);

        for (var i = 0; i < NumberOfEnchantments; i++)
        {
            var registryId = dataTypes.ReadNextVarInt(data);
            var level = dataTypes.ReadNextVarInt(data);
            Enchantments.Add(new Enchantment(EnchantmentMapping.GetEnchantmentByRegistryId1206(registryId), level));
        }
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(Enchantments.Count));
        foreach (var enchantment in Enchantments)
        {
            data.AddRange(DataTypes.GetVarInt(EnchantmentMapping.GetRegistryId1206ByEnchantment(enchantment.Type)));
            data.AddRange(DataTypes.GetVarInt(enchantment.Level));
        }
        return new Queue<byte>(data);
    }
}
