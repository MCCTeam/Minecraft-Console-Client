using System;
using System.Collections.Generic;
using System.Linq;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Mapping;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_20_6;

public class FireworksComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int FlightDuration { get; set; }
    public int NumberOfExplosions { get; set; }

    public List<FireworkExplosionSubComponent> Explosions { get; set; } = [];
    
    public override void Parse(Queue<byte> data)
    {
        FlightDuration = dataTypes.ReadNextVarInt(data);
        NumberOfExplosions = dataTypes.ReadNextVarInt(data);

        if (NumberOfExplosions > 0)
        {
            for(var i = 0; i < NumberOfExplosions; i++)
                Explosions.Add(
                    (FireworkExplosionSubComponent)subComponentRegistry.ParseSubComponent(SubComponents.FireworkExplosion,
                        data));
        }
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(FlightDuration));
        data.AddRange(DataTypes.GetVarInt(NumberOfExplosions));
        if (NumberOfExplosions > 0)
        {
            if (NumberOfExplosions != Explosions.Count)
                throw new Exception("Can't serialize FireworksComponent because NumberOfExplosions and the lenght of Explosions differ!");
            
            foreach(var explosion in Explosions)
                data.AddRange(explosion.Serialize().ToList());
        }
        return new Queue<byte>(data);
    }
}