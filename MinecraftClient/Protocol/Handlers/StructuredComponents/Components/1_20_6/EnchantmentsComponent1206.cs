using System.Collections.Generic;
using MinecraftClient.Inventory;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class EnchantmentsComponent1206(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int NumberOfEnchantments { get; set; }
    public List<Enchantment> Enchantments { get; set; } = new();
    public bool ShowTooltip { get; set; }

    public override void Parse(Queue<byte> data)
    {
        NumberOfEnchantments = dataTypes.ReadNextVarInt(data);

        for (var i = 0; i < NumberOfEnchantments; i++)
            Enchantments.Add(new Enchantment((Enchantments)dataTypes.ReadNextVarInt(data), dataTypes.ReadNextVarInt(data)));

        ShowTooltip = dataTypes.ReadNextBool(data);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(Enchantments.Count));
        foreach (var enchantment in Enchantments)
        {
            data.AddRange(DataTypes.GetVarInt((int)enchantment.Type));
            data.AddRange(DataTypes.GetVarInt(enchantment.Level));
        }
        data.AddRange(DataTypes.GetBool(ShowTooltip));
        return new Queue<byte>(data);
    }
}