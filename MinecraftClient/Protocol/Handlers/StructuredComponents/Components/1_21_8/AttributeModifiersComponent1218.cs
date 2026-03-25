using System;
using System.Collections.Generic;
using System.IO;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;
using MinecraftClient.Protocol.Message;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_8;

public class AttributeModifiersComponent1218(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public int NumberOfAttributes { get; set; }
    public List<SubComponent> Attributes { get; set; } = [];
    public List<AttributeModifierDisplay> Displays { get; set; } = [];

    public override void Parse(Queue<byte> data)
    {
        NumberOfAttributes = DataTypes.ReadNextVarInt(data);
        Attributes = new List<SubComponent>(NumberOfAttributes);
        Displays = new List<AttributeModifierDisplay>(NumberOfAttributes);

        for (var i = 0; i < NumberOfAttributes; i++)
        {
            Attributes.Add(SubComponentRegistry.ParseSubComponent(SubComponents.Attribute, data));
            Displays.Add(ReadDisplay(data));
        }
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(NumberOfAttributes));

        if (Attributes.Count != NumberOfAttributes)
            throw new ArgumentNullException(nameof(Attributes), "Attributes count must match NumberOfAttributes.");

        if (Displays.Count != NumberOfAttributes)
            throw new ArgumentNullException(nameof(Displays), "Displays count must match NumberOfAttributes.");

        for (var i = 0; i < NumberOfAttributes; i++)
        {
            data.AddRange(Attributes[i].Serialize());
            data.AddRange(SerializeDisplay(Displays[i]));
        }

        return new Queue<byte>(data);
    }

    private AttributeModifierDisplay ReadDisplay(Queue<byte> data)
    {
        var displayType = DataTypes.ReadNextVarInt(data);
        return displayType switch
        {
            0 => new AttributeModifierDisplay(AttributeModifierDisplayType.Default),
            1 => new AttributeModifierDisplay(AttributeModifierDisplayType.Hidden),
            2 => ReadOverrideDisplay(data),
            _ => throw new InvalidDataException($"Unknown attribute modifier display type: {displayType}")
        };
    }

    private AttributeModifierDisplay ReadOverrideDisplay(Queue<byte> data)
    {
        var overrideTextNbt = DataTypes.ReadNextNbt(data);
        var overrideText = ChatParser.ParseText(overrideTextNbt);
        return new AttributeModifierDisplay(AttributeModifierDisplayType.Override, overrideTextNbt, overrideText);
    }

    private Queue<byte> SerializeDisplay(AttributeModifierDisplay display)
    {
        var data = new List<byte>
        {
        };
        data.AddRange(DataTypes.GetVarInt((int)display.Type));

        if (display.Type == AttributeModifierDisplayType.Override)
        {
            if (display.OverrideTextNbt is null)
                throw new ArgumentNullException(nameof(display.OverrideTextNbt), "Override display requires component NBT.");

            data.AddRange(DataTypes.GetNbt(display.OverrideTextNbt));
        }

        return new Queue<byte>(data);
    }
}

public sealed record AttributeModifierDisplay(
    AttributeModifierDisplayType Type,
    Dictionary<string, object>? OverrideTextNbt = null,
    string? OverrideText = null);

public enum AttributeModifierDisplayType
{
    Default = 0,
    Hidden = 1,
    Override = 2
}
