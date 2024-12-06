using System;
using System.Collections.Generic;
using MinecraftClient.Inventory.ItemPalettes;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_21;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components._1_21;

public class JukeBoxPlayableComponent(DataTypes dataTypes, ItemPalette itemPalette, SubComponentRegistry subComponentRegistry) 
    : StructuredComponent(dataTypes, itemPalette, subComponentRegistry)
{
    public bool DirectMode { get; set; }
    public string? SongName { get; set; }
    public int? SongType { get; set; }
    public SoundEventSubComponent? SoundEvent { get; set; }
    public string? Description { get; set; }
    public float? Duration { get; set; }
    public int? Output { get; set; }
    public bool ShowTooltip { get; set; }
    
    public override void Parse(Queue<byte> data)
    {
        DirectMode = dataTypes.ReadNextBool(data);

        if (!DirectMode)
            SongName = dataTypes.ReadNextString(data);

        if (DirectMode)
        {
            SongType = dataTypes.ReadNextVarInt(data);

            if (SongType == 0)
            {
                SoundEvent =
                    (SoundEventSubComponent)subComponentRegistry.ParseSubComponent(SubComponents.SoundEvent, data);
                Description = dataTypes.ReadNextString(data);
                Duration = dataTypes.ReadNextFloat(data);
                Output = dataTypes.ReadNextVarInt(data);
            }
        }

        ShowTooltip = dataTypes.ReadNextBool(data);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        
        data.AddRange(DataTypes.GetBool(DirectMode));

        if (!DirectMode)
        {
            if (string.IsNullOrEmpty(SongName?.Trim()))
                throw new ArgumentNullException($"Can not serialize JukeBoxPlayableComponent due to SongName being null or empty!");
            
            data.AddRange(DataTypes.GetString(SongName));
        }

        if (DirectMode)
        {
            if(SongType is null)
                throw new ArgumentNullException($"Can not serialize JukeBoxPlayableComponent due to SongType being null!");
            
            data.AddRange(DataTypes.GetVarInt((int)SongType));

            if (SongType == 0)
            {
                if (SoundEvent is null)
                    throw new ArgumentNullException(
                        $"Can not serialize JukeBoxPlayableComponent due to SoundEvent being null");

                data.AddRange(SoundEvent.Serialize());

                if (string.IsNullOrEmpty(Description?.Trim()))
                    throw new ArgumentNullException(
                        $"Can not serialize JukeBoxPlayableComponent due to Description being null or empty!");

                data.AddRange(DataTypes.GetString(Description));

                if (Duration is null)
                    throw new ArgumentNullException(
                        $"Can not serialize JukeBoxPlayableComponent due to Duration being null!");

                data.AddRange(DataTypes.GetFloat((float)Duration));

                if (Output is null)
                    throw new ArgumentNullException(
                        $"Can not serialize JukeBoxPlayableComponent due to Description being null!");

                data.AddRange(DataTypes.GetVarInt((int)Output));
            }
        }
        
        data.AddRange(DataTypes.GetBool(ShowTooltip));
        
        return new Queue<byte>(data);
    }
}