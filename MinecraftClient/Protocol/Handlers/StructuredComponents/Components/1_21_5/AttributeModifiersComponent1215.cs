using System;
using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_5;

public class AttributeModifiersComponent1215(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int NumberOfAttributes { get; set; }
    public List<SubComponent> Attributes { get; set; } = [];

    public override void Parse(Queue<byte> data)
    {
        NumberOfAttributes = DataTypes.ReadNextVarInt(data);
        Attributes = new List<SubComponent>(NumberOfAttributes);

        for (var i = 0; i < NumberOfAttributes; i++)
            Attributes.Add(SubComponentRegistry.ParseSubComponent(SubComponents.Attribute, data));
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(NumberOfAttributes));

        if (Attributes.Count != NumberOfAttributes)
            throw new ArgumentNullException(nameof(Attributes), "Attributes count must match NumberOfAttributes.");

        foreach (var attribute in Attributes)
            data.AddRange(attribute.Serialize());

        return new Queue<byte>(data);
    }
}
