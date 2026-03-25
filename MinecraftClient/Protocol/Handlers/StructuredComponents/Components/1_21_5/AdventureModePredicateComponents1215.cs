using System;
using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_5;

/// <summary>
/// 1.21.5+ uses AdventureModePredicate.STREAM_CODEC:
/// list<BlockPredicate>, where each BlockPredicate ends with DataComponentMatchers.
/// Tooltip visibility moved to minecraft:tooltip_display.
/// </summary>
public sealed class CanBreakComponent1215(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : AdventureModePredicateComponent1215Base(dataTypes, itemPalette, subComponentRegistry);

/// <summary>
/// 1.21.5+ uses AdventureModePredicate.STREAM_CODEC:
/// list<BlockPredicate>, where each BlockPredicate ends with DataComponentMatchers.
/// Tooltip visibility moved to minecraft:tooltip_display.
/// </summary>
public sealed class CanPlaceOnComponent1215(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : AdventureModePredicateComponent1215Base(dataTypes, itemPalette, subComponentRegistry);

public abstract class AdventureModePredicateComponent1215Base(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public List<AdventureModeBlockPredicate1215> BlockPredicates { get; set; } = [];

    public override void Parse(Queue<byte> data)
    {
        var predicateCount = DataTypes.ReadNextVarInt(data);
        BlockPredicates = new List<AdventureModeBlockPredicate1215>(predicateCount);
        var componentHandler = new StructuredComponentsHandler(DataTypes.ProtocolVersion, DataTypes, ItemPalette);

        for (var i = 0; i < predicateCount; i++)
            BlockPredicates.Add(ParseBlockPredicate(data, componentHandler));
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(BlockPredicates.Count));

        foreach (var predicate in BlockPredicates)
            SerializeBlockPredicate(data, predicate);

        return new Queue<byte>(data);
    }

    private AdventureModeBlockPredicate1215 ParseBlockPredicate(Queue<byte> data, StructuredComponentsHandler componentHandler)
    {
        BlockSetSubcomponent? blockSet = null;
        if (DataTypes.ReadNextBool(data))
            blockSet = (BlockSetSubcomponent)SubComponentRegistry.ParseSubComponent(SubComponents.BlockSet, data);

        List<PropertySubComponent> properties = [];
        if (DataTypes.ReadNextBool(data))
        {
            var propertyCount = DataTypes.ReadNextVarInt(data);
            properties = new List<PropertySubComponent>(propertyCount);
            for (var i = 0; i < propertyCount; i++)
                properties.Add((PropertySubComponent)SubComponentRegistry.ParseSubComponent(SubComponents.Property, data));
        }

        Dictionary<string, object>? nbt = null;
        if (DataTypes.ReadNextBool(data))
            nbt = DataTypes.ReadNextNbt(data);

        var exactComponentCount = DataTypes.ReadNextVarInt(data);
        var exactComponents = new List<StructuredComponent>(exactComponentCount);
        for (var i = 0; i < exactComponentCount; i++)
        {
            var componentTypeId = DataTypes.ReadNextVarInt(data);
            exactComponents.Add(componentHandler.Parse(componentTypeId, data));
        }

        var partialPredicateCount = DataTypes.ReadNextVarInt(data);
        var partialPredicates = new List<DataComponentPredicatePayload1215>(partialPredicateCount);
        for (var i = 0; i < partialPredicateCount; i++)
        {
            partialPredicates.Add(new DataComponentPredicatePayload1215(
                DataTypes.ReadNextVarInt(data),
                DataTypes.ReadNextNbt(data)));
        }

        return new AdventureModeBlockPredicate1215(blockSet, properties, nbt, exactComponents, partialPredicates);
    }

    private void SerializeBlockPredicate(List<byte> data, AdventureModeBlockPredicate1215 predicate)
    {
        data.AddRange(DataTypes.GetBool(predicate.BlockSet is not null));
        if (predicate.BlockSet is not null)
            data.AddRange(predicate.BlockSet.Serialize());

        data.AddRange(DataTypes.GetBool(predicate.Properties.Count > 0));
        if (predicate.Properties.Count > 0)
        {
            data.AddRange(DataTypes.GetVarInt(predicate.Properties.Count));
            foreach (var property in predicate.Properties)
                data.AddRange(property.Serialize());
        }

        data.AddRange(DataTypes.GetBool(predicate.Nbt is not null));
        if (predicate.Nbt is not null)
            data.AddRange(DataTypes.GetNbt(predicate.Nbt));

        data.AddRange(DataTypes.GetVarInt(predicate.ExactComponents.Count));
        foreach (var component in predicate.ExactComponents)
        {
            if (component.TypeId < 0)
                throw new ArgumentException("Exact predicate component is missing its data component type id.", nameof(component));

            data.AddRange(DataTypes.GetVarInt(component.TypeId));
            data.AddRange(component.Serialize());
        }

        data.AddRange(DataTypes.GetVarInt(predicate.PartialPredicates.Count));
        foreach (var predicatePayload in predicate.PartialPredicates)
        {
            data.AddRange(DataTypes.GetVarInt(predicatePayload.TypeId));
            data.AddRange(DataTypes.GetNbt(predicatePayload.Payload));
        }
    }
}

public sealed record AdventureModeBlockPredicate1215(
    BlockSetSubcomponent? BlockSet,
    List<PropertySubComponent> Properties,
    Dictionary<string, object>? Nbt,
    List<StructuredComponent> ExactComponents,
    List<DataComponentPredicatePayload1215> PartialPredicates);

public sealed record DataComponentPredicatePayload1215(int TypeId, Dictionary<string, object> Payload);
