using System;
using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class AttributeModifiersComponent1206(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int NumberOfAttributes { get; set; }
    public List<AttributeSubComponent1206> Attributes { get; set; } = new();
    public bool ShowInTooltip { get; set; }
    
    public override void Parse(Queue<byte> data)
    {
        NumberOfAttributes = dataTypes.ReadNextVarInt(data);

        for (var i = 0; i < NumberOfAttributes; i++)
            Attributes.Add((AttributeSubComponent1206)subComponentRegistry.ParseSubComponent(SubComponents.Attribute, data));

        ShowInTooltip = dataTypes.ReadNextBool(data);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(NumberOfAttributes));
        
        if(Attributes.Count != NumberOfAttributes)
            throw new ArgumentNullException($"Can not serialize a AttributeModifiersComponent when the Attributes count != NumberOfAttributes!");
        
        foreach (var attribute in Attributes)
            data.AddRange(attribute.Serialize());
        
        data.AddRange(DataTypes.GetBool(ShowInTooltip));
        return new Queue<byte>(data);
    }
}