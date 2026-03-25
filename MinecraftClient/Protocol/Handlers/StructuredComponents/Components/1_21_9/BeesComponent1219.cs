using System;
using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_9;

public class BeesComponent1219(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int NumberOfBees { get; set; }
    public List<TypedBee> Bees { get; set; } = [];

    public override void Parse(Queue<byte> data)
    {
        NumberOfBees = DataTypes.ReadNextVarInt(data);
        Bees = new List<TypedBee>(NumberOfBees);

        for (var i = 0; i < NumberOfBees; i++)
        {
            Bees.Add(
                new TypedBee(
                    DataTypes.ReadNextVarInt(data),
                    DataTypes.ReadNextNbt(data),
                    DataTypes.ReadNextVarInt(data),
                    DataTypes.ReadNextVarInt(data)));
        }
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(NumberOfBees));

        if (NumberOfBees != Bees.Count)
            throw new InvalidOperationException("Can't serialize the BeesComponent1219 because NumberOfBees and Bees.Count differ!");

        foreach (var bee in Bees)
        {
            data.AddRange(DataTypes.GetVarInt(bee.EntityTypeId));
            data.AddRange(DataTypes.GetNbt(bee.EntityDataNbt));
            data.AddRange(DataTypes.GetVarInt(bee.TicksInHive));
            data.AddRange(DataTypes.GetVarInt(bee.MinTicksInHive));
        }

        return new Queue<byte>(data);
    }
}

public sealed record TypedBee(int EntityTypeId, Dictionary<string, object>? EntityDataNbt, int TicksInHive, int MinTicksInHive);
