using System;
using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_5;

public class ToolComponent1215(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int NumberOfRules { get; set; }
    public List<RuleSubComponent> Rules { get; set; } = [];
    public float DefaultMiningSpeed { get; set; }
    public int DamagePerBlock { get; set; }
    public bool CanDestroyBlocksInCreative { get; set; }

    public override void Parse(Queue<byte> data)
    {
        NumberOfRules = DataTypes.ReadNextVarInt(data);
        Rules = new List<RuleSubComponent>(NumberOfRules);

        for (var i = 0; i < NumberOfRules; i++)
            Rules.Add((RuleSubComponent)SubComponentRegistry.ParseSubComponent(SubComponents.Rule, data));

        DefaultMiningSpeed = DataTypes.ReadNextFloat(data);
        DamagePerBlock = DataTypes.ReadNextVarInt(data);
        CanDestroyBlocksInCreative = DataTypes.ReadNextBool(data);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(NumberOfRules));

        if (Rules.Count != NumberOfRules)
            throw new ArgumentNullException(nameof(Rules), "Rules count must match NumberOfRules.");

        foreach (var rule in Rules)
            data.AddRange(rule.Serialize());

        data.AddRange(DataTypes.GetFloat(DefaultMiningSpeed));
        data.AddRange(DataTypes.GetVarInt(DamagePerBlock));
        data.AddRange(DataTypes.GetBool(CanDestroyBlocksInCreative));
        return new Queue<byte>(data);
    }
}
