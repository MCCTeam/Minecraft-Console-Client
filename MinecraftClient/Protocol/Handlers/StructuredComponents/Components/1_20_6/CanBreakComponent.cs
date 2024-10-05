using System;
using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class CanBreakComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int NumberOfPredicates { get; set; }
    public List<BlockPredicateSubcomponent> BlockPredicates { get; set; } = new();
    public bool ShowInTooltip { get; set; }
    
    public override void Parse(Queue<byte> data)
    {
        NumberOfPredicates = dataTypes.ReadNextVarInt(data);

        for (var i = 0; i < NumberOfPredicates; i++)
            BlockPredicates.Add((BlockPredicateSubcomponent)subComponentRegistry.ParseSubComponent(SubComponents.BlockPredicate, data));

        ShowInTooltip = dataTypes.ReadNextBool(data);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(NumberOfPredicates));
        
        if(NumberOfPredicates > 0 && BlockPredicates.Count == 0)
            throw new ArgumentNullException($"Can not serialize a CanBreakComponent when the BlockPredicates is empty but NumberOfPredicates is > 0!");
        
        foreach (var blockPredicate in BlockPredicates)
            data.AddRange(blockPredicate.Serialize());
        
        data.AddRange(DataTypes.GetBool(ShowInTooltip));
        return new Queue<byte>(data);
    }
}