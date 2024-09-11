using System;
using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;

public class BlockPredicateSubcomponent1206(DataTypes dataTypes, SubComponentRegistry subComponentRegistry) : SubComponent(dataTypes, subComponentRegistry)
{
    public bool HasBlocks { get; set; }
    public BlockSetSubcomponent1206? BlockSet { get; set; }
    public bool HasProperities { get; set; }
    public List<PropertySubComponent1206>? Properties { get; set; }
    public bool HasNbt { get; set; }
    public Dictionary<string, object>? Nbt { get; set; }
        
    protected override void Parse(Queue<byte> data)
    {
        HasBlocks = dataTypes.ReadNextBool(data);

        if (HasBlocks)
            BlockSet = (BlockSetSubcomponent1206)subComponentRegistry.ParseSubComponent(SubComponents.BlockSet, data);

        HasProperities = dataTypes.ReadNextBool(data);

        if (HasProperities)
        {
            Properties = new();
            var numberOfProperties = dataTypes.ReadNextVarInt(data);
            for (var i = 0; i < numberOfProperties; i++)
                Properties.Add((PropertySubComponent1206)subComponentRegistry.ParseSubComponent(SubComponents.Property, data));
        }

        HasNbt = dataTypes.ReadNextBool(data);
        
        if (HasNbt)
            Nbt = dataTypes.ReadNextNbt(data);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        
        // Block Sets
        data.AddRange(DataTypes.GetBool(HasBlocks));
        if (HasBlocks)
        {
            if(BlockSet == null)
                throw new ArgumentNullException($"Can not serialize a BlockPredicate when the BlockSet is empty but HasBlocks is true!");
            
            data.AddRange(BlockSet.Serialize());
        }
        
        // Properites
        data.AddRange(DataTypes.GetBool(HasProperities));
        if (HasProperities)
        {
            if(Properties == null || Properties.Count == 0)
                throw new ArgumentNullException($"Can not serialize a BlockPredicate when the Properties is empty but HasProperties is true!");

            foreach (var property in Properties)
                data.AddRange(property.Serialize());
        }

        // NBT
        if (HasNbt)
        {
            if(Nbt == null)
                throw new ArgumentNullException($"Can not serialize a BlockPredicate when the Nbt is empty but HasNbt is true!");
            
            data.AddRange(DataTypes.GetNbt(Nbt));
        }
        
        return new Queue<byte>(data);
    }
}