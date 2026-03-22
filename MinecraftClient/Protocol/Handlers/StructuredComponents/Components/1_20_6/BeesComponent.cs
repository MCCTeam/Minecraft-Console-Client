using System;
using System.Collections.Generic;
using MinecraftClient.Inventory;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class BeesComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int NumberOfBees { get; set; }
    public List<Bee> Bees { get; set; } = [];
    
    public override void Parse(Queue<byte> data)
    {
        NumberOfBees = dataTypes.ReadNextVarInt(data);
        for (var i = 0; i < NumberOfBees; i++)
        {
            Bees.Add(new Bee(dataTypes.ReadNextNbt(data), dataTypes.ReadNextVarInt(data), dataTypes.ReadNextVarInt(data)));
        }
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(NumberOfBees));

        if (NumberOfBees > 0)
        {
            if (NumberOfBees != Bees.Count)
                throw new Exception("Can't serialize the BeeComponent because NumberOfBees and Bees.Count differ!");
            
            foreach (var bee in Bees)
            {
                data.AddRange(DataTypes.GetNbt(bee.EntityDataNbt));
                data.AddRange(DataTypes.GetVarInt(bee.TicksInHive));
                data.AddRange(DataTypes.GetVarInt(bee.MinTicksInHive));
            }
        }

        return new Queue<byte>(data);
    }
}

public record Bee(Dictionary<string, object>? EntityDataNbt, int TicksInHive, int MinTicksInHive);