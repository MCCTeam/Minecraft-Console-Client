using System.Collections.Generic;
using MinecraftClient.Inventory;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class RarityComponent1206(DataTypes dataTypes, SubComponentRegistry subComponentRegistry) : StructuredComponent(dataTypes, subComponentRegistry)
{
    public ItemRarity Rarity { get; set; }
    
    public override void Parse(Queue<byte> data)
    {
        Rarity = (ItemRarity)dataTypes.ReadNextVarInt(data);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt((int)Rarity));
        return new Queue<byte>(data);
    }
}