using System;
using System.Collections.Generic;
using MinecraftClient.Protocol.Handlers.StructuredComponents.Core;

namespace MinecraftClient.Protocol.Handlers.StructuredComponents.Components.Subcomponents._1_20_6;

public class DetailsSubComponent(DataTypes dataTypes, SubComponentRegistry subComponentRegistry) : SubComponent(dataTypes, subComponentRegistry)
{
    public int Amplifier { get; set; }
    public int Duration { get; set; }
    public bool Ambient { get; set; }
    public bool ShowParticles { get; set; }
    public bool ShowIcon { get; set; }
    public bool HasHiddenEffects { get; set; }
    public DetailsSubComponent? Detail { get; set; }
    
    protected override void Parse(Queue<byte> data)
    {
        Amplifier = dataTypes.ReadNextVarInt(data);
        Duration = dataTypes.ReadNextVarInt(data);
        Ambient = dataTypes.ReadNextBool(data);
        ShowParticles = dataTypes.ReadNextBool(data);
        ShowIcon = dataTypes.ReadNextBool(data);
        HasHiddenEffects = dataTypes.ReadNextBool(data);
        
        if(HasHiddenEffects)
            Detail = (DetailsSubComponent)subComponentRegistry.ParseSubComponent(SubComponents.Details, data);
    }

    public override Queue<byte> Serialize()
    {
        var data = new List<byte>();
        data.AddRange(DataTypes.GetVarInt(Amplifier));
        data.AddRange(DataTypes.GetVarInt(Duration));
        data.AddRange(DataTypes.GetBool(Ambient));
        data.AddRange(DataTypes.GetBool(ShowParticles));
        data.AddRange(DataTypes.GetBool(ShowIcon));
        data.AddRange(DataTypes.GetBool(HasHiddenEffects));

        if (HasHiddenEffects)
        {
            if(Detail is null)
                throw new ArgumentNullException($"Can not serialize a DetailSubComponent1206 when the Detail is empty but HasHiddenEffects is true!");
                
            data.AddRange(Detail.Serialize());
        }

        return new Queue<byte>(data);
    }
}