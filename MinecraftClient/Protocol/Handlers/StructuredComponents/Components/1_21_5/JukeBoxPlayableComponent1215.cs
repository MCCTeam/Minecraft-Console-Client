using System;
using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_21;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;
using MinecraftClient.Protocol.Message;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21_5;

public class JukeBoxPlayableComponent1215(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry)
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public bool IsHolder { get; set; }
    public int HolderId { get; set; }
    public string? ResourceKey { get; set; }
    public SoundEventSubComponent? SoundEvent { get; set; }
    public Dictionary<string, object>? DescriptionNbt { get; set; }
    public string Description { get; set; } = string.Empty;
    public float Duration { get; set; }
    public int ComparatorOutput { get; set; }

    public override void Parse(Queue<byte> data)
    {
        IsHolder = DataTypes.ReadNextBool(data);

        if (IsHolder)
        {
            HolderId = DataTypes.ReadNextVarInt(data);
            if (HolderId == 0)
            {
                SoundEvent = (SoundEventSubComponent)SubComponentRegistry.ParseSubComponent(SubComponents.SoundEvent, data);
                DescriptionNbt = DataTypes.ReadNextNbt(data);
                Description = ChatParser.ParseText(DescriptionNbt);
                Duration = DataTypes.ReadNextFloat(data);
                ComparatorOutput = DataTypes.ReadNextVarInt(data);
            }
        }
        else
        {
            ResourceKey = DataTypes.ReadNextString(data);
        }
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetBool(IsHolder));

        if (IsHolder)
        {
            data.AddRange(DataTypes.GetVarInt(HolderId));
            if (HolderId == 0)
            {
                if (SoundEvent is null)
                    throw new ArgumentNullException(nameof(SoundEvent), "Inline jukebox song requires a sound event.");

                if (DescriptionNbt is null)
                    throw new ArgumentNullException(nameof(DescriptionNbt), "Inline jukebox song requires a description.");

                data.AddRange(SoundEvent.Serialize());
                data.AddRange(DataTypes.GetNbt(DescriptionNbt));
                data.AddRange(DataTypes.GetFloat(Duration));
                data.AddRange(DataTypes.GetVarInt(ComparatorOutput));
            }
        }
        else
        {
            if (string.IsNullOrEmpty(ResourceKey))
                throw new ArgumentNullException(nameof(ResourceKey), "Resource key is required for key-backed jukebox songs.");

            data.AddRange(DataTypes.GetString(ResourceKey));
        }

        return new Queue<byte>(data);
    }
}
